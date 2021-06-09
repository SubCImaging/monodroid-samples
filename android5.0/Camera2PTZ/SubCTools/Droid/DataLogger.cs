//-----------------------------------------------------------------------
// <copyright file="DataLogger.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using SubCTools.Attributes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Extensions;
    using SubCTools.Interfaces;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Timers;

    /// <summary>
    /// Class for logging data from communicators.
    /// </summary>
    public class DataLogger : INotifier
    {
        /// <summary>
        /// Pattern used to match whether data is NMEA.
        /// </summary>
        private const string NMEAPattern = @"(^\$.{5}),";

        private readonly StringBuilder buffer = new StringBuilder();
        private readonly Dictionary<string, string> data = new Dictionary<string, string>();

        /// <summary>
        /// Directory to save NMEA data.
        /// </summary>
        private readonly DirectoryInfo directory;

        private readonly Timer dumpTimer = new Timer() { Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };

        /// <summary>
        /// Logger used to save data.
        /// </summary>
        private readonly ILogger logger;

        private readonly Timer loggingTimer = new Timer() { Interval = TimeSpan.FromSeconds(1).TotalMilliseconds };

        /// <summary>
        /// Output communicator for rebroadcasting NMEA.
        /// </summary>
        private readonly ICommunicator output;

        private readonly ISettingsService settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLogger"/> class.
        /// </summary>
        /// <param name="logger">Logger used to write data.</param>
        /// <param name="output">Communicator to output incoming NMEA data.</param>
        /// <param name="directory">Directory to save data with logger.</param>
        /// <param name="settings">Settings service for saving and loading.</param>
        /// <param name="input">List of communicators to listen to.</param>
        public DataLogger(
            ILogger logger,
            ICommunicator output,
            DirectoryInfo directory,
            ISettingsService settings,
            params ICommunicator[] input)
            : this(logger, directory, settings, input)
        {
            this.output = output;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLogger"/> class.
        /// </summary>
        /// <param name="logger">Logger used to write data.</param>
        /// <param name="directory">Directory to save data with logger.</param>
        /// <param name="settings">Settings service for saving and loading.</param>
        /// <param name="input">List of communicators to listen to.</param>
        public DataLogger(
            ILogger logger,
            DirectoryInfo directory,
            ISettingsService settings,
            params ICommunicator[] input)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            input.AsParallel()
                .ForAll(c => c.DataReceived += (_, e) => DataReceived(e));

            loggingTimer.Elapsed += (_, __) => LoggingTimer_Elapsed();
            dumpTimer.Elapsed += (_, __) => DumpLogToFile();
        }

        /// <summary>
        /// Event to fire when data is processed.
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Gets or sets a value indicating whether the object is logging data.
        /// </summary>
        [RemoteState(true)]
        public bool IsLogging { get; set; }

        /// <summary>
        /// Gets or sets the logging grequency.
        /// </summary>
        [RemoteState(true)]
        public int LoggingFrequency { get; set; } = 1;

        /// <summary>
        /// Load the settings from the service.
        /// </summary>
        public void LoadSettings()
        {
            if (settings.TryLoad(nameof(LoggingFrequency), out int f))
            {
                UpdateLoggingFrequency(f);
            }

            if (settings.TryLoad(nameof(IsLogging), out bool isLogging) && isLogging)
            {
                StartLogging();
            }
        }

        /// <summary>
        /// Start logging data.
        /// </summary>
        [RemoteCommand]
        public void StartLogging()
        {
            IsLogging = true;
            settings.Update(nameof(IsLogging), IsLogging);
            Notify?.Invoke(this, new NotifyEventArgs($"{nameof(IsLogging)}:{IsLogging}", MessageTypes.Information));

            loggingTimer.Start();
            dumpTimer.Start();
        }

        /// <summary>
        /// Stop logging data.
        /// </summary>
        [RemoteCommand]
        public void StopLogging()
        {
            IsLogging = false;
            settings.Update(nameof(IsLogging), IsLogging);
            Notify?.Invoke(this, new NotifyEventArgs($"{nameof(IsLogging)}:{IsLogging}", MessageTypes.Information));
            loggingTimer.Stop();
            dumpTimer.Stop();

            DumpLogToFile();
        }

        /// <summary>
        /// Update the logging frequency to the given frequency.
        /// </summary>
        /// <param name="frequency">Frequency to log data.</param>
        [Alias(nameof(LoggingFrequency))]
        public void UpdateLoggingFrequency(int frequency)
        {
            frequency = frequency.Clamp(1, 10);
            LoggingFrequency = frequency;
            settings.Update(nameof(LoggingFrequency), LoggingFrequency);

            loggingTimer.Interval = TimeSpan.FromSeconds(1 / LoggingFrequency).TotalMilliseconds;
        }

        /// <summary>
        /// Handler for when data is received.
        /// </summary>
        /// <param name="e">String of data received.</param>
        private void DataReceived(string e)
        {
            var match = Regex.Match(e, NMEAPattern);

            // return if it's not a NMEA string
            if (!match.Success)
            {
                return;
            }

            var header = match.Groups[1].Value;

            data.Update(header, e);
        }

        private void DumpLogToFile()
        {
            var l = buffer.ToString();
            buffer.Clear();
            logger.LogAsync(l, new FileInfo(Path.Combine(directory?.ToString() ?? string.Empty, DateTime.Now.ToString("yyyy-MM-dd") + ".csv")));
        }

        private void LoggingTimer_Elapsed()
        {
            // go through each entry
            foreach (var item in data)
            {
                // send the parsed NMEA data over the output communicator if it's not null
                output?.SendAsync(item.Value);

                var toLog = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")},{item.Value}";

                Notify?.Invoke(this, new NotifyEventArgs(item.Value, MessageTypes.SensorData));

                if (IsLogging)
                {
                    buffer.Append(toLog);
                }
            }

            data.Clear();
        }
    }
}
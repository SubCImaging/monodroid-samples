//-----------------------------------------------------------------------
// <copyright file="DataAppender.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators
{
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Timers;

    /// <summary>
    /// Append data to a partial string which sends based on time, or terminating characters.
    /// </summary>
    public class DataAppender : INotifier, IDisposable
    {
        /// <summary>
        /// Timers to send trigger when to send data.
        /// </summary>
        private readonly Timer dataTimer = new Timer();

        /// <summary>
        /// Object used to lock on the appender.
        /// </summary>
        private readonly object sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAppender"/> class.
        /// </summary>
        public DataAppender()
            : this(string.Empty, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(100))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAppender"/> class.
        /// </summary>
        /// <param name="terminator">Termination string indicating a split and send of data.</param>
        public DataAppender(string terminator)
        {
            Terminator = terminator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAppender"/> class.
        /// </summary>
        /// <param name="masterTimeout">Consistent timer for sending data.</param>
        /// <param name="dataTimeout">How long to wait for new data before sending.</param>
        public DataAppender(
            TimeSpan masterTimeout,
            TimeSpan dataTimeout)
            : this(string.Empty, masterTimeout, dataTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAppender"/> class.
        /// </summary>
        /// <param name="terminator">Termination string indicating a split and send of data.</param>
        /// <param name="masterTimeout">Consistent timer for sending data.</param>
        /// <param name="dataTimeout">How long to wait for new data before sending.</param>
        public DataAppender(
            string terminator,
            TimeSpan masterTimeout,
            TimeSpan dataTimeout)
            : this(terminator)
        {
            dataTimer.Interval = dataTimeout.TotalMilliseconds;
            MasterTimer.Interval = masterTimeout.TotalMilliseconds;

            dataTimer.Elapsed += Timer_Elapsed;
            MasterTimer.Elapsed += Timer_Elapsed;
        }

        /// <summary>
        /// Notification event to send data to other classes
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Gets or sets the Time the appender will wait for new data. If data is not received, the appender will send.
        /// </summary>
        public TimeSpan DataTimeout
        {
            get => TimeSpan.FromMilliseconds(dataTimer.Interval);

            set => dataTimer.Interval = value.TotalMilliseconds;
        }

        /// <summary>
        /// Gets a value indicating whether the appender is finished appending.
        /// </summary>
        public bool IsFinished { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether the appender is started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets or sets the Timer that ticks at a specific interval, regardless of incoming data. This is to prevent the appender from waiting long periods of time before sending data.
        /// </summary>
        public TimeSpan MasterTimeout
        {
            get => TimeSpan.FromMilliseconds(MasterTimer.Interval);

            set => MasterTimer.Interval = value.TotalMilliseconds;
        }

        /// <summary>
        /// Gets or sets the master timer.
        /// </summary>
        public Timer MasterTimer { get; set; } = new Timer();

        /// <summary>
        /// Gets partial string before data is sent. This string is appended.
        /// </summary>
        public StringBuilder PartialString { get; } = new StringBuilder();

        /// <summary>
        /// Gets or sets a value to match a pattern to the incoming data.
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Gets or sets the String of characters the appender with split the data on, sending everything prior to.
        /// </summary>
        public string Terminator { get; set; } = string.Empty;

        /// <summary>
        /// Append data to the temporary holder.
        /// </summary>
        /// <param name="data">Data to append.</param>
        public virtual void Append(string data)
        {
            IsFinished = false;

            // data is received append it to the partial string
            lock (sync)
            {
                // start the master timer if it's not already started
                if (!MasterTimer.Enabled)
                {
                    MasterTimer.Start();
                }

                // reset the data timer
                dataTimer.Stop();
                dataTimer.Start();

                PartialString.Append(data);
            }

            if (!string.IsNullOrEmpty(Pattern))
            {
                if (MatchesPattern(out var match))
                {
                    Console.WriteLine("Information: Data Appender: Match Success - " + PartialString);
                    Stop();
                }
            }

            // if the terminator is not null, split the data on it and send it off
            if (!string.IsNullOrEmpty(Terminator))
            {
                string tempPartial;

                lock (sync)
                {
                    // clear the partial string, and append whatever remains
                    tempPartial = PartialString.ToString();
                    PartialString.Clear();
                }

                // split and send everything before the terminator
                var splits = Regex.Split(tempPartial, @"(?<=[" + Terminator + "])");

                foreach (var item in splits)
                {
                    // make sure the split data ends with the terminator before you send it, in the case nothing was actually split so append it back to the partialString
                    if (item.EndsWith(Terminator))
                    {
                        lock (sync)
                        {
                            dataTimer.Stop();
                            dataTimer.Start();

                            MasterTimer.Stop();
                            MasterTimer.Start();
                        }

                        Notify?.Invoke(this, new NotifyEventArgs(item, MessageTypes.Receive));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            lock (sync)
                            {
                                PartialString.Append(item);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        public void Dispose()
        {
            MasterTimer.Elapsed -= Timer_Elapsed;
            dataTimer.Elapsed -= Timer_Elapsed;

            dataTimer.Dispose();
            MasterTimer.Dispose();
        }

        /// <summary>
        /// Start the master timer, and clear the partial string; for synchronous use.
        /// </summary>
        public void Start()
        {
            IsStarted = true;
            IsFinished = false;
            MasterTimer.Start();

            lock (sync)
            {
                PartialString.Clear();
            }
        }

        /// <summary>
        /// Stop the data appending and the timers. Clear the partial string.
        /// </summary>
        public void Stop()
        {
            string message;

            lock (sync)
            {
                MasterTimer.Stop();
                dataTimer.Stop();
                if (string.IsNullOrEmpty(Pattern))
                {
                    message = PartialString.ToString();
                }
                else
                {
                    if (!MatchesPattern(out message))
                    {
                        Console.WriteLine($"Error: Data Appender: Match Failure. Pattern: {Pattern} Data Appended: {PartialString}");
                    }
                }

                PartialString.Clear();
            }

            OnNotify(message);

            IsStarted = false;
            IsFinished = true;
        }

        /// <summary>
        /// Fire the notification method.
        /// </summary>
        /// <param name="data">Data to notify.</param>
        protected void OnNotify(string data)
        {
            Notify?.Invoke(this, new NotifyEventArgs(data, MessageTypes.Receive));
        }

        /// <summary>
        /// Tests the current partialString agains the regex pattern.
        /// </summary>
        /// <param name="data">out parameter which sets the value to the matched string.</param>
        /// <returns>true if the pattern was matched or false otherwise.</returns>
        private bool MatchesPattern(out string data)
        {
            data = string.Empty;
            if (Extensions.RegexExtensions.IsValidPattern(Pattern))
            {
                var regex = new Regex(Pattern);

                string strToMatch;

                lock (sync)
                {
                    // grab a local copy of the partial string
                    strToMatch = PartialString.ToString();
                }

                // compare it to the regex pattern
                var match = regex.Match(strToMatch);

                if (match.Success)
                {
                    lock (sync)
                    {
                        // set the partial string to the first occurance of the matched pattern
                        data = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Finish the appending sequence.
        /// </summary>
        /// <param name="sender">Timer sender.</param>
        /// <param name="e">Generic arguments.</param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Console.WriteLine("DataAppender: Timeout Reached");
            Stop();
        }
    }
}
namespace SubCTools.Droid
{
    using Android.App;
    using Android.Content;
    using Android.Hardware;
    using Android.Runtime;
    using SubCTools.Attributes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Droid.Attributes;
    using SubCTools.Droid.Listeners;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Timers;

    public class SubCSensorManager : DroidBase
    {
        private readonly Timer sensorsPoll = new Timer();
        private readonly Timer recordingLog = new Timer();
        private readonly ICommunicator communicator;

        private int sensorPollPeriod = 1000;
        private bool isSensorPollEnabled;
        private bool logDataOnStill = true;
        private int recordingLogPeriod = 1000;
        private bool logDataOnRecording;

        private string recordingLogDirectory;
        private string recordingLogName;

        public SubCSensorManager(
            Activity activity,
            ISettingsService settings,
            Action<EventHandler<string>> stillTaken,
            Action<EventHandler<string>> recordingStarted,
            Action<EventHandler> recordingStopped,
            ICommunicator communicator
            )
            : base(settings)
        {
            this.communicator = communicator;
            communicator.DataReceived += Communicator_DataReceived;

            SensorListener = new SensorListener(activity);

            sensorsPoll.Interval = SensorPollInterval;
            sensorsPoll.Elapsed += SensorsPoll_Elapsed;

            recordingLog.Interval = RecordingLogInterval;
            recordingLog.Elapsed += RecordingLog_Elapsed;

            stillTaken?.Invoke((s, e) => Log(e));

            recordingStarted?.Invoke((s, e) => RecordingStarted(e));
            recordingStopped?.Invoke((s, e) => recordingLog.Stop());

            MessageRouter.Instance.Add(this);

            communicator.SendAsync("$NMEAPrintValuesAll");
        }

        public SensorListener SensorListener { get; }

        /// <summary>
        /// The frequency at which to poll sensors
        /// </summary>
        [Savable]
        [RemoteState]
        public int SensorPollInterval
        {
            get => sensorPollPeriod;
            set
            {
                if (Set(nameof(SensorPollInterval), ref sensorPollPeriod, value))
                {
                    sensorsPoll.Interval = value;
                }
            }
        }

        /// <summary>
        /// The frequency at which to write to the recording log
        /// </summary>
        [Savable]
        [RemoteState]
        public int RecordingLogInterval
        {
            get => recordingLogPeriod;
            set
            {
                if (Set(nameof(RecordingLogInterval), ref recordingLogPeriod, value))
                {
                    recordingLog.Interval = value;
                }
            }
        }

        /// <summary>
        /// True if the sensor poll is enabled
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool IsSensorPollEnabled
        {
            get => isSensorPollEnabled;
            private set => Set(nameof(IsSensorPollEnabled), ref isSensorPollEnabled, value);
        }

        /// <summary>
        /// If true, logs sensor data when taking a still
        /// </summary>
        [Savable]
        [RemoteState]
        public bool LogDataOnStill
        {
            get => logDataOnStill;
            set => Set(nameof(LogDataOnStill), ref logDataOnStill, value);
        }

        /// <summary>
        /// If true, logs sensor data during recording
        /// </summary>
        [Savable]
        [RemoteState]
        public bool LogDataOnRecording
        {
            get => logDataOnRecording;
            set => Set(nameof(LogDataOnRecording), ref logDataOnRecording, value);
        }

        /// <summary>
        /// Turns sensor polling on
        /// </summary>
        [RemoteCommand]
        public void SensorPollOn()
        {
            sensorsPoll.Start();
            IsSensorPollEnabled = true;
        }

        /// <summary>
        /// Turns sensor polling off
        /// </summary>
        [RemoteCommand]
        public void SensorPollOff()
        {
            sensorsPoll.Stop();
            IsSensorPollEnabled = false;
        }

        public override void LoadSettings()
        {
            base.LoadSettings();
            if (IsSensorPollEnabled)
            {
                SensorPollOn();
            }
        }

        public string SensorData { get; private set; } = string.Empty;

        public override string ToString() => string.Join("\n", $"DateTime,{DateTime.Now:yyyy-MM-dd hhmmss.fff}", SensorData);

        private void Communicator_DataReceived(object sender, string e)
        {
            if (e.StartsWith("$NMEA") || e.StartsWith("$--"))
            {
                SensorData = e;
            }
        }

        private void Log(string path)
        {
            if (!LogDataOnStill) return;

            var directory = Path.GetDirectoryName(path);
            var filename = Path.GetFileNameWithoutExtension(path);

            SubCLogger.Instance.Write(this.ToString(), filename + ".csv", directory);
        }

        private void RecordingStarted(string path)
        {
            if (!LogDataOnRecording) return;

            recordingLogDirectory = Path.GetDirectoryName(path);
            var filename = Path.GetFileNameWithoutExtension(path);

            recordingLogName = filename + ".csv";

            SubCLogger.Instance.Write(this.ToString(), recordingLogName, recordingLogDirectory);
            recordingLog.Start();
        }

        private void RecordingLog_Elapsed(object sender, ElapsedEventArgs e)
        {
            SubCLogger.Instance.Write(this.ToString(), recordingLogName, recordingLogDirectory);
        }

        private void SensorsPoll_Elapsed(object sender, ElapsedEventArgs e)
        {
            communicator.SendAsync("$NMEAPrintValuesAll");
            //OnNotify(this.ToString());
        }
    }
}
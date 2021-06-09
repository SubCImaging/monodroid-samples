//-----------------------------------------------------------------------
// <copyright file="Intervals.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Camera
{
    using Android.Graphics;
    using SubCTools.Attributes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Converters;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// A class that controls the camera in intervals mode.
    /// </summary>
    public class Intervals : DroidBase
    {
        /// <summary>
        /// The camera object
        /// </summary>
        private readonly Rayfin camera;

        /// <summary>
        /// A timer that raises an event when the stills are due to stop being taken
        /// </summary>
        private readonly System.Timers.Timer stillDurationTimer = new System.Timers.Timer { AutoReset = false };

        /// <summary>
        /// A timer that raises an the event to trigger a still to be taken
        /// </summary>
        private readonly System.Timers.Timer stillPeriodTimer = new System.Timers.Timer { AutoReset = true };

        /// <summary>
        /// A timer that raises an event when recording is due to stop
        /// </summary>
        private readonly System.Timers.Timer recordingDurationTimer = new System.Timers.Timer { AutoReset = false };

        /// <summary>
        /// A timer that raises an event when the WaitToStart is done
        /// </summary>
        private readonly System.Timers.Timer waitingTimer = new System.Timers.Timer { AutoReset = false };

        /// <summary>
        /// A precision timer that ticks once per second and reports the current interval times and triggers the end of an interval
        /// </summary>
        private readonly PrecisionTimer totalRunningTimer = new PrecisionTimer();

        /// <summary>
        /// The amount of time since the entire intervals session started
        /// </summary>
        private TimeSpan totalRunningTime;

        /// <summary>
        /// An amount to subtract from totalRunningTime when the calculating CycleCount to allow for fencepost errors when saving media.  Sometimes the camera cant execute your command right away
        /// </summary>
        private TimeSpan offset = TimeSpan.Zero;

        /// <summary>
        /// The amount of time it takes the camera to shut down
        /// </summary>
        private TimeSpan ShutdownTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The amount of time it takes the camera to start up.  An estimate from benchmark
        /// </summary>
        private TimeSpan StartupTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The number of cycles completed
        /// </summary>
        private int cycleCompletedCount = 0;

        /// <summary>
        /// An amount of time waited during WaitToStart
        /// </summary>
        private TimeSpan waitedTime = TimeSpan.Zero;

        /// <summary>
        /// The number of cycles to complete before stopping
        /// </summary>
        private int numberOfCycles = 0;

        /// <summary>
        /// Are Intervals Enabled?
        /// </summary>
        private bool areIntervalsEnabled = false;

        /// <summary>
        /// The amount of time to idle after intervals actions are completed but before starting a new cycle.
        /// </summary>
        private TimeSpan idleDuration = TimeSpan.Zero;

        /// <summary>
        /// The amount of time to spend taking stills during one intervals cycle
        /// </summary>
        private TimeSpan stillDuration = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The amount of time between each still
        /// </summary>
        private TimeSpan stillPeriod = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The amount of time to wait before starting
        /// </summary>
        private TimeSpan waitToStart = TimeSpan.Zero;

        /// <summary>
        /// The amount of time to spend recording video during one intervals cycle
        /// </summary>
        private TimeSpan durationToRecord = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Take stills during the interval
        /// </summary>
        private bool takeStills = true;

        /// <summary>
        /// Record video during the interval
        /// </summary>
        private bool recordVideo;

        /// <summary>
        /// If true the rayfin will entirely shut down during an idle period
        /// </summary>
        private bool sleepWhileIdle = true;

        /// <summary>
        /// Take stills and record video at the same time during an interval
        /// </summary>
        private bool simultaneousStillsAndVideo;

        /// <summary>
        /// If true take stills first, if false record video first
        /// </summary>
        private bool stillsFirst;

        /// <summary>
        /// Indicates wheather a stills action continues right up until another one starts.
        /// </summary>
        private bool ContinuousStills => (idleDuration == TimeSpan.Zero) && (!WillRecordVideo || (simultaneousStillsAndVideo && stillDuration > durationToRecord));

        /// <summary>
        /// Indicates wheather a recording action continues right up until another one starts.
        /// </summary>
        private bool ContinuousRecording => (idleDuration == TimeSpan.Zero) && (!WillTakeStills || (simultaneousStillsAndVideo && durationToRecord > stillDuration));

        /// <summary>
        /// The Date and Time that the intervals session started
        /// </summary>
        private DateTime? startTime;

        /// <summary>
        /// A cancellation token to use in case the user cancels the intervals session before the WaitToStart timer elapses.
        /// </summary>
        private CancellationToken token;

        /// <summary>
        /// A cancellation token source, same as above
        /// </summary>
        private CancellationTokenSource tokenSource;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="camera">The camera object</param>
        /// <param name="settings">The settings service</param>
        public Intervals(Rayfin camera, ISettingsService settings)
            : base(settings)
        {
            this.camera = camera;

            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            // Timer Inits
            stillDurationTimer.Interval = stillDuration.TotalMilliseconds;
            stillPeriodTimer.Interval = stillPeriod.TotalMilliseconds;
            recordingDurationTimer.Interval = durationToRecord.TotalMilliseconds;
            waitingTimer.Interval = (waitToStart == TimeSpan.Zero) ? 1 : waitToStart.TotalMilliseconds;

            // Timer Subscriptions
            stillDurationTimer.Elapsed += StillCycleTimer_Elapsed;
            stillPeriodTimer.Elapsed += StillTimer_Elapsed;
            recordingDurationTimer.Elapsed += RecordingTimer_Elapsed;
            waitingTimer.Elapsed += NotifyWaiting;
            totalRunningTimer.Tick += CycleDurationTimer_Tick;

            camera.StillHandler.ImageFormatChanged += ImageFormat_Changed;
        }

        /// <summary>
        /// The minimum amount of time between stills
        /// </summary>
        [RemoteState]
        public TimeSpan MinimumStillsPeriod { get; private set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The Date and Time that the intervals session started
        /// </summary>
        [Savable]
        public DateTime? StartTime
        {
            get => startTime;
            set => Set(nameof(StartTime), ref startTime, value);
        }

        /// <summary>
        /// Are Intervals Enabled?
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool AreIntervalsEnabled
        {
            get => areIntervalsEnabled;
            set
            {
                if (Set(nameof(AreIntervalsEnabled), ref areIntervalsEnabled, value))
                {
                    OnNotify($"{nameof(AreIntervalsEnabled)}:{AreIntervalsEnabled}");
                }
            }
        }

        /// <summary>
        /// Indicates wheather a stills action continues right up until another one starts.
        /// </summary>
        [Savable]
        [RemoteState]
        public bool StillsFirst
        {
            get => stillsFirst;
            set => Set(nameof(StillsFirst), ref stillsFirst, value);
        }

        /// <summary>
        /// Take stills and record video at the same time during an interval
        /// </summary>
        [Savable]
        [RemoteState]
        public bool SimultaneousStillsAndVideo
        {
            get => simultaneousStillsAndVideo;
            set => Set(nameof(SimultaneousStillsAndVideo), ref simultaneousStillsAndVideo, value);
        }

        /// <summary>
        /// Take stills during the interval
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool WillTakeStills
        {
            get => takeStills;
            set => Set(nameof(WillTakeStills), ref takeStills, value);
        }

        /// <summary>
        /// Record video during the interval
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool WillRecordVideo
        {
            get => recordVideo;
            set => Set(nameof(WillRecordVideo), ref recordVideo, value);
        }

        /// <summary>
        /// The total amount of time since the entire intervals session started
        /// </summary>
        [RemoteState(true)]
        public TimeSpan TotalRunningTime
        {
            get => totalRunningTime;
            set => Set(nameof(TotalRunningTime), ref totalRunningTime, value);
        }

        /// <summary>
        /// The number of cycles to complete before stopping
        /// </summary>
        [Savable]
        [RemoteState]
        public int NumberOfCycles
        {
            get => numberOfCycles;
            set => Set(nameof(NumberOfCycles), ref numberOfCycles, value);
        }

        /// <summary>
        /// The amount of time to spend taking stills during an interval
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public TimeSpan StillDuration
        {
            get => stillDuration;
            set
            {
                if (Set(nameof(StillDuration), ref stillDuration, value))
                {
                    if (value == TimeSpan.Zero) return;

                    stillDurationTimer.Interval = value.TotalMilliseconds;
                }
                if (StillPeriod > StillDuration)
                {
                    StillPeriod = StillDuration;
                    OnNotify($"{nameof(StillPeriod)}:{StillPeriod}");
                }
            }
        }

        /// <summary>
        /// The amount of time to spend recording video during an interval
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public TimeSpan DurationToRecord
        {
            get => durationToRecord;
            set
            {
                if (Set(nameof(DurationToRecord), ref durationToRecord, value))
                {
                    if (value == TimeSpan.Zero) return;
                    recordingDurationTimer.Interval = value.TotalMilliseconds;
                }
            }
        }

        /// <summary>
        /// The amount of time between stills
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public TimeSpan StillPeriod
        {
            get => stillPeriod;
            set
            {
                if (value == TimeSpan.Zero) return;
                if (value < MinimumStillsPeriod) value = MinimumStillsPeriod;

                if (Set(nameof(StillPeriod), ref stillPeriod, value))
                {
                    stillPeriodTimer.Interval = value.TotalMilliseconds;
                }
            }
        }

        /// <summary>
        /// The amount of time to idle after intervals actions are completed but before starting a new cycle.
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public TimeSpan IdleDuration
        {
            get => idleDuration;
            set
            {
                Set(nameof(IdleDuration), ref idleDuration, value);
            }
        }

        /// <summary>
        /// If true the rayfin will entirely shut down during an idle period
        /// </summary>
        [Savable]
        [RemoteState]
        public bool SleepWhileIdle
        {
            get => sleepWhileIdle;
            set => Set(nameof(SleepWhileIdle), ref sleepWhileIdle, value);
        }

        /// <summary>
        /// The amount of time to wait before starting
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public TimeSpan WaitToStart
        {
            get => waitToStart;
            set
            {
                Set(nameof(WaitToStart), ref waitToStart, value);
                waitingTimer.Interval = (value == TimeSpan.Zero) ? 1 : value.TotalMilliseconds;
            }
        }

        /// <summary>
        /// The length of time the cycle wants to run
        /// </summary>
        [RemoteState]
        public TimeSpan CycleLength => GetActionDuration() + IdleDuration;

        /// <summary>
        /// The number of cycles completed
        /// </summary>
        [RemoteState]
        public int CycleCount => (int)((TotalRunningTime - offset).TotalSeconds / CycleLength.TotalSeconds);

        /// <summary>
        /// How long has this current cycle been running
        /// </summary>
        [RemoteState]
        public TimeSpan CycleTime
        {
            get
            {
                var length = CycleLength.TotalSeconds;
                return length == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds((TotalRunningTime - offset).TotalSeconds % length);
            }
        }

        /// <summary>
        /// Update function to set WillTakeStills
        /// </summary>
        [RemoteCommand]
        public void EnableIntervalsStills()
        {
            WillTakeStills = true;
            OnNotify($"{nameof(WillTakeStills)}:{WillTakeStills}");
        }

        /// <summary>
        /// Update function to set WillTakeStills
        /// </summary>
        [RemoteCommand]
        public void DisableIntervalsStills()
        {
            WillTakeStills = false;
            if (!WillTakeStills && !WillRecordVideo)
            {
                WillRecordVideo = true;
            }

            OnNotify($"{nameof(WillTakeStills)}:{WillTakeStills}");
            OnNotify($"{nameof(WillRecordVideo)}:{WillRecordVideo}");
        }

        /// <summary>
        /// Update function to set WillRecordVideo
        /// </summary>
        [RemoteCommand]
        public void EnableIntervalsVideo()
        {
            WillRecordVideo = true;
            OnNotify($"{nameof(WillRecordVideo)}:{WillRecordVideo}");
        }

        /// <summary>
        /// Update function to set WillRecordVideo
        /// </summary>
        [RemoteCommand]
        public void DisableIntervalsVideo()
        {
            WillRecordVideo = false;
            if (!WillTakeStills && !WillRecordVideo)
            {
                WillTakeStills = true;
            }

            OnNotify($"{nameof(WillTakeStills)}:{WillTakeStills}");
            OnNotify($"{nameof(WillRecordVideo)}:{WillRecordVideo}");
        }

        /// <summary>
        /// Starts the intervals session.  Called only at the command to start intervals
        /// </summary>
        [RemoteCommand]
        public void StartIntervals()
        {
            cycleCompletedCount = 0;
            TotalRunningTime = TimeSpan.Zero;
            offset = TimeSpan.Zero;
            StartTime = DateTime.Now + WaitToStart;

            OnNotify($"{nameof(TotalRunningTime)}:{TotalRunningTime}");
            OnNotify($"{nameof(CycleTime)}:{CycleTime}");
            OnNotify($"{nameof(CycleCount)}:{CycleCount}");

            StartIntervals(WaitToStart);
        }

        /// <summary>
        /// Called when rayfin wakes up and intervals are already enabled.
        /// </summary>
        public void ResumeIntervals()
        {
            offset = TimeSpan.Zero;
            TotalRunningTime = DateTime.Now - StartTime.Value;
            cycleCompletedCount = CycleCount;
            OnNotify($"{nameof(CycleCount)}:{CycleCount}");

            totalRunningTimer.Start();
        }

        /// <summary>
        /// Stops the intervals session
        /// </summary>
        [RemoteCommand]
        public void StopIntervals()
        {
            stillDurationTimer.Stop();
            stillPeriodTimer.Stop();
            recordingDurationTimer.Stop();
            waitingTimer.Stop();
            totalRunningTimer.Stop();

            if (camera.RecordingHandler.IsRecording ?? true)
            {
                camera.StopRecording();
            }

            tokenSource.Cancel();

            StartTime = null;

            AreIntervalsEnabled = false;
        }

        /// <summary>
        /// Loads the settings from the settings file
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();

            if (AreIntervalsEnabled)
            {
                ResumeIntervals();
            }
        }

        /// <summary>
        /// Starts each interval.  Each one beyond the first will be given TimeSpan.Zero as an argument
        /// </summary>
        /// <param name="waitToStart">The amount of time to wait before starting intervals</param>
        private async void StartIntervals(TimeSpan waitToStart)
        {
            if (!WillRecordVideo && !WillTakeStills)
            {
                OnNotify("No intervals actions selected!", Messaging.Models.MessageTypes.Error);
                StopIntervals();
                return;
            }

            if (WillRecordVideo && WillTakeStills && camera.RecordingHandler.Is4K && SimultaneousStillsAndVideo)
            {
                OnNotify("Can't record 4K and capture stills at the same time.  Consider sequential ordering or a lower bitrate.", Messaging.Models.MessageTypes.Error);
                StopIntervals();
                return;
            }

            if (CycleLength == TimeSpan.Zero)
            {
                OnNotify("Cycle Time is Zero", Messaging.Models.MessageTypes.Error);
                StopIntervals();
                return;
            }

            AreIntervalsEnabled = true;
            waitedTime = TimeSpan.Zero;
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            try
            {
                if (waitToStart.TotalSeconds > 0)
                {
                    waitingTimer.Start();
                }

                OnNotify("WaitToStartCountdown: " + waitToStart);
                await Task.Delay(waitToStart, token);
            }
            catch
            {
                OnNotify("Wait_to_start task threw an exception");
            }

            if (token.IsCancellationRequested)
            {
                OnNotify("Wait to start cancelled");
                StopIntervals();
                return;
            }

            if (WillTakeStills && (!WillRecordVideo || (ContinuousStills && cycleCompletedCount == 0) || (!ContinuousStills && (SimultaneousStillsAndVideo || StillsFirst))))
            {
                StartStillsAction();
            }

            if (WillRecordVideo && (!WillTakeStills || (ContinuousRecording && cycleCompletedCount == 0) || (!ContinuousRecording && (SimultaneousStillsAndVideo || !StillsFirst))))
            {
                StartRecordingAction();
            }

            if (!totalRunningTimer.IsEnabled)
            {
                totalRunningTimer.Start();
            }

            OnNotify("Interval is started");
        }

        /// <summary>
        /// Starts a stills action
        /// </summary>
        private void StartStillsAction()
        {
            if (camera.CanTakePicture)
            {
                camera.TakePicture();
            }

            StartCycleAction("stills", WillTakeStills, ContinuousStills, stillDurationTimer, () => stillPeriodTimer.Start());
        }

        /// <summary>
        /// Starts a recording action
        /// </summary>
        private void StartRecordingAction() => StartCycleAction("recording", WillRecordVideo, ContinuousRecording, recordingDurationTimer, () => camera.StartRecording());

        /// <summary>
        /// Starts a recording or stills action
        /// </summary>
        /// <param name="actionName">the name of the action</param>
        /// <param name="willStart">indicates if the action is set to start</param>
        /// <param name="isInfinite">indicates if the action will continue without stopping</param>
        /// <param name="durationTimer">specifies the duration of the action</param>
        /// <param name="startAction">the action to start</param>
        private void StartCycleAction(string actionName, bool willStart, bool isInfinite, System.Timers.Timer durationTimer, Action startAction)
        {
            OnNotify($"Attempting to start {actionName} action");
            if (!willStart) return;

            if (!isInfinite)
            {
                durationTimer.Start();
            }

            startAction();
            OnNotify($"{actionName} action Started");
        }

        /// <summary>
        /// Sends a message up to RCS reporting the amount of time waited while waiting to start
        /// </summary>
        /// <param name="sender">The parameter is not used.</param>
        /// <param name="e">The parameter is not used.</param>
        private void NotifyWaiting(object sender, EventArgs e)
        {
            waitedTime += TimeSpan.FromSeconds(1);
            if (waitedTime < WaitToStart)
            {
                waitingTimer.Start();
            }

            OnNotify($"{nameof(TotalRunningTime)}:{waitedTime - WaitToStart}");
        }

        /// <summary>
        /// Once per second this method reports the TotalRunningTime and CycleTime to RCS, and checks if the current interval is complete
        /// </summary>
        /// <param name="sender">The parameter is not used.</param>
        /// <param name="e">The parameter is not used.</param>
        private void CycleDurationTimer_Tick(object sender, EventArgs e)
        {
            TotalRunningTime = DateTime.Now - StartTime.Value;

            OnNotify($"{nameof(TotalRunningTime)}:{TotalRunningTime}");
            OnNotify($"{nameof(CycleTime)}:{CycleTime}");

            if (cycleCompletedCount < CycleCount)
            {
                if (!recordingDurationTimer.Enabled
                    && !stillDurationTimer.Enabled)
                {
                    // the cycle is complete, restart or finish
                    CompleteCycle();
                }
                else
                {
                    // if you can't complete the cycle, increase the offset so your cycle time remains the same
                    offset += TimeSpan.FromSeconds(1);
                    OnNotify($"DEBUG: Offset Incremented.  Rec:{recordingDurationTimer.Enabled}|Pic:{stillDurationTimer.Enabled}");
                }
            }
        }

        /// <summary>
        /// When recording is finished, go here
        /// </summary>
        /// <param name="sender">The parameter is not used.</param>
        /// <param name="e">The parameter is not used.</param>
        private void RecordingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnNotify("Recording complete");

            camera.StopRecording();
            recordingDurationTimer.Stop();

            if (WillTakeStills && !SimultaneousStillsAndVideo && !StillsFirst)
            {
                StartStillsAction();
            }

            CheckForSleep();
        }

        /// <summary>
        /// When stills are finished, go here
        /// </summary>
        /// <param name="sender">The parameter is not used.</param>
        /// <param name="e">The parameter is not used.</param>
        private void StillCycleTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnNotify("Still cycle complete");

            stillPeriodTimer.Stop();
            stillDurationTimer.Stop();

            if (WillRecordVideo && !SimultaneousStillsAndVideo && StillsFirst)
            {
                StartRecordingAction();
            }

            CheckForSleep();
        }

        /// <summary>
        /// Check to see if it's time for bed
        /// </summary>
        private void CheckForSleep()
        {
            if (!recordingDurationTimer.Enabled && !stillDurationTimer.Enabled && SleepWhileIdle && (IdleDuration > ShutdownTime + StartupTime + TimeSpan.FromSeconds(10)))
            {
                OnNotify($"~hibernate set time:{(IdleDuration - ShutdownTime - StartupTime).TotalSeconds}");
            }
        }

        /// <summary>
        /// When it's time to take a picture, go here
        /// </summary>
        /// <param name="sender">The parameter is not used.</param>
        /// <param name="e">The parameter is not used.</param>
        private async void StillTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (camera.CanTakePicture)
            {
                await camera.StillHandler.TakeStillAsync();
            }
        }

        /// <summary>
        /// Completes an intervals cycle and determines if another needs to start
        /// </summary>
        private void CompleteCycle()
        {
            cycleCompletedCount = CycleCount;
            OnNotify($"{nameof(CycleCount)}:{CycleCount}");

            if (NumberOfCycles <= 0)
            {
                // you're continuous, keep going
                StartIntervals(TimeSpan.Zero);
                return;
            }

            if (CycleCount >= NumberOfCycles)
            {
                OnNotify("Number of cycles reached");
                StopIntervals();
            }
            else
            {
                StartIntervals(TimeSpan.Zero);
                return;
            }
        }

        /// <summary>
        /// Returns the amount of time the camera will spend capturing media during each interval
        /// </summary>
        /// <returns>Returns the amount of time the camera will spend capturing media</returns>
        private TimeSpan GetActionDuration()
        {
            if (WillTakeStills && WillRecordVideo)
            {
                if (SimultaneousStillsAndVideo)
                {
                    return StillDuration > DurationToRecord ? StillDuration : DurationToRecord;
                }
                else
                {
                    return StillDuration + DurationToRecord;
                }
            }
            else if (WillTakeStills && !WillRecordVideo)
            {
                return StillDuration;
            }
            else if (!WillTakeStills && WillRecordVideo)
            {
                return DurationToRecord;
            }
            else
            {
                return TimeSpan.Zero;
            }
        }

        private void ImageFormat_Changed(object sender, ImageFormatType format)
        {
            if (format == ImageFormatType.Jpeg)
            {
                MinimumStillsPeriod = TimeSpan.FromSeconds(1);
            }
            else
            {
                MinimumStillsPeriod = TimeSpan.FromSeconds(6);
            }
            OnNotify($"{nameof(MinimumStillsPeriod)}:{MinimumStillsPeriod}");
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="RecordingHandler.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Camera
{
    using Android.Media;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.DataTypes;
    using SubCTools.DiveLog;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.Extensions;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Listeners;
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;
    using static SubCTools.Helpers.Paths;

    /// <summary>
    /// Manages recording in the Rayfin using the Camera2Api.
    /// </summary>
    public class RecordingHandler : DroidBase//, IDiveRecorder
    {
        private const int DefaultBitRate = 25;
        private const VideoEncoder DefaultEncoder = VideoEncoder.Hevc;
        private const int DefaultFileDuration = 10;
        private const int DefaultResolutionHeight = 1080;
        private const int DefaultResolutionWidth = 1920;
        private const string DefaultVideoDirectory = "/Videos";

        /// <summary>
        /// The default video name to use if the settings file doesn't contain one.
        /// </summary>
        private const string DefaultVideoName = "${yyyy}-${MM}-${dd} - ${hh}${mm}${ss}";

        //private const string SwapFolder = "/data/local/tmp/swap";

        /// <summary>
        /// The lock object that keeps <see cref="StartRecording()"/>
        /// from getting called more than once at a time.
        /// </summary>
        private static readonly object RecordingSync = new object();

        /// <summary>
        /// The lock object that keeps <see cref="SetUpMediaRecorder()"/>
        /// from getting called more than once at a time.
        /// </summary>
        private static readonly object SetUpSync = new object();

        /// <summary>
        /// The <see cref="SubCCaptureSession"/> responsible for managing the
        /// recording/preview/stills surfaces.
        /// </summary>
        private readonly SubCCaptureSession captureSession;

        /// <summary>
        /// The <see cref="DiskSpaceManager"/> used to keep track of
        /// whether or not the file is growing and the recording is
        /// happening as intended.
        /// </summary>
        private readonly Droid.DiskSpaceMonitor diskMonitor;

        /// <summary>
        /// The <see cref="DiskSpaceManager"/> that is used
        /// to generate the <see cref="DiskSpaceManager"/>
        /// </summary>
        private readonly DiskSpaceManager diskSpaceManager;

        private readonly IMediaRecorder mediaRecorder;

        /// <summary>
        /// The default bitrate in mbit/s
        /// </summary>
        private int bitRate = DefaultBitRate;

        /// <summary>
        /// The current file that is being saved/recorded.
        /// </summary>
        private FileInfo currentFile;

        /// <summary>
        /// The default <see cref="VideoEncoder"/>
        /// </summary>
        private VideoEncoder encoder = DefaultEncoder;

        /// <summary>
        /// The target framerate to record at.
        /// </summary>
        private int framerate = 30;

        /// <summary>
        /// The field that holds a <see cref="bool"/> representing
        /// whether or not the <see cref="RecordingHandler"/> is recording.
        /// </summary>
        private bool? isRecording = false;

        /// <summary>
        /// The listener that handles the <see cref="SubCMediaRecorder"/> events
        /// such as a file filling up or reaching the max duration, etc.
        /// </summary>
        private MediaRecorderListener listener = new MediaRecorderListener();

        /// <summary>
        /// Gets the value to clamp the bitrate at according to the resolution.  (bursting while recording at 100Mbps ended in recording errors)
        /// </summary>
        private int maxBitRate = 100;

        /// <summary>
        /// Gets the value to clamp the bitrate at according to the resolution.  (bursting while recording at 100Mbps ended in recording errors)
        /// </summary>
        private int minBitRate = 1;

        /// <summary>
        /// The default maximum <see cref="TimeSpan"/> a file can
        /// record for before being split into another file.
        /// </summary>
        //private TimeSpan maxFileDuration = TimeSpan.FromMinutes(DefaultFileDuration);
        private VideoFormat preset = VideoFormat.FHD30;

        private TimeSpan previousRecordingTime;

        /// <summary>
        /// The <see cref="RecordingTimer"/> that keeps track of the
        /// ecording duration.
        /// </summary>
        private RecordingTimer recordingTimer;

        /// <summary>
        /// A File observer
        /// </summary>
        private SubCFileObserver videoFileObserver;

        /// <summary>
        /// The field that holds a <see cref="string"/> representing the
        /// name to save the video file to.  This is set to the default if
        /// there is no name saved in the settings file.
        /// </summary>
        private string videoName = DefaultVideoName;

        /// <summary>
        /// The default <see cref="SubCSize"/> to record video at if
        /// one isn't specified in the settings.
        /// </summary>
        private Size videoResolution = new Size(DefaultResolutionWidth, DefaultResolutionHeight);

        /// <summary>
        /// The default <see cref="DirectoryInfo"/> to save the files to
        /// if no directory is saved in the settings.
        /// </summary>
        private DirectoryInfo videosDirectory = new DirectoryInfo(DefaultVideoDirectory);

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingHandler"/> class.
        /// </summary>
        /// <param name="captureSession">The <see cref="SubCCaptureSession"/></param>
        /// <param name="diskSpaceManager">The <see cref="DiskSpaceManager"/> used to keep track of remaining space on device.</param>
        /// <param name="settings">The <see cref="ISettingsService"/> to save/load settings from.</param>
        public RecordingHandler(
            SubCCaptureSession captureSession,
            DiskSpaceManager diskSpaceManager,
            ISettingsService settings,
            IMediaRecorder mediaRecorder)
            : base(settings)
        {
            MaxFileDuration = TimeSpan.FromMinutes(DefaultFileDuration);
            this.captureSession = captureSession;
            this.diskSpaceManager = diskSpaceManager;
            this.mediaRecorder = mediaRecorder;

            //diskMonitor = new Droid.DiskSpaceMonitor(diskSpaceManager);
            //diskMonitor.Warning += (s, e) => OnNotify($"File write warning. The file size isn't increasing: {currentFile}");
            //diskMonitor.Error += (s, e) => DiskMonitor_Error();
        }

        /// <summary>
        /// Triggers when video starts and stops recording
        /// </summary>
        public event EventHandler<bool?> IsRecordingChanged;

        /// <summary>
        /// Triggers when the <see cref="RecordingDuration"/> changes.
        /// </summary>
        public event EventHandler<TimeSpan> RecordingDurationChanged;

        /// <summary>
        /// Triggers when video recording starts.
        /// </summary>
        public event EventHandler<string> RecordingStarted;

        /// <summary>
        /// Triggers when a video has been recorded.
        /// </summary>
        public event EventHandler<RecordedFile> VideoRecorded;

        /// <summary>
        /// Gets the available video recording resolutions
        /// </summary>
        public static IEnumerable<Size> VideoResolutions { get; } = new Size[] { new Size(3840, 2160), new Size(2976, 2976), new Size(1920, 1080), new Size(1280, 720), new Size(720, 480) };

        /// <summary>
        /// Gets or sets the recording bitrate in Mbps
        /// </summary>
        [Savable]
        [RemoteState]
        public int BitRate
        {
            get => bitRate;
            set => Set(nameof(BitRate), ref bitRate, value.Clamp(minBitRate, maxBitRate));
        }

        public string Directory
        {
            get => VideosDirectory.FullName;
            set => VideosDirectory = new DirectoryInfo(value);
        }

        /// <summary>
        /// Gets or sets the <see cref="VideoEncoder"/> that determines the codec to encode the video with.
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToEncoder))]
        public VideoEncoder Encoder
        {
            get => encoder;
            set => Set(nameof(Encoder), ref encoder, UpdateEncoder(value));
        }

        /// <summary>
        /// Gets the available encoders for the Rayfin.
        /// </summary>
        public IEnumerable<string> Encoders { get; } = new string[] { "H264", "H265" };

        /// <summary>
        /// Gets or sets the target framerate to record video.
        /// </summary>
        [Savable]
        public int Framerate
        {
            get => framerate;
            set => Set(nameof(Framerate), ref framerate, value);
        }

        /// <summary>
        /// Gets a value indicating whether a 4K profile is selected
        /// </summary>
        public bool Is4K => VideoResolution.Width > 1920 || VideoResolution.Height > 1080;

        /// <summary>
        /// Gets whether the device is currently recording.
        /// If it returns null it means it is either stopping recording
        /// or starting but hasn't fully started or stopped.
        /// </summary>
        [RemoteState(true)]
        public bool? IsRecording
        {
            get => isRecording;
            private set
            {
                if (isRecording == value)
                {
                    return;
                }

                isRecording = value;
                IsRecordingChanged?.Invoke(this, value);
                OnNotify($"{nameof(IsRecording)}:{IsRecording?.ToString() ?? "null"}");
            }
        }

        /// <summary>
        /// Gets a value indicating whether 4K video is currently being recorded
        /// </summary>
        public bool IsRecording4K => (IsRecording ?? true) && Is4K;

        /// <summary>
        /// Gets a value indicating whether the media recorder is started recording.
        /// </summary>
        public bool IsStarted => IsRecording ?? false;

        /// <summary>
        /// Gets or sets the recording max file duration
        /// </summary>
        [Alias("MaxFileLength")]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public TimeSpan MaxFileDuration
        {
            get;
            private set;
        }

        [RemoteState]
        public int MaxFileSize { get; set; } = 10;

        [RemoteState]
        [Savable]
        [PropertyConverter(typeof(StringToVideoFormat))]
        public VideoFormat Preset
        {
            get => preset;
            private set
            {
                if (Set(nameof(Preset), ref preset, value))
                {
                    OnNotify($"{nameof(Preset)}:{Preset}");
                }
            }
        }

        /// <summary>
        /// Gets the current total recording time (duration)
        /// </summary>
        [RemoteState]
        public TimeSpan RecordingDuration => recordingTimer?.RecordingDuration ?? TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the name of the video that is being saved.
        /// </summary>
        [Savable]
        [RemoteState]
        public string VideoName
        {
            get => videoName;
            set
            {
                Set(nameof(VideoName), ref videoName, UpdateVideoName(value).Substring(0, Math.Min(value.Length, ValidFilename.MaxLength)));
            }
        }

        /// <summary>
        /// Gets the <see cref="SubCSize"/> of the video that is to be recorded.
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToVideoResolutionConverter))]
        public Size VideoResolution
        {
            get => videoResolution;
            private set
            {
                Set(nameof(VideoResolution), ref videoResolution, value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="DirectoryInfo"/> that contains the videos that are recorded.
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToDirectoryInfo))]
        public DirectoryInfo VideosDirectory
        {
            get => videosDirectory;
            set
            {
                Set(nameof(VideosDirectory), ref videosDirectory, value.RemoveIllegalPathCharacters(true).LimitDirectoryLength());
                DroidSystem.ShellSync($@"setprop rayfin.video.directory {value}");
            }
        }

        public async Task ForceStopRecording()
        {
            // WriteToLog("ForceStopRecording: started stoprecording");
            if (!IsRecording ?? true)
            {
                return;
            }

            IsRecording = null;

            // WriteToLog("ForceStopRecording: stopping diskmonitor/recordingTimer");

            //diskMonitor.Stop();
            recordingTimer.Reset();

            try
            {
                // WriteToLog("Trying to stop the SubCMediaRecorder");
                mediaRecorder.Stop();
                // WriteToLog("Stopped the recorder, resetting now");
                mediaRecorder.Reset();
            }
            catch (Java.Lang.RuntimeException e)
            {
                e.PrintStackTrace();
                OnNotify("Could not save video, No valid audio/video data", MessageTypes.Error);
                mediaRecorder.Reset();
                currentFile.Delete();
            }
            catch (TimeoutException)
            {
                // WriteToLog("TimeoutException caught, setting recorder to null");
                mediaRecorder.Init();
            }

            // WriteToLog("Recording stopped");

            // unstash the still surface if you were recording 4K
            // WriteToLog("ForceStopRecording: unstashing surfaces");
            if (Is4K)
            {
                captureSession.UnStashSurface(SurfaceTypes.Still);
            }

            // WriteToLog("ForceStopRecording: removing surfaces");
            await captureSession.RemoveSurface(SurfaceTypes.Recording);

            // WriteToLog("ForceStopRecording: repeating");
            captureSession.Repeat();

            IsRecording = false;
            // WriteToLog("ForceStopRecording: finished stoprecording");

            return;
        }

        /// <summary>
        /// Loads all defaults relating to video recording
        /// </summary>
        [Alias("LoadVideoDefaults", "VideoNameToDefault")]
        [RemoteCommand]
        public void LoadDefaults()
        {
            VideosDirectory = new DirectoryInfo(DefaultVideoDirectory);
            VideoName = DefaultVideoName;
            MaxFileDuration = TimeSpan.FromMinutes(DefaultFileDuration);
            Encoder = DefaultEncoder;
            VideoResolution = new Size(DefaultResolutionWidth, DefaultResolutionHeight);

            OnNotify(this, new NotifyEventArgs($"{nameof(VideoName)}:{VideoName}", MessageTypes.Information));
            OnNotify(this, new NotifyEventArgs($"{nameof(VideosDirectory)}:{VideosDirectory}", MessageTypes.Information));
            OnNotify(this, new NotifyEventArgs($"{nameof(MaxFileDuration)}:{MaxFileDuration}", MessageTypes.Information));
            OnNotify(this, new NotifyEventArgs($"{nameof(Encoder)}:{Encoder}", MessageTypes.Information));
            OnNotify(this, new NotifyEventArgs($"{nameof(VideoResolution)}:{VideoResolution}", MessageTypes.Information));
        }

        /// <summary>
        /// Loads the settings from the <see cref="ISettingsService"/>.
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();

            if (Settings.TryLoad(nameof(VideoResolution), out string tempVideoRes))
            {
                if (new StringToVideoResolutionConverter().TryConvert(tempVideoRes, out object res))
                {
                    VideoResolution = (Size)res;
                }
            }
        }

        /// <summary>
        /// The command that starts recording a video.
        /// </summary>
        public void StartRecording()
        {
            recordingTimer?.Reset();
            recordingTimer = new RecordingTimer(MaxFileDuration);
            recordingTimer.Split += (s, e) => Split();
            recordingTimer.Tick += RecordingTimer_Tick;
            StartRecording(VideoFile());
        }

        /// <summary>
        /// Stops recording video
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task StopRecording()
        {
            WriteToLog($"StopRecording: started stoprecording - IsRecording:{IsRecording}");
            if (!IsRecording ?? true)
            {
                return;
            }

            IsRecording = null;

            //diskMonitor.Stop();
            recordingTimer.Reset();

            try
            {
                WriteToLog("StopRecording: trying to stop MediaRecorder");
                mediaRecorder.Stop();
                WriteToLog("StopRecording: trying to Reset MediaRecorder");
                mediaRecorder.Reset();
            }
            catch (Java.Lang.RuntimeException e)
            {
                e.PrintStackTrace();
                OnNotify("Could not save video, No valid audio/video data", MessageTypes.Error);
                mediaRecorder.Reset();
                currentFile.Delete();
            }

            WriteToLog("Recording stopped");

            //unstash the still surface if you were recording 4K
            WriteToLog("StopRecording: unstashing surfaces");
            if (Is4K)
            {
                captureSession.UnStashSurface(SurfaceTypes.Still);
            }

            WriteToLog("StopRecording: removing surfaces");
            await captureSession.RemoveSurface(SurfaceTypes.Recording);

            WriteToLog("StopRecording: repeating");
            captureSession.Repeat();

            IsRecording = false;
            WriteToLog($"StopRecording: finished stoprecording - IsRecording:{IsRecording}");

            return;
        }

        //async void IDiveRecorder.StopRecording()
        //{
        //    await StopRecording();
        //}

        [RemoteCommand]
        [Alias(nameof(Preset))]
        [PropertyConverter(typeof(StringToVideoFormat))]
        public void UpdatePreset(VideoFormat videoFormat)
        {
            switch (videoFormat)
            {
                case VideoFormat.NTSC:
                case VideoFormat.PAL:
                    BitRate = 1;
                    VideoResolution = new Size(720, 480);
                    break;

                case VideoFormat.FHD30:
                    BitRate = 25;
                    VideoResolution = new Size(1920, 1080);
                    break;

                case VideoFormat.UHD30:
                    BitRate = 100;
                    VideoResolution = new Size(3840, 2160);
                    break;
            }

            Preset = videoFormat;
        }

        /// <summary>
        /// Updates the video resolution with the given value.
        /// </summary>
        /// <param name="value">The <see cref="SubCSize"/> of the video to be recorded.</param>
        [Alias("VideoResolution")]
        [RemoteCommand]
        [PropertyConverterAttribute(typeof(StringToVideoResolutionConverter))]
        public void UpdateVideoResolution(Size value)
        {
            if (IsRecording ?? true)
            {
                OnNotify("Please stop recording before setting the video resolution", MessageTypes.Error);
                return;
            }

            VideoResolution = value;
            maxBitRate = Is4K ? 100 : 25;
            minBitRate = Is4K ? 50 : 1;

            if (BitRate > maxBitRate)
            {
                BitRate = maxBitRate;
            }

            if (BitRate < minBitRate)
            {
                BitRate = minBitRate;
            }

            OnNotify($"{nameof(VideoResolution)}:{VideoResolution.Width}x{VideoResolution.Height}");
        }

        private void Instance_Info(MediaRecorder.InfoEventArgs e)
        {
            OnNotify($"Info with recording {currentFile}: {e.What.ToString()} {e.Extra}", MessageTypes.Information);
        }

        /// <summary>
        /// The event that logs any errors that may arise during recording.
        /// </summary>
        /// <param name="e">The <see cref="MediaRecorder.ErrorEventArgs"/></param>
        private async void Recorder_Error(MediaRecorder.ErrorEventArgs e)
        {
            SubCLogger.Instance.Write($"{DateTime.Now},Filename,{currentFile},SplitTime,{MaxFileDuration},Encoder,{Encoder},Resolution,{VideoResolution},Bitrate,{BitRate},What,{e.What},Extra,{e.Extra}", "RecordingErrors.csv", DroidSystem.LogDirectory);
            WriteToLog("Recorder_Error:See RecordingErrors.csv for details");
            await StopRecording();
            OnNotify($"Error with recording {currentFile}: {e.What} {e.Extra}", MessageTypes.Error);
            // WriteToLog($"Error with recording {currentFile}: {e.What} {e.Extra}");
        }

        /// <summary>
        /// Timer event that sends the recording duration up to RCS
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/></param>
        private void RecordingTimer_Tick(object sender, EventArgs e)
        {
            RecordingDurationChanged?.Invoke(this, RecordingDuration);
            OnNotify($"RecordingDuration:{RecordingDuration.ToString("hh\\:mm\\:ss")}");
        }

        /// <summary>
        /// Sets up the <see cref="SubCMediaRecorder"/> and prepares it for recording video.
        /// </summary>
        private void SetUpMediaRecorder()
        {
            SetUpMediaRecorder(VideoFile());
        }

        /// <summary>
        /// Sets up the <see cref="SubCMediaRecorder"/> and prepares it for recording video.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> to record the video to.</param>
        /// <param name="isSwitching">Whether or not this video is being created from a split.</param>
        private void SetUpMediaRecorder(FileInfo file, bool isSwitching = false)
        {
            // WriteToLog("Starting SetUpMediaRecorder");
            OnNotify($"RecordingDuration:{RecordingDuration.ToString("hh\\:mm\\:ss")}");

            if (SetUpSync.IsLocked())
            {
                return;
            }

            lock (SetUpSync)
            {
                var stopwatch = new Stopwatch();

                // var realFile = new FileInfo(file.FullName.Replace(DroidSystem.BaseDirectory, SwapFolder));
                // realFile.Directory.CreateIfMissing();
                mediaRecorder.Configure(VideoSource.Surface, file, Encoder, BitRate, VideoResolution, MaxFileSize);
                stopwatch.Start();
                mediaRecorder.Prepare();
                stopwatch.Stop();
                var prepareTime = stopwatch.Elapsed;

                mediaRecorder.Info += (_, e) => Instance_Info(e);
                mediaRecorder.Error += (_, e) => Recorder_Error(e);

                try
                {
                    var message = $"{DateTime.Now},PrepareTime,{prepareTime},Filename,{file.FullName},SplitTime,{MaxFileDuration}" +
                       $",Encoder,{Encoder},Resolution,{VideoResolution},IsSwitching,{isSwitching},Bitrate,{BitRate}";
                    SubCLogger.Instance.Write(message, "Prepare.csv", DroidSystem.LogDirectory);
                }
                catch
                {
                }

                OnNotify("Prepare time: " + prepareTime);
            }
        }

        /// <summary>
        /// Sets up the <see cref="SubCMediaRecorder"/> on a background thread.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> to record the video to.</param>
        /// <returns>A <see cref="Task"/></returns>
        private async Task SetUpMediaRecorderAsync(FileInfo file) => await Task.Run(() => SetUpMediaRecorder(file));

        /// <summary>
        /// Stops the current recording and starts a new one to minimize
        /// the amount of lost footage in the event of a system failure.
        /// </summary>
        private async void Split()
        {
            WriteToLog("Starting Split");
            // WriteToLog("Split");

            if (diskSpaceManager.IsDiskSpaceLow)
            {
                OnNotify("Cannot record video\nNo space remaining on device", MessageTypes.Error);
                StopRecording();
                return;
            }

            // stop monitoring the disk, there are no files growing
            // diskMonitor.Stop();

            IsRecording = null;

            OnNotify("Splitting file: " + currentFile);

            // stop recording and wait for the file to save before moving on
            captureSession.StopRepeating();
            // WriteToLog("Stopping MediaRecorder");
            mediaRecorder.Stop();
            // WriteToLog("Recorder stopped, awaiting the file observer now");

            try
            {
                await videoFileObserver.Wait();
            }
            catch (Exception e)
            {
                // WriteToLog(e.ToString());
            }

            //// WriteToLog("File observer complete, invoking VideoRecorded");
            //VideoRecorded?.Invoke(this, currentFile.FullName);

            // start recording
            // WriteToLog("Finishing Split, calling StartRecording");
            StartRecording(VideoFile(), true);
        }

        /// <summary>
        /// Starts recording video and sets save path to the pram filePath
        /// </summary>
        /// <param name="file">The file to save the video to.</param>
        /// <param name="isSwitching">A <see cref="bool"/> representing whether or not the file is being created from a split or not.</param>
        private async void StartRecording(FileInfo file, bool isSwitching = false)
        {
            // WriteToLog("Starting Recording");
            lock (RecordingSync)
            {
                if ((IsRecording ?? true) && !isSwitching)
                {
                    return;
                }

                IsRecording = null;
            }

            if (file == null)
            {
                IsRecording = false;
                return;
            }

            DroidSystem.SetProp("rayfin.preparing", "true");
            await Task.Delay(500);

            file.Directory.CreateIfMissing();

            try
            {
                SetUpMediaRecorder(file, isSwitching);
            }
            catch (Java.Lang.IllegalStateException e)
            {
                // WriteToLog(e.Message);
                OnNotify("There was an error with recording, please try again, restart, or contact support@subcimaging.com\n" + e.Message, MessageTypes.Error);
                mediaRecorder.Reset();
                return;
            }

            // WriteToLog("StartRecording:Stashing surfaces");

            // stash the still surface if you're recording 4K
            if (Is4K && !isSwitching)
            {
                captureSession.StashSurface(SurfaceTypes.Still);
            }

            // WriteToLog("StartRecording: instantiate builder");
            var builder = captureSession.CreateCaptureBuilder(SubCCameraTemplate.Record);

            // WriteToLog("StartRecording: adding builder targets");

            try
            {
                // WriteToLog($"Recorder Surface Is Null = {recorder.Surface == null}");
                builder.AddTarget(new SubCSurface(mediaRecorder.Surface));
            }
            catch (Java.Lang.IllegalStateException e)
            {
                e.PrintStackTrace();
                WriteToLog($"Dying... \n{e.Message}\nWill attempt to restart media encoder");
                await StopRecording();
                StartRecording();
                return;
            }

            if (!await captureSession.UpdateSurface(SurfaceTypes.Recording, new SubCSurface(mediaRecorder.Surface)))
            {
                // WriteToLog("StartRecording: starting CaptureSession.repeat");
                captureSession.Repeat();
                // WriteToLog("StartRecording: finish captureSession.Repeat");
                OnNotify("Recording failed to start", Messaging.Models.MessageTypes.Error);
                IsRecording = false;
                return;
            }

            // WriteToLog("StartRecording: started captureSession.repeat(builder)");
            captureSession.Repeat(builder);
            // WriteToLog("StartRecording: finished captureSession.repeat(builder)");
            try
            {
                // WriteToLog("StartRecording: trying recorder.start");
                mediaRecorder.Start();
            }
            catch (Java.Lang.IllegalStateException e)
            {
                e.PrintStackTrace();
                // WriteToLog("Prepare has not been called?");
            }

            recordingTimer.Start();

            RecordingStarted?.Invoke(this, file.FullName);
            IsRecording = true;
            // WriteToLog("Recording started");

            // diskMonitor.Start();
            currentFile = file;
            videoFileObserver = new SubCFileObserver(mediaRecorder.File.FullName);
            videoFileObserver.FileClosed += VideoSaved;

            if (DroidSystem.StorageType == RayfinStorageType.NAS)
            {
                DroidSystem.UserChmod(777, file.FullName);
            }

            await Task.Delay(500);
            DroidSystem.SetProp("rayfin.preparing", "false");
            // WriteToLog("StartRecording: finished");
        }

        /// <summary>
        /// Updates the video encoder/codec.
        /// </summary>
        /// <param name="value">The <see cref="VideoEncoder"/> to be set.</param>
        /// <returns>The <see cref="VideoEncoder"/> passed in if it is valid, otherwise returns the current <see cref="Encoder"/></returns>
        private VideoEncoder UpdateEncoder(VideoEncoder value)
        {
            if (value == VideoEncoder.H264 || value == VideoEncoder.Hevc)
            {
                return value;
            }

            OnNotify($"VideoEncoder {value} not supported please select Hevc or H264");
            return Encoder;
        }

        /// <summary>
        /// Strips file name of illegal characters.
        /// </summary>
        /// <param name="value">The filename to clean.</param>
        /// <returns>The clean filename</returns>
        private string UpdateVideoName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DefaultVideoName;
            }

            value = value.RemoveIllegalFileOrFolderCharacters(FileSystem.Any, true);

            // OnNotify(ValidFilename.UpdateFileName(ref value));
            return value;
        }

        /// <summary>
        /// The method that generates the <see cref="FileInfo"/> to record to.
        /// </summary>
        /// <returns>The <see cref="FileInfo"/> to record to.</returns>
        private FileInfo VideoFile()
        {
            var dir = new DirectoryInfo(System.IO.Path.Combine(DroidSystem.BaseDirectory, VideosDirectory.FullName.TrimStart('/')));

            if (dir.Exists)
            {
                DroidSystem.ShellSync($"chmod 777 {dir.FullName}");
            }

            var videoFileName = $"{VideoName}.mp4";

            var videoPath = new Java.IO.File(System.IO.Path.Combine(dir.FullName, videoFileName));

            return new FileInfo(videoPath.AbsolutePath).ParseFileAddSeqNum();
        }

        /// <summary>
        /// The event that executes when the video is saved.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The message</param>
        private void VideoSaved(object sender, string e)
        {
            (sender as SubCFileObserver).FileClosed -= VideoSaved;

            Task.Run(async () =>
            {
                WriteToLog("VideoSaved");

                await StopRecording();
                WriteToLog("Notify VideoRecorded");

                // get the length of the video
                //var retriever = new MediaMetadataRetriever();
                //retriever.SetDataSource(e, new Dictionary<string, string>());
                //var length = retriever.ExtractMetadata(MetadataKey.Duration);
                //var lengthseconds = Convert.ToInt32(length) / 1000;
                //var t = TimeSpan.FromSeconds(lengthseconds);

                VideoRecorded?.Invoke(this, new RecordedFile(e, previousRecordingTime));
            });
        }

        /// <summary>
        /// Appends data to the log at /storage/emulated/0/Logs/debug.txt
        /// Prepends the Date,
        /// </summary>
        /// <param name="data">The <see cref="string"/> to append to the log.</param>
        private void WriteToLog(string data)
        {
            try
            {
                SubCLogger.Instance.Write($"{DateTime.Now},{data}", "debug.txt", DroidSystem.LogDirectory);
            }
            catch
            {
            }
        }
    }
}
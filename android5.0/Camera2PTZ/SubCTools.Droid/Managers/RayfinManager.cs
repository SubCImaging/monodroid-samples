namespace SubCTools.Droid.Managers
{
    using Android.App;
    using Android.Content;
    using Android.Hardware.Camera2;
    using Android.Hardware.Usb;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using EmbedIO;
    using EmbedIO.WebApi;
    using SubCTools.Communicators;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.DiveLog;
    using SubCTools.Droid.Attributes;
    using SubCTools.Droid.Camera;
    using SubCTools.Droid.Communicators;
    using SubCTools.Droid.Controllers;
    using SubCTools.Droid.Extensions;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.IO;
    using SubCTools.Droid.Listeners;
    using SubCTools.Enums;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Class responsible for setting up all the rayfin camera functions and storage
    /// </summary>
    public class RayfinManager : INotifier
    {
        /// <summary>
        /// Monitor disk space so you know if you can record or take pictures
        /// </summary>
        private readonly DiskSpaceManager diskSpaceMonitor;

        private bool isDataLogFeature;

        /// <summary>
        /// Nas to configure and save data
        /// </summary>
        private Nas nas;

        /// <summary>
        /// Rayfin camera object
        /// </summary>
        private Rayfin rayfin;

        /// <summary>
        /// Base camera manager that sets up all the base functionality
        /// </summary>
        private GrenadierManager subCCameraManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RayfinManager"/> class.
        /// </summary>
        public RayfinManager(
            GrenadierManager subCCameraManager,
            DiskSpaceManager diskSpaceMonitor,
            AuxInputManager auxManager)
        {
            this.subCCameraManager = subCCameraManager;
            this.diskSpaceMonitor = diskSpaceMonitor;

            // made the rayfin camera object with the base camera managers camera
            rayfin = new Rayfin(
                subCCameraManager.Settings,
                subCCameraManager.PowerController,
                diskSpaceMonitor,
                subCCameraManager.Camera,
                auxManager);

            rayfin.ResolutionChanged += (s, e) => ResolutionChanged?.Invoke(this, new Size(e.Width, e.Height));

            // fire off a chain of methods when the camera is finished booting up
            rayfin.Initialized += (s, e) => Rayfin_Initialized();

            nas = new Nas(subCCameraManager.Settings);
            nas.ConnectionChanged += (s, b) => Nas_Connection_Changed(b);

            DiveLogFeatureToggle = subCCameraManager.Features.IsFeatureOn("DiveLog");
            isDataLogFeature = subCCameraManager.Features.IsFeatureOn("DataLog");
        }

        /// <summary>
        /// Notification to the MessageRouter
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Event to fire when the recording resolution changes
        /// </summary>
        public event EventHandler<Size> ResolutionChanged;

        public bool DiveLogFeatureToggle
        {
            get;
            private set;
        }

        /// <summary>
        /// Event that fires when connection to nas changes.  It checks wheather the value of IsDiskSpaceLow also changes as a result.
        /// </summary>
        /// <param name="status"></param>
        private void Nas_Connection_Changed(bool status)
        {
            if (!status && (rayfin?.RecordingHandler?.IsRecording ?? false) != false)
            {
                _ = rayfin.RecordingHandler.ForceStopRecording();
            }

            OnNotify($"{nameof(diskSpaceMonitor.IsDiskSpaceLow)}:{diskSpaceMonitor.IsDiskSpaceLow}");
        }

        private void OnNotify(string message, MessageTypes messageType = MessageTypes.Information)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));
        }

        /// <summary>
        /// Initialization code to run after the camera is finished booting up
        /// </summary>
        private void Rayfin_Initialized()
        {
            // Server must be started, before WebView is initialized,
            // because we have no reload implemented in this sample.
            Task.Factory.StartNew(async () =>
            {
                using var server = new WebServer(HttpListenerMode.EmbedIO, "http://*:8080");

                server.WithLocalSessionManager()
                        .WithCors()
                        .WithWebApi("/api/recording", m => m.WithController(() => new RecordingController(new Services.RecordingService())))
                        .WithWebApi("/api/stills", m => m.WithController(() => new StillController(new Services.StillService())));

                await server.RunAsync();
            });

            MessageIOC.Instance.Add(MessageTypes.Debug, rayfin);

            // reboot the camera when the static ip changes
            DroidSystem.Instance.StaticIPChanged += (s, ee) => subCCameraManager.PowerController.Reboot(1);

            var script = new ScriptBuilder(
                subCCameraManager.Interpreter,
                subCCameraManager.Settings,
                DroidSystem.ScriptDirectory,
                null);

            MessageIOC.Instance.Add(MessageTypes.Information, script);

            subCCameraManager.Interpreter.Register(new object[]
            {
                rayfin,
                rayfin.StillHandler,
                rayfin.RecordingHandler,
                diskSpaceMonitor,
                nas,
                script
            });

            MessageRouter.Instance.Add(rayfin);

            MessageRouter.Instance.Add(
                nas,
                diskSpaceMonitor,
                subCCameraManager.PowerController,
                script);

            OnNotify("All components registered");

            OnNotify("Loading settings");

            subCCameraManager.PowerController.LoadSettings();
            script.LoadSettings();

            rayfin.RecordingHandler.IsRecordingChanged += (s, e) => RecordingHandler_IsRecordingChanged(e);

            if (isDataLogFeature)
            {
                // Logs NMEA data that is received over Serial, UDP or TCP.
                var dataLoggerComms = new ICommunicator[] { new SubCUDP(8889), new SubCTCPServer(8889), subCCameraManager.SerialManager.Serial };

                var dataLogger = new DataLogger(
                    SubCLogger.Instance,
                    new SubCUDP(8886),
                    DroidSystem.DataDirectory,
                    subCCameraManager.Settings,
                    dataLoggerComms);

                dataLogger.LoadSettings();

                MessageRouter.Instance.Add(dataLogger);
                subCCameraManager.Interpreter.Register(dataLogger);
            }

            if (DiveLogFeatureToggle)
            {
                //rayfin.StillHandler,
                //var dive = new Inspection(rayfin.RecordingHandler, dataLogger);
                //dive.LoadSettings();
                //dive.(
                //    new System.IO.FileInfo(System.IO.Path.Combine(DroidSystem.DataDirectory.FullName, DateTime.Now.ToString("yyyy-MM-dd") + ".si")));
            }
        }

        /// <summary>
        /// IsRecording changed, alert what the new preview resolution should be
        /// </summary>
        /// <param name="e">True if is recording, false otherwise</param>
        private void RecordingHandler_IsRecordingChanged(bool? e)
        {
            // Debug code
            SubCLogger.Instance.Write($"========IsRecording:{e}========", "StackTrace.log", DroidSystem.LogDirectory);
            SubCLogger.Instance.Write($"{DateTime.Now}", "StackTrace.log", DroidSystem.LogDirectory);
            var stackTrace = new StackTrace();
            var stackFrames = stackTrace.GetFrames();

            // write call stack method names
            foreach (var stackFrame in stackFrames)
            {
                SubCLogger.Instance.Write(stackFrame.GetMethod().Name, "StackTrace.log", DroidSystem.LogDirectory);
            }

            SubCLogger.Instance.Write("=================================\n\n", "StackTrace.log", DroidSystem.LogDirectory);

            // return if you're transitioning
            if (e == null)
            {
                return;
            }

            if (!rayfin.IsAspectRatioLocked)
            {
                // if you're recording, use the video resolution, otherwise use the still resolution
                var size = e ?? false ? rayfin.RecordingHandler.VideoResolution : rayfin.StillHandler.JPEGResolution;
                ResolutionChanged?.Invoke(this, new Size(size.Width, size.Height));
            }
        }
    }
}
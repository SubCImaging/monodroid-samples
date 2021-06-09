//-----------------------------------------------------------------------
// <copyright file="SubCCamera.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Camera
{
    using Android.Content;
    using Android.Graphics;
    using Android.Hardware.Camera2;
    using Android.Hardware.Camera2.Params;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using SubCTools.DiveLog.Interfaces;
    using SubCTools.Droid.Attributes;
    using SubCTools.Droid.Callbacks;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.EventArguments;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.Lenses;
    using SubCTools.Droid.Listeners;
    using SubCTools.Droid.Tools;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class Grenadier : DroidBase, INotifier
    {
        /// <summary>
        /// Android camera ID, 0 is back, 1 is front
        /// </summary>
        private const string CameraId = "0";

        /// <summary>
        /// Android camera manager to grab the camera device
        /// </summary>
        private readonly CameraManager cameraManager;

        private string nickname;

        /// <summary>
        /// Variable to store if the camera was in auto focus before switching to manual exposure
        /// </summary>
        private bool wasAutoFocus;

        /// <summary>
        /// Initializes a new instance of the <see cref="Grenadier"/> class.
        /// </summary>
        /// <param name="cameraManager">Manager used to retrieve camera device</param>
        /// <param name="preview">Preview screen to display video</param>
        /// <param name="settings">Settings service to save and load all info</param>
        public Grenadier(
            CameraManager cameraManager,
            TextureView preview,
            ISettingsService settings)
            : base(settings)
        {
            this.cameraManager = cameraManager;
            Preview = preview;
            var listener = new SurfaceTextureListener();
            preview.SurfaceTextureListener = listener;

            // open the camera if the preview is available, listen to when it come available and open otherwise
            if (preview.IsAvailable)
            {
                OpenCamera();
            }
            else
            {
                listener.SurfaceTextureAvailable += (s, e) =>
                {
                    OpenCamera();
                };
            }

            listener.SurfaceTextureChanged += Listener_SurfaceTextureChanged;
            listener.SurfaceTextureChanged += Check_Green_Preview;
        }

        /// <summary>
        /// Event to fire once the camera is initialized
        /// </summary>
        public event EventHandler Initialized;

        /// <summary>
        /// Gets the value of the configured camera device
        /// </summary>
        public CameraDevice Camera { get; private set; }

        /// <summary>
        /// Gets all characteristics of the camera system
        /// </summary>
        public CameraCharacteristics Characteristics { get; private set; }

        /// <summary>
        /// Gets get the exposure settings
        /// </summary>
        public ExposureSettings ExposureSettings { get; private set; }

        /// <summary>
        /// Gets a value indicating whether true if the camera is finished it's initialization process, false otherwise
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets get the camera lens
        /// </summary>
        public StockLens Lens { get; private set; }

        /// <summary>
        /// Sets a nickname for the camera
        /// </summary>
        [Savable]
        [RemoteState]
        public string Nickname
        {
            get => nickname;
            set => Set(nameof(Nickname), ref nickname, value);
        }

        /// <summary>
        /// Gets texture view that the preview will be applied to
        /// </summary>
        public TextureView Preview { get; }

        /// <summary>
        /// Gets capture session for taking images, video, and running preview
        /// </summary>
        public SubCCaptureSession Session { get; private set; }

        /// <summary>
        /// Gets get the still handler
        /// </summary>
        //public StillHandler StillHandler { get; private set; }

        /// <summary>
        /// Gets stream map for get resolutions for stills
        /// </summary>
        public StreamConfigurationMap StreamMap { get; private set; }

        /// <summary>
        /// Load all the settings for this class and all composed objects
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();
            ExposureSettings.LoadSettings();
            Lens.LoadSettings();
            //StillHandler.LoadSettings();
        }

        /// <summary>
        /// Checks to see if the preview is green and unsubscribes to avoid checking again.
        /// </summary>
        /// <param name="sender">The <see cref="SurfaceTextureListener"/></param>
        /// <param name="e">The <see cref="SurfaceTextureArgs"/></param>
        private void Check_Green_Preview(object sender, SurfaceTextureArgs e)
        {
            if (GreenPictureDetector.BitmapIsGreen(Preview.GetBitmap(640, 480)))
            {
                DroidSystem.ShellSync("reboot");
            }

            Android.Util.Log.Debug("GREEN_PREVIEW", "Checking...");
            (sender as SurfaceTextureListener).SurfaceTextureChanged -= Check_Green_Preview;
        }

        /// <summary>
        /// Switch back to auto focus if you were in auto focus when you switched to auto exposure
        /// </summary>
        /// <param name="isAutoExposure">Bool whether auto exposure changed to auto</param>
        private void ExposureSettings_IsAutoExposureChanged(bool isAutoExposure)
        {
            // switch to manual focus when you switch to manual exposure
            if (!isAutoExposure)
            {
                if (!Lens.IsManualFocus)
                {
                    wasAutoFocus = true;
                    Lens.EnableManualFocus();
                }
                else
                {
                    wasAutoFocus = false;
                }
            }
            else
            {
                if (wasAutoFocus)
                {
                    Lens.EnableAutoFocus();
                }
            }
        }

        /// <summary>
        /// Change the buffer size when surface texture changes
        /// </summary>
        /// <param name="sender">Who sent</param>
        /// <param name="e">Parameter sent</param>
        // TODO: Managers responsability
        private async void Listener_SurfaceTextureChanged(object sender, SurfaceTextureArgs e)
        {
            var texture = e.Texture;
            await Session.UpdatePersistentSurface(SurfaceTypes.Preview, new SubCSurface(new Surface(texture)));
            Session.Repeat();
        }

        /// <summary>
        /// Open the camera once the texture view is ready
        /// </summary>
        private async void OpenCamera()
        {
            try
            {
                // get the camera from the camera manager with the given ID
                Camera = await Helpers.Camera.OpenCameraAsync(CameraId, cameraManager);
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }

            Characteristics = cameraManager.GetCameraCharacteristics(CameraId);

            // make sure you can retrieve the stream map, likely means Width Must Be Positive error if you can't
            try
            {
                StreamMap = (StreamConfigurationMap)Characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            }
            catch (Java.Lang.IllegalArgumentException ex)
            {
                ex.PrintStackTrace();
                SubCLogger.Instance.Write($"{DateTime.Now}:  Error opening camera, restarting", "DriverCrash.log", DroidSystem.LogDirectory);
                SubCLogger.Instance.Write($"{DateTime.Now}:  {ex.Message}", "DriverException.log", DroidSystem.LogDirectory);
                DroidSystem.ShellSync("reboot");
                return;
            }

            var sessionCallbackThread = new SubCHandlerThread(new HandlerThread("SessionCallbackThread"));
            sessionCallbackThread.Start();

            var handler = new SubCHandler(new Handler(sessionCallbackThread.HandlerThread.Looper));

            // create the camera session to start the preview
            // TODO: Probably be in the manager
            Session = new SubCCaptureSession(new SubCCameraDevice(Camera), handler);

            ExposureSettings = new ExposureSettings(
                Characteristics,
                Settings,
                Session);

            ExposureSettings.IsAutoExposureChanged += (s, e) => ExposureSettings_IsAutoExposureChanged(e);

            // stock lens needs the sesnsor array for pan tilt zoom
            var sensorArray = (Rect)Characteristics.Get(CameraCharacteristics.SensorInfoActiveArraySize);

            var outputSizes = StreamMap.GetOutputSizes(Java.Lang.Class.FromType(typeof(SurfaceTexture)));
            var previewSize = outputSizes.First();

            //var fps = StreamMap.GetHighSpeedVideoFpsRangesFor(new Android.Util.Size(1920, 1080));

            ////)
            //var framerates = (Java.Lang.Object[])Characteristics.Get(CameraCharacteristics.ControlAeAvailableTargetFpsRanges);

            //foreach (var item in framerates)
            //{
            //    var f = (Android.Util.Range)item;
            //    Console.WriteLine(f);
            //}

            Lens = new StockLens(new Size(previewSize.Width, previewSize.Height), Session, Settings, ExposureSettings)
            {
                SensorMaxSize = new Size(sensorArray.Right - sensorArray.Left, sensorArray.Bottom - sensorArray.Top)
            };

            //// We need this here so the preview is the full 21MP
            //StillHandler = new StillHandler(
            //    new[] { new Size(5344, 4008), new Size(4608, 3456), new Size(4000, 3000) },
            //    new[] { streamMap.GetOutputSizes((int)ImageFormatType.RawSensor).First() },
            //    Settings,
            //    Session,
            //    Characteristics);

            //StillHandler.Notify += OnNotify;
            //StillHandler.JPEGResolutionChanged += (s, e) => Lens.UpdatePreviewResolution(e);

            // TODO: Move to manager to call when preview is ready
            // update the camera sessions persistent surface with the preview surface, this must always be present for captures
            await Session.UpdatePersistentSurface(SurfaceTypes.Preview, new SubCSurface(new Surface(Preview.SurfaceTexture)));

            // load all the settings for the composed objects
            LoadSettings();

            // start the preview
            Session.Repeat();

            // change the preview resolution
            //ResolutionChanged?.Invoke(this, new Size(StillHandler.JPEGResolution.Width, StillHandler.JPEGResolution.Height));

            Initialized?.Invoke(this, EventArgs.Empty);
            IsInitialized = true;
        }
    }
}
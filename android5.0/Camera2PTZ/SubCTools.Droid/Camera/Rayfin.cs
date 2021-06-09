//-----------------------------------------------------------------------
// <copyright file="Rayfin.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Camera
{
    using Android.Graphics;
    using Android.Media;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.DataTypes;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.IO;
    using SubCTools.Droid.IO.AuxDevices;
    using SubCTools.Droid.Lenses;
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A class that represents the Rayfin camera.
    /// </summary>
    public class Rayfin : DroidBase, INotifiable
    {
        /// <summary>
        /// Minimum number of seconds remaining before taking action during a power failure
        /// </summary>
        private const int PowerFailureThreshold = 5;

        /// <summary>
        /// Manager for aux ports, need to get LEDs
        /// </summary>
        private readonly AuxInputManager auxManager;

        /// <summary>
        /// Alerts when disk space is low
        /// </summary>
        private readonly DiskSpaceManager diskSpaceManager;

        /// <summary>
        /// Base subccamera object that performs al basic functionality
        /// </summary>
        private readonly Grenadier subCCamera;

        /// <summary>
        /// Variable for storing whether a picture can be taken
        /// </summary>
        private bool canTakePicture = true;

        private bool isAspectRatioLocked = false;

        /// <summary>
        /// Monitor power disconnects
        /// </summary>
        private PowerController powerController;

        /// <summary>
        /// Saves the lamp brightness while the lamp is turned off during strobe.
        /// </summary>
        private int previousLampBrightness;

        /// <summary>
        /// The <see cref="TimeoutDictionary{TKey, TValue}"/> that holds the exif
        /// strobe data for each image, set to flush each key 1 minute after it is
        /// entered into the <see cref="TimeoutDictionary{TKey, TValue}"/>.
        /// </summary>
        private TimeoutDictionary<string, string> strobeExifData =
            new TimeoutDictionary<string, string>(TimeSpan.FromMinutes(1));

        /// <summary>
        /// Initializes a new instance of the <see cref="Rayfin"/> class.
        /// </summary>
        /// <param name="settings">Settings serve to save/load</param>
        /// <param name="powerController">Controller to let the Rayfin know when to stop recording on power loss</param>
        /// <param name="diskSpaceManager">Is there enough space to save media</param>
        /// <param name="subCCamera">Camera object</param>
        /// <param name="auxManager">Aux manager to get LEDs</param>
        public Rayfin(
            ISettingsService settings,
            PowerController powerController,
            DiskSpaceManager diskSpaceManager,
            Grenadier subCCamera,
            AuxInputManager auxManager)
            : base(settings)
        {
            this.powerController = powerController;
            this.diskSpaceManager = diskSpaceManager;
            this.subCCamera = subCCamera;
            this.auxManager = auxManager;

            if (diskSpaceManager != null)
            {
                diskSpaceManager.LowDiskSpace += (s, e) => DiskSpaceManager_LowDiskSpace();
            }

            powerController.ShutdownTimer.Elapsed += (s, e) => PowerController_ShutDownTick();

            if (subCCamera.IsInitialized)
            {
                SubCCamera_Initialized();
            }
            else
            {
                subCCamera.Initialized += (s, e) => SubCCamera_Initialized();
            }
        }

        /// <summary>
        /// Event to fire when the camera is completed it's start up routine
        /// </summary>
        public event EventHandler Initialized;

        /// <summary>
        /// Event to fire when the resolution changes
        /// </summary>
        public event EventHandler<Size> ResolutionChanged;

        /// <summary>
        /// Gets a value indicating whether gets the value on whether on not a picture can be taken
        /// </summary>
        [RemoteState(true)]
        public bool CanTakePicture
        {
            get => canTakePicture;
            private set
            {
                if (canTakePicture != value)
                {
                    canTakePicture = value;
                    OnNotify($"{nameof(CanTakePicture)}:{value}");
                }
            }
        }

        /// <summary>
        /// Gets exposure class for handling all exposure related actions
        /// </summary>
        public ExposureSettings ExposureSettings => subCCamera.ExposureSettings;

        /// <summary>
        /// Gets or sets a value indicating whether the aspect ratio is locked.
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool IsAspectRatioLocked
        {
            get => isAspectRatioLocked;
            set
            {
                Set(nameof(IsAspectRatioLocked), ref isAspectRatioLocked, value);
            }
        }

        /// <summary>
        /// Gets true if recording, false otherwise
        /// </summary>
        public bool? IsStarted => RecordingHandler?.IsRecording ?? false;

        /// <summary>
        /// Gets conected lens system, control zoom and focus
        /// </summary>
        public StockLens Lens => subCCamera.Lens;

        /// <summary>
        /// Gets get the recording handler
        /// </summary>
        public RecordingHandler RecordingHandler { get; private set; }

        /// <summary>
        /// Gets the still handler
        /// </summary>
        public StillHandler StillHandler { get; private set; }

        /// <summary>
        /// Put the camera in auto exposure mode
        /// </summary>
        [RemoteCommand(ovrride: true)]
        [Alias("AutoExpose")]
        public void AutoExposure()
        {
            if (!StillHandler.IsBursting ?? true)
            {
                subCCamera.ExposureSettings.AutoExposure();
            }
            else
            {
                OnNotify($"{nameof(subCCamera.ExposureSettings.IsAutoExposure)}:{subCCamera.ExposureSettings.IsAutoExposure}");
            }
        }

        /// <summary>
        /// Go through all the aux devices, check to see if the strobe is enabled
        /// </summary>
        /// <returns>True if strobe is on or recharging</returns>
        [RemoteState]
        public bool IsStrobeOn() => auxManager.AuxDevices.Values.Any(a => a is AquoreaMk3 aq && aq.IsStrobeEnabled); // && (aq.StrobeState == StrobeStates.Recharge || aq.StrobeState == StrobeStates.On));

        /// <summary>
        /// Load the settings of all the components
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();
            StillHandler.LoadSettings();
            RecordingHandler.LoadSettings();

            if (IsAspectRatioLocked)
            {
                LockAspectRatio();
            }
        }

        /// <summary>
        /// Lock the aspect ratio to 16:9.
        /// </summary>
        [RemoteCommand]
        public void LockAspectRatio()
        {
            IsAspectRatioLocked = true;
            OnNotify($"{nameof(IsAspectRatioLocked)}:{IsAspectRatioLocked}");
            ResolutionChanged?.Invoke(this, new Size(16, 9));
        }

        /// <summary>
        /// Receive debug information from other classes and rebroadcast as info if debug is enabled
        /// </summary>
        /// <param name="sender">Who sent the notification</param>
        /// <param name="e">Message sent</param>
        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            // if you're debugging, rebroadcast the messages as information
            if (DroidSystem.Instance.IsDebugging && e.MessageType == MessageTypes.Debug)
            {
                OnNotify(e.Message, MessageTypes.Information);
            }
        }

        /// <summary>
        /// Start continuously shooting stills
        /// </summary>
        [RemoteCommand]
        [Alias("StartBurst", "Burst", "Continuous")]
        [CancelWhen(nameof(Droid.Camera.RecordingHandler.IsRecording4K), true, "Cannot take continuous still while recording 4K")]
        public void StartContinuous()
        {
            if (!CanCaptureStill())
            {
                return;
            }

            subCCamera.ExposureSettings.ManualExposure();
            Lens.EnableManualFocus();

            UpdateCanTakePicture(false);

            // turn the lamp off when starting continuous photos. See RAYF-175.
            if (auxManager.AuxDevices.Values.FirstOrDefault(a => a as AquoreaMk3 != null) is AquoreaMk3 aquorea)
            {
                previousLampBrightness = aquorea.LampBrightness;
                aquorea.LampOff();
            }

            StillHandler.StartContinuous();
        }

        /// <summary>
        /// Starts recording a video
        /// </summary>
        [RemoteCommand]
        [Alias("RecordVideo")]
        [CancelWhen(nameof(Droid.Camera.RecordingHandler.IsRecording), ComparisonOperators.NotEqualTo, false, "Cannot start recording until recording is stopped")]
        [CancelWhen(nameof(Droid.Camera.RecordingHandler.Is4K), ComparisonOperators.EqualTo, true, nameof(Droid.Camera.StillHandler.IsBursting), ComparisonOperators.EqualTo, true, "Cannot record 4K while taking continuous stills")]
        public void StartRecording()
        {
            if (RecordingHandler.IsRecording ?? true)
            {
                return;
            }

            if (!(diskSpaceManager?.IsDiskSpaceLow ?? false))
            {
                if (!subCCamera.ExposureSettings.IsAutoExposure && subCCamera.ExposureSettings.ShutterSpeed > new Fraction(1, 30).ToNanoseconds())
                {
                    OnNotify("Using shutter speeds slower than 1/30th of a second can cause issues with the number of frames recorded", MessageTypes.Error);
                }

                RecordingHandler.StartRecording();
            }
            else
            {
                OnNotify("Disk space is too low to record, please free up some space", MessageTypes.Error);
                OnNotify($"{nameof(RecordingHandler.IsRecording)}:{RecordingHandler.IsRecording}");
            }
        }

        /// <summary>
        /// Stop shooting continuous stills
        /// </summary>
        [RemoteCommand]
        [Alias("StopBurst")]
        public void StopContinuous()
        {
            // set the lamp back to the previous brightness before starting continuous
            if (auxManager.AuxDevices.Values.FirstOrDefault(a => a as AquoreaMk3 != null) is AquoreaMk3 aquorea)
            {
                aquorea.SetLampBrightness(previousLampBrightness);
            }

            StillHandler.StopContinuous();
        }

        /// <summary>
        /// Stops recording a video
        /// </summary>
        [RemoteCommand]
        [CancelWhen(nameof(Droid.Camera.RecordingHandler.IsRecording), ComparisonOperators.NotEqualTo, "True", "Cannot stop recording until recording is started")]
        public async void StopRecording()
        {
            if (RecordingHandler.IsRecording ?? false)
            {
                await RecordingHandler.StopRecording();
                UpdateCanTakePicture(true);
            }
        }

        /// <summary>
        /// Take a picture with the camera object
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
        [RemoteCommand]
        [Alias("TakeStill", "Take Still", "Take Picture")]
        public async Task TakePicture()
        {
            if (!CanTakePicture)
            {
                return;
            }

            if (!CanCaptureStill())
            {
                return;
            }

            UpdateCanTakePicture(false);

            try
            {
                // compensate for the strobe if it's enabled
                if (IsStrobeOn())
                {
                    // find out if you're auto exposure so you can switch back when finished
                    var isAutoExposure = ExposureSettings.IsAutoExposure;

                    // goto manual if you were
                    if (isAutoExposure)
                    {
                        ExposureSettings.ManualExposure();
                    }

                    // compensate the iso and shutter for the amount of light the strobe is going to produce
                    ExposureSettings.Compensate();

                    // get the iso and shutter so you can go back
                    var iso = ExposureSettings.ISO;
                    var shutter = ExposureSettings.ShutterSpeed;

                    // update the iso and shutter with the new compensated values
                    ExposureSettings.UpdateISO(ExposureSettings.StrobeISO);
                    ExposureSettings.UpdateShutterSpeed(ExposureSettings.StrobeShutter);
                    Thread.Sleep(250);

                    // take the still with the given builder
                    await StillHandler.TakeStillAsync();

                    // go back to what they were set to previous
                    ExposureSettings.UpdateISO(iso);
                    ExposureSettings.UpdateShutterSpeed(shutter);

                    // switch back to auto if previous
                    if (isAutoExposure)
                    {
                        AutoExposure();
                    }
                }
                else
                {
                    await StillHandler.TakeStillAsync();
                }
            }
            catch (Exception e)
            {
                OnNotify(e.Message, MessageTypes.Error);
                Console.WriteLine(e.StackTrace);
                throw e;
            }
            finally
            {
                UpdateCanTakePicture(true);
            }
        }

        [RemoteCommand]
        [PropertyConverter(typeof(StringToDateTime))]
        [Alias("TakeStill", "Take Still", "Take Picture")]
        public async Task TakePicture(DateTime time)
        {
            await StillHandler.TakeStill(time);
        }

        /// <summary>
        /// Lock the aspect ratio to 16:9.
        /// </summary>
        [RemoteCommand]
        public void UnlockAspectRatio()
        {
            IsAspectRatioLocked = false;
            OnNotify($"{nameof(IsAspectRatioLocked)}:{IsAspectRatioLocked}");
            ResolutionChanged?.Invoke(this, StillHandler.JPEGResolution);
        }

        /// <summary>
        /// Update the image format the still handler is using
        /// </summary>
        /// <param name="imageFormat">Type of image format to set</param>
        [Alias("ImageFormat")]
        [RemoteCommand]
        [PropertyConverter(typeof(StringToImageFormatType))]
        [CancelWhen(nameof(Droid.Camera.StillHandler.IsBursting), true)]
        [CancelWhen(nameof(Droid.Camera.RecordingHandler.IsRecording), true)]
        public void UpdateImageFormat(ImageFormatType imageFormat)
        {
            if (RecordingHandler.IsRecording ?? false)
            {
                OnNotify("Please stop recording to change the image format", MessageTypes.Error);
                OnNotify($"{nameof(StillHandler.ImageFormat)}:{StillHandler.ImageFormat}");
                return;
            }

            StillHandler.UpdateImageFormat(imageFormat);
        }

        /// <summary>
        /// Update the preview resolution.
        /// </summary>
        /// <param name="width">Preview width.</param>
        /// <param name="height">Preview Height.</param>
        [RemoteCommand]
        public void UpdateResolution(int width, int height)
        {
            ResolutionChanged?.Invoke(this, new Size(width, height));
        }

        /// <summary>
        /// See if the camera can capture a still
        /// </summary>
        /// <returns>True if a still can be captured, false otherwise</returns>
        private bool CanCaptureStill()
        {
            // TODO: Do away with this method and just use CanTakePicture.  CanTakePicture would then have to account for diskSpaceManager?.IsDiskSpaceLow
            if (diskSpaceManager?.IsDiskSpaceLow ?? false)
            {
                OnNotify("Disk space is too low, please free up some space", MessageTypes.Error);
                UpdateCanTakePicture(true);
                return false;
            }

            if (RecordingHandler.IsRecording4K)
            {
                OnNotify("Camera cannot take stills when recording 4K", MessageTypes.Error);
                UpdateCanTakePicture(true);
                return false;
            }

            if (StillHandler.IsBursting ?? true)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Method to execute when the disk space is low
        /// </summary>
        private void DiskSpaceManager_LowDiskSpace()
        {
            OnNotify("Disk space is low");
            StopRecording();
            StopContinuous();
        }

        /// <summary>
        /// Notify when someone tries to record with a shutter speed less than 1/30
        /// </summary>
        /// <param name="exposure">Exposure time camera is set</param>
        private void ExposureSettings_ShutterSpeedChanged(double exposure)
        {
            if (exposure > 33_333_333 && exposure < 33_333_333 && (RecordingHandler.IsRecording ?? true))
            {
                OnNotify("Using shutter speeds lower than 1/30th of a second can cause issues with the number of frames recorded", Messaging.Models.MessageTypes.Error);
            }
        }

        /// <summary>
        /// Turn both aquorea lamps off
        /// </summary>
        private void LampOff()
        {
            foreach (var item in auxManager.AuxDevices.Values)
            {
                if (item is Aquorea aquorea)
                {
                    aquorea.LampOff();
                }
            }
        }

        /// <summary>
        /// Turn both aquorea lamps on
        /// </summary>
        private void LampOn()
        {
            foreach (var item in auxManager.AuxDevices.Values)
            {
                if (item is Aquorea aquorea)
                {
                    aquorea.LampOn();
                }
            }
        }

        /// <summary>
        /// Stop recording when there's only 5 seconds left to shut down
        /// </summary>
        private void PowerController_ShutDownTick()
        {
            if (powerController.ShutdownTimeRemaining.Seconds < PowerFailureThreshold)
            {
                if (RecordingHandler.IsRecording ?? false)
                {
                    StopRecording();
                }
            }
        }

        /// <summary>
        /// Notify that recording has started, can take picture is false if you're recording 4K
        /// </summary>
        /// <param name="filename">Filename of the new recoroding file</param>
        private void RecordingHandler_RecordingStarted(string filename)
        {
            UpdateCanTakePicture(CanTakePicture);
            var message = $"Recording Started: {filename}";
            OnNotify(message);
        }

        /// <summary>
        /// Reset can take picture to true when you've stopped recording
        /// </summary>
        /// <param name="filename">Filename of the video that just finished</param>
        private void RecordingHandler_VideoRecorded(string filename)
        {
            UpdateCanTakePicture(true);
            var message = $"Video Recorded: {filename}";
            OnNotify(message);
        }

        /// <summary>
        /// Set up all the objects that make up the Rayfin's functionality
        /// </summary>
        private void SubCCamera_Initialized()
        {
            var jpegSizes = subCCamera.StreamMap.GetOutputSizes((int)ImageFormatType.Jpeg);
            var rawSize = subCCamera.StreamMap.GetOutputSizes((int)ImageFormatType.RawSensor).First();

            // We need this here so the preview is the full 21MP
            StillHandler = new StillHandler(
                jpegSizes.Select(j => new Size(j.Width, j.Height)),
                new[] { new Size(rawSize.Width, rawSize.Height) },
                Settings,
                subCCamera.Session,
                subCCamera.Characteristics);

            StillHandler.Notify += OnNotify;
            StillHandler.JPEGResolutionChanged += (s, e) => Lens.UpdatePreviewResolution(new Size(e.Width, e.Height));

            RecordingHandler = new RecordingHandler(
                subCCamera.Session,
                diskSpaceManager,
                subCCamera.Settings,
                SubCMediaRecorder.Instance);

            // change the file access to full r/w on stills taken
            StillHandler.PictureTaken += (s, e) =>
            {
                OnNotify($"Still saved: {e}");
                DroidSystem.UserChmod(777, e);

                StillHandler.WriteExif(e, ExifInterface.TagFlash, strobeExifData[e]);
            };

            subCCamera.ExposureSettings.ShutterSpeedChanged += (s, e) => ExposureSettings_ShutterSpeedChanged(e);

            // subscribe to the still handlers events that you need to react to
            StillHandler.StillCaptured += (s, e) =>
            {
                OnNotify("Still captured");
                UpdateCanTakePicture(true);

                var isSo = IsStrobeOn();
                Console.WriteLine("Information: " + isSo);
                // check to see if the strobe is on or recharging
                if (strobeExifData.ContainsKey(e))
                {
                    strobeExifData.Remove(e);
                }

                strobeExifData.Add(e, isSo ? "1" : "0");
            };

            StillHandler.StoppedContinuous += (s, e) => UpdateCanTakePicture(true);

            // change the preview resolution
            ResolutionChanged?.Invoke(this, new Size(StillHandler.JPEGResolution.Width, StillHandler.JPEGResolution.Height));

            // listen to the appropriate recording handlers events
            RecordingHandler.VideoRecorded += (s, e) => RecordingHandler_VideoRecorded(e.Path);
            RecordingHandler.RecordingStarted += (s, e) => RecordingHandler_RecordingStarted(e);
            RecordingHandler.Notify += OnNotify;

            LoadSettings();

            Initialized?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates the value of CanTakePicture consistently and accounts for the states of IsRecording4K and IsBursting
        /// </summary>
        /// <param name="value">The value to set it to (maybe)</param>
        private void UpdateCanTakePicture(bool value)
        {
            if (!value)
            {
                CanTakePicture = value;
            }
            else
            {
                CanTakePicture = RecordingHandler.IsRecording4K ? false : StillHandler.IsBursting ?? true ? false : value;
            }
        }
    }
}
namespace SubCTools.Droid.Lenses
{
    using Android.Graphics;
    using Android.Hardware.Camera2;
    using Android.OS;
    using SubCTools.Attributes;
    using SubCTools.Droid.Camera;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Extensions;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Drawing;
    using System.Threading.Tasks;

    /// <summary>
    /// Responsible for handling all aspects of the camera object relating to the lens such as focus
    /// and zoom.
    /// </summary>
    public class StockLens : DroidBase
    {
        private readonly SubCCaptureSession captureSession;

        private readonly ExposureSettings exposure;

        private readonly Handler handler;
        private readonly HandlerThread handlerThread;
        private ControlAFState autoFocusState;
        private DigitalPTZ digitalPTZ;
        private float focusDistance;
        private TimeSpan focusWaitTime = TimeSpan.FromSeconds(4);
        private bool isManualFocus;
        private int moveRateX;
        private int moveRateY;
        private float moveStep;
        private Size previewResolution;
        private bool stepMotion;
        private float zoomRate;
        private float zoomStep;

        public StockLens(
            Size previewResolution,
            SubCCaptureSession captureSession,
            ISettingsService settings,
            ExposureSettings exposure)
            : base(settings)
        {
            this.captureSession = captureSession;
            this.exposure = exposure;
            this.previewResolution = previewResolution;

            captureSession.CaptureCompleted += CaptureSession_CaptureCompleted;

            digitalPTZ = new DigitalPTZ(captureSession, previewResolution);
            digitalPTZ.UpdateStepMotion(DigitalPTZ.MotionTypes.Continuous);
            digitalPTZ.ZoomLevelChanged += ZoomLevel_Changed;

            handlerThread = new HandlerThread("Focus");
            handlerThread.Start();
            handler = new Handler(handlerThread.Looper);
        }

        /// <summary>
        /// Get the value for the stat of the auto focus. E.g. Focused, Scanning...
        /// </summary>
        [RemoteState(true)]
        public ControlAFState AutoFocusState
        {
            get => autoFocusState;
            private set
            {
                if (autoFocusState != value)
                {
                    autoFocusState = value;
                    OnNotify($"{nameof(AutoFocusState)}:{AutoFocusState}");
                }
            }
        }

        public Rect CurrentFrameRect { get; private set; }

        public Size CurrentFrameSize => new Size(CurrentFrameRect.Right - CurrentFrameRect.Left, CurrentFrameRect.Bottom - CurrentFrameRect.Top);

        /// <summary>
        /// Gets the value for the distance at which the camera is focused
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public float FocusDistance
        {
            get => focusDistance;
            set
            {
                value = value.Clamp(MinimumFocusDistance, MaximumFocusDistance);

                if (Set(nameof(FocusDistance), ref focusDistance, value))
                {
                    OnNotify($"{nameof(FocusDistance)}:{FocusDistance}"); //+ (RawFocus < InfinityValue ? "∞" : FocusDistance.ToString()));
                }
            }
        }

        /// <summary>
        /// Gets or sets the value for how long the auto focusing will hunt for focus when a picture
        /// is requested before giving up
        /// </summary>
        [Savable]
        [RemoteState]
        public TimeSpan FocusWaitTime
        {
            get => focusWaitTime;
            set => Set(nameof(FocusWaitTime), ref focusWaitTime, value);
        }

        /// <summary>
        /// Gets the value for whether the camera is in manual focus
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool IsManualFocus
        {
            get => isManualFocus;
            private set
            {
                Set(nameof(IsManualFocus), ref isManualFocus, value);
                OnNotify($"{nameof(IsManualFocus)}:{IsManualFocus}");
            }
        }

        /// <summary>
        /// Returns the Maximum Focus Distance in cm.
        /// </summary>
        [RemoteState]
        public float MaximumFocusDistance { get; } = 15f;

        /// <summary>
        /// Returns the Minimum Focus Distance in cm.
        /// </summary>
        [RemoteState]
        public float MinimumFocusDistance { get; } = 0.1f;

        /// <summary>
        /// Rate at which the camera pans
        /// </summary>
        public int MoveRateX
        {
            get => moveRateX;
            set
            {
                Set(nameof(MoveRateX), ref moveRateX, value);
                digitalPTZ.UpdatePanSpeed(value);
            }
        }

        /// <summary>
        /// Rate at which the camera tilts
        /// </summary>
        public int MoveRateY
        {
            get => moveRateY;
            set
            {
                Set(nameof(MoveRateY), ref moveRateY, value);
                digitalPTZ.UpdateTiltSpeed(value);
            }
        }

        /// <summary>
        /// Step move distance as a fraction of the frame size (for step operations)
        /// </summary>
        public float MoveStep
        {
            get => moveStep;
            set
            {
                digitalPTZ.MoveStep = value;
                Set(nameof(MoveStep), ref moveStep, value);
            }
        }

        public Size SensorMaxSize { get; set; }

        //=> MaxFocusDistance;
        /// <summary>
        /// Indicates wheather or not to use PTZ StepMotion
        /// </summary>
        [RemoteState]
        public bool StepMotion
        {
            get => stepMotion;
            set
            {
                Set(nameof(StepMotion), ref stepMotion, value);
                digitalPTZ.UpdateStepMotion(value ? DigitalPTZ.MotionTypes.Step : DigitalPTZ.MotionTypes.Continuous);
            }
        }

        //MinFocusDistance;
        /// <summary>
        /// Gets the value of the level of digital zoom
        /// </summary>
        [RemoteState]
        public double ZoomLevel => digitalPTZ.ZoomLevel;

        /// <summary>
        /// Rate at which the camera zooms in or out
        /// </summary>
        public float ZoomRate
        {
            get => zoomRate;
            set
            {
                //digitalPTZ.ZoomRate = value;
                Set(nameof(ZoomRate), ref zoomRate, value);
            }
        }

        /// <summary>
        /// The amount to multiply zoom per step.
        /// </summary>
        public float ZoomStep
        {
            get => zoomStep;
            set
            {
                digitalPTZ.ZoomStep = value;
                Set(nameof(ZoomStep), ref zoomStep, value);
            }
        }

        /// <summary>
        /// Enables autofocus / Disables Manual Focus
        /// </summary>
        [RemoteCommand]
        [Alias("AutoFocus")]
        public void EnableAutoFocus()
        {
            if (!exposure.IsAutoExposure)
            {
                OnNotify("Please switch to Auto Exposure to use Auto Focus", MessageTypes.Error);
                IsManualFocus = true;
                return;
            }

            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfTrigger), (int)ControlAFTrigger.Idle);
            captureSession.Remove(new SubCCaptureRequestKey(CaptureRequest.LensFocusDistance));
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfMode), (int)ControlAFMode.ContinuousPicture);
            //await StartAutoFocus();

            captureSession.Repeat();
            IsManualFocus = false;
        }

        /// <summary>
        /// Enables Manual Focus / Disables Auto Focus.
        /// </summary>
        [RemoteCommand]
        [Alias("ManualFocus")]
        public void EnableManualFocus()
        {
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.LensFocusDistance), FocusDistance);
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfMode), (int)ControlAFMode.Off);
            captureSession.Repeat();
            IsManualFocus = true;
        }

        /// <summary>
        /// Instructs the lens to focus on a target one stop farther away
        /// </summary>
        [RemoteCommand]
        [Alias("StepFocusFar")]
        public void FocusFar()
        {
            var newFocusDistance = 0.0f;

            if (FocusDistance / 2 > MinimumFocusDistance)
            {
                newFocusDistance = focusDistance / 2;
            }
            else
            {
                newFocusDistance = MinimumFocusDistance;
            }

            UpdateFocus(newFocusDistance);
        }

        /// <summary>
        /// Instructs the lens to focus on a target one stop closer
        /// </summary>
        [RemoteCommand]
        [Alias("StepFocusNear")]
        public void FocusNear()
        {
            var newFocusDistance = 0.0f;

            if (FocusDistance * 2 < MaximumFocusDistance)
            {
                newFocusDistance = focusDistance * 2;
            }
            else
            {
                newFocusDistance = MaximumFocusDistance;
            }

            UpdateFocus(newFocusDistance);
        }

        public override void LoadSettings()
        {
            base.LoadSettings();

            IsManualFocus = Settings.TryLoad(nameof(IsManualFocus), out bool tempFocus) ? tempFocus : IsManualFocus;

            if (IsManualFocus)
            {
                EnableManualFocus();
            }
        }

        /// <summary>
        /// Sets the digital PTZ to pan at a constant speed in any direction without changing the
        /// zoom level
        /// </summary>
        /// <param name="PanVelocity">The speed to move in the x direction between -100 and 100</param>
        /// <param name="TiltVelocity">The speed to move in the y direction between -100 and 100</param>
        [RemoteCommand]
        public void MovePTZ(int PanVelocity, int TiltVelocity) => MovePTZ(PanVelocity, TiltVelocity, 0);

        /// <summary>
        /// Sets the digital PTZ to pan at a constant speed in any 3D direction
        /// </summary>
        /// <param name="PanVelocity">The speed to move in the x direction between -100 and 100</param>
        /// <param name="TiltVelocity">The speed to move in the y direction between -100 and 100</param>
        /// <param name="ZoomVelocity">The speed to move in the z direction between -100 and 100</param>
        [RemoteCommand]
        public void MovePTZ(int PanVelocity, int TiltVelocity, int ZoomVelocity)
        {
            digitalPTZ.MovePTZ(PanVelocity, TiltVelocity, ZoomVelocity);
        }

        /// <summary>
        /// Activates a pan left action
        /// </summary>
        [RemoteCommand]
        public void PanLeft() => digitalPTZ.StepMoveLeft();

        /// <summary>
        /// Activates a pan left action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        [RemoteCommand]
        public void PanLeft(int speed) => digitalPTZ.PanLeft(speed);

        /// <summary>
        /// Activates a pan right action
        /// </summary>
        [RemoteCommand]
        public void PanRight() => digitalPTZ.StepMoveRight();

        /// <summary>
        /// Activates a pan right action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        [RemoteCommand]
        public void PanRight(int speed) => digitalPTZ.PanRight(speed);

        /// <summary>
        /// Instructs the lens to start the autofocus routine
        /// </summary>
        /// <returns>A focus result</returns>
        [RemoteCommand]
        public async Task<FocusResult> StartAutoFocus()
        {
            if (IsManualFocus)
            {
                OnNotify("Enabling autofocus", MessageTypes.Debug);
                EnableAutoFocus();
            }

            // just lock the focus if it's already found
            if (IsFocusLocked(AutoFocusState))
            {
                LockFocus();
                return new FocusResult() { IsFocusLocked = true };
            }

            // Start the auto focus triggering
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfMode), (int)ControlAFMode.Auto);
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfTrigger), (int)ControlAFTrigger.Start);
            captureSession.Repeat();

            var count = 0;

            while (!IsFocusLocked() && count <= FocusWaitTime.TotalMilliseconds)
            {
                await Task.Delay(100);
                count += 100;
            }

            LockFocus();

            return new FocusResult() { IsFocusLocked = IsFocusLocked() };
        }

        public void StopAutoFocus()
        {
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfTrigger), (int)ControlAFTrigger.Cancel);
            captureSession.Repeat();
        }

        /// <summary>
        /// Instructs the digital PTZ to stop all motion
        /// </summary>
        [RemoteCommand]
        public void StopPTZ() => digitalPTZ.StopPTZ();

        /// <summary>
        /// Instructs the digital ptz to stop zooming
        /// </summary>
        [RemoteCommand]
        public void StopZoom() => digitalPTZ.StopZoom();

        /// <summary>
        /// Starts the tilt down action
        /// </summary>
        [RemoteCommand]
        public void TiltDown() => digitalPTZ.TiltDown();

        /// <summary>
        /// Activates a tilt down action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        [RemoteCommand]
        public void TiltDown(int speed) => digitalPTZ.TiltDown(speed);

        /// <summary>
        /// Starts the tilt up action
        /// </summary>
        [RemoteCommand]
        public void TiltUp() => digitalPTZ.TiltUp();

        /// <summary>
        /// Activates a tilt up action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        [RemoteCommand]
        public void TiltUp(int speed) => digitalPTZ.TiltUp(speed);

        /// <summary>
        /// Update the distance of focus in cm
        /// </summary>
        /// <param name="distance">Distance in cm</param>
        [RemoteCommand]
        [Alias("Focus")]
        [CancelWhen(nameof(IsManualFocus), false)]
        public void UpdateFocus(float distance)
        {
            if (!IsManualFocus)
            {
                OnNotify("Please set to manual focus", MessageTypes.Error);
                return;
            }

            FocusDistance = distance;
            OnNotify("OpticalFocus:" + FocusDistance, MessageTypes.CameraCommand);
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.LensFocusDistance), FocusDistance);//focusValue);
            captureSession.Repeat();
        }

        /// <summary>
        /// Update the preview resolution.
        /// </summary>
        /// <param name="width">New private width.</param>
        /// <param name="height">New preview height.</param>
        [RemoteCommand]
        public void UpdatePreviewResolution(int width, int height)
        {
            UpdatePreviewResolution(new Size(width, height));
        }

        /// <summary>
        /// Update the preview resolution and recalculate the zoom
        /// </summary>
        /// <param name="resolution">New size to perform the zoom algorithm on</param>
        public void UpdatePreviewResolution(Size resolution)
        {
            previewResolution = resolution;
            digitalPTZ.UpdatePreviewResolution(resolution);
            Zoom(ZoomLevel);
        }

        /// <summary>
        /// Makes the lens move directly to the new zoom level
        /// </summary>
        /// <param name="level">the level to zoom to between 1 and 10</param>
        [RemoteCommand]
        public void Zoom(double level)
        {
            Zoom(level, false);
        }

        /// <summary>
        /// Makes the lens move directly to the new zoom level
        /// </summary>
        /// <param name="level">the level to zoom to between 1 and 10</param>
        /// <param name="silent">
        /// if this flag is false a notification will be sent if an invalid zoom level is specified
        /// </param>
        [RemoteCommand]
        public void Zoom(double level, bool silent)
        {
            var templevel = Math.Abs(level);
            templevel = templevel.Clamp(DigitalPTZ.MinZoom, DigitalPTZ.MaxZoom);
            if (level != templevel && !silent)
                OnNotify($"Invalid zoom value sent.  Using {Math.Round(templevel, 1)} instead");

            digitalPTZ.Zoom(templevel);
            if (!silent)
                OnNotify($"{nameof(ZoomLevel)}:{Math.Round(ZoomLevel, 1)}");
        }

        /// <summary>
        /// Starts the zoom in action
        /// </summary>
        [RemoteCommand]
        public void ZoomIn() => digitalPTZ.ZoomIn();

        /// <summary>
        /// Instructs the digital PTZ to initiate a smooth step in zoom which moves slowly from zoom
        /// level to zoom level + 1
        /// </summary>
        [RemoteCommand]
        [Alias("JumpZoomIn")]
        public void ZoomJumpIn() => digitalPTZ.JumpZoomIn();

        /// <summary>
        /// Instructs the digital PTZ to initiate a smooth step out zoom which moves slowly from
        /// zoom level to zoom level - 1
        /// </summary>
        [RemoteCommand]
        [Alias("JumpZoomOut")]
        public void ZoomJumpOut() => digitalPTZ.JumpZoomOut();

        /// <summary>
        /// Starts the zoom out action
        /// </summary>
        [RemoteCommand]
        public void ZoomOut() => digitalPTZ.ZoomOut();

        /// <summary>
        /// Instructs the digital PTZ to initiate a smooth step in zoom which moves slowly from zoom
        /// level to zoom level + 1
        /// </summary>
        [RemoteCommand]
        [Alias("StepZoomIn")]
        public void ZoomStepIn() => digitalPTZ.StepZoomIn();

        /// <summary>
        /// Instructs the digital PTZ to initiate a smooth step out zoom which moves slowly from
        /// zoom level to zoom level - 1
        /// </summary>
        [RemoteCommand]
        [Alias("StepZoomOut")]
        public void ZoomStepOut() => digitalPTZ.StepZoomOut();

        private void CaptureSession_CaptureCompleted(object sender, ICaptureResult e)
        {
            var f = (float)e.Get(new SubCCaptureResultKey(CaptureResult.LensFocusDistance));

            if (!IsManualFocus)
            {
                FocusDistance = f;
                //OnNotify("OpticalFocus:" + f, MessageTypes.CameraCommand);
            }

            // check the focus result
            AutoFocusState = (ControlAFState)(int)e.Get(new SubCCaptureResultKey(CaptureResult.ControlAfState));
            CurrentFrameRect = (Rect)e.Get(new SubCCaptureResultKey(CaptureResult.ScalerCropRegion));
        }

        private bool IsFocusLocked() => IsFocusLocked(AutoFocusState);

        private bool IsFocusLocked(ControlAFState afState) =>
            afState == ControlAFState.FocusedLocked
            || afState == ControlAFState.PassiveFocused
            || afState == ControlAFState.PassiveUnfocused
            || afState == ControlAFState.NotFocusedLocked;

        private void LockFocus()
        {
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfMode), (int)ControlAFMode.ContinuousPicture);
            captureSession.Update(new SubCCaptureRequestKey(CaptureRequest.ControlAfTrigger), (int)ControlAFTrigger.Idle);
            captureSession.Repeat();
        }

        private void ZoomLevel_Changed(object sender, double e)
        {
            OnNotify($"{nameof(ZoomLevel)}:{Math.Round(ZoomLevel, 1)}");
        }
    }
}
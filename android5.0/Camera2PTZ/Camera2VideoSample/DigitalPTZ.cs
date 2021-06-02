//-----------------------------------------------------------------------
// <copyright file="DigitalPTZ.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson & Mark Mercer</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Camera
{
    using Android.Graphics;
    using Android.Hardware.Camera2;
    using Camera2PTZ;
    using SubCTools.DataTypes;
    using System;
    using System.Drawing;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Responsible for controlling the digital pan, tilt and zoom of the android camera object.
    /// </summary>
    public class DigitalPTZ
    {
        /// <summary>
        /// Maximum zoom level
        /// </summary>
        public const double MaxZoom = 10.0;

        /// <summary>
        /// Min zoom, 1x
        /// </summary>
        public const double MinZoom = 1.0;

        /// <summary>
        /// The maximum rate that the camera will pan and tilt
        /// </summary>
        private const double MaxMoveRate = 0.02d;

        /// <summary>
        /// The maximum rate that the camera will zoom
        /// </summary>
        private const double MaxZoomRate = 0.014d;

        /// <summary>
        /// An object to lock so that the smooth motion tasks only runs one at a time
        /// </summary>
        private static readonly object MotionLock = new object();

        /// <summary>
        /// An object to lock so that zoom method spam will not lock up the system
        /// </summary>
        private static readonly object ZoomLock = new object();

        /// <summary>
        /// The timer that calls for zoom actions as the PTZ rectangle is in motion
        /// </summary>
        private readonly System.Timers.Timer motionTimer;

        /// <summary>
        /// Number of frames used for transition (Default 30fps)
        /// </summary>
        private readonly uint transitionFrames = 1_500 / 30;

        /// <summary>
        /// The amount of time to spend on the transition in milliseconds. (Aproximate, actual time
        /// will vary)
        /// </summary>
        private readonly uint transitionTime = 1_500;

        /// <summary>
        /// The camera capture builder
        /// </summary>
        private CaptureRequest.Builder builder;

        /// <summary>
        /// The direction and rate at which the view rectangle is moving. All values from 0 to 100
        /// </summary>
        private Vector3 motionVector;

        /// <summary>
        /// A rect representing the camera preview surface
        /// </summary>
        private Rect previewRect;

        /// <summary>
        /// The resolution of the camera preview surface
        /// </summary>
        private Size previewRes;

        /// <summary>
        /// The digital zoom level
        /// </summary>
        private double zoomLevel = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalPTZ" /> class.
        /// </summary>
        /// <param name="capSession">the capture session</param>
        /// <param name="previewResolution">the resolution of the preview surface</param>
        public DigitalPTZ(CaptureRequest.Builder builder, Size previewResolution)
        {
            this.builder = builder;
            UpdatePreviewResolution(previewResolution);
            ViewRect = new Rect(0, 0, previewRes.Width, previewRes.Height);
            motionVector = new Vector3(0, 0, 0);

            motionTimer = new System.Timers.Timer(transitionTime / transitionFrames) { AutoReset = true };
            motionTimer.Elapsed += MotionTimer_Tick;
        }

        /// <summary>
        /// An event that notifies StockLens when the zoom level has changed so that it can notifty
        /// topside control software
        /// </summary>
        public event EventHandler<double> ZoomLevelChanged;

        /// <summary>
        /// Types of motion that the camera can perform
        /// </summary>
        public enum MotionTypes
        {
            /// <summary>
            /// The camera will pan, tilt, and zoom in steps
            /// </summary>
            Step,

            /// <summary>
            /// Once a command to start motion is sent the camera will continuously move at a
            /// constant speed until instructed to stop or a boundary is reached.
            /// </summary>
            Continuous
        }

        /// <summary>
        /// Gets a value indicating whether pan and tilt actions move in steps True when the PTZ
        /// controls are set to move in steps False when the PTZ controls are set to move constantly
        /// until instructed to stop or a limit is reached.
        /// </summary>
        public MotionTypes MotionType { get; private set; }

        /// <summary>
        /// Gets or sets the step move distance as a fraction of the frame size (for step operations)
        /// </summary>
        public float MoveStep { get; set; } = 0.5f;

        /// <summary>
        /// Gets the speed at which the view rectangle pans
        /// </summary>
        public int PanSpeed { get; private set; } = 75;

        /// <summary>
        /// Gets the speed at which the view rectangle tilts
        /// </summary>
        public int TiltSpeed { get; private set; } = 75;

        /// <summary>
        /// Gets a rectangle representing the section of the preview surface that is currently being
        /// displayed on the screen
        /// </summary>
        public Rect ViewRect { get; private set; }

        /// <summary>
        /// Gets the digital zoom level
        /// </summary>
        public double ZoomLevel
        {
            get => zoomLevel;
            private set
            {
                if (zoomLevel != value)
                {
                    zoomLevel = value;
                    ZoomLevelChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the amount to add to zoomlevel per step.
        /// </summary>
        public float ZoomStep { get; set; } = 1f; // 2f; it was 2 when this was a multiplier

        /// <summary>
        /// Jump-Zoom In
        /// </summary>
        public void JumpZoomIn()
        {
            if (ZoomLevel == MaxZoom)
            {
                return;
            }

            Zoom(ZoomLevel + 1);
        }

        /// <summary>
        /// Jump-Zoom Out
        /// </summary>
        public void JumpZoomOut()
        {
            if (ZoomLevel == MinZoom)
            {
                return;
            }

            Zoom(ZoomLevel - 1);
        }

        /// <summary>
        /// A single command that sets all of the PTZ motiona parameters and begins motion. This
        /// method does nothing if StepMotion is enabled.
        /// </summary>
        /// <param name="panVelocity">the velocity at which to pan. Between -100 and 100</param>
        /// <param name="tiltVelocity">the velocity at which to tilt. Between -100 and 100</param>
        /// <param name="zoomVelocity">the velocity at which to zoom. Between -100 and 100</param>
        public void MovePTZ(int panVelocity, int tiltVelocity, int zoomVelocity)
        {
            if (MotionType != MotionTypes.Step)
            {
                panVelocity = panVelocity.Clamp(-100, 100);
                tiltVelocity = tiltVelocity.Clamp(-100, 100);
                zoomVelocity = zoomVelocity.Clamp(-100, 100);

                var vec = new Vector3(panVelocity, tiltVelocity, zoomVelocity);
                if (vec.Magnitude == 0)
                {
                    StopPTZ();
                }
                else
                {
                    vec.Z = (zoomVelocity == 0) ? motionVector.Z : zoomVelocity;
                    PanSpeed = (int)Math.Abs(vec.X);
                    TiltSpeed = (int)Math.Abs(vec.Y);
                    motionVector = vec;
                    motionTimer.Start();
                }
            }
        }

        /// <summary>
        /// Activates a pan left action
        /// </summary>
        public void PanLeft()
        {
            if (MotionType == MotionTypes.Step)
            {
                StepMoveLeft();
            }
            else
            {
                motionVector.X = -PanSpeed;
                motionTimer.Start();
            }
        }

        /// <summary>
        /// Activates a pan left action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        public void PanLeft(int speed)
        {
            PanSpeed = speed.Clamp();
            PanLeft();
        }

        /// <summary>
        /// Activates a pan right action
        /// </summary>
        public void PanRight()
        {
            if (MotionType == MotionTypes.Step)
            {
                StepMoveRight();
            }
            else
            {
                motionVector.X = PanSpeed;
                motionTimer.Start();
            }
        }

        /// <summary>
        /// Activates a pan right action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        public void PanRight(int speed)
        {
            PanSpeed = speed.Clamp();
            PanRight();
        }

        /// <summary>
        /// Step-Tilt the view rectangle down.
        /// </summary>
        public void StepMoveDown() => SmoothTranslation(new System.Drawing.Point(0, (int)((float)ViewRect.Height() * MoveStep)));

        /// <summary>
        /// Step-Pan the view rectangle to the left.
        /// </summary>
        public void StepMoveLeft() => SmoothTranslation(new System.Drawing.Point(-((int)((float)ViewRect.Width() * MoveStep)), 0));

        /// <summary>
        /// Step-Pan the view rectangle to right.
        /// </summary>
        public void StepMoveRight() => SmoothTranslation(new System.Drawing.Point((int)((float)ViewRect.Width() * MoveStep), 0));

        /// <summary>
        /// Step-Tilt the view rectangle up.
        /// </summary>
        public void StepMoveUp() => SmoothTranslation(new System.Drawing.Point(0, -((int)((float)ViewRect.Height() * MoveStep))));

        /// <summary>
        /// Step-Zoom In
        /// </summary>
        public void StepZoomIn()
        {
            if (ZoomLevel == MaxZoom)
            {
                return;
            }

            if (MotionType == MotionTypes.Step)
            {
                Zoom((float)(ZoomLevel + ZoomStep).Clamp(MinZoom, MaxZoom));
            }
            else
            {
                SmoothZoom(ZoomStep);
            }
        }

        /// <summary>
        /// Step-Zoom Out
        /// </summary>
        public void StepZoomOut()
        {
            if (ZoomLevel == MinZoom)
            {
                return;
            }

            if (MotionType == MotionTypes.Step)
            {
                Zoom((float)(ZoomLevel - ZoomStep).Clamp(MinZoom, MaxZoom));
            }
            else
            {
                SmoothZoom(-ZoomStep);
            }
        }

        /// <summary>
        /// Stops all continuous PTZ actions
        /// </summary>
        public void StopPTZ()
        {
            motionVector = new Vector3(0, 0, 0);
            motionTimer.Stop();
        }

        /// <summary>
        /// Stops all Zoom actions
        /// </summary>
        public void StopZoom()
        {
            motionVector.Z = 0;
        }

        /// <summary>
        /// Activates a tilt down action
        /// </summary>
        public void TiltDown()
        {
            if (MotionType == MotionTypes.Step)
            {
                StepMoveDown();
            }
            else
            {
                motionVector.Y = TiltSpeed;
                motionTimer.Start();
            }
        }

        /// <summary>
        /// Activates a tilt down action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        public void TiltDown(int speed)
        {
            TiltSpeed = speed.Clamp();
            TiltDown();
        }

        /// <summary>
        /// Activates a tilt up action
        /// </summary>
        public void TiltUp()
        {
            if (MotionType == MotionTypes.Step)
            {
                StepMoveUp();
            }
            else
            {
                motionVector.Y = -TiltSpeed;
                motionTimer.Start();
            }
        }

        /// <summary>
        /// Activates a tilt up action and specifies the speed
        /// </summary>
        /// <param name="speed">The speed as a percentage integer</param>
        public void TiltUp(int speed)
        {
            TiltSpeed = speed.Clamp();
            TiltUp();
        }

        /// <summary>
        /// Updates the pan speed
        /// </summary>
        /// <param name="speed">the speed</param>
        public void UpdatePanSpeed(int speed)
        {
            PanSpeed = speed;
            motionVector.X = motionVector.X / Math.Abs(motionVector.X) * PanSpeed;
        }

        /// <summary>
        /// Updates the preview resolution
        /// </summary>
        /// <param name="prevRes">the resolution of the preview surface</param>
        public void UpdatePreviewResolution(Size prevRes)
        {
            previewRes = new Size(prevRes.Width - 1, prevRes.Height - 1);
            previewRect = new Rect(0, 0, previewRes.Width, previewRes.Height);
        }

        /// <summary>
        /// Updates the value of StepMotion
        /// </summary>
        /// <param name="newType">a boolean value</param>
        public void UpdateStepMotion(MotionTypes newType)
        {
            if (MotionType == MotionTypes.Step)
            {
                StopPTZ();
            }

            MotionType = newType;
        }

        /// <summary>
        /// Updates the tilt speed
        /// </summary>
        /// <param name="speed">the speed</param>
        public void UpdateTiltSpeed(int speed)
        {
            TiltSpeed = speed;
            motionVector.Y = motionVector.Y / Math.Abs(motionVector.Y) * TiltSpeed;
        }

        /// <summary>
        /// Zoom in/out to a specific level (centered on the same point as current)
        /// </summary>
        /// <param name="level">Level at which to digitally zoom, e.g. 2x, 3x... Min:1x, Max40x</param>
        public void Zoom(double level)
        {
            //if (ZoomLevel == level)
            //{
            //    return;
            //}
            // Bail if zoom is currently being completed.
            if (ZoomLock.IsLocked())
            { return; }

            if (level < MinZoom || level > MaxZoom)
            {
                StopZoom();
            }

            level = Math.Abs(level).Clamp(MinZoom, MaxZoom);

            // calculate difference in rectangles
            var dx = (int)((ViewRect.Width() - (previewRes.Width / level)) / 2);
            var dy = (int)((ViewRect.Height() - (previewRes.Height / level)) / 2);
            var rect = new Rect(ViewRect);
            rect.Inset(dx, dy);

            Zoom(rect, level != ZoomLevel);
        }

        /// <summary>
        /// Zooms with an x and y offset from the current position and specifys a new zoom level
        /// </summary>
        /// <param name="dx">the change in x</param>
        /// <param name="dy">the change in y</param>
        /// <param name="level">the new zoom level</param>
        public void Zoom(int dx, int dy, double level)
        {
            if (ZoomLock.IsLocked())
            {
                return;
            }

            ViewRect.Offset(dx, dy);
            Zoom(level);
        }

        /// <summary>
        /// Zoom in to a specific rectangle
        /// </summary>
        /// <param name="left">Rectangle left edge</param>
        /// <param name="top">Rectangle top edge</param>
        /// <param name="right">Rectangle right edge</param>
        /// <param name="bottom">Rectangle bottom edge</param>
        /// <param name="recalcZoomLevel">if true the ZoomLevel will be recalculated</param>
        public void Zoom(int left, int top, int right, int bottom, bool recalcZoomLevel = false)
        {
            if (ZoomLock.IsLocked())
            {
                return;
            }

            if (left > right)
            {
                var temp = left;
                left = right;
                right = temp;
            }

            if (top > bottom)
            {
                var temp = top;
                top = bottom;
                bottom = temp;
            }

            Zoom(new Rect(left, top, right, bottom), recalcZoomLevel);
        }

        /// <summary>
        /// Zoom to a specific rectangle
        /// </summary>
        /// <param name="rect">the rectangle to zoom to</param>
        /// <param name="recalcZoomLevel">if true the ZoomLevel will be recalculated</param>
        public void Zoom(Rect rect, bool recalcZoomLevel)
        {
            if (ZoomLock.IsLocked())
            {
                return;
            }

            Zoom(rect);
            if (recalcZoomLevel)
            {
                ZoomLevel = CalculateZoomLevel(rect);
            }
        }

        /// <summary>
        /// The main zoom method. Every zoom method leads to this one. Zooms to a specific rectangle
        /// </summary>
        /// <param name="rect">the rectangle to zoom to</param>
        public void Zoom(Rect rect)
        {
            if (ZoomLock.IsLocked())
            {
                return;
            }

            lock (ZoomLock)
            {
                CorrectAspectRatio(rect);
                CheckMotionBounds(rect);
                rect.Restrain(previewRect);
                ViewRect = new Rect(rect);

                //Console.WriteLine($">>>Preview:{previewRect} - View:{ViewRect}");
                builder.Set(CaptureRequest.ScalerCropRegion, ViewRect);
                //session.Update(new SubCCaptureRequestKey(CaptureRequest.ScalerCropRegion), ViewRect);
                //session.Repeat();
            }
        }

        /// <summary>
        /// Activates a zoom in action
        /// </summary>
        public void ZoomIn()
        {
            if (MotionType == MotionTypes.Step)
            {
                StepZoomIn();
            }
            else
            {
                motionVector.Z = 100;
                motionTimer.Start();
            }
        }

        /// <summary>
        /// Activates a zoom out action
        /// </summary>
        public void ZoomOut()
        {
            if (MotionType == MotionTypes.Step)
            {
                StepZoomOut();
            }
            else
            {
                motionVector.Z = -100;
                motionTimer.Start();
            }
        }

        /// <summary>
        /// Calculates steps for zoom step array using ease out function.
        /// </summary>
        /// <param name="startLocation">the starting value</param>
        /// <param name="endLocation">the ending value</param>
        /// <param name="numberOfSteps">number of steps to take</param>
        /// <param name="ease">An easing function</param>
        /// <returns>an array of step values</returns>
        private static float[] CalculateSteps(float startLocation, float endLocation, uint numberOfSteps, Func<int, uint, float> ease)
        {
            var distance = endLocation - startLocation;
            var output = new float[numberOfSteps];

            for (int x = 0; x < numberOfSteps; x++)
            {
                output[x] = startLocation + (distance * ease(x, numberOfSteps));
            }

            output[0] = startLocation;
            output[numberOfSteps - 1] = endLocation;

            return output;
        }

        /// <summary>
        /// Calculates steps for zoom step array using ease out function.
        /// </summary>
        /// <param name="startLocation">the starting value</param>
        /// <param name="endLocation">the ending value</param>
        /// <param name="numberOfSteps">number of steps to take</param>
        /// <param name="ease">An easing function</param>
        /// <returns>an array of step values</returns>
        private static int[] CalculateSteps(int startLocation, int endLocation, uint numberOfSteps, Func<int, uint, float> ease)
        {
            return CalculateSteps(startLocation, endLocation, numberOfSteps, ease);
        }

        /// <summary>
        /// Calculates steps for zoom step array using sigmoid function.
        /// </summary>
        /// <param name="startLocation">the starting value</param>
        /// <param name="endLocation">the ending value</param>
        /// <param name="numberOfSteps">number of steps to take</param>
        /// <returns>an array of step values</returns>
        private static float[] CalculateSteps_Sigmoid(float startLocation, float endLocation, int numberOfSteps)
        {
            const float Slope = 20f;
            var reverse = endLocation < startLocation;

            if (reverse)
            {
                var temp = endLocation;
                endLocation = startLocation;
                startLocation = temp;
            }

            var distance = endLocation - startLocation;
            var output = new float[numberOfSteps];
            var slopeVal = Slope / numberOfSteps;

            for (int x = 0; x < numberOfSteps; x++)
            {
                output[x] = Sigmoid(x, slopeVal, distance, numberOfSteps) + startLocation;
            }

            output[0] = startLocation;
            output[numberOfSteps - 1] = endLocation;

            if (reverse)
            {
                Array.Reverse(output);
            }

            return output;
        }

        /// <summary>
        /// Calculates a sinusoidal ease out function
        /// </summary>
        /// <param name="x">The location to calculate the value for.</param>
        /// <param name="steps">The total number of steps calculated</param>
        /// <returns>a sigmoid</returns>
        private static float EaseOut(int x, uint steps)
            => (float)Math.Sin(((double)x / steps) * Math.PI / 2d);

        /// <summary>
        /// Calculates the sigmoid of a given value.
        /// </summary>
        /// <param name="x">The location to calculate the value for.</param>
        /// <param name="slope">The slope of the logistic function.</param>
        /// <param name="targetLocation">The maximum value of the function</param>
        /// <param name="steps">The total number of steps calculated</param>
        /// <returns>a sigmoid</returns>
        private static float Sigmoid(int x, float slope, float targetLocation, int steps)
            => targetLocation / (1 + (float)Math.Exp(-slope * (x - (steps / 2))));

        /// <summary>
        /// Calculates the zoom level based on the supplied rectangle
        /// </summary>
        /// <param name="rect">the rectangle</param>
        /// <returns>The zoom level</returns>
        private double CalculateZoomLevel(Rect rect)
        {
            return (((double)previewRes.Width / rect.Width()) + ((double)previewRes.Height / rect.Height())) / 2d;
        }

        /// <summary>
        /// Stops motion in any direction if a boundary has been reached
        /// </summary>
        /// <param name="rect">the new rectangle</param>
        private void CheckMotionBounds(Rect rect)
        {
            if (rect.Left <= 0 || rect.Right >= previewRes.Width)
            {
                motionVector.X = 0;
            }

            if (rect.Top <= 0 || rect.Bottom >= previewRes.Height)
            {
                motionVector.Y = 0;
            }

            var zl = CalculateZoomLevel(rect);
            if (zl < MinZoom || zl > MaxZoom)
            {
                motionVector.Z = 0;
            }
        }

        /// <summary>
        /// If the aspect ratio of the rect doesn't match the aspect ratio of the preview due to
        /// integer casting this method corrects the rect height to match the rect width and aspect
        /// ratio of the preview
        /// </summary>
        /// <param name="rect">the rectangle</param>
        private void CorrectAspectRatio(Rect rect)
        {
            var prar = previewRect.AspectRatio();
            var tolerance = 0.0002 * ZoomLevel * prar;
            var arr = prar / rect.AspectRatio();
            if (arr < 1 - tolerance || arr > 1 + tolerance)
            {
                var newHeight = rect.Width() / prar;
                rect.Top = (int)(rect.ExactCenterY() - (newHeight / 2));
                rect.Bottom = rect.Top + (int)newHeight;
            }
        }

        /// <summary>
        /// Executes once per motionTimer tick and updates the current ViewRect.
        /// </summary>
        /// <param name="sender">The parameter is not used.</param>
        /// <param name="e">The parameter is not used.</param>
        private void MotionTimer_Tick(object sender, EventArgs e)
        {
            if (ZoomLock.IsLocked())
            {
                return;
            }

            // If the motion vector is zero then stop the timer, thus stop recalulating the view rectangle
            if (motionVector.Magnitude == 0)
            {
                motionTimer.Stop();
                return;
            }

            // The movement rates are communicated in terms of percent (between 0 and 100) positive
            // values mean motion to the right, down, or in negative values mean motion to the left,
            // up, or out
            var rateX = motionVector.X.Clamp(-100, 100) / 100d * MaxMoveRate;
            var rateY = motionVector.Y.Clamp(-100, 100) / 100d * MaxMoveRate;
            var rateZ = motionVector.Z.Clamp(-100, 100) / 100d * MaxZoomRate;

            // These values resolve how much to change the view rectangle by according to the motion vector
            var panChange = (int)(ViewRect.Width() * rateX);
            var tiltChange = (int)(ViewRect.Height() * rateY);
            var zoomChange = ZoomLevel * rateZ;

            // Update the view rectangle
            Zoom(panChange, tiltChange, ZoomLevel + zoomChange);
        }

        /// <summary>
        /// Moves the frame by the amount specified in <see cref="translation" /> Calculates an
        /// array of rect points to translate smoothly to the target using a logistic smoothing function.
        /// </summary>
        /// <param name="translation">a point indicating the amount and direction to move the frame</param>
        private void SmoothTranslation(System.Drawing.Point translation)
        {
            Task.Run(() =>
            {
                lock (MotionLock)
                {
                    // calculate the new target location and the steps to transition there
                    var targetFrame = new Rect(ViewRect);
                    targetFrame.Offset(translation.X, translation.Y);
                    targetFrame.Restrain(previewRect);
                    var stepsLeft = CalculateSteps(ViewRect.Left, targetFrame.Left, transitionFrames, EaseOut);
                    var stepsTop = CalculateSteps(ViewRect.Top, targetFrame.Top, transitionFrames, EaseOut);

                    for (int i = 0; i < transitionFrames; i++)
                    {
                        var startTime = DateTime.Now;

                        // perform the zoom action to transition the view rectangle to the destination
                        Zoom(new Rect(stepsLeft[i], stepsTop[i], stepsLeft[i] + targetFrame.Width(), stepsTop[i] + targetFrame.Height()));

                        // calculate how long it took to do the zoom, and if it was less than the
                        // time between frames delay for the difference
                        var delay = TimeSpan.FromMilliseconds(transitionTime / transitionFrames) - (DateTime.Now - startTime);
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Calculates zoom values into array and steps through each value using a logistic function
        /// to smooth the zoom.
        /// </summary>
        /// <param name="multiplier">The amount to zoom</param>
        private void SmoothZoom(float stepAmount)
        {
            Task.Run(() =>
            {
                lock (MotionLock)
                {
                    // calculate the new zoomlevel and steps to transition there
                    var level = (float)(ZoomLevel + stepAmount).Clamp(MinZoom, MaxZoom);
                    var steps = CalculateSteps((float)ZoomLevel, level, transitionFrames, EaseOut);

                    for (int i = 0; i < transitionFrames; i++)
                    {
                        var startTime = DateTime.Now;

                        // perform the zoom action to transition the view rectangle to the destination
                        Zoom(steps[i]);

                        // calculate how long it took to do the zoom, and if it was less than the
                        // time between frames delay for the difference
                        var delay = TimeSpan.FromMilliseconds(transitionTime / transitionFrames) - (DateTime.Now - startTime);
                        if (delay > TimeSpan.Zero)
                        {
                            Thread.Sleep(delay);
                        }
                    }
                };
            });
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="SubCMediaCaptureSdkHandler.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Rtsp
{
    using Android.OS;
    using Android.Util;
    using static Veg.Mediacapture.Sdk.MediaCapture;

    /// <summary>
    /// This is a class that handles messages and other logic from the MediaCaptureSDK,
    /// it inherits from <see cref="Handler"/> and implements <see cref="IMediaCaptureSdkHandler"/>.
    /// </summary>
    public class SubCMediaCaptureSdkHandler : Handler, IMediaCaptureSdkHandler
    {
        /// <summary>
        /// The tag to be used when logging information to logcat.
        /// </summary>
        private const string Tag = "MediaCaptureSdkHandler";

        /// <summary>
        /// The <see cref="IMediaCapturer"/> that captures the information for the
        /// MediaCaptureSDK.
        /// </summary>
        private IMediaCapturer capturer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCMediaCaptureSdkHandler"/> class.
        /// </summary>
        /// <param name="capturer">The <see cref="IMediaCapturer"/> the capture the information.</param>
        public SubCMediaCaptureSdkHandler(IMediaCapturer capturer)
        {
            this.capturer = capturer;
        }

        /// <summary>
        /// The logic that handles messages coming in and logs them to the logcat.
        /// </summary>
        /// <param name="msg">The <see cref="Message"/></param>
        public override void HandleMessage(Message msg)
        {
            CaptureNotifyCodes status = (CaptureNotifyCodes)msg.Obj;
            string strText = null;

            // Get the status from the Message and check it against multiple possibilites.
            if (status.Equals(CaptureNotifyCodes.CapOpened))
            {
                strText = "Opened";
            }
            else if (status.Equals(CaptureNotifyCodes.CapSurfaceCreated))
            {
                strText = "Camera surface created surfaceView=" + capturer.SurfaceView;
            }
            else if (status.Equals(CaptureNotifyCodes.CapSurfaceDestroyed))
            {
                strText = "Camera surface destroyed";
            }
            else if (status.Equals(CaptureNotifyCodes.CapStarted))
            {
                strText = "Started";
            }
            else if (status.Equals(CaptureNotifyCodes.CapStopped))
            {
                strText = "Stopped";
            }
            else if (status.Equals(CaptureNotifyCodes.CapClosed))
            {
                strText = "Closed";
            }
            else if (status.Equals(CaptureNotifyCodes.CapError))
            {
                strText = "Error";
            }

            if (strText != null)
            {
                // Log the info to logcat.
                Log.Info(Tag, "=Status handleMessage str=" + strText);
            }
        }

        /// <summary>
        /// Sets the capturer source for the image data.
        /// </summary>
        /// <param name="capturer">The <see cref="IMediaCapturer"/> that will capture the
        /// image data.</param>
        public void SetCapturer(IMediaCapturer capturer)
        {
            this.capturer = capturer;
        }
    }
}
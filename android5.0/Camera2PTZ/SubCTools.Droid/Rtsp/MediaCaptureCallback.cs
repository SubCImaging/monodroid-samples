//-----------------------------------------------------------------------
// <copyright file="MediaCaptureCallback.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Rtsp
{
    using Android.OS;
    using Android.Util;
    using Java.Nio;
    using Veg.Mediacapture.Sdk;
    using static Veg.Mediacapture.Sdk.MediaCapture;

    /// <summary>
    /// Media capture callback class.
    /// </summary>
    public class MediaCaptureCallback : Java.Lang.Object, IMediaCaptureCallback
    {
        /// <summary>
        /// The tag to use for logging to logcat.
        /// </summary>
        private readonly string tag = typeof(MediaCaptureCallback).Name;

        /// <summary>
        /// The <see cref="IMediaCaptureSdkHandler"/> to 
        /// </summary>
        private readonly IMediaCaptureSdkHandler handler;

        /// <summary>
        /// The last received message containing the capture status.
        /// </summary>
        private int oldMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCaptureCallback"/> class.
        /// </summary>
        /// <param name="handler">The <see cref="IMediaCaptureSdkHandler"/> to use.</param>
        public MediaCaptureCallback(IMediaCaptureSdkHandler handler)
        {
            this.handler = handler;
        }

        /// <summary>
        /// Logs the information from the OnCaptureReceiveData.
        /// </summary>
        /// <param name="buffer">The <see cref="ByteBuffer"/> that contains the data</param>
        /// <param name="type">The type of data</param>
        /// <param name="size">The size of the data</param>
        /// <param name="pts">The total data of the stream</param>
        /// <returns>Return code</returns>
        public int OnCaptureReceiveData(ByteBuffer buffer, int type, int size, long pts)
        {
            Log.Verbose(tag, "=OnCaptureReceiveData buffer=" + buffer + " type=" + type + " size=" + size + " pts=" + pts);
            return 0;
        }

        /// <summary>
        /// The method that gets called when the capture status changes.
        /// </summary>
        /// <param name="arg">The <see cref="CaptureNotifyCodes"/> status</param>
        /// <returns>Return code</returns>
        public int OnCaptureStatus(int arg)
        {
            CaptureNotifyCodes status = CaptureNotifyCodes.ForValue(arg);

            if (status == null)
            {
                return 0;
            }

            // Removes the previous message from the handler and send the current capture status.
            Message msg = new Message();
            msg.Obj = status;
            handler.RemoveMessages(oldMessage);
            oldMessage = msg.What;
            handler.SendMessage(msg);

            return 0;
        }
    }
}
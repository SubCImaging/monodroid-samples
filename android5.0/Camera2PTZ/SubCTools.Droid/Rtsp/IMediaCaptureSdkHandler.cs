//-----------------------------------------------------------------------
// <copyright file="IMediaCaptureSdkHandler.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Rtsp
{
    using Android.OS;

    /// <summary>
    /// This is a <see cref="interface"/> that handles messages and other logic
    /// from the MediaCaptureSDK.
    /// </summary>
    public interface IMediaCaptureSdkHandler
    {
        /// <summary>
        /// The logic that handles messages coming in and logs them to the logcat.
        /// </summary>
        /// <param name="msg">The <see cref="Message"/></param>
        void HandleMessage(Message msg);

        /// <summary>
        /// Remove any pending posts of messages with code 'what' that are in the
        /// message queue.
        /// </summary>
        /// <param name="what">Unknown, Xamarin says "To be added."</param>
        void RemoveMessages(int what);

        /// <summary>
        /// Pushes a message onto the end of the message queue after all pending
        /// messages before the current time.  It will be received in
        /// Android.OS.Handler.HandleMessage(Android.OS.Message), in the thread
        /// attached to this handler.
        /// </summary> 
        /// <param name="msg">The <see cref="Message"/> to send.</param>
        /// <returns>Unknown, Xamarin says "To be added."</returns>
        bool SendMessage(Message msg);

        /// <summary>
        /// Sets the capturer source for the image data.
        /// </summary>
        /// <param name="capture">The <see cref="IMediaCapturer"/> that will capture the
        /// image data.</param>
        void SetCapturer(IMediaCapturer capture);
    }
}
//-----------------------------------------------------------------------
// <copyright file="IMediaCapturer.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Rtsp
{
    using Android.Content;
    using Android.Views;
    using Veg.Mediacapture.Sdk;
    using static Veg.Mediacapture.Sdk.MediaCapture;

    /// <summary>
    /// <see cref="interface"/> for the media capturers written primarily for use with the
    /// <see cref="RtspServer"/> object.  Should be compatible with the MediaCaptureSDK.
    /// </summary>
    public interface IMediaCapturer
    {
        /// <summary>
        /// Gets the <see cref="CaptureState"/>, this can be useful
        /// for checking if the <see cref="IMediaCapturer"/> has
        /// started capturing the stream or not(among other
        /// things).
        /// </summary>
        CaptureState State { get; }

        /// <summary>
        /// Gets the <see cref="MediaCaptureConfig"/> that is used
        /// for handling all the settings for the <see cref="IMediaCapturer"/>.
        /// </summary>
        MediaCaptureConfig Config { get; }

        /// <summary>
        /// Gets the <see cref="SurfaceView"/> of the <see cref="IMediaCapturer"/>.
        /// </summary>
        SurfaceView SurfaceView { get; }

        /// <summary>
        /// Call this when parent gets destroyed.
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// Open the capturer and return the status.
        /// </summary>
        /// <param name="config">The <see cref="MediaCaptureConfig"/>
        /// used to configure the capturer.</param>
        /// <param name="callback">The <see cref="IMediaCaptureCallback"/>
        /// that handles the callbacks from the <see cref="IMediaCapturer"/></param>
        /// <returns>Result code</returns>
        int Open(MediaCaptureConfig config, IMediaCaptureCallback callback);

        /// <summary>
        /// Ask the user for permission to obtain the image data.
        /// This could be asking to record the screen, open camera,
        /// etc.
        /// </summary>
        /// <param name="context">The <see cref="Context"/> required
        /// to make the permission request.</param>
        void RequestPermission(Context context);

        /// <summary>
        /// Set the permission request result from OnActivityReceived().
        /// </summary>
        /// <param name="resultCode">The result from the permission
        /// request</param>
        /// <param name="data">The <see cref="Intent"/></param>
        void SetPermissionRequestResults(int resultCode, Intent data);

        /// <summary>
        /// Start capturing the image data.
        /// </summary>
        void Start();

        /// <summary>
        /// Start transcoding the image data.
        /// </summary>
        void StartTranscoding();

        /// <summary>
        /// Stop capturing the image data.
        /// </summary>
        void Stop();

        /// <summary>
        /// Stop transcoding the image data.
        /// </summary>
        void StopTranscoding();
    }
}
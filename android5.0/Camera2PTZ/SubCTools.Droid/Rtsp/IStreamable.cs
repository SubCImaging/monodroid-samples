//-----------------------------------------------------------------------
// <copyright file="IStreamable.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Rtsp
{
    using System.Drawing;
    using Veg.Mediacapture.Sdk;

    /// <summary>
    /// A <see cref="interface"/> that should allow us to have different
    /// streamable types.
    /// </summary>
    public interface IStreamable
    {
        /// <summary>
        /// Gets a value indicating whether or not the stream is currently
        /// active(streaming).
        /// </summary>
        bool Streaming { get; }

        /// <summary>
        /// Gets the bitrate of the stream.
        /// </summary>
        int Bitrate { get; }

        /// <summary>
        /// Gets the resolution that the stream is streaming at.
        /// </summary>
        Size Resolution { get; }

        /// <summary>
        /// Start the streaming server.
        /// </summary>
        void StartStreaming();

        /// <summary>
        /// Stop the streaming server.
        /// </summary>
        void StopStreaming();

        /// <summary>
        /// Update the stream settings with a different resolution and bitrate.
        /// </summary>
        /// <param name="resolution">The
        /// <see cref="MediaCaptureConfig.CaptureVideoResolution"/> to set
        /// the resolution.</param>
        /// <param name="bitrate">The bitrate to set in kbit/s</param>
        void UpdateSettings(int width, int height, int bitrate);
    }
}
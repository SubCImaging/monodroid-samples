//-----------------------------------------------------------------------
// <copyright file="RtspServer.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Rtsp
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Java.Nio;
    using SubCTools.Attributes;
    using SubCTools.Droid.Converters;
    using SubCTools.Settings;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Veg.Mediacapture.Sdk;
    using static Veg.Mediacapture.Sdk.MediaCapture;
    using static Veg.Mediacapture.Sdk.MediaCaptureConfig;

    /// <summary>
    /// Instantiates a new instance of the <see cref="RtspServer"/> class.
    /// </summary>
    public class RtspServer : DroidBase, IStreamable
    {
        /// <summary>
        /// The default port to use for streaming if one isn't provided.
        /// </summary>
        private const int DefaultPort = 5540;

        private const string Tag = nameof(RtspServer);

        /// <summary>
        /// The valid range for available bitrates
        /// </summary>
        private readonly Android.Util.Range bitrateRange = new Android.Util.Range(64, 30_000);

        /// <summary>
        /// The data capturer that provides the information that is going to be
        /// the content of the stream.
        /// </summary>
        private readonly IMediaCapturer capturer;

        /// <summary>
        /// The <see cref="MediaConfigBuilder"/> that handles building the settings
        /// and settings them in the capturer.
        /// </summary>
        private readonly MediaConfigBuilder configBuilder;

        private readonly Dictionary<Size, CaptureVideoResolution> supportedResolutions
                                    = new Dictionary<Size, CaptureVideoResolution>()
        {
            { new Size(1920, 1080), CaptureVideoResolution.VR1920x1080 },
            { new Size(1280, 720), CaptureVideoResolution.VR1280x720 },

            // { new Size(176, 144), CaptureVideoResolution.VR176x144 },
            // { new Size(1920, 1200), CaptureVideoResolution.VR1920x1200 },
            // { new Size(320, 240), CaptureVideoResolution.VR320x240 },
            // { new Size(352, 288), CaptureVideoResolution.VR352x288 },
            // { new Size(3840, 2160), CaptureVideoResolution.VR3840x2160 },
            // { new Size(4096, 2160), CaptureVideoResolution.VR4096x2160 },
            // { new Size(640, 360), CaptureVideoResolution.VR640x360 },
            // { new Size(640, 480), CaptureVideoResolution.VR640x480 },
            // { new Size(720, 405), CaptureVideoResolution.VR720x405 },
            // { new Size(720, 480), CaptureVideoResolution.VR720x480 },
            // { new Size(720, 576), CaptureVideoResolution.VR720x576 },
            // { new Size(864, 486), CaptureVideoResolution.VR864x486 },
            { new Size(960, 540), CaptureVideoResolution.VR960x540 }
        };

        /// <summary>
        /// The bitrate to stream at in kbps.
        /// </summary>
        private int bitrate = 5_000;

        /// <summary>
        /// The resolution to stream at.
        /// </summary>
        private Size resolution = new Size(1920, 1080);

        /// <summary>
        /// Initializes a new instance of the <see cref="RtspServer"/> class.  It creates a RTSP
        /// server on Android which then streams.  The default port used to stream is 5540.
        /// </summary>
        /// <param name="capturer">The <see cref="IMediaCapturer"/> that provides the information
        /// to stream.</param>
        public RtspServer(IMediaCapturer capturer)
            : this(capturer, DefaultPort)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RtspServer"/> class.  It creates a RTSP
        /// server on Android which then streams.  The default port used to stream is 5540.
        /// </summary>
        /// <param name="capturer">The <see cref="IMediaCapturer"/> that provides the information
        /// to stream.</param>
        /// <param name="port">The port to server RTSP on</param>
        public RtspServer(IMediaCapturer capturer, int port)
        {
            this.capturer = capturer ?? throw new InvalidOperationException("Capturer cannot be null");

            // Instantiate a new instance of the config builder and load default settings
            configBuilder = new MediaConfigBuilder(capturer.Config);
            configBuilder.BuildDefaultSettings();

            LoadSettings();
        }

        /// <summary>
        /// Gets the bitrate of the stream.
        /// </summary>
        [Savable(nameof(RtspServer) + nameof(Bitrate))]
        public int Bitrate
        {
            get => bitrate;
            private set
            {
                Set(nameof(Bitrate), ref bitrate, value);
                OnNotify($"Stream{nameof(Bitrate)}:{Bitrate}");
            }
        }

        /// <summary>
        /// Gets the resolution that the stream is streaming at.
        /// </summary>
        [Savable(nameof(RtspServer) + nameof(Resolution))]
        public Size Resolution
        {
            get => resolution;
            private set
            {
                Set(nameof(Resolution), ref resolution, value);
                OnNotify($"Stream{nameof(Resolution)}:{Resolution.Width}x{Resolution.Height}");
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the stream is currently running.
        /// </summary>
        public bool Streaming => capturer.State == CaptureState.Started;

        /// <summary>
        /// Gets a collection of supported resolutions;
        /// </summary>
        [RemoteState(nameof(RtspServer))]
        public IEnumerable<string> SupportedStreamResolutions
            => supportedResolutions.Keys.Select(k => $"{k.Width}x{k.Height}");

        /// <summary>
        /// Starts the Rtsp stream,
        /// </summary>
        [RemoteCommand]
        public void StartStreaming()
        {
            capturer.Start();

            if (capturer.State == CaptureState.Started)
            {
                OnNotify(
                    "The stream has started.",
                    Messaging.Models.MessageTypes.Information);
            }
            else
            {
                OnNotify(
                    "There was a problem starting the stream",
                    Messaging.Models.MessageTypes.Error);
            }
        }

        /// <summary>
        /// Stops the Rtsp stream.
        /// </summary>
        [RemoteCommand]
        public void StopStreaming()
        {
            capturer.Stop();

            if (capturer.State == CaptureState.Stopped)
            {
                OnNotify(
                    "The stream has stopped.",
                    Messaging.Models.MessageTypes.Information);
            }
            else
            {
                OnNotify(
                    "There was a problem stopping the stream",
                    Messaging.Models.MessageTypes.Error);
            }
        }

        /// <summary>
        /// Updates the stream settings and restarts the stream.
        /// </summary>
        /// <param name="bitrate">The bitrate in kbit/s, for example if you want 15mbps enter 15_000</param>
        [Alias("UpdateStreamSettings")]
        [RemoteCommand]
        public void UpdateSettings(int width, int height, int bitrate)
        {
            var newResolution = new Size(width, height);

            // Before we do anything lets validate the input
            if (bitrate < (int)bitrateRange.Lower || bitrate > (int)bitrateRange.Upper)
            {
                // If the bitrate is invalid that's fine we'll just clamp it and let the user know
                // what happened and why.
                bitrate = (int)bitrateRange.Clamp(bitrate);
                OnNotify(
                    $"Stream bitrate was outside acceptable range ({bitrateRange}), clamping value to {bitrate}kbit/s",
                    Messaging.Models.MessageTypes.Warning);
            }

            if (!supportedResolutions.ContainsKey(newResolution))
            {
                // If the size isn't a supported resolution lets let the user know and exit since
                // defaulting isn't very useful because the stream is already working and the
                // user wants to change the size.
                var supportedResolutionString = string.Join(',', supportedResolutions.Keys);
                OnNotify(
                    $"{newResolution} is not a supported resolution, please see list of supported resolutions ->" +
                    $" {supportedResolutionString}", Messaging.Models.MessageTypes.Error);
                return;
            }

            // Looks good, lets stop the stream and update our config
            StopStreaming();
            var vxgResolution = supportedResolutions[newResolution];
            capturer.Config.VideoResolution = vxgResolution;
            capturer.Config.VideoBitrate = bitrate;

            // Restart the stream and update the state
            StartStreaming();
            Resolution = newResolution;
            Bitrate = bitrate;

            // Let the user know the stream has restarted
            OnNotify(
                $"Stream restarted with new settings {resolution} at {bitrate}kbps",
                Messaging.Models.MessageTypes.Information);

            OnNotify("Reboot:0", Messaging.Models.MessageTypes.CameraCommand);
        }
    }
}
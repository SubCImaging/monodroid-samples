//-----------------------------------------------------------------------
// <copyright file="MediaConfigBuilder.cs" company="SubCImaging">
//     Copyright (c) SubCImaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Rtsp
{
    using Veg.Mediacapture.Sdk;
    using static Veg.Mediacapture.Sdk.MediaCapture;
    using static Veg.Mediacapture.Sdk.MediaCaptureConfig;

    /// <summary>
    /// A class for building tings configurations for a <see cref="IMediaCapturer"/>.
    /// </summary>
    internal sealed class MediaConfigBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaConfigBuilder"/> class.
        /// </summary>
        /// <param name="config">The <see cref="MediaCaptureConfig"/> file that </param>
        internal MediaConfigBuilder(MediaCaptureConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Gets the <see cref="MediaCaptureConfig"/> that the class builds.
        /// </summary>
        internal MediaCaptureConfig Config { get; private set; }

        /// <summary>
        /// Static implicit operator for returning a <see cref="MediaCaptureConfig"/>.
        /// </summary>
        /// <param name="builder"><see cref="this"/>.</param>
        public static implicit operator MediaCaptureConfig(MediaConfigBuilder builder) => builder.Config;

        /// <summary>
        /// Builds the <see cref="MediaCaptureConfig"/> with the default tings used in the
        /// MediaCaptureSDK RTSP example.  A good quick up for streaming RTSP on port
        /// 5540 at rtsp://x.x.x.x:5540/ch0
        /// </summary>
        internal void BuildDefaultSettings()
        {
            var ncm = Config.CaptureMode;
            ncm = ~CaptureModes.PpModeAudio.Val() & ncm;

            // auto start and split by time
            var recordFlags = PlayerRecordFlags.ForType(PlayerRecordFlags.PpRecordAutoStart) |
                PlayerRecordFlags.ForType(PlayerRecordFlags.PpRecordSplitByTime);

            // 30 sec
            var recordSplitTime = ((recordFlags & PlayerRecordFlags.ForType(PlayerRecordFlags.PpRecordSplitByTime)) != 0) ? 30 : 0;

            var key = "-----BEGIN RSA PRIVATE KEY-----\n" +
                    "MIICXQIBAAKBgQC6pKw/FG9ujxEJb5kRTRAoFNBNO0B7DP9LJ/ME6KI2JAC8utDH\n" +
                    "uhaEOU/UIFl+0uW80+Nl/BBPf6s1vrPjcDfbrUGcW1DOjsvGwPEjtrmH4jalnj2N\n" +
                    "kyZ7VSin1vVUl0EBTiFGJf4aBfPl2RhLQ7WoG24jvPjgorfjTLzpnIRkIwIDAQAB\n" +
                    "AoGAa+j3mYT0JETdQcpfAmy+0Z2vDWgbkMlj9Q0E6aqi1pCcSIHuYfuKNyp3qBqI\n" +
                    "A9Zlc3ZCfF1vBLe4wlse4HmqIP9M9ee+PfEuhJhJrj+ETMzz3KPSHrlCHrbYIsan\n" +
                    "sBL7Buq6J+TIfQdll9rZrPfvdR2P8WX8KxX7IjUSFIwlGkkCQQD0JBe1o4Z4HmMJ\n" +
                    "DPUAbBmXuLRjqHdALmdz48EysIl6ffbtHbttbrBCGMhnzcK8ls1KX95inojm/7FA\n" +
                    "FsBROBMHAkEAw7WXselRy16NfJHhnZPnH9JrCDrY8PbgD1q2bIbZzzMj9a1gJoBq\n" +
                    "ZOgSXbi3Ck9ZvuGQtKAuaUWYXvp7RcmTBQJBANinBttLyFLkNGKduvWq+HMpl/sw\n" +
                    "TtMH2wp+vL3s57NqASyey+rq5UNilsV3VS7ibD9qIAFISpkjovoKtpPcvRUCQQC4\n" +
                    "jwgl29ypx1nwDnZQLsk3xivvT+eDnZyAflAoGidO8XBI354b0OAElqGzRl0+2MPO\n" +
                    "cVMQMzpxRhNCBj63+jatAkB9jvvcMlxLJYheEAQq0fcBHKNTPFIMyEt7aJh2sUTb\n" +
                    "jzV40Dt3ecGSigFYT8lmzNKN5m5kSU5AWumWkkQ+Fs98\n" +
                    "-----END RSA PRIVATE KEY-----";
            var cert = "-----BEGIN RSA PRIVATE KEY-----\n" +
                    "MIICXQIBAAKBgQC6pKw/FG9ujxEJb5kRTRAoFNBNO0B7DP9LJ/ME6KI2JAC8utDH\n" +
                    "uhaEOU/UIFl+0uW80+Nl/BBPf6s1vrPjcDfbrUGcW1DOjsvGwPEjtrmH4jalnj2N\n" +
                    "kyZ7VSin1vVUl0EBTiFGJf4aBfPl2RhLQ7WoG24jvPjgorfjTLzpnIRkIwIDAQAB\n" +
                    "AoGAa+j3mYT0JETdQcpfAmy+0Z2vDWgbkMlj9Q0E6aqi1pCcSIHuYfuKNyp3qBqI\n" +
                    "A9Zlc3ZCfF1vBLe4wlse4HmqIP9M9ee+PfEuhJhJrj+ETMzz3KPSHrlCHrbYIsan\n" +
                    "sBL7Buq6J+TIfQdll9rZrPfvdR2P8WX8KxX7IjUSFIwlGkkCQQD0JBe1o4Z4HmMJ\n" +
                    "DPUAbBmXuLRjqHdALmdz48EysIl6ffbtHbttbrBCGMhnzcK8ls1KX95inojm/7FA\n" +
                    "FsBROBMHAkEAw7WXselRy16NfJHhnZPnH9JrCDrY8PbgD1q2bIbZzzMj9a1gJoBq\n" +
                    "ZOgSXbi3Ck9ZvuGQtKAuaUWYXvp7RcmTBQJBANinBttLyFLkNGKduvWq+HMpl/sw\n" +
                    "TtMH2wp+vL3s57NqASyey+rq5UNilsV3VS7ibD9qIAFISpkjovoKtpPcvRUCQQC4\n" +
                    "jwgl29ypx1nwDnZQLsk3xivvT+eDnZyAflAoGidO8XBI354b0OAElqGzRl0+2MPO\n" +
                    "cVMQMzpxRhNCBj63+jatAkB9jvvcMlxLJYheEAQq0fcBHKNTPFIMyEt7aJh2sUTb\n" +
                    "jzV40Dt3ecGSigFYT8lmzNKN5m5kSU5AWumWkkQ+Fs98\n" +
                    "-----END RSA PRIVATE KEY-----\n" +
                    "-----BEGIN CERTIFICATE-----\n" +
                    "MIICATCCAWoCCQCkiVNSr0w0DDANBgkqhkiG9w0BAQsFADBFMQswCQYDVQQGEwJB\n" +
                    "VTETMBEGA1UECAwKU29tZS1TdGF0ZTEhMB8GA1UECgwYSW50ZXJuZXQgV2lkZ2l0\n" +
                    "cyBQdHkgTHRkMB4XDTE1MDcwODE2MjgzNVoXDTE2MDcwNzE2MjgzNVowRTELMAkG\n" +
                    "A1UEBhMCQVUxEzARBgNVBAgMClNvbWUtU3RhdGUxITAfBgNVBAoMGEludGVybmV0\n" +
                    "IFdpZGdpdHMgUHR5IEx0ZDCBnzANBgkqhkiG9w0BAQEFAAOBjQAwgYkCgYEAuqSs\n" +
                    "PxRvbo8RCW+ZEU0QKBTQTTtAewz/SyfzBOiiNiQAvLrQx7oWhDlP1CBZftLlvNPj\n" +
                    "ZfwQT3+rNb6z43A3261BnFtQzo7LxsDxI7a5h+I2pZ49jZMme1Uop9b1VJdBAU4h\n" +
                    "RiX+GgXz5dkYS0O1qBtuI7z44KK340y86ZyEZCMCAwEAATANBgkqhkiG9w0BAQsF\n" +
                    "AAOBgQAGOdhgYAd3LAV9xt8aYAEONMDivrlWxC849PX+PSh25mQXTPAsfEEP2\n" +
                    "4dWCxtkKaIMIRiYfcSCGqErtUVufB0jkwS+oE9/RIpmGFRh3zMH/NBsI4eNcjJwV\n" +
                    "R6G0eVEvNUdCPixHTYs/9VPzJ2MJgI+AsQPxC6/kg78SJAbcwA==\n" +
                    "-----END CERTIFICATE-----";

            // TODO: Remove values that might not be used IE audio?
            // TODO: Add consts for default values outside class, maybe in a file??
            Streaming(true)
                .CaptureMode(ncm)
                .ServerType(StreamerTypes
                .StreamTypeRtspServer)
                .AudioFormat(TypeAudioAac)
                .AudioBitrate(128)
                .AudioSamplingRate(44_100)
                .AudioChannels(2)
                .PrimaryUrl("rtsp://@:" + 5540)
                .SecondaryUrl(string.Empty)
                .VideoOrientation(0)
                .VideoFramerate(30)
                .VideoKeyFrameInterval(1)
                .VideoBitrate(4_000)
                .VideoBitrateMode(BitrateModeCbr)
                .VideoSecBitrateMode(BitrateModeCbr)
                .VideoResolution(CaptureVideoResolution.VR1920x1080)
                .SecondaryEnabled(false)
                .SecondaryRecord(false)
                .SecondaryRecordPrefix("secondary")
                .Recording(false)
                .RecordPath(string.Empty)
                .RecordFlags(recordFlags)
                .RecordSplitTime(recordSplitTime)
                .RecordSplitSize(10)
                .Transcoding(false)
                .TranscodingWidth(320)
                .TranscodingHeight(240)
                .TrandscodingFramerate(1)
                .TranscodingFormat(MediaCaptureConfig.TypeVideoRaw)
                .SecureStreaming(true, cert, key)
                .CaptureSource(CaptureSources.PpModeVirtualDisplay);
        }

        /// <summary>
        /// Sets the audio bitrate.
        /// </summary>
        /// <param name="bitrate">The audio bitrate to  in kbit/s</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder AudioBitrate(int bitrate)
        {
            Config.AudioBitrate = bitrate;
            return this;
        }

        /// <summary>
        /// Sets the number of audio channels in the stream.
        /// </summary>
        /// <param name="channels">The number of channels</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder AudioChannels(int channels)
        {
            Config.AudioChannels = channels;
            return this;
        }

        /// <summary>
        /// Sets the audio format for the audio stream.
        /// </summary>
        /// <param name="audioFormat">The audio format to use, values can be
        /// found in <see cref="MediaCaptureConfig"/>, an example would be
        /// <see cref="MediaCaptureConfig.TypeAudioAac"/></param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder AudioFormat(string audioFormat)
        {
            Config.AudioFormat = audioFormat;
            return this;
        }

        /// <summary>
        /// Sets the audio sampling rate in Hz.
        /// </summary>
        /// <param name="sampleRate">The desired sample rate in Hz</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder AudioSamplingRate(int sampleRate)
        {
            Config.AudioSamplingRate = sampleRate;
            return this;
        }

        /// <summary>
        /// Sets the capture mode for the stream.
        /// </summary>
        /// <param name="captureMode">The capture mode is a binary value
        /// that is the result of multiple bit wise operations.  To find more info
        /// you will need to look into the MediaCaptureSDK yourself.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder CaptureMode(int captureMode)
        {
            Config.CaptureMode = captureMode;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="CaptureSources"/> for the stream.
        /// </summary>
        /// <param name="source">The <see cref="CaptureSources"/> to use
        /// to capture information from the <see cref="IMediaCapturer"/></param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder CaptureSource(CaptureSources source)
        {
            Config.CaptureSource = source.Val();
            return this;
        }

        /// <summary>
        /// Sets the stream port using a default RTSP url for the primary url.
        /// </summary>
        /// <param name="port">The port value to use, 5540 recommended.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder PrimaryStreamPort(int port) => PrimaryUrl("rtsp://@:" + port);

        /// <summary>
        /// Sets the url for the primary stream.
        /// </summary>
        /// <param name="url">The url to use, example="rtsp://@:5540"</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder PrimaryUrl(string url)
        {
            Config.Url = url;
            return this;
        }

        /// <summary>
        /// Sets a <see cref="bool"/> representing whether or not recording
        /// is enabled.
        /// </summary>
        /// <param name="recording">Whether or not recording is enabled.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder Recording(bool recording)
        {
            Config.Recording = recording;
            return this;
        }

        /// <summary>
        /// Sets the flags for the recording.
        /// </summary>
        /// <param name="recordFlags">The record flags are the result of a bit
        /// wise operation in the SDK, for more information please refer to the
        /// MediaCaptureSDK documentation.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder RecordFlags(int recordFlags)
        {
            Config.RecordFlags = recordFlags;
            return this;
        }

        /// <summary>
        /// Sets the file path for the recordings.
        /// </summary>
        /// <param name="recordPath">The path to the folder to put the recordings.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder RecordPath(string recordPath)
        {
            Config.RecordPath = recordPath;
            return this;
        }

        /// <summary>
        /// Sets the split size for the recording.
        /// </summary>
        /// <param name="splitSize">Unknown if this bytes, MB or what.  Please fill this in if you find out!</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder RecordSplitSize(int splitSize)
        {
            Config.RecordSplitSize = splitSize;
            return this;
        }

        /// <summary>
        /// Sets the split time used for recording.
        /// </summary>
        /// <param name="recordSplitTime">Unknown if this seconds, minutes or what.  Please fill this in if you find out!</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder RecordSplitTime(int recordSplitTime)
        {
            Config.RecordSplitTime = recordSplitTime;
            return this;
        }

        /// <summary>
        /// Sets a <see cref="bool"/> indicating whether or not the secondary
        /// stream is enabled.
        /// </summary>
        /// <param name="secEnabled">Whether or not the secondary stream is enabled.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder SecondaryEnabled(bool secEnabled)
        {
            Config.UseSec = secEnabled;
            return this;
        }

        /// <summary>
        /// Sets a <see cref="bool"/> representing whether or not the secondary recording
        /// is enabled.
        /// </summary>
        /// <param name="secondaryRecord">Whether or not the secondary recording
        /// is enabled.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder SecondaryRecord(bool secondaryRecord)
        {
            Config.UseSecRecord = secondaryRecord;
            return this;
        }

        /// <summary>
        /// Sets the prefix for the secondary recording file.
        /// </summary>
        /// <param name="prefix">The string to prefix the file
        /// name on recording.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder SecondaryRecordPrefix(string prefix)
        {
            Config.RecordPrefixSec = prefix;
            return this;
        }

        /// <summary>
        /// Sets the url for the secondary stream.
        /// </summary>
        /// <param name="url">The url to use, example = "rtsp://@:5540"</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder SecondaryUrl(string url)
        {
            Config.UrlSec = url;
            return this;
        }

        /// <summary>
        /// Configures secure streaming as either enabled or disabled
        /// depending on the <see cref="bool"/> that is passed in.
        /// </summary>
        /// <param name="enabled">Whether or not secure streaming
        /// is enabled.</param>
        /// <param name="cert">The RSA certificate to use.</param>
        /// <param name="key">The RSA key to use.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder SecureStreaming(bool enabled, string cert, string key)
        {
            Config.SetSecureStreaming(enabled, cert, key);
            return this;
        }

        /// <summary>
        /// Sets the server type for the stream.
        /// </summary>
        /// <param name="serverType">The server type, example=<see cref="StreamerTypes.StreamTypeRtspServer"/></param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder ServerType(StreamerTypes serverType)
        {
            Config.StreamType = serverType.Val();
            return this;
        }

        /// <summary>
        /// Sets a <see cref="bool"/> representing whether or not
        /// streaming is enabled.
        /// </summary>
        /// <param name="streaming">Whether or not streaming is enabled.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder Streaming(bool streaming)
        {
            Config.Streaming = streaming;
            return this;
        }

        /// <summary>
        /// Sets a <see cref="bool"/> representing whether or not transcoding
        /// is enabled.
        /// </summary>
        /// <param name="transcoding">Whether or not transcoding is
        /// enabled</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder Transcoding(bool transcoding)
        {
            Config.Transcoding = transcoding;
            return this;
        }

        /// <summary>
        /// Sets the format for the transcoding output.
        /// </summary>
        /// <param name="format">The transcoding format to use,
        /// example=<see cref="MediaCaptureConfig.TypeVideoRaw"/></param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder TranscodingFormat(string format)
        {
            Config.TransFormat = format;
            return this;
        }

        /// <summary>
        /// Sets the framerate for the transcoding output.
        /// </summary>
        /// <param name="framerate">The framerate, example=30</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder TrandscodingFramerate(int framerate)
        {
            Config.TransFps = framerate;
            return this;
        }

        /// <summary>
        /// Sets the height for the transcoding output.
        /// </summary>
        /// <param name="height">A value representing the height, example=240</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder TranscodingHeight(int height)
        {
            Config.TransHeight = height;
            return this;
        }

        /// <summary>
        /// Sets the  width for the transcoding output.
        /// </summary>
        /// <param name="width">A value representing the width, example 320</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder TranscodingWidth(int width)
        {
            Config.TransWidth = width;
            return this;
        }

        /// <summary>
        /// Sets the bitrate value for the video stream.
        /// </summary>
        /// <param name="bitrate">The bitrate in kbit/s from 500-30_000</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder VideoBitrate(int bitrate)
        {
            Config.VideoBitrate = bitrate;
            return this;
        }

        /// <summary>
        /// Sets the bitrate mode for the video stream.
        /// </summary>
        /// <param name="bitrateMode">The bitrate mode to use, example <see cref="MediaCaptureConfig.BitrateModeCbr"/></param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder VideoBitrateMode(int bitrateMode)
        {
            Config.VideoBitrateMode = bitrateMode;
            return this;
        }

        /// <summary>
        /// Sets the framerate for the video stream.
        /// </summary>
        /// <param name="framerate">The framerate to set in frames per second, example 30</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder VideoFramerate(int framerate)
        {
            Config.VideoFramerate = framerate;
            return this;
        }

        /// <summary>
        /// Sets the key frame interval for the video stream.
        /// </summary>
        /// <param name="keyFrameInterval">The key frame interval in frames.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder VideoKeyFrameInterval(int keyFrameInterval)
        {
            Config.VideoKeyFrameInterval = keyFrameInterval;
            return this;
        }

        /// <summary>
        /// Sets the orientation for the video stream.
        /// </summary>
        /// <param name="orientation">The orientation value, 0=landscape.  I don't know
        /// what the other values are for but I would wager 1 is portrait since this is
        /// Android based.</param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder VideoOrientation(int orientation)
        {
            Config.SetvideoOrientation(orientation);
            return this;
        }

        /// <summary>
        /// Sets the video resolution for the video stream.
        /// </summary>
        /// <param name="videoResolution">The video resolution to use, an example
        /// would be <see cref="CaptureVideoResolution.VR1920x1080"/></param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder VideoResolution(CaptureVideoResolution videoResolution)
        {
            Config.VideoResolution = videoResolution;
            return this;
        }

        /// <summary>
        /// Sets the bitrate mode for the video stream.
        /// </summary>
        /// <param name="bitrateMode">The bitrate mode to use, an example
        /// would be <see cref="MediaCaptureConfig.BitrateModeCbr"/></param>
        /// <returns><see cref="this"/></returns>
        internal MediaConfigBuilder VideoSecBitrateMode(int bitrateMode)
        {
            Config.VideoSecBitrateMode = bitrateMode;
            return this;
        }
    }
}
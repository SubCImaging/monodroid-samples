//-----------------------------------------------------------------------
// <copyright file="SubCMediaRecorder.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Camera
{
    using Android.App;
    using Android.Content;
    using Android.Media;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubCMediaRecorder"/> class used to record audio and video.
    /// </summary>
    public class OldSubCMediaRecorder : MediaRecorder
    {
        /// <summary>
        /// The maximum number of attemps to prepare the <see cref="MediaRecorder"/> before throwing
        /// an error and giving up.
        /// </summary>
        private const int MaxPrepareAttempts = 5;

        /// <summary>
        /// The lock that keeps <see cref="Prepare()"/> from executing if it is already being called.
        /// This prevents multiple <see cref="Java.Lang.IllegalStateException"/>s.
        /// </summary>
        private static readonly object PrepareSync = new object();

        /// <summary>
        /// The maximum <see cref="TimeSpan"/> the <see cref="MediaRecorder"/> is allowed to
        /// hang for when <see cref="Prepare()"/> gets called.
        /// </summary>
        private readonly TimeSpan maxPrepare = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The video encoding bit rate for recording.
        /// </summary>
        private int bitRate;

        /// <summary>
        /// Video frame capture rate.
        /// </summary>
        private double captureRate;

        /// <summary>
        /// The frame rate of the video to be captured.
        /// </summary>
        private int frameRate;

        /// <summary>
        /// A <see cref="bool"/> representing whether or not the current instance of <see cref="SubCMediaRecorder"/> has been initialized.
        /// </summary>
        private bool initialized = false;

        /// <summary>
        /// The maximum duration (in ms) of the recording session.
        /// </summary>
        private int maxDuration;

        /// <summary>
        /// The maximum filesize (in bytes) of the recording session.
        /// </summary>
        private long maxFileSize;

        /// <summary>
        /// A <see cref="bool"/> representing whether or not <see cref="Prepare()"/> has completed successfully.
        /// </summary>
        private bool mediaRecorderPrepared = false;

        /// <summary>
        /// The path of the output file to be produced.
        /// </summary>
        private string outputFile;

        /// <summary>
        /// The format of the output file produced during recording.
        /// </summary>
        private Android.Media.OutputFormat outputFormat;

        /// <summary>
        /// A <see cref="bool"/> representing whether or not the current attempt to prepare has failed.
        /// </summary>
        private bool prepareFailed = false;

        /// <summary>
        /// The <see cref="Timer"/> that calls <see cref="Prepare_Failed()"/> when <see cref="Prepare()"/>
        /// takes longer than <see cref="maxPrepare"/>.
        /// </summary>
        private Timer prepareFailedTimer = new Timer();

        /// <summary>
        /// The video encoder to be used for recording
        /// </summary>
        private Android.Media.VideoEncoder videoEncoder;

        /// <summary>
        /// The width and height of the video to be captured.
        /// </summary>
        private Size videoSize;

        /// <summary>
        /// The video source to be used for recording.
        /// </summary>
        private Android.Media.VideoSource videoSource;

        /// <summary>
        /// Configures the <see cref="prepareFailedTimer"/> if <see cref="initialized"/> == <see cref="false"/>.
        /// </summary>
        public void Init()
        {
            if (initialized)
            {
                return;
            }

            prepareFailedTimer.AutoReset = false;
            prepareFailedTimer.Interval = maxPrepare.TotalMilliseconds;
            prepareFailedTimer.Enabled = false;
            prepareFailedTimer.Stop();
            prepareFailedTimer.Elapsed += (s, e) => Prepare_Failed();
            initialized = true;
        }

        /// <summary>
        /// Prepares the recorder to begin capturing and encoding data.
        /// </summary>
        public override void Prepare()
        {
            if (!TryPrepare())
            {
                throw new TimeoutException($"The SubCMediaRecorder could not prepare, exceeded {MaxPrepareAttempts} attempts.");
            }
        }

        /// <summary>
        /// Set video frame capture rate.
        /// </summary>
        /// <param name="fps">Video frame capture rate. </param>
        public override void SetCaptureRate(double fps)
        {
            captureRate = fps;
            base.SetCaptureRate(fps);
        }

        /// <summary>
        /// Sets the maximum duration (in ms) of the recording session.
        /// </summary>
        /// <param name="max_duration_ms">The maximum duration (in ms) of the recording session. </param>
        public override void SetMaxDuration(int max_duration_ms)
        {
            maxDuration = max_duration_ms;
            base.SetMaxDuration(max_duration_ms);
        }

        /// <summary>
        /// Sets the maximum filesize (in bytes) of the recording session.
        /// </summary>
        /// <param name="max_filesize_bytes">The maximum filesize (in bytes) of the recording session. </param>
        public override void SetMaxFileSize(long max_filesize_bytes)
        {
            maxFileSize = max_filesize_bytes;
            base.SetMaxFileSize(max_filesize_bytes);
        }

        /// <summary>
        /// Sets the path of the output file to be produced.
        /// </summary>
        /// <param name="path">The path of the output file to be produced. </param>
        public override void SetOutputFile(string path)
        {
            outputFile = path;
            base.SetOutputFile(path);
        }

        /// <summary>
        /// Sets the format of the output file produced during recording.
        /// </summary>
        /// <param name="output_format">The format of the output file produced during recording. </param>
        public override void SetOutputFormat([GeneratedEnum] Android.Media.OutputFormat output_format)
        {
            outputFormat = output_format;
            base.SetOutputFormat(output_format);
        }

        /// <summary>
        /// Sets the video encoder to be used for recording.
        /// </summary>
        /// <param name="video_encoder"> The video encoder to be used for recording. </param>
        public override void SetVideoEncoder([GeneratedEnum] Android.Media.VideoEncoder video_encoder)
        {
            videoEncoder = video_encoder;
            base.SetVideoEncoder(video_encoder);
        }

        /// <summary>
        /// Sets the video encoding bit rate for recording.
        /// </summary>
        /// <param name="bitRate">The video encoding bit rate for recording. </param>
        public override void SetVideoEncodingBitRate(int bitRate)
        {
            this.bitRate = bitRate;
            base.SetVideoEncodingBitRate(bitRate);
        }

        /// <summary>
        /// Sets the frame rate of the video to be captured.
        /// </summary>
        /// <param name="rate">The frame rate of the video to be captured. </param>
        public override void SetVideoFrameRate(int rate)
        {
            frameRate = rate;
            base.SetVideoFrameRate(rate);
        }

        /// <summary>
        /// Sets the width and height of the video to be captured.
        /// </summary>
        /// <param name="width">The width of the video to be captured. </param>
        /// <param name="height">The height of the video to be captured. </param>
        public override void SetVideoSize(int width, int height)
        {
            videoSize = new Size(width, height);
            base.SetVideoSize(width, height);
        }

        /// <summary>
        /// Sets the video source to be used for recording.
        /// </summary>
        /// <param name="video_source">The video source to be used for recording. </param>
        public override void SetVideoSource([GeneratedEnum] Android.Media.VideoSource video_source)
        {
            videoSource = video_source;
            base.SetVideoSource(video_source);
        }

        /// <summary>
        /// Attempts to prepare this <see cref="MediaRecorder"/>, if for any reason it fails it will retry
        /// a maximum of <see cref="MaxPrepareAttempts"/>.
        /// </summary>
        /// <returns>A <see cref="bool"/> representing whether or not the <see cref="MediaRecorder"/> successfully prepared.</returns>
        public bool TryPrepare()
        {
            var count = 0;

            while (count < MaxPrepareAttempts)
            {
                try
                {
                    prepareFailed = false;
                    prepareFailedTimer.Start();
                    base.Prepare();
                    prepareFailedTimer.Stop();
                    if (prepareFailed)
                    {
                        throw new Java.Lang.IllegalStateException();
                    }

                    return true;
                }
                catch (Java.Lang.IllegalStateException)
                {
                    Console.WriteLine("Prepare Failed.");
                    DroidSystem.ShellSync($"echo \"$(date)Prepare Failed {count + 1} time(s) > /storage/emulated/0/Logs/prepare_failures");
                    Reset();
                    SetupMediaRecorder();
                }

                count++;
            }

            return false;
        }

        public async Task<bool> TryStop(int timeout)
        {
            var task = new Task(() => Stop());
            if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
            {
                return true;
            }
            else
            {
                throw new TimeoutException($"SubCMediaRecorder:  Could not stop MediaRecorder in {timeout}ms");
            }
        }

        /// <summary>
        /// Calls <see cref="MediaRecorder.Reset()"/> and sets <see cref="prepareFailed"/> to <see cref="true"/>
        /// </summary>
        private void Prepare_Failed()
        {
            Task.Run(() =>
            {
                prepareFailed = true;
                Reset();
            });
        }

        /// <summary>
        /// If <see cref="TryPrepare()"/> fails and needs to reconfigure the <see cref="MediaRecorder"/> this will
        /// configure the <see cref="MediaRecorder"/> based off all the values that were last passed in.
        /// </summary>
        private void SetupMediaRecorder()
        {
            Reset();

            SetVideoSource(videoSource);
            SetOutputFormat(outputFormat);
            SetMaxDuration(maxDuration);
            SetOutputFile(outputFile);
            SetVideoEncodingBitRate(bitRate);
            SetVideoFrameRate(frameRate);
            SetCaptureRate(captureRate);
            SetVideoSize(videoSize.Width, videoSize.Height);
            SetVideoEncoder(videoEncoder);
            SetMaxFileSize(maxFileSize);
            prepareFailedTimer.Start();
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="SubCMediaRecorder.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Camera
{
    using Android.Media;
    using SubCTools.Enums;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading;

    public class SubCMediaRecorder : Android.Media.MediaRecorder, IMediaRecorder
    {
        private const int TargetFramerate = 30;

        //private const long MaxFileDuration = 25_000_000_000l;

        private static readonly TimeSpan MinimumRecordingTime = TimeSpan.FromSeconds(4);
        private static readonly object OperationSync = new object();

        private static SubCMediaRecorder instance;

        private static DateTime startTime;
        private static MediaRecorderState state;

        private SubCMediaRecorder()
        {
            LogAccess("IDLE");
            state = MediaRecorderState.IDLE;
        }

        public static SubCMediaRecorder Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SubCMediaRecorder();
                }

                return instance;
            }
        }

        public FileInfo File { get; private set; }
        public MediaRecorderState State => state;

        public void Configure(
            Android.Media.VideoSource source,
            FileInfo file,
            Android.Media.VideoEncoder encoder,
            int bitrate,
            Size resolution,
            int maxFileSizeGB)
        {
            lock (OperationSync)
            {
                if (State != MediaRecorderState.IDLE)
                {
                    ThrowError("Configure", MediaRecorderState.IDLE, State);
                }

                Reset();

                SetVideoSource(source);

                SetOutputFormat(Android.Media.OutputFormat.Mpeg4);

                // 11 minutes
                SetMaxDuration(660000);

                SetOutputFile(file.FullName);

                SetVideoEncodingBitRate(bitrate * 1000000);

                SetVideoFrameRate(30);

                SetCaptureRate(30);

                SetVideoSize(resolution.Width, resolution.Height);

                SetVideoEncoder(encoder);

                SetMaxFileSize(SubCTools.Helpers.Numbers.GBToBytes(maxFileSizeGB));

                File = file;

                //SetVideoSource(source);
                //LogAccess("INITIAL");
                //state = MediaRecorderState.INITIAL;

                //SetOutputFormat(Android.Media.OutputFormat.Mpeg4);
                //LogAccess("INITIALIZED");
                //state = MediaRecorderState.INITIALIZED;

                //SetVideoEncoder(encoder);
                //SetMaxFileSize(SubCTools.Helpers.Numbers.GBToBytes(maxFileSizeGB));
                //SetOutputFile(file.FullName);
                //File = file;
                //SetVideoSize(resolution.Width, resolution.Height);
                //SetCaptureRate(TargetFramerate);
                //SetVideoEncodingBitRate(bitrate * 1000000);
                //LogAccess("DATASOURCE_CONFIGURED");
                state = MediaRecorderState.DATASOURCE_CONFIGURED;
            }
        }

        public void Init()
        {
            instance?.Release();
            instance = new SubCMediaRecorder();
        }

        public override void Prepare()
        {
            lock (OperationSync)
            {
                if (State != MediaRecorderState.DATASOURCE_CONFIGURED)
                {
                    ThrowError("Prepare", MediaRecorderState.DATASOURCE_CONFIGURED, State);
                }

                var sw = Stopwatch.StartNew();
                base.Prepare();
                sw.Stop();
                LogPrepareTime(sw.Elapsed);
                LogAccess("PREPARED");
                state = MediaRecorderState.PREPARED;
            }
        }

        public override void Start()
        {
            lock (OperationSync)
            {
                if (State != MediaRecorderState.PREPARED)
                {
                    ThrowError("Start", MediaRecorderState.PREPARED, State);
                }

                base.Start();
                LogAccess("RECORDING");
                state = MediaRecorderState.RECORDING;
                startTime = DateTime.Now;
            }
        }

        public override void Stop()
        {
            lock (OperationSync)
            {
                if (State != MediaRecorderState.RECORDING)
                {
                    ThrowError("Stop", MediaRecorderState.RECORDING, State);
                }

                if ((DateTime.Now - startTime) < MinimumRecordingTime)
                {
                    Console.WriteLine("Cannot stop recording with " +
                    $"duration less than {MinimumRecordingTime.TotalSeconds}s");
                    Thread.Sleep(MinimumRecordingTime);
                }

                LogAccess("IDLE-S");
                base.Stop();

                state = MediaRecorderState.IDLE;
            }
        }

        private void LogAccess(string method)
        {
            //DroidSystem.ShellSync($"echo $(date),{method} >> /data/local/tmp/recorder_access.csv");
        }

        private void LogPrepareTime(TimeSpan time)
        {
            //DroidSystem.ShellSync($"echo $(date),{time.TotalMilliseconds}, {DroidSystem.Instance.CPUTemp * 10} >> /data/local/tmp/prepare_new.csv");
        }

        private void ThrowError(string method, MediaRecorderState expectedState, MediaRecorderState actualState)
        {
            throw new InvalidOperationException($"Can't call {method}, the recorder must be " +
            $"in {expectedState} state, however it was in {actualState} state.");
        }
    }
}
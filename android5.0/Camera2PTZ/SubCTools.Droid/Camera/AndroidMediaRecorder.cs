//-----------------------------------------------------------------------
// <copyright file="SubCMediaRecorder.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Camera
{
    using Android.Media;
    using Android.Views;
    using SubCTools.Enums;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading;

    public interface IMediaRecorder
    {
        event EventHandler<MediaRecorder.ErrorEventArgs> Error;

        event EventHandler<MediaRecorder.InfoEventArgs> Info;

        FileInfo File
        {
            get;
        }

        Surface Surface { get; }

        void Configure(
            VideoSource source,
            FileInfo file,
            VideoEncoder encoder,
            int bitrate,
            Size resolution,
            int maxFileSizeGB);

        void Init();

        void Prepare();

        void Reset();

        void Start();

        void Stop();
    }
}
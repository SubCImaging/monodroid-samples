// <copyright file="IDiveRecorder.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using System;
    using System.IO;

    public interface IDiveRecorder
    {
        event EventHandler<FileInfo> Started;

        event EventHandler<Tuple<FileInfo, TimeSpan>> Stopped;

        TimeSpan VideoTime { get; }

        // string Directory { get; set; }
        // bool? IsStarted { get; }

        //TimeSpan RecordingDuration { get; }

        //string VideoName { get; set; }

        //void Stop();
    }
}
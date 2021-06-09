// <copyright file="IRecordingTimer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;

    public interface IRecordingTimer
    {
        event EventHandler<TimeSpan> RecordingDurationChanged;

        event EventHandler<TimeSpan> VideoLengthChanged;

        TimeSpan RecordingDuration { get; }

        TimeSpan VideoLength { get; }
    }
}

// <copyright file="IDiveLoggable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog.Interfaces
{
    using System;

    public interface IDiveLoggable
    {
        bool? IsStarted { get; }

        TimeSpan CurrentTime { get; }
    }
}

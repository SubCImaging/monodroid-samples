// <copyright file="IEventLogger.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid
{
    using SubCTools.DiveLog;
    using System.Collections.Generic;

    public interface IEventLogger
    {
        IEnumerable<DiveEntry> Events { get; }

        void LogEvent(string title);

        void LogEvent(string title, string description);
    }
}
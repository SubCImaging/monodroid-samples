// <copyright file="IDiveOpenable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog.Interfaces
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// An interface for use with openable divelogs.
    /// </summary>
    public interface IDiveOpenable
    {
        /// <summary>
        /// An event fired when a dive is opened;
        /// </summary>
        event EventHandler DiveOpened;

        /// <summary>
        /// Gets a flag that indicates if the dive is open, closed, or transitioning.
        /// </summary>
        bool? IsOpen { get; }

        /// <summary>
        /// Opens a dive file.
        /// </summary>
        /// <param name="file">The file to open.</param>
        /// <returns>A task for use with async await operations.</returns>
        Task OpenDiveAsync(FileInfo file);
    }
}

// <copyright file="VideoEntry.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using SubCTools.Attributes;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Data structure for a video entry.
    /// </summary>
    public class VideoEntry : Entry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoEntry"/> class.
        /// </summary>
        /// <param name="path">Path to the video.</param>
        /// <param name="creationDate">Date the video was created.</param>
        /// <param name="videoLength">Length of the video.</param>
        public VideoEntry(
            string path,
            DateTime creationDate,
            TimeSpan videoLength)
            : base(creationDate)
        {
            Path = path;
            VideoLength = videoLength;
        }

        /// <summary>
        /// Gets the data entries from the set.
        /// </summary>
        public List<DataEntry> DataEntries { get; } = new List<DataEntry>();

        /// <summary>
        /// Gets all the entries to the set.
        /// </summary>
        public List<DiveEntry> Entries { get; } = new List<DiveEntry>();

        /// <summary>
        /// Gets the path to the file.
        /// </summary>
        [PrimaryKey]
        public string Path { get; }

        /// <summary>
        /// Gets or sets the length of the video.
        /// </summary>
        public TimeSpan VideoLength { get; set; }
    }
}
// <copyright file="StillEntry.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using SubCTools.Attributes;
    using System;

    /// <summary>
    /// Data structure representing a still entry.
    /// </summary>
    public class StillEntry : Entry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StillEntry"/> class.
        /// </summary>
        /// <param name="path">Path to the still.</param>
        /// <param name="creationDate">Date the still was created.</param>
        public StillEntry(string path, DateTime creationDate)
            : base(creationDate)
        {
            Path = path;
        }

        /// <summary>
        /// Gets or sets the data entry present when the still was taken;
        /// </summary>
        public DataEntry Data { get; set; }

        /// <summary>
        /// Gets the path to the still.
        /// </summary>
        [PrimaryKey]
        public string Path { get; }
    }
}
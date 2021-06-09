// <copyright file="DiveEntry.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using System;

    /// <summary>
    /// Data structure representing a dive entry.
    /// </summary>
    public class DiveEntry : Entry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiveEntry"/> class.
        /// </summary>
        /// <param name="creationDate">Date the entry was created.</param>
        /// <param name="recordingTime">Time in the video recording the entry was created.</param>
        /// <param name="title">Title of the entry.</param>
        public DiveEntry(DateTime creationDate, TimeSpan recordingTime, string title, string description)
            : base(creationDate)
        {
            RecordingTime = recordingTime;
            Title = title;
            Description = description;
        }

        /// <summary>
        /// Gets or sets the data present at the time of the event.
        /// </summary>
        public DataEntry Data { get; set; }

        /// <summary>
        /// Gets or sets the description of the event.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the recording time in the video that the event occured.
        /// </summary>
        public TimeSpan RecordingTime { get; set; }

        /// <summary>
        /// Gets or sets the title of the dive entry.
        /// </summary>
        public string Title { get; set; }
    }
}
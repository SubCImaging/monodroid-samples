// <copyright file="DataEntry.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
namespace SubCTools.DiveLog
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An class that represents one entry in a data log file.
    /// Json converts log entries to and from objects of this type.
    /// </summary>
    public class DataEntry : Entry
    {
        private IEnumerable<string> fields;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataEntry"/> class.
        /// </summary>
        /// <param name="creationDate">Date the entry was created.</param>
        /// <param name="data">CSV data to create entry from.</param>
        public DataEntry(DateTime creationDate, string data)
            : base(creationDate)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("message", nameof(data));
            }

            Data = data;
            SplitCSV = data.Split(',').ToArray();
        }

        /// <summary>
        /// Gets the original CSV data.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// Gets the csv fields.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Fields => fields ?? (fields = SplitCSV.Skip(1));

        /// <summary>
        /// Gets the header of the data string.
        /// </summary>
        [JsonIgnore]
        public string Header => SplitCSV[0];

        /// <summary>
        /// Gets or sets the recording time.
        /// </summary>
        public TimeSpan RecordingTime { get; set; }

        /// <summary>
        /// Gets the original string split in to individual cells.
        /// </summary>
        public string[] SplitCSV { get; }

        /// <summary>
        /// Represent the object as a string.
        /// </summary>
        /// <returns>Data present on object.</returns>
        public override string ToString()
        {
            return Data;
        }
    }
}
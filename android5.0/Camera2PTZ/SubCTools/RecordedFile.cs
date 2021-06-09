// <copyright file="RecordedFile.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools
{
    using System;

    /// <summary>
    /// Class for holding information on a recorded file.
    /// </summary>
    public class RecordedFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecordedFile"/> class.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="videoLength">Length of the video.</param>
        public RecordedFile(string path, TimeSpan videoLength)
        {
            Path = path;
            VideoLength = videoLength;
        }

        /// <summary>
        /// Gets the path of the file.
        /// </summary>
        public string Path
        {
            get;
        }

        /// <summary>
        /// Gets the length of the video in seconds.
        /// </summary>
        public TimeSpan VideoLength
        {
            get;
        }
    }
}
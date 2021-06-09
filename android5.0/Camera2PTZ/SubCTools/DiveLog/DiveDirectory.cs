// <copyright file="DiveDirectory.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    ///
    /// </summary>
    public class DiveDirectory
    {
        /// <summary>
        /// Gets the children dive cirectories of the directory.
        /// </summary>
        public List<DiveDirectory> Children { get; } = new List<DiveDirectory>();

        /// <summary>
        /// Gets the directory on the file system.
        /// </summary>
        public DirectoryInfo Directory { get; }

        public bool IsData { get; set; }

        public bool IsStills { get; set; }

        public bool IsVideo { get; set; }

        public DiveDirectory Parent { get; set; }

        public void AddChild(DiveDirectory directory)
        {
            directory.Parent = this;
            Children.Add(directory);
        }
    }
}
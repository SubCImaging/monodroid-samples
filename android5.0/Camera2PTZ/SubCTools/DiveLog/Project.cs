// <copyright file="Project.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using SubCTools.Attributes;
    using System;
    using System.IO;

    /// <summary>
    /// Class representing a dive project for transmission to Inspector.
    /// </summary>
    public class Project
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Project"/> class.
        /// </summary>
        /// <param name="file">File location of the project.</param>
        public Project(FileInfo file)
        {
            File = file;
        }

        /// <summary>
        /// Gets the date the project was created.
        /// </summary>
        public DateTime CreationDate => File.CreationTime;

        /// <summary>
        /// Gets the path of the file.
        /// </summary>
        public FileInfo File { get; }

        /// <summary>
        /// Gets the full path to the project. Used as a primary Key.
        /// </summary>
        [PrimaryKey]
        public string FilePath => File.FullName;

        /// <summary>
        /// Gets or sets the last Accessed time.
        /// </summary>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        /// Gets the date of when the project was last modified.
        /// </summary>
        public DateTime LastModified => File.LastWriteTime;

        /// <summary>
        /// Gets the name of the project.
        /// </summary>
        public string Name => Path.GetFileNameWithoutExtension(File.Name);
    }
}
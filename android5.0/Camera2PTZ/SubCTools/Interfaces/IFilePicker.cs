// <copyright file="IFilePicker.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System.IO;

    /// <summary>
    /// Interface for picking files.
    /// </summary>
    public interface IFilePicker
    {
        /// <summary>
        /// Try to select a file to use.
        /// </summary>
        /// <param name="file">File output.</param>
        /// <param name="initialDirectory">Where to start looking on the filesystem.</param>
        /// <param name="filter">Any filters the picker uses.</param>
        /// <returns>True if a file was selected, false if cancelled.</returns>
        bool TrySelectFile(out FileInfo file, string initialDirectory, string filter);
    }
}
// <copyright file="IDiveCreator.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
namespace SubCTools.DiveLog.Interfaces
{
    using System.IO;

    /// <summary>
    /// An interface indicating the ability to create dive projects.
    /// </summary>
    public interface IDiveCreator
    {
        /// <summary>
        /// Create a dive.
        /// </summary>
        /// <param name="file">The dive to create.</param>
        void CreateDive(FileInfo file);
    }
}

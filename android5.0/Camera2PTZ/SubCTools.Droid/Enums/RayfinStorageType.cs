//-----------------------------------------------------------------------
// <copyright file="RayfinStorageType.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Enums
{
    /// <summary>
    /// <see cref="enum"/> that represents the various storage types for the Rayfin camera.
    /// </summary>
    public enum RayfinStorageType
    {
        /// <summary>
        /// The SD card "/mnt/expand/$(ls /mnt/expand)"
        /// </summary>
        SD = 0,

        /// <summary>
        /// The Internal mount point "/storage/emulated/0"
        /// </summary>
        Internal,

        /// <summary>
        /// The network attached mount point "/mnt/nas"
        /// </summary>
        NAS
    }
}
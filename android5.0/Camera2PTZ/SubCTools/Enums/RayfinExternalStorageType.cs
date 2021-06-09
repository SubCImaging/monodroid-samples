//-----------------------------------------------------------------------
// <copyright file="RayfinExternalStorageType.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Enums
{
    /// <summary>
    /// A collection of SD card configurations in the Rayfin.
    /// </summary>
    public enum RayfinExternalStorageType
    {
        /// <summary>
        /// This storage type indicates that the SD card
        /// is configured as a public storage system.
        /// This means that the card is formated as FAT32
        /// and is mounted in /mnt/media_rw/{mount
        /// name}.
        /// </summary>
        Public,

        /// <summary>
        /// This storage type indicates that the SD card
        /// is configured as a private storage system.
        /// This means that the card is formatted as
        /// EXT4 and is mounted at /mnt/expand/{guid}.
        /// </summary>
        Private,

        /// <summary>
        /// This storage type indicates that the SD card 
        /// is partitioned as both private and public 
        /// storage at the same time.  This means the 
        /// card is formatted as EXT4 and FAT32 at the 
        /// same time with mounts at both 
        /// /mnt/media_rw/{mount name} and 
        /// /mnt/expand/{guid}
        /// </summary>
        Mixed,
    }
}

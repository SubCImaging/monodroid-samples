//-----------------------------------------------------------------------
// <copyright file="VideoFormat.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Enums
{
    /// <summary>
    /// List of <see cref="enum"/>s for the <see cref="VideoFormat"/>
    /// Not sure of the exact refresh rates, 30 might be 29.97, 60 might be 59.94,
    /// and 24 might be 23.976, I'm not sure at the time of writing this (2018-07-17).
    /// </summary>
    public enum VideoFormat
    {
        /// <summary>
        /// VGA resolution of 640 pixels x 480 lines  updating at 60Hz
        /// </summary>
        VGA = 1,

        /// <summary>
        /// NTSC resolution of 720 pixels x 486 lines updating at 60Hz
        /// </summary>
        NTSC = 3,

        /// <summary>
        /// PAL resolution of 720 pixels x 576 lines updating at 50Hz
        /// </summary>
        PAL = 18,

        /// <summary>
        /// HD resolution of 1280 pixels x 720 lines updating at 50Hz
        /// </summary>
        HD50 = 19,

        /// <summary>
        /// HD resolution of 1280 pixels x 720 lines updating at 60Hz
        /// </summary>
        HD60 = 4,

        /// <summary>
        /// Full HD resolution of 1920 pixels x 1080 lines updating at 24Hz.
        /// </summary>
        FHD24 = 32,

        /// <summary>
        /// Full HD resolution of 1920 pixels x 1080 lines updating at 25Hz.
        /// </summary>
        FHD25 = 33,

        /// <summary>
        /// Full HD resolution of 1920 pixels x 1080 lines updating at 30Hz.
        /// </summary>
        FHD30 = 34,

        /// <summary>
        /// Full HD resolution of 1920 pixels x 1080 lines updating at 50Hz.
        /// </summary>
        FHD50 = 31,

        /// <summary>
        /// Full HD resolution of 1920 pixels x 1080 lines updating at 60Hz.
        /// This is the default output of the Rayfin in the current OS as of 2018-07-17
        /// </summary>
        FHD60 = 16,

        /// <summary>
        /// Ultra HD(QHD) resolution of  3840 pixels x 2160 lines updating at 24Hz.
        /// </summary>
        UHD24 = 130,

        /// <summary>
        /// Ultra HD(QHD) resolution of 3840 pixels x 2160 lines updating at 25Hz.
        /// </summary>
        UHD25 = 129,

        /// <summary>
        /// Ultra HD(QHD) resolution of 3840 pixels x 2160 lines updating at 30Hz.
        /// </summary>
        UHD30 = 128,

        /// <summary>
        /// DCI 4K resolution of 4096 pixels x 2160 lines updating at 24Hz.
        /// </summary>
        DCI4K24 = 98,

        /// <summary>
        /// DCI 4K resolution of 4096 pixels x 2160 lines updating at 25Hz
        /// </summary>
        DCI4K25 = 99,
    }
}
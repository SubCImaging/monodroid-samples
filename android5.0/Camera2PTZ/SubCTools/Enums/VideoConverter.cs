//-----------------------------------------------------------------------
// <copyright file="VideoConverter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Enums
{
    /// <summary>
    /// List of <see cref="VideoConverter"/>s contained in the Rayfin.
    /// </summary>
    public enum VideoConverter
    {
        /// <summary>
        /// Chrontel SD composite converter.
        /// </summary>
        Chrontel = 0,

        /// <summary>
        /// Blackmagic HD/3G-SDI converter.
        /// </summary>
        BlackmagicMicroHDMISDI = 1,

        /// <summary>
        /// Converter for optical fiber transmission.
        /// </summary>
        FiberOpticConverter = 2,
    }
}
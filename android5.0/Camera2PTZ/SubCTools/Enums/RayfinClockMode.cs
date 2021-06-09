//-----------------------------------------------------------------------
// <copyright file="RayfinClockMode.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Enums
{
    /// <summary>
    /// The clock mode to use in the Rayfin.
    /// </summary>
    public enum RayfinClockMode
    {
        /// <summary>
        /// The auto clockmode that syncs the 
        /// system clock with a NTP server to keep time in sync
        /// </summary>
        Auto = 0,

        /// <summary>
        /// The manual clockmode that allows the user to set 
        /// the clock manually
        /// </summary>
        Manual = 1,
    }
}

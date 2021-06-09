//-----------------------------------------------------------------------
// <copyright file="LensType.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Enums
{
    /// <summary>
    /// The various <see cref="LensType"/>s the Rayfin offers.
    /// </summary>
    public enum LensType
    {
        /// <summary>
        /// The water correcting standard lens for the Rayfin.
        /// </summary>
        LiquidOptics = 0,

        /// <summary>
        /// The ultra wide angle lens for the Rayfin.
        /// </summary>
        UltraOptics = 1,

        /// <summary>
        /// The power zoom lens for the Rayfin.
        /// </summary>
        Zoom = 2,
    }
}
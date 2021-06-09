// <copyright file="ColorsLibrary.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools
{
    using System.Collections.Generic;

    /// <summary>
    /// Class for holding colors for Overlay.
    /// </summary>
    public static class ColorsLibrary
    {
        /// <summary>
        /// Gets the applicable background colors.
        /// </summary>
        public static List<string> BGColors
        {
            get
            {
                var l = Colors;
                l.Add("Transparent");
                return l;
            }
        }

        /// <summary>
        /// Gets the applicable colors for font color.
        /// </summary>
        public static List<string> Colors => new List<string>()
        {
            "Black",
            "White",
            "Red",
            "Orange",
            "Yellow",
            "Lime",
            "Green",
            "Cyan",
            "Blue",
            "Purple",
        };
    }
}
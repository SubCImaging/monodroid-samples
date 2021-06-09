//-----------------------------------------------------------------------
// <copyright file="NullHelper.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Helpers
{
    using System.Linq;

    /// <summary>
    /// A <see cref="static"/> helper with functions to assist with 
    /// <see cref="null"/> <see cref="object"/>s.
    /// </summary>
    public static class NullHelper
    {
        /// <summary>
        /// Checks to see if any of the <see cref="object"/>s
        /// passed in are <see cref="null"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/>s to validate.</param>
        /// <returns>A <see cref="bool"/> representing whether or not any 
        /// of the <see cref="object"/>s are <see cref="null"/>.</returns>
        public static bool AreAnyNull(params object[] obj)
        {
            return obj.Any(o => o == null);
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="StructExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    /// <summary>
    /// Class used to extend the <see cref="struct"/>.
    /// </summary>
    public static class StructExtensions
    {
        /// <summary>
        /// Returns whether or not the <see cref="struct"/> is set to the <see cref="default"/> value.
        /// </summary>
        /// <typeparam name="T">The <see cref="typeof"/> <see cref="struct"/>.</typeparam>
        /// <param name="value">The <see cref="struct"/> to check if it's default.</param>
        /// <returns>A <see cref="bool"/> representing whether or not the <see cref="struct"/> is <see cref="default"/>.</returns>
        public static bool IsDefault<T>(this T value) where T : struct
        {
            var isDefault = value.Equals(default(T));

            return isDefault;
        }
    }
}

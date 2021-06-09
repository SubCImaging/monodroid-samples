//-----------------------------------------------------------------------
// <copyright file="DateTimeExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    using System;

    /// <summary>
    /// An extension class for the <see cref="DateTime"/> object.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Trims the milliseconds off the end, leaving whole seconds.
        /// </summary>
        /// <param name="dt">The time to be trimmed.</param>
        /// <returns>A new datetime the same as the old but with Milliseconds == 0.</returns>
        public static DateTime TrimMilliseconds(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0);
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="Numbers.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    using System;

    /// <summary>
    /// Extension class that does work on numbers.(Floats, Longs, etc.)
    /// </summary>
    public static class Numbers
    {
        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static float Clamp(this float value, float min = 0, float max = 100)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static uint Clamp(this uint value, uint min = 0, uint max = 100)
        {
            return (uint)Clamp((float)value, min, max);
        }

        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static double Clamp(this double value, double min = 0, double max = 100)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static int Clamp(this int value, int min = 0, int max = 100)
        {
            return (int)Clamp((float)value, min, max);
        }

        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static long Clamp(this long value, long min = 0, long max = 100)
        {
            return (long)Clamp((float)value, min, max);
        }

        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static TimeSpan Clamp(this TimeSpan value, TimeSpan? min, TimeSpan? max)
        {
            return TimeSpan.FromTicks(Clamp(value.Ticks, (min ?? TimeSpan.Zero).Ticks, (max ?? TimeSpan.MaxValue).Ticks));
        }
    }
}

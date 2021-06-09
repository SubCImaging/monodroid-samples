//-----------------------------------------------------------------------
// <copyright file="RegexExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Extensions
{
    using System;
    using System.Text.RegularExpressions;

    public static class RegexExtensions
    {
        /// <summary>
        /// Check to see if the supplied pattern is valid.
        /// </summary>
        /// <param name="pattern">Pattern to check.</param>
        /// <returns>True if the pattern is valid, false if it is not.</returns>
        public static bool IsValidPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            try
            {
                Regex.Match(string.Empty, pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check to see if the supplied pattern is valid.
        /// </summary>
        /// <param name="regex">Regex to test on.</param>
        /// <param name="pattern">Pattern to check.</param>
        /// <returns>True if the pattern is valid, false if it is not.</returns>
        public static bool IsValidPattern(this Regex regex, string pattern)
        {
            return IsValidPattern(pattern);
        }
    }
}

// <copyright file="ShutterSpeed.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using SubCTools.Extensions;
    using System;
    using System.Collections.Generic;

    public class ShutterSpeeds
    {
        /// <summary>
        /// Generate a collection of shutter speeds given a min and max exposure time.
        /// </summary>
        /// <param name="minExposure">Minimum exposure supported by the camera.</param>
        /// <param name="maxExposure">Maximum exposure supported by the camera.</param>
        /// <returns>Collection of common shutter speeds between the min and max exposure.</returns>
        public static IEnumerable<Fraction> Generate(long minExposure, long maxExposure)
        {
            var shortestExposure = maxExposure / 1_000_000_000L;
            var longestExposure = FractionExtensions.RealToFraction(minExposure / 1_000_000_000d).Denominator;
            var fractions = new Fraction[CountValues(shortestExposure) + 1 + CountValues(longestExposure)];
            BuildValues(longestExposure, shortestExposure, fractions);
            return Trim(fractions);
        }

        private static void BuildValues(long longestExposure, long shortestExposure, Fraction[] fractions)
        {
            var denominators = new int[CountValues(longestExposure)];
            var positiveValues = new int[CountValues(shortestExposure) + 2];
            var j = 0;

            for (var i = denominators.Length - 1; i >= 0; i--)
            {
                denominators[j] = (int)(Math.Pow(2, i + 1) - Math.Pow(2, i + 1) % (Convert.ToInt32(!(i > 2)) + (Convert.ToInt32(i > 2) * (5 * Math.Pow(10, (i - 3) / 4)))));
                j++;
            }

            if (positiveValues.Length > 0)
            {
                positiveValues[0] = 1;
            }

            for (var i = 0; i < positiveValues.Length - 1; i++)
            {
                positiveValues[i + 1] = (int)(Math.Pow(2, i + 1) - Math.Pow(2, i + 1) % (Convert.ToInt32(!(i > 2)) + (Convert.ToInt32(i > 2) * (5 * Math.Pow(10, (i - 3) / 4)))));
            }

            for (var i = 0; i < fractions.Length; i++)
            {
                if (i < denominators.Length)
                {
                    fractions[i] = new Fraction(1, denominators[i]);
                }
                else
                {
                    fractions[i] = new Fraction(positiveValues[i - denominators.Length], 1);
                }
            }
        }

        private static IEnumerable<Fraction> Trim(Fraction[] input)
        {
            var output = new Fraction[input.Length - 1];
            for (var i = 1; i < input.Length; i++)
            {
                output[i - 1] = input[i - 1];
            }

            return output;
        }

        private static int CountValues(double value)
        {
            var count = 0;
            while (value >= 1.953125)
            {
                count++;
                value /= 2;
            }

            return count;
        }
    }
}
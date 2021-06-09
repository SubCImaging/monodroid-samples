//-----------------------------------------------------------------------
// <copyright file="FractionExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    using System;

    /// <summary>
    /// Extension class for the <see cref="Fraction"/> object.
    /// </summary>
    public static class FractionExtensions
    {
        /// <summary>
        /// Converts a <see cref="double"/> to a <see cref="Fraction"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="error">The acceptable error in the conversion(0-1, 0.01 by default).</param>
        /// <returns>The <see cref="Fraction"/> result from the conversion.</returns>
        public static Fraction RealToFraction(this double value, double error = 0.01)
        {
            if (error <= 0.0 || error >= 1.0)
            {
                throw new ArgumentOutOfRangeException("error", "Must be between 0 and 1 (exclusive).");
            }

            var sign = Math.Sign(value);

            if (sign == -1)
            {
                value = Math.Abs(value);
            }

            if (sign != 0)
            {
                // error is the maximum relative error; convert to absolute
                error *= value;
            }

            var n = (int)Math.Floor(value);
            value -= n;

            if (value < error)
            {
                return new Fraction(sign * n, 1);
            }

            if (1 - error < value)
            {
                return new Fraction(sign * (n + 1), 1);
            }

            // The lower fraction is 0/1
            var lower_n = 0;
            var lower_d = 1;

            // The upper fraction is 1/1
            var upper_n = 1;
            var upper_d = 1;

            while (true)
            {
                // The middle fraction is (lower_n + upper_n) / (lower_d + upper_d)
                var middle_n = lower_n + upper_n;
                var middle_d = lower_d + upper_d;

                if (middle_d * (value + error) < middle_n)
                {
                    // real + error < middle : middle is our new upper
                    upper_n = middle_n;
                    upper_d = middle_d;
                }
                else if (middle_n < (value - error) * middle_d)
                {
                    // middle < real - error : middle is our new lower
                    lower_n = middle_n;
                    lower_d = middle_d;
                }
                else
                {
                    // Middle is our best fraction
                    return new Fraction(((n * middle_d) + middle_n) * sign, middle_d);
                }
            }
        }

        // public static Fraction ToFraction(this double number) => ToFraction(number, double.Epsilon);

        /// <summary>
        /// Converts a <see cref="Fraction"/> of a second to a <see cref="long"/> representing the total number of nanoseconds.
        /// </summary>
        /// <param name="f">The <see cref="Fraction"/> to convert.</param>
        /// <returns>A <see cref="long"/> representing the total number of nanoseconds.</returns>
        public static long ToNanoseconds(this Fraction f)
        {
            if (f.Numerator.Equals(0) || f.Denominator.Equals(0))
            {
                return 0;
            }
            else if (f.Denominator > 1)
            {
                var seconds = 1 / (double)f.Denominator;
                return (long)Math.Round(f.Numerator * (seconds * 1_000_000_000L), 0);
            }

            return f.Numerator * 1_000_000_000L;
        }

        /// <summary>
        /// Convert a <see cref="double"/> value to a fraction.
        /// </summary>
        /// <param name="value"><see cref="double"/> to convert to a <see cref="Fraction"/>.</param>
        /// <returns><see cref="Fraction"/> result from the conversion.</returns>
        public static Fraction ToFraction(this double value)
        {
            return value.Equals(0) ? Fraction.Zero : new Fraction(1, (int)Math.Round(1 / value, 0));
        }


        /// <summary>
        /// Convert a <see cref="double"/> value to a fraction.
        /// </summary>
        /// <param name="number">Input value.</param>
        /// <param name="accuracy">Acceptable accuracy of conversion.</param>
        /// <returns><see cref="Fraction"/> result from the conversion.</returns>
        public static Fraction ToFraction(this double number, double accuracy)
        {
            return Helper(number, accuracy, 10);
        }

        /// <summary>
        /// Convert a <see cref="double"/> value to a fraction.
        /// </summary>
        /// <param name="number">Input value.</param>
        /// <param name="accuracy">Acceptable accuracy of conversion.</param>
        /// <param name="passes">The max number of passes.</param>
        /// <returns><see cref="Fraction"/> result from the conversion.</returns>
        private static Fraction Helper(this double number, double accuracy, int passes)
        {
            if (number == 0 || passes == 0)
            {
                return Fraction.Zero;
            }
            else
            {
                var wholeNumber = (int)number;
                var decPart = number - wholeNumber;

                if (1 / number <= accuracy)
                {
                    return Fraction.Zero;
                }

                var wholeNumberFraction = new Fraction(wholeNumber, 1);
                var denominator = Helper(1 / decPart, accuracy, passes - 1);

                denominator = wholeNumberFraction + denominator;

                if (wholeNumber == 0)
                {
                    return denominator;
                }
                else
                {
                    return new Fraction(1, denominator);
                }
            }
        }
    }
}

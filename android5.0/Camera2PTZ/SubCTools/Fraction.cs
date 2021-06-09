//-----------------------------------------------------------------------
// <copyright file="Fraction.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Visual C# Kicks</author>
//-----------------------------------------------------------------------
namespace SubCTools
{
    using System;

    /// <summary>
    /// <see cref="Fraction"/> struct for various math on fractions as well as conversions.
    /// </summary>
    public struct Fraction
    {
        /// <summary>
        /// Represents a zero <see cref="Fraction"/>.
        /// </summary>
        public static Fraction Zero = new Fraction(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="Fraction"/> struct with a numerator and a denominator.
        /// </summary>
        /// <param name="numerator">The top number of a fraction.</param>
        /// <param name="denominator">The bottom number of a fraction.</param>
        public Fraction(int numerator, int denominator)
        {
            Numerator = numerator;
            Denominator = denominator;

            // If denominator negative...
            if (Denominator < 0)
            {
                // ...move the negative up to the numerator
                Numerator = -Numerator;
                Denominator = -Denominator;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fraction"/> struct with a numerator and a Fraction for the denominator.
        /// </summary>
        /// <param name="numerator">The top number of a fraction.</param>
        /// <param name="denominator">Denominator represented by a fraction.</param>
        public Fraction(int numerator, Fraction denominator)
        {
            // divide the numerator by the denominator fraction
            var f = new Fraction(numerator, 1) / denominator;

            Denominator = f.Denominator;
            Numerator = f.Numerator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fraction"/> struct with a Fraction and a denominator.
        /// </summary>
        /// <param name="numerator">Numerator represented by a fraction.</param>
        /// <param name="denominator">The bottom number of a fraction.</param>
        public Fraction(Fraction numerator, int denominator)
        {
            // multiply the numerator fraction by 1 over the denominator
            var f = numerator * new Fraction(1, denominator);
            Denominator = f.Denominator;
            Numerator = f.Numerator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fraction"/> struct from a existing <see cref="Fraction"/>.
        /// </summary>
        /// <param name="fraction">Fraction to use to create the new instance.</param>
        public Fraction(Fraction fraction)
        {
            Numerator = fraction.Numerator;
            Denominator = fraction.Denominator;
        }

        /// <summary>
        /// Gets the part of a <see cref="Fraction"/> that is below the line and that functions as the divisor of the numerator .
        /// </summary>
        public int Denominator { get; private set; }

        /// <summary>
        /// Gets the number above the line in a common <see cref="Fraction"/> showing how many of the parts indicated by the denominator are taken, for example, 2 in 2/3.
        /// </summary>
        public int Numerator { get; private set; }

        /// <summary>
        /// Returns the difference in two <see cref="Fraction"/>s.
        /// </summary>
        /// <param name="fraction1">fraction 1.</param>
        /// <param name="fraction2">fraction 2.</param>
        /// <returns>Difference in two <see cref="Fraction"/>s.</returns>
        public static Fraction operator -(Fraction fraction1, Fraction fraction2)
        {
            // Get Least Common Denominator
            var lcd = GetLCD(fraction1.Denominator, fraction2.Denominator);

            // Transform the fractions
            fraction1 = fraction1.ToDenominator(lcd);
            fraction2 = fraction2.ToDenominator(lcd);

            // Return difference
            return new Fraction(fraction1.Numerator - fraction2.Numerator, lcd).GetReduced();
        }

        /// <summary>
        /// Returns a bool representing the NOT equality of the two <see cref="Fraction"/>s.
        /// </summary>
        /// <param name="fraction1">fraction 1.</param>
        /// <param name="fraction2">fraction 2.</param>
        /// <returns>NOT equality of the two <see cref="Fraction"/>s.</returns>
        public static bool operator !=(Fraction fraction1, Fraction fraction2)
        {
            return fraction1.Numerator != fraction2.Numerator || fraction1.Denominator != fraction2.Denominator;
        }

        /// <summary>
        /// Multiplies two <see cref="Fraction"/>s.
        /// </summary>
        /// <param name="fraction1">fraction 1.</param>
        /// <param name="fraction2">fraction 2.</param>
        /// <returns>The product of the two <see cref="Fraction"/>s.</returns>
        public static Fraction operator *(Fraction fraction1, Fraction fraction2)
        {
            return new Fraction(fraction1.Numerator * fraction2.Numerator, fraction1.Denominator * fraction2.Denominator).GetReduced();
        }

        /// <summary>
        /// Returns the quotient of the two <see cref="Fraction"/>s.
        /// </summary>
        /// <param name="fraction1">fraction 1.</param>
        /// <param name="fraction2">fraction 2.</param>
        /// <returns>Quotient of the two <see cref="Fraction"/>s.</returns>
        public static Fraction operator /(Fraction fraction1, Fraction fraction2)
        {
            return new Fraction(fraction1 * fraction2.GetReciprocal()).GetReduced();
        }

        /// <summary>
        /// Returns the sum of the two <see cref="Fraction"/>s.
        /// </summary>
        /// <param name="fraction1">fraction 1.</param>
        /// <param name="fraction2">fraction 2.</param>
        /// <returns>The sum of the two <see cref="Fraction"/>s.</returns>
        public static Fraction operator +(Fraction fraction1, Fraction fraction2)
        {
            // Check if either fraction is zero
            if (fraction1.Denominator == 0)
            {
                return fraction2;
            }
            else if (fraction2.Denominator == 0)
            {
                return fraction1;
            }

            // Get Least Common Denominator
            var lcd = GetLCD(fraction1.Denominator, fraction2.Denominator);

            // Transform the fractions
            fraction1 = fraction1.ToDenominator(lcd);
            fraction2 = fraction2.ToDenominator(lcd);

            // Return sum
            return new Fraction(fraction1.Numerator + fraction2.Numerator, lcd).GetReduced();
        }

        /// <summary>
        /// Returns a bool representing the equality of the two <see cref="Fraction"/>s.
        /// </summary>
        /// <param name="fraction1">fraction 1.</param>
        /// <param name="fraction2">fraction 2.</param>
        /// <returns>The equality of the two <see cref="Fraction"/>s.</returns>
        public static bool operator ==(Fraction fraction1, Fraction fraction2)
        {
            return fraction1.Numerator == fraction2.Numerator && fraction1.Denominator == fraction2.Denominator;
        }

        /// <summary>
        /// Check to see if both objects are equal.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True if are equal.</returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Get the hash code.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Flip the numerator and denominator.
        /// </summary>
        /// <returns>The reciprocal or multiplicative inverse fraction.</returns>
        public Fraction GetReciprocal()
        {
            return new Fraction(Denominator, Numerator);
        }

        /// <summary>
        /// Reduces the <see cref="Fraction"/> to the lowest terms.
        /// </summary>
        /// <returns>A <see cref="Fraction"/> with the lowest terms.</returns>
        public Fraction GetReduced()
        {
            // Reduce the fraction to lowest terms
            var modifiedFraction = this;

            // While the numerator and denominator share a greatest common denominator,
            // keep dividing both by it
            var gcd = 0;
            while (Math.Abs(gcd = GetGCD(modifiedFraction.Numerator, modifiedFraction.Denominator)) != 1)
            {
                modifiedFraction.Numerator /= gcd;
                modifiedFraction.Denominator /= gcd;
            }

            // Make sure only a single negative sign is on the numerator
            if (modifiedFraction.Denominator < 0)
            {
                modifiedFraction.Numerator = -Numerator;
                modifiedFraction.Denominator = -Denominator;
            }

            return modifiedFraction;
        }

        /// <summary>
        /// Returns a equal <see cref="Fraction"/> with the same Denominator.
        /// </summary>
        /// <param name="targetDenominator">The target denominator to change the <see cref="Fraction"/> to.</param>
        /// <returns>Equal <see cref="Fraction"/> with the same Denominator.</returns>
        public Fraction ToDenominator(int targetDenominator)
        {
            // Multiply the fraction by a factor to make the denominator
            // match the target denominator
            var modifiedFraction = this;

            // Cannot reduce to smaller denominators
            if (targetDenominator < Denominator)
            {
                return modifiedFraction;
            }

            // The target denominator must be a factor of the current denominator
            if (targetDenominator % Denominator != 0)
            {
                return modifiedFraction;
            }

            if (Denominator != targetDenominator)
            {
                var factor = targetDenominator / Denominator;
                modifiedFraction.Denominator = targetDenominator;
                modifiedFraction.Numerator *= factor;
            }

            return modifiedFraction;
        }

        /// <summary>
        /// Returns the <see cref="Fraction"/> as a decimal.
        /// </summary>
        /// <returns>A double representing the value of the fraction.</returns>
        public double ToDouble()
        {
            return (double)Numerator / Denominator;
        }

        /// <summary>
        /// Returns a <see cref="string"/> representing the <see cref="Fraction"/>.
        /// </summary>
        /// <returns><see cref="string"/> representing the <see cref="Fraction"/>.</returns>
        public override string ToString()
        {
            return $"{Numerator}/{Denominator}";
        }

        /// <summary>
        /// Calculates the greatest common denominator.
        /// </summary>
        /// <param name="a">Integer a.</param>
        /// <param name="b">Integer b.</param>
        /// <returns>The GCD of the two integers.</returns>
        private static int GetGCD(int a, int b)
        {
            // Drop negative signs
            a = Math.Abs(a);
            b = Math.Abs(b);

            // Return the greatest common denominator between two integers
            while (a != 0 && b != 0)
            {
                if (a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }

            if (a == 0)
            {
                return b;
            }
            else
            {
                return a;
            }
        }

        /// <summary>
        /// Return the Least Common Denominator between two integers.
        /// </summary>
        /// <param name="a">Integer a.</param>
        /// <param name="b">Integer b.</param>
        /// <returns>Least Common Denominator between two integers.</returns>
        private static int GetLCD(int a, int b)
        {
            return a * b / GetGCD(a, b);
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="FractionToNanoSeconds.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------
namespace SubCTools.Converters
{
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Interfaces;
    using System;

    /// <summary>
    /// Converts a <see cref="Fraction"/> of a second to nanoseconds.
    /// </summary>
    public class FractionToNanoSeconds : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets contains the format of a <see cref="Fraction"/>.
        /// </summary>
        public string Format => "#/#";

        /// <summary>
        /// Try to convert the <see cref="Fraction"/> of a second to a <see cref="double"/> representing nanoseconds.
        /// </summary>
        /// <param name="data"><see cref="Fraction"/> to convert.</param>
        /// <param name="value">out <see cref="double"/> representing the nanoseconds.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            value = data;
            try
            {
                // convert the fraction to a double
                var d = SubCTools.Helpers.Numbers.FractionToDouble(data.ToString());

                // convert in to nano seconds
                value = d.SecondsToNano();
                return true;
            }
            catch (FormatException e)
            {
                // invalid fraction
                Console.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Tries to convert a <see cref="double"/> representing nanoseconds to a <see cref="Fraction"/>.
        /// </summary>
        /// <param name="data"><see cref="double"/> representing the nanoseconds.</param>
        /// <param name="value">out <see cref="Fraction"/> containing the converted <see cref="Fraction"/>.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            value = data;
            try
            {
                var seconds = Convert.ToDouble(data).NanoToSeconds();
                value = seconds.ToFraction();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attemps to convert a <see cref="string"/> containing nanoseconds to a <see cref="Fraction"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> containing nanoseconds.</param>
        /// <param name="convertedData">out <see cref="Fraction"/> containing the converted <see cref="Fraction"/>.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            var result = TryConvertBack(data, out convertedData);

            // convertedData = convertedData;
            return result;
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="StringToDateTime.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------
namespace SubCTools.Converters
{
    using SubCTools.Interfaces;
    using System;
    using System.Globalization;

    /// <summary>
    /// Converts a <see cref="string"/> to a <see cref="DateTime"/> and back.
    /// </summary>
    public class StringToDateTime : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets format for the <see cref="DateTime"/> string.
        /// </summary>
        public string Format => @"MM/dd/yyyy HH-mm-ss";

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="DateTime"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="value">out <see cref="DateTime"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            if (DateTime.TryParse(data.ToString(), out var result))
            {
                value = result;
                return true;
            }
            else if (DateTime.TryParseExact(data.ToString(), "MM/dd/yyyy HH-mm-ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
            {
                value = result;
                return true;
            }

            value = data;
            return false;
        }

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="DateTime"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="convertedData">out <see cref="DateTime"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            convertedData = data;
            return TryConvert((object)data, out convertedData);
        }

        /// <summary>
        /// Attemps to cast the input <see cref="object"/> as a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt cast on.</param>
        /// <param name="value">out <see cref="DateTime"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            value = (DateTime)data;
            return true;
        }
    }
}
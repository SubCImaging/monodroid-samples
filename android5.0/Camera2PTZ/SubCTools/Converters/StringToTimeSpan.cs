//-----------------------------------------------------------------------
// <copyright file="StringToTimeSpan.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------
namespace SubCTools.Converters
{
    using SubCTools.Interfaces;
    using System;

    /// <summary>
    /// Converts <see cref="string"/> to <see cref="TimeSpan"/>.
    /// </summary>
    public class StringToTimeSpan : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets format for the string to be converted.
        /// </summary>
        public string Format => "hh:mm:ss";

        /// <summary>
        /// Attempts to convert a <see cref="object"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt conversion.</param>
        /// <param name="value">out <see cref="TimeSpan"/> from the conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            value = data;

            if (!TimeSpan.TryParse(data.ToString(), out var result))
            {
                return false;
            }

            value = result;
            return true;
        }

        /// <summary>
        /// Attempts to convert a <see cref="string"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to attempt conversion.</param>
        /// <param name="convertedData">out <see cref="TimeSpan"/> from the conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            convertedData = data;
            return TryConvert((object)data, out convertedData);
        }

        /// <summary>
        /// Attempts to cast a <see cref="object"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt cast.</param>
        /// <param name="value">out <see cref="TimeSpan"/> from the cast.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            value = (TimeSpan)data;
            return true;
        }
    }
}

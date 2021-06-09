//-----------------------------------------------------------------------
// <copyright file="StringToVersion.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------
namespace SubCTools.Converters
{
    using SubCTools.Interfaces;
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Converts <see cref="string"/> to <see cref="Version"/>.
    /// </summary>
    public class StringToVersion : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets format for the string to be converted.
        /// </summary>
        public string Format => "vx.x.x";

        /// <summary>
        /// Attempts to convert a <see cref="string"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to attempt conversion.</param>
        /// <param name="convertedData">out <see cref="Version"/> from the conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            return TryConvert((object)data, out convertedData);
        }

        /// <summary>
        /// Attempts to convert a <see cref="object"/> to a <see cref="Version"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt conversion.</param>
        /// <param name="value">out <see cref="Version"/> from the conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            value = data;
            var v = Regex.Match(data.ToString(), @"v?(\d+\.\d+(\.\d+)?)\s?(\w+)?");
            if (v.Success)
            {
                value = new Version(v.Groups[1].Value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to cast a <see cref="object"/> to a <see cref="Version"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt conversion.</param>
        /// <param name="value">out <see cref="Version"/> from the conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            value = (Version)data;
            return true;
        }
    }
}

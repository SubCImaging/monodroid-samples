//-----------------------------------------------------------------------
// <copyright file="StringToRayfinClockMode.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Converters
{
    using SubCTools.Enums;
    using SubCTools.Interfaces;
    using System;

    /// <summary>
    /// Converts a <see cref="string"/> to a <see cref="RayfinClockMode"/> and back.
    /// </summary>
    public class StringToRayfinClockMode : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets format for the <see cref="RayfinClockMode"/> string.
        /// </summary>
        public string Format => @"Auto, Manual, etc.";

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="RayfinClockMode"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="value">out <see cref="RayfinClockMode"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            return TryConvert((string)data, out value);
        }

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="RayfinClockMode"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="convertedData">out <see cref="RayfinClockMode"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            var success = Enum.TryParse(data, out RayfinClockMode output);
            convertedData = output;
            return success;
        }

        /// <summary>
        /// Attemps to cast the input <see cref="object"/> as a <see cref="RayfinClockMode"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt cast on.</param>
        /// <param name="value">out <see cref="RayfinClockMode"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            value = (RayfinClockMode)data;
            return true;
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="StringToConnectionStatus.cs" company="SubC Imaging">
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
    /// Converts a <see cref="string"/> to a <see cref="ConnectionStatus"/> and back.
    /// </summary>
    public class StringToConnectionStatus : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets format for the <see cref="ConnectionStatus"/> string.
        /// </summary>
        public string Format => @"Online, Offline, etc.";

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="ConnectionStatus"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="value">out <see cref="ConnectionStatus"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            return TryConvert((string)data, out value);
        }

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="ConnectionStatus"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="convertedData">out <see cref="ConnectionStatus"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            var success = Enum.TryParse(data, out ConnectionStatus result);
            convertedData = result;
            return success;
        }

        /// <summary>
        /// Attemps to cast the input <see cref="object"/> as a <see cref="ConnectionStatus"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt cast on.</param>
        /// <param name="value">out <see cref="ConnectionStatus"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            value = (ConnectionStatus)data;
            return true;
        }
    }
}
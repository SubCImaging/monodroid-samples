//-----------------------------------------------------------------------
// <copyright file="StringToLensType.cs" company="SubC Imaging">
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
    /// Converts a <see cref="string"/> to a <see cref="LensType"/> and back.
    /// </summary>
    public class StringToLensType : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets format for the <see cref="LensType"/> string.
        /// </summary>
        public string Format => @"LiquidOptics, UltraOptics, etc.";

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="LensType"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="value">out <see cref="LensType"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            return TryConvert((string)data, out value);
        }

        /// <summary>
        /// Attemps to convert a <see cref="string"/> to a <see cref="LensType"/> using the specified <see cref="Format"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to convert.</param>
        /// <param name="convertedData">out <see cref="LensType"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            var success = Enum.TryParse(data, out LensType output);
            convertedData = output;
            return success;
        }

        /// <summary>
        /// Attemps to cast the input <see cref="object"/> as a <see cref="LensType"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt cast on.</param>
        /// <param name="value">out <see cref="LensType"/> result from conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            value = (LensType)data;
            return true;
        }
    }
}

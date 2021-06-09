// <copyright file="StringToVideoFormat.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Converters
{
    using SubCTools.Enums;
    using SubCTools.Interfaces;
    using System;

    public class StringToVideoFormat : IPropertyConverter, IConvert
    {
        /// <inheritdoc/>
        public string Format => "NTSC, FHD30, UHD30";

        /// <inheritdoc/>
        public bool TryConvert(object data, out object value)
        {
            value = data;

            if (string.IsNullOrEmpty(data?.ToString()))
            {
                return false;
            }

            if (Enum.TryParse(value.ToString(), out VideoFormat mode))
            {
                value = mode;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool TryConvert(string data, out object convertedData)
        {
            return TryConvert((object)data, out convertedData);
        }

        /// <inheritdoc/>
        public bool TryConvertBack(object data, out object value)
        {
            value = data?.ToString() ?? string.Empty;
            return true;
        }
    }
}
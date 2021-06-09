//-----------------------------------------------------------------------
// <copyright file="StringToUri.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Converters
{
    using SubCTools.Interfaces;
    using System;

    /// <summary>
    /// Converts <see cref="string"/> to a <see cref="Uri"/>.
    /// </summary>
    public class StringToUri : IPropertyConverter, IConvert
    {
        /// <summary>
        /// Gets the format for a <see cref="Uri"/>.
        /// </summary>
        public string Format => "x.x.x.x:xxxx/x/x, http://www.x.com, file:///X:/x/x/x.x";

        /// <summary>
        /// Attempts to convert a <see cref="object"/> to a <see cref="Uri"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt conversion.</param>
        /// <param name="value">out <see cref="Uri"/> from the conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(object data, out object value)
        {
            value = data;
            if (TryConvert((string)data, out var output))
            {
                value = output;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to convert a <see cref="string"/> to a <see cref="Uri"/>.
        /// </summary>
        /// <param name="data"><see cref="string"/> to attempt conversion.</param>
        /// <param name="convertedData">out <see cref="Uri"/> from the conversion.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvert(string data, out object convertedData)
        {
            convertedData = data;
            Uri uri;

            try
            {
                if (!(Uri.TryCreate(data, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + data, UriKind.Absolute, out uri)) && (uri?.Scheme == Uri.UriSchemeHttp || uri?.Scheme == Uri.UriSchemeHttps))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            if (uri.Host == string.Empty)
            {
                // Invalid address
                return false;
            }

            convertedData = uri;
            return true;
        }

        /// <summary>
        /// Attempts to cast a <see cref="object"/> to a <see cref="Uri"/>.
        /// </summary>
        /// <param name="data"><see cref="object"/> to attempt cast.</param>
        /// <param name="value">out <see cref="Uri"/> from the cast.</param>
        /// <returns><see cref="bool"/> representing the success of conversion.</returns>
        public bool TryConvertBack(object data, out object value)
        {
            try
            {
                value = ((Uri)data).Host;
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }
}

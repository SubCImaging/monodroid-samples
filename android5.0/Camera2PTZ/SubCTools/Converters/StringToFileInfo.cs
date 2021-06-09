//-----------------------------------------------------------------------
// <copyright file="StringToFileInfo.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------
namespace SubCTools.Converters
{
    using SubCTools.Interfaces;
    using System.IO;

    /// <summary>
    /// Converts a <see cref="string"/> to a <see cref="DateTime"/> and back.
    /// </summary>
    public class StringToFileInfo : IPropertyConverter, IConvert
    {
        /// <inheritdoc/>
        public string Format => "Valid directory";

        /// <inheritdoc/>
        public bool TryConvert(object data, out object value)
        {
            return TryConvert(data.ToString(), out value);
        }

        /// <inheritdoc/>
        public bool TryConvert(string data, out object value)
        {
            var file = new FileInfo(data);
            value = file;

            return true;
        }

        /// <inheritdoc/>
        public bool TryConvertBack(object data, out object value)
        {
            try
            {
                value = ((FileInfo)data).FullName;
            }
            catch
            {
                value = null;
                return false;
            }

            return true;
        }
    }
}

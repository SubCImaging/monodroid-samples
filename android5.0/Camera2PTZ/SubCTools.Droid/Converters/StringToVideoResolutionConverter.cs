//-----------------------------------------------------------------------
// <copyright file="StringToVideoResolutionConverter.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Converters
{
    using SubCTools.DataTypes;
    using SubCTools.Droid.Camera;
    using SubCTools.Interfaces;
    using System;
    using System.Drawing;
    using System.Linq;

    public class StringToVideoResolutionConverter : IPropertyConverter
    {
        private StringToSizeConverter baseConverter = new StringToSizeConverter();
        public string Format => string.Join(",", RecordingHandler.VideoResolutions);

        public bool TryConvert(object data, out object value)
        {
            if (!baseConverter.TryConvert(data, out value))
            {
                return false;
            }

            if (!RecordingHandler.VideoResolutions.Select(s => $"{s.Width}x{s.Height}").Contains($"{((Size)value).Width}x{((Size)value).Height}"))
            {
                return false;
            }

            return true;
        }

        public bool TryConvertBack(object data, out object value) => baseConverter.TryConvertBack(data, out value);
    }
}
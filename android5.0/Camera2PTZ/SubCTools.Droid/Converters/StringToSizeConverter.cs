namespace SubCTools.Droid.Converters
{
    using SubCTools.Interfaces;
    using System;
    using System.Drawing;
    using System.Text.RegularExpressions;

    public class StringToSizeConverter : IPropertyConverter
    {
        public string Format => "WidthxHeight";

        public bool TryConvert(object data, out object value)
        {
            var match = Regex.Match(data?.ToString() ?? string.Empty, @"{Width=(\d+), Height=(\d+)}");

            if (!match.Success)
            {
                match = Regex.Match(data?.ToString() ?? string.Empty, @"(\d+)(?:x|, )(\d+)");
            }

            value = match.Success ? new Size(Convert.ToInt32(match.Groups[1].Value), Convert.ToInt32(match.Groups[2].Value)) : data;

            return match.Success;
        }

        public bool TryConvertBack(object data, out object value)
        {
            var size = (Size)data;

            value = size != null ? $"{size.Width}x{size.Height}" : data;
            return data != null;
        }
    }
}
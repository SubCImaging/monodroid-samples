namespace SubCTools.Droid.Converters
{
    using Android.App;
    using Android.Content;
    using Android.Media;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Enums;
    using SubCTools.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class StringToEncoder : IPropertyConverter
    {
        public string Format => "H264,H265"; // string.Join(",", Enum.GetNames(typeof(VideoEncoder))) + ",H265";

        public bool TryConvert(object data, out object value)
        {
            if (data as string == "H265")
            {
                value = VideoEncoder.Hevc;
            }
            else
            {
                value = data;
            }

            if (string.IsNullOrEmpty(data?.ToString()))
            {
                return false;
            }

            if (Enum.TryParse(value.ToString(), out VideoEncoder mode))
            {
                value = mode;
                return true;
            }

            return false;
        }

        public bool TryConvertBack(object data, out object value)
        {
            if ((VideoEncoder)data == VideoEncoder.Hevc)
            {
                value = "H265";
            }
            else
            {
                value = data.ToString();
            }

            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubCTools.Droid.Enums;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SubCTools.Droid.Converters
{
    class StringToResolution
    {
        public string Format => "NTSC, PAL, FullHD";

        public bool TryConvert(object data, out object value)
        {
            value = data;

            if (string.IsNullOrEmpty(data?.ToString()))
            {
                return false;
            }

            var strData = data.ToString().ToLower();
            value = strData.ToUpper() == "NTSC" ? Resolution.NTSC : strData.ToUpper() == "PAL" ? Resolution.PAL : Resolution.FullHD;
            return true;
        }

        public bool TryConvertBack(object data, out object value)
        {
            value = data;
            value = (Resolution)data == Resolution.NTSC ? "NTSC" : (Resolution)data == Resolution.PAL ? "PAL" : "FullHD";
            return true;
        }        
    }
}
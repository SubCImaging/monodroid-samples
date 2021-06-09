using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Interfaces;
using Android.Media;

namespace SubCTools.Droid.Converters
{
    public class StringToOutputFormat : IPropertyConverter
    {
        public string Format => string.Join(",", Enum.GetNames(typeof(OutputFormat)));

        public bool TryConvert(object data, out object value)
        {
            value = data;

            if (string.IsNullOrEmpty(data?.ToString()))
            {
                return false;
            }

            if (Enum.TryParse(value.ToString(), out OutputFormat mode))
            {
                value = mode;
                return true;
            }

            return false;
        }

        public bool TryConvertBack(object data, out object value)
        {
            value = data.ToString();
            return true;
        }
    }
}
using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Converters
{
    public class StringToAWB : IPropertyConverter
    {
        public string Format => string.Join(",", Enum.GetNames(typeof(ControlAwbMode)));

        public bool TryConvert(object data, out object value)
        {
            value = data;

            int toIntValue;
            if (!Int32.TryParse(data.ToString(), out toIntValue)) toIntValue = 0;
            if (!Enum.IsDefined(typeof(ControlAwbMode), toIntValue)) return false;
            // if data is a number this will validate that it is defined in the enum
            // if data is a word it will evaluate to 0 and allow the below code to validate it

            if (string.IsNullOrEmpty(data?.ToString()))
            {
                return false;
            }

            if (Enum.TryParse(value.ToString(), out ControlAwbMode mode))
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
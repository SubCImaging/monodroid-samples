using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Enums;
using SubCTools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Converters
{
    public class StringToFlash : IPropertyConverter
    {
        public string Format => "On, Off, Lamp";

        public bool TryConvert(object data, out object value)
        {
            value = data;

            if (string.IsNullOrEmpty(data?.ToString()))
            {
                return false;
            }

            var strData = data.ToString().ToLower();
            value = strData == "on" ? FlashModes.On : strData == "lamp" ? FlashModes.Lamp : FlashModes.Off;
            //ControlAEMode.OnAlwaysFlash : ControlAEMode.On;
            return true;
        }

        public bool TryConvertBack(object data, out object value)
        {
            value = data;
            value = (FlashModes)data == FlashModes.On ? "On" : (FlashModes)data == FlashModes.Lamp ? "Lamp" : "Off";//(ControlAEMode)data == ControlAEMode.OnAlwaysFlash ? "On" : "Off";
            return true;
        }
    }
}
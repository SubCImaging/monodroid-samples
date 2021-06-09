using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Helpers;
using SubCTools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Converters
{
    public class RangeToString : IPropertyConverter
    {
        public string Format => "Range<T>";

        public bool TryConvert(object data, out object value)
        {
            value = data;
            var r = data as Range<Java.Lang.Object>;

            if (r != null)
            {
                value = r.Lower + "," + r.Lower;
            }

            return false;
        }

        public bool TryConvertBack(object data, out object value)
        {
            throw new NotImplementedException();
        }
    }
}
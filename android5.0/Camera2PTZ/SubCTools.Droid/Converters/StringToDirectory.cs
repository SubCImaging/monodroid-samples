using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Helpers;
using SubCTools.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Converters
{
    public class StringToDirectoryInfo : IPropertyConverter
    {
        public string Format => "Valid directory";

        public bool TryConvert(object data, out object value)
        {
            var file = new DirectoryInfo(data.ToString().RemoveIllegalPathCharacters(true));
            value = file;

            return true;
        }

        public bool TryConvertBack(object data, out object value)
        {
            try
            {
                value = ((DirectoryInfo)data).FullName;
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
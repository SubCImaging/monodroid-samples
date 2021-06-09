using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubCTools.Droid;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Xml.Linq;

namespace SubCTools.Droid.Helpers
{
    public static class Settings
    {
        const string settingsLocation = @"/storage/emulated/0/Settings/settings.xml";


        public static bool Replace(string tagName, string value)
        {
            try
            {
                var doc = XDocument.Load(settingsLocation);
                var tag = doc.Root.Element(tagName);
                if (tag != null)
                {
                    tag.Value = value;
                    doc.Save(settingsLocation);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            catch

            {
                return false;
            }
        }
    }
}
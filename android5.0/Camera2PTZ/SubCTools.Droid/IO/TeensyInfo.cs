using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.IO
{
    public class TeensyInfo
    {
        public string From { get; set; } = "";
        public string Type { get; set; } = "";
        public string Property { get; set; } = "";
        public string Value { get; set; } = "";

        public string Raw { get; set; } = string.Empty;

        public override string ToString() => $"From: {From} Type: {Type} Property {Property} Value: {Value}";
    }
}
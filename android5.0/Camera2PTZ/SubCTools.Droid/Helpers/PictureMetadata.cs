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

namespace SubCTools.Droid.Helpers
{
    public class PictureMetadata
    {
        public PictureMetadata(
            string iso,
            string shutter,
            string exposure,
            string time)
        {
            ISO = iso;
            Shutter = shutter;
            Exposure = exposure;
            Time = time;
        }

        public string ISO { get; }
        public string Shutter { get; }
        public string Exposure { get; }
        public string Time { get; }

        public override string ToString() => string.Join("\n", $"Time: {Time}", $"ISO: {ISO}", $"Shutter: {Shutter}", $"Exposure: {Exposure}");
    }
}
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
    public class AuxInput
    {
        public int Input { get; set; }
        public string Device { get; set; }
        public string Standard { get; set; }
        public int Baud { get; set; }

        public override string ToString()
            => string.Join("\n", $"{nameof(Device)}{Input}:{Device}", $"{nameof(Standard)}{Input}:{Standard}", $"{nameof(Baud)}{Input}:{Baud}");
    }
}
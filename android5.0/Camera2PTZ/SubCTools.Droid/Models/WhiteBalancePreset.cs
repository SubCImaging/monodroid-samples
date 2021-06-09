using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Util;
using Java.Lang;
using SubCTools.Attributes;
using SubCTools.Droid.Attributes;
using SubCTools.Droid.Converters;
using SubCTools.Droid.Helpers;
using SubCTools.Droid.Interfaces;
using SubCTools.Enums;
using SubCTools.Extensions;
using SubCTools.Helpers;
using SubCTools.Messaging.Models;
using SubCTools.Settings;
using SubCTools.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubCTools.Droid.Models
{
    public class WhiteBalancePreset
    {
        public string PresetName { get; set; }
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }

        public WhiteBalancePreset(float R, float G, float B, string PresetName)
        {
            this.R = R;
            this.G = G;
            this.B = B;
            this.PresetName = PresetName;
        }

        public static bool operator ==(WhiteBalancePreset p1, WhiteBalancePreset p2) => p1.Equals(p2);

        public static bool operator !=(WhiteBalancePreset p1, WhiteBalancePreset p2) => !p1.Equals(p2);

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var p = (WhiteBalancePreset)obj;
            return p.PresetName == PresetName && p.R == R && p.G == G && p.B == B;
        }

        public override int GetHashCode() => PresetName?.GetHashCode() ?? 0 + R.GetHashCode() + G.GetHashCode() + B.GetHashCode();

    }
}
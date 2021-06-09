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
    public class FocusResult
    {
        public bool IsFocusLocked { get; set; }
        public bool IsPreCaptureRequired { get; set; }

        public override string ToString() => $"{nameof(IsFocusLocked)}: {IsFocusLocked}, {nameof(IsPreCaptureRequired)}: {IsPreCaptureRequired}";
    }
}
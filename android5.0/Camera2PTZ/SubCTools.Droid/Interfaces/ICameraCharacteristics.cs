using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Interfaces
{
    public interface ICameraCharacteristics
    {
        T Get<T>(CameraCharacteristics.Key key) where T : Java.Lang.Object;
    }
}
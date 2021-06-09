using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Camera
{
    public class SubCCameraCharacteristics : ICameraCharacteristics
    {
        private readonly CameraCharacteristics characteristics;

        public SubCCameraCharacteristics(CameraCharacteristics characteristics)
        {
            this.characteristics = characteristics;
        }

        public T Get<T>(CameraCharacteristics.Key key) where T : Java.Lang.Object => (T)characteristics.Get(key);
    }
}
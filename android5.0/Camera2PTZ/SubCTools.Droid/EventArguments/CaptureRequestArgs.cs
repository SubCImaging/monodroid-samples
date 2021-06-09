using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Camera;
using SubCTools.Droid.Interfaces;
using SubCTools.Droid.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.EventArguments
{
    public class CaptureRequestArgs : EventArgs
    {
        public CaptureRequestArgs(ICaptureBuilder builder, ICameraCaptureListener listener)
        {
            Builder = builder;
            Listener = listener;
        }

        public ICaptureBuilder Builder { get; }
        public ICameraCaptureListener Listener { get; }
    }
}
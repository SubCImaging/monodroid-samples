using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Camera;
using SubCTools.Droid.EventArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Interfaces
{
    public interface IImageListener : ImageReader.IOnImageAvailableListener
    {
        event EventHandler<ImageAvailableEventArgs> ImageAvailable;
    }
}
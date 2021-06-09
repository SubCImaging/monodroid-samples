using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Java.IO;
using Java.Lang;
using SubCTools.Droid.Camera;
using SubCTools.Droid.EventArguments;
using SubCTools.Droid.Helpers;
using SubCTools.Droid.Interfaces;
using SubCTools.Messaging.Interfaces;
using SubCTools.Messaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubCTools.Droid.Listeners
{

    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener, IImageListener
    {
        public event EventHandler<ImageAvailableEventArgs> ImageAvailable;

        public void OnImageAvailable(ImageReader reader)
        {
            ImageAvailable?.Invoke(this, new ImageAvailableEventArgs(reader.AcquireLatestImage(), null));
        }
    }
}
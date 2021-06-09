using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Camera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Interfaces
{
    public interface IImageReaderGrabber
    {
        IImageReader NewInstance(int width, int height, ImageFormatType format, int maxImages);
    }
}
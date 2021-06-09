using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
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
    public interface IImageReader
    {
        int Width { get; }
        int Height { get; }
        ImageFormatType ImageFormat { get; }
        Surface Surface { get; }

        void Close();
        void SetOnImageAvailableListener(ImageReader.IOnImageAvailableListener listener, Handler handler);
        Image AcquireLatestImage();
    }
}
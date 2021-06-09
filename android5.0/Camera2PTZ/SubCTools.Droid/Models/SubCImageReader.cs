using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Camera;
using SubCTools.Droid.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Models
{
    public class SubCImageReader : IImageReader
    {
        private readonly ImageReader reader;

        public SubCImageReader(ImageReader reader)
        {
            this.reader = reader;
        }

        public int Width => reader.Width;

        public int Height => reader.Height;

        public ImageFormatType ImageFormat => reader.ImageFormat;

        public Surface Surface => reader.Surface;

        public Image AcquireLatestImage() => reader.AcquireLatestImage();

        public void Close()
        {
            reader.Close();
        }

        public void SetOnImageAvailableListener(ImageReader.IOnImageAvailableListener listener, Handler handler)
        {
            reader.SetOnImageAvailableListener(listener, handler);
        }
    }
}
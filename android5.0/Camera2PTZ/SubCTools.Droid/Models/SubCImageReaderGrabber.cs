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
    public class SubCImageReaderGrabber : IImageReaderGrabber
    {
        public IImageReader NewInstance(int width, int height, ImageFormatType format, int maxImages)
        {
            return new SubCImageReader(ImageReader.NewInstance(width, height, format, maxImages));
        }
    }
}
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.EventArguments
{
    public class ImageAvailableEventArgs : EventArgs
    {
        public ImageAvailableEventArgs(Image image, FileInfo file)
        {
            Image = image;
            File = file;
        }

        public Image Image { get; }
        public FileInfo File { get; }
    }
}
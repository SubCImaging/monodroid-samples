using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using Java.Util;
using SubCTools.Droid.Callbacks;
using SubCTools.Droid.Enums;
using SubCTools.Droid.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubCTools.Droid.Helpers
{

    internal class LoadBitmapThread : Java.Lang.Thread
    {
        private byte[] jpeg = null;
        private BitmapFactory.Options options = null;

        public LoadBitmapThread(BitmapFactory.Options options, byte[] jpeg)
        {
            this.options = options;
            this.jpeg = jpeg;
        }

        public Bitmap Bitmap { get; set; }

        public override void Run()
        {
            Bitmap = BitmapFactory.DecodeByteArray(jpeg, 0, jpeg.Length, options);
        }
    }
}
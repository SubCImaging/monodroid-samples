namespace SubCTools.Droid.Helpers
{
    using Android.App;
    using Android.Content;
    using Android.Hardware.Camera2;
    using Android.Hardware.Usb;
    using Android.Net.Nsd;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Droid.Camera;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.Listeners;
    using SubCTools.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Logger : ILogger
    {
        public async Task LogAsync(string data)
        {
            SubCLogger.Instance.Write(data, directory: Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies) + "/Log/");
        }

        public async Task LogAsync(string data, FileInfo file)
        {
            SubCLogger.Instance.Write(data, file.Name, file.DirectoryName);
        }
    }
}
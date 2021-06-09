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

    public class Shell : IShell
    {
        public string ShellSync(string command) => DroidSystem.ShellSync(command);
    }
}
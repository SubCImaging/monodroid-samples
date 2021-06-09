using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Helpers
{
    public class AndroidDiskSpaceMonitor : IDiskSpaceMonitor
    {
        private readonly Func<string> path;

        public AndroidDiskSpaceMonitor()
            : this(() => DroidSystem.BaseDirectory)
        {
        }

        public AndroidDiskSpaceMonitor(string path)
            : this(() => path)
        {
        }

        public AndroidDiskSpaceMonitor(Func<string> path)
        {
            this.path = path;
        }

        public double GetDiskSpaceRemaining()
        {
            var fs = new StatFs(path());
            var free = fs.AvailableBlocksLong * fs.BlockSizeLong;
            return free;
        }
    }
}
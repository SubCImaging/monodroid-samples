using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid
{
    public class DroidDevices
    {
        private readonly UsbManager manager;

        public DroidDevices(UsbManager manager)
        {
            this.manager = manager;
        }

        public string USBDevices => string.Join("\n", manager.DeviceList.Select(d => d.Value.DeviceName));
    }
}
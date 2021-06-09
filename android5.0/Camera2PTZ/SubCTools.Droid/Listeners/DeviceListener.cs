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

namespace SubCTools.Droid.Listeners
{
    public class DeviceListener : BroadcastReceiver
    {
        private readonly Context context;

        public DeviceListener(Context context)
        {
            this.context = context;

            context.RegisterReceiver(this, new IntentFilter(UsbManager.ActionUsbDeviceAttached));
            context.RegisterReceiver(this, new IntentFilter(UsbManager.ActionUsbDeviceDetached));
        }

        public event EventHandler<UsbDevice> DeviceAttached;
        public event EventHandler<UsbDevice> DeviceDetached;

        public void Unregister()
        {
            context.UnregisterReceiver(this);
        }

        public override void OnReceive(Context context, Intent intent)
        {
            var action = intent.Action;

            if (action.Equals(UsbManager.ActionUsbDeviceDetached)
                || action.Equals(UsbManager.ActionUsbDeviceAttached))
            {
                var d = (UsbDevice)
                    intent.Extras.Get(UsbManager.ExtraDevice);

                if (action == UsbManager.ActionUsbDeviceAttached)
                {
                    DeviceAttached?.Invoke(this, d);
                }
                else
                {
                    DeviceDetached?.Invoke(this, d);
                }


                Console.WriteLine($"{d}");

                //if (d.VendorId == MY_VENDOR_ID && d.getDeviceId() == MY_DEVICE_ID)
                //{
                //    // Your code here
                //}
            }
        }
    }
}
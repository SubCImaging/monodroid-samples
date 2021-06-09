using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.Widget;
using Hoho.Android.UsbSerial.Driver;
using Hoho.Android.UsbSerial.Util;
using SubCTools.Communicators;
using SubCTools.Communicators.DataTypes;
using SubCTools.Communicators.Interfaces;
using SubCTools.Messaging.Models;
using SubCTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubCTools.Droid.Communicators
{
    public class AndroidCommunicatorAddress : CommunicatorAddress
    {
        public AndroidCommunicatorAddress(IUsbSerialPort port, int baudRate)
        {
            Port = port;
            BaudRate = baudRate;

            Add(nameof(BaudRate), BaudRate.ToString());
            Add(nameof(Port), Port.ToString());
        }

        public int BaudRate { get; }
        public IUsbSerialPort Port { get; }
    }
}
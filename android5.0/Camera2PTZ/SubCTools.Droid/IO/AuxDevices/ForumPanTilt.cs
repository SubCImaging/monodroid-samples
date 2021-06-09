using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Communicators;
using SubCTools.Helpers;
using SubCTools.Messaging.Interfaces;
using SubCTools.Messaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubCTools.Droid.IO.AuxDevices
{
    public class ForumPanTilt : AuxDevice
    {
        public ForumPanTilt(
            AndroidSerial serial,
            int input,
            TeensyListener listener)
            : base(serial, input, listener)
        {
        }

        public static int Combine(byte b1, byte b2) => b1 << 8 | b2;

        private void Execute(string command, int speed)
        {
            Console.WriteLine("Information: " + command + ":" + speed);
            OnNotify(command + ":" + speed, MessageTypes.CameraCommand);
        }

        protected override void AuxDataReceived(AuxData e)
        {
            //var hexMatch = Regex.Match(e.Message, @"([A-Fa-f0-9]+)");
            //if (!hexMatch.Success)
            //{
            //    return;
            //}

            //var hexArray = new byte[0];

            //try
            //{
            //    hexArray = Strings.HexToByteArray(hexMatch.Groups[1].Value);
            //}
            //catch
            //{
            //    //OnNotify("Unable to convert hex string: " + e, MessageTypes.Error);
            //    return;
            //}

            var hexArray = e.Hex;

            int panSpeed = 0, tiltSpeed = 0;

            // you've got an associated speed
            if (hexArray.Length > 9)
            {
                panSpeed = (int)Math.Round(Combine(hexArray[6], hexArray[7]) / 262d * 100);
                tiltSpeed = (int)Math.Round(Combine(hexArray[8], hexArray[9]) / 262d * 100);
            }

            // you've got a pan tilt command
            if (hexArray.Length > 4)
            {
                var command = hexArray[4];

                switch (command)
                {
                    case 0:
                        OnNotify("StopPTZ", MessageTypes.CameraCommand);
                        break;
                    case 1:
                        Execute("PanLeft", panSpeed);
                        break;
                    case 2:
                        Execute("TiltUp", tiltSpeed);
                        break;
                    case 3:
                        Execute("PanLeft", panSpeed);
                        Execute("TiltUp", tiltSpeed);
                        break;
                    case 5:
                        Execute("PanRight", panSpeed);
                        break;
                    case 7:
                        Execute("PanRight", panSpeed);
                        Execute("TiltUp", tiltSpeed);
                        break;
                    case 10:
                        Execute("TiltDown", tiltSpeed);
                        break;
                    case 11:
                        Execute("PanLeft", panSpeed);
                        Execute("TiltDown", tiltSpeed);
                        break;
                    case 15:
                        Execute("PanRight", panSpeed);
                        Execute("TiltDown", tiltSpeed);
                        break;
                    default:
                        break;
                }
            }
        }

        protected override void Connected()
        {
            // ignore
        }
    }
}

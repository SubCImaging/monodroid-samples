namespace SubCTools.Droid.IO.AuxDevices
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    public class Controller : AuxDevice, INotifiable
    {
        /// <summary>
        /// End of Transmission.
        /// </summary>
        private const string EOT = "\u0004";

        /// <summary>
        /// End of Text
        /// </summary>
        private const string ETX = "\u0003";

        /// <summary>
        /// Start of Header.
        /// </summary>
        private const string SOH = "\u0001";

        /// <summary>
        /// Start of Text
        /// </summary>
        private const string STX = "\u0002";

        public Controller(
            AndroidSerial serial,
            int input,
            TeensyListener listener) :
            base(serial, input, listener)
        {
        }

        public static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        [RemoteCommand]
        public void GetImage(string path)
        {
            path = Path.Combine(DroidSystem.BaseDirectory, path);

            var img = Helpers.Camera.DecodeSampledBitmapFromResource(path, 320, 240);

            var stream = new MemoryStream();
            img.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 10, stream);

            var arr = stream.ToArray();

            var strArray = SubCTools.Helpers.Strings.ByteArrayToHexString(arr);

            // Turn gyro off
            serial.Send("~nmea set update fusion:0");
            Thread.Sleep(1000);

            ReceiveNotification(this, new NotifyEventArgs(strArray, MessageTypes.Gauntlet));

            // Turn gyro on
            serial.Send("~nmea set update fusion:1");
        }

        /// <summary>
        /// We want to send data through the teensy as we recieve it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            var data = e.Message;

            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            // in case you get multiple lines of data, split them on a new line and send 1 at a time
            var split = data.Split('\n');

            foreach (var item in split)
            {
                // chunk up the data in to 64 character sized lengths to prevent the teensy input buffer from overflowing
                var chunkedData = ChunksUpto(item, 64).ToArray();

                for (int i = 0; i < chunkedData.Count(); i++)
                {
                    if (string.IsNullOrEmpty(chunkedData[i]))
                    {
                        continue;
                    }

                    var toSend = chunkedData[i];

                    // if you have multiple chunks:
                    if (chunkedData.Count() > 1 && !item.StartsWith(SubCTeensy.TeensyPassthroughCharacter) && !item.StartsWith(SubCTeensy.TeensyStartCharacter) && !item.StartsWith(SubCTeensy.TeensyEchoCharacter))
                    {
                        // put a start of text character and end of text character at the start and end of each chunk
                        toSend = STX + toSend + ETX;

                        // put a start of transmission character at the front of the first chunk, and an end of transmission character at the end of the last chunk
                        toSend = (i == 0 ? SOH : string.Empty)
                            + toSend
                            + (i == chunkedData.Count() - 1 ? EOT : string.Empty);

                        toSend = SubCTeensy.TeensyEchoCharacter + toSend;
                    }

                    Send(toSend);
                }
            }
        }

        /// <summary>
        /// Bubble up the camera command when you receive data
        /// </summary>
        /// <param name="e">Aux data received</param>
        protected override void AuxDataReceived(AuxData e)
        {
            OnNotify(e.Data, Messaging.Models.MessageTypes.CameraCommand);
        }

        /// <summary>
        /// Don't care when we're connected
        /// </summary>
        protected override void Connected() { }
    }
}
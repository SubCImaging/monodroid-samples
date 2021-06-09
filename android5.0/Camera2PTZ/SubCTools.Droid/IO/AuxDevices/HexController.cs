using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Attributes;
using SubCTools.Communicators.Interfaces;
using SubCTools.Droid.Communicators;
using SubCTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubCTools.Droid.IO.AuxDevices
{
    public abstract class HexController : AuxDevice
    {
        protected char newLine = '\n';

        public HexController(
            AndroidSerial serial,
            int input,
            TeensyListener listener) : base(serial, input, listener)
        {
        }

        /// <summary>
        /// Wrap the data in the teensys required hex package and hex the string
        /// </summary>
        /// <param name="command">Command to send through the teensy</param>
        /// <returns>Wrapped, hexified string</returns>
        public static string Wrap(string command, char newLine)
            => "Hex:" + (command + newLine).ToHex().Replace(" ", string.Empty);

        /// <summary>
        /// Wrap data and convert to hex when sending
        /// </summary>
        /// <param name="data">Data to hexify and send</param>
        public override void Send(string data)
        {
            base.Send(Wrap(data, newLine));
        }

        public void SendBytes(byte[] bytes)
        {
            (serial as AndroidSerial).Send(bytes);
        }

        /// <summary>
        /// Secret passthrough message for debugging
        /// </summary>
        /// <param name="input">Data to hexify and send through teensy</param>
        [RemoteCommand]
        public void SendHex(string input)
        {
            Send(input);
        }

        protected abstract override void AuxDataReceived(AuxData e);

        protected abstract override void Connected();
    }

    public class TeensyHexCommunicator : ISenderReceiver<byte[]>
    {
        protected char newLine = '\n';
        private readonly int input;
        private readonly TeensyListener listener;
        private readonly AndroidSerial serial;

        public TeensyHexCommunicator(AndroidSerial serial, int input, TeensyListener listener)
        {
            this.serial = serial;
            this.input = input;
            this.listener = listener;

            listener.AuxDataReceived += Listener_AuxDataReceived;
        }

        public event EventHandler<string> DataReceived;

        public Task SendAsync(byte[] data)
        {
            var d = Strings.ByteArrayToHexString(data);

            d = d.Replace(" ", string.Empty).HexToAscii();

            var s = AuxDevice.PrependTo(HexController.Wrap(d, '\n'), input);

            return serial.SendAsync(s);
        }

        /// <summary>
        /// Data received method for incoming aux device
        /// </summary>
        /// <param name="sender">Who sent</param>
        /// <param name="e">Aux data received</param>
        private void Listener_AuxDataReceived(object sender, AuxData e)
        {
            // we only can about aux data coming in for this specific port
            if (e.From == input)
            {
                DataReceived?.Invoke(this, e.Data);
            }
        }
    }
}
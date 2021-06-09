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
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public abstract class AuxDevice : INotifier
    {
        /// <summary>
        /// Aux input the device is connected to
        /// </summary>
        protected readonly int input;

        /// <summary>
        /// Listener for incoming aux data
        /// </summary>
        protected readonly TeensyListener listener;

        /// <summary>
        /// Serial device for sending data
        /// </summary>
        protected readonly AndroidSerial serial;

        public AuxDevice(
            AndroidSerial serial,
            int input,
            TeensyListener listener)
        {
            this.input = input;
            this.serial = serial;
            this.listener = listener;

            listener.AuxDataReceived += Listener_AuxDataReceived;

            if (serial.IsConnected)
            {
                Connected();
            }

            serial.IsConnectedChanged += (s, e) =>
            {
                if (e)
                {
                    Connected();
                }
            };
        }

        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Prepend which aux device this data is to be sent
        /// </summary>
        /// <param name="data">Data to send to aux device</param>
        /// <returns>>#data</returns>
        public static string PrependTo(string data, int input) => ">" + (input == 0 ? 2 : input) + data;

        /// <summary>
        /// Check to see if the selected device on the aux port is correct if the device has that capability
        /// </summary>
        /// <returns>True if the device sends back the correct response, false otherwise</returns>
        public virtual bool IsDevice() { return true; }

        public virtual void LoadSettings()
        {
        }

        /// <summary>
        /// ToHex the data, and wrap it with the to input and newline to send to auxinput
        /// </summary>
        /// <param name="data">Data to send to auxinput</param>
        public virtual async void Send(string data)
        {
            var toSend = PrependTo(data, input);
            await serial.SendAsync(toSend);
        }

        /// <summary>
        /// Callback when aux data is received for the given input
        /// </summary>
        /// <param name="e">Aux data received</param>
        protected abstract void AuxDataReceived(AuxData e);

        /// <summary>
        /// Callback for when the teensy is connected
        /// </summary>
        protected abstract void Connected();

        /// <summary>
        /// Send a message off to the message router
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <param name="messageType">Type of message to bubble up</param>
        protected void OnNotify(string message, MessageTypes messageType = MessageTypes.Information)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));
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
                AuxDataReceived(e);
            }
        }
    }
}
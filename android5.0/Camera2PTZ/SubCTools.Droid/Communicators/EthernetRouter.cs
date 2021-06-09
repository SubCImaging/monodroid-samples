//-----------------------------------------------------------------------
// <copyright file="EthernetRouter.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Communicators
{
    using SubCTools.Communicators;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Droid.IO;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Routs communication between TCP and Teensy
    /// </summary>
    public class EthernetRouter : INotifier, INotifiable
    {
        /// <summary>
        /// The communicator
        /// </summary>
        private readonly ICommunicator communicator;

        /// <summary>
        /// Initializes a new instance of the <see cref="EthernetRouter"/> class.
        /// </summary>
        /// <param name="communicator">the ethernet communicator</param>
        public EthernetRouter(ICommunicator communicator)
        {
            this.communicator = communicator;
            communicator.DataReceived += TCP_DataReceived;
            communicator.Notify += TcpServer_Notify;
            communicator.IsConnectedChanged += (s, e) => IsConnectedChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Notify event
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Connection changed event
        /// </summary>
        public event EventHandler<bool> IsConnectedChanged;

        /// <summary>
        /// Receives messages to be sent over the communicator
        /// </summary>
        /// <param name="sender">the sending object</param>
        /// <param name="e">the message data to send</param>
        public async void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            foreach (var item in e.Message.Split('\n'))
            {
                if (string.IsNullOrEmpty(item)) { continue; }
                await communicator.SendAsync(item);
            }
        }

        /// <summary>
        /// Raises the <see cref="Notify"/> event
        /// </summary>
        /// <param name="message">the message to send</param>
        /// <param name="messageType">the <see cref="MessageTypes"/> ie, Information, error, alert, debug, etc...</param>
        protected void OnNotify(string message, MessageTypes messageType)
        {
            Notify(this, new NotifyEventArgs(message, messageType));
        }

        /// <summary>
        /// Data received over TCP to send to the Teensy. Calls <see cref="OnNotify(string, MessageTypes)"/>
        /// </summary>
        /// <param name="sender">the sending object</param>
        /// <param name="e">the data received</param>
        private void TCP_DataReceived(object sender, string e)
        {
            if (e.StartsWith(SubCTeensy.TeensyStartCharacter) && !DroidSystem.Instance.IsDebugging)
            {
                communicator.SendAsync("Enable debugging to communicate with the Teensy");
                return;
            }

            var commandType = e.StartsWith(SubCTeensy.TeensyStartCharacter) && DroidSystem.Instance.IsDebugging ? MessageTypes.TeensyCommand : Regex.Match(e, @"^>\d+").Success ? MessageTypes.TeensyCommand : MessageTypes.CameraCommand;

            OnNotify(e, commandType);
        }

        /// <summary>
        /// Calls the <see cref="OnNotify(string, MessageTypes)"/> method if the message type is <see cref="MessageTypes.Error"/>, <see cref="MessageTypes.Debug"/>, or <see cref="MessageTypes.Alert"/>
        /// </summary>
        /// <param name="sender">the sending object</param>
        /// <param name="e">message details</param>
        private void TcpServer_Notify(object sender, NotifyEventArgs e)
        {
            if (e.MessageType == MessageTypes.Error || e.MessageType == MessageTypes.Debug || e.MessageType == MessageTypes.Alert)
            {
                OnNotify(e.Message, e.MessageType);
            }
        }
    }

    ////public class TCPRouter
    ////{
    ////    public TCPRouter()
    ////        : this(new SubCTCPServer(8888, 1))
    ////    {
    ////    }

    ////    public TCPRouter(SubCTCPServer tcpServer)
    ////    {
    ////        tcpServer.IsConnectedChanged += TCP_IsConnectedChanged;
    ////    }

    ////    private void TCP_IsConnectedChanged(object sender, bool e)
    ////    {
    ////        IsConnectedChanged?.Invoke(sender, e);
    ////    }
    ////}
}
//-----------------------------------------------------------------------
// <copyright file="SubCTCPBase.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Communicators
{
    using Newtonsoft.Json;
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// <see cref="SubCTCPBase"/> class containing the basic TCP communication
    /// logic for the <see cref="SubCTCPClient"/> and <see cref="SubCTCPServer"/>.
    /// </summary>
    public abstract class SubCTCPBase
    {
        /// <summary>
        /// <see cref="bool"/> representing whether or not <see cref="SubCTCPBase"/> is currently sending.
        /// </summary>
        protected bool isSending;

        /// <summary>
        /// <see cref="TcpListener"/> TCP Server.
        /// </summary>
        protected ITcpListener tcpServer;

        private const int TCPBufferSize = 8192;

        private readonly List<byte> rx = new List<byte>();

        /// <summary>
        /// <see cref="bool"/> representing the current connection status.
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// DataReceived event.
        /// </summary>
        public event EventHandler<string> DataReceived;

        /// <summary>
        /// IsConnectedChanged event.
        /// </summary>
        public event EventHandler<bool> IsConnectedChanged;

        /// <summary>
        /// IsSendingChanged event.
        /// </summary>
        public event EventHandler<bool> IsSendingChanged;

        /// <summary>
        /// Notify event.
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        // public string Append { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets <see cref="string"/> to append to the message.
        /// </summary>
        public string Append { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether there is a current connection.
        /// </summary>
        public bool IsConnected
        {
            get => isConnected;
            protected set
            {
                if (isConnected == value)
                {
                    return;
                }

                isConnected = value;
                IsConnectedChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="SubCTCPBase"/> is currently sending.
        /// </summary>
        [JsonIgnore]
        public bool IsSending
        {
            get => isSending;
            private set
            {
                if (isSending == value)
                {
                    return;
                }

                isSending = value;
                IsSendingChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Gets or sets the value on how to process the output. E.g. you can set a func to remove a preceeding character.
        /// </summary>
        [JsonIgnore]
        public Func<string, string> Output { get; set; } = (s) => s;

        /// <summary>
        /// Gets or sets <see cref="string"/> to prepend to the message.
        /// </summary>
        public string Prepend { get; set; } = string.Empty;

        /// <summary>
        /// Converts the <see cref="message"/> to a byte array package
        /// in the correct format for sending.
        /// </summary>
        /// <param name="message">Message to encode into package.</param>
        /// <returns><see cref="byte[]"/> containing the package.</returns>
        protected static byte[] PackageString(string message)
        {
            // convert the data in to bytes
            var dataBytes = Encoding.ASCII.GetBytes(message);

            // determine the length of the data
            var lengthPrefix = BitConverter.GetBytes(message.Length);

            // create a new byte array the size of the data, plus the prefix
            var package = new byte[lengthPrefix.Length + dataBytes.Length];

            // copy the length prefix in to the package
            lengthPrefix.CopyTo(package, 0);

            // copy the data in to the package at the index of the length of the prefix
            dataBytes.CopyTo(package, lengthPrefix.Length);

            return package;
        }

        /// <summary>
        /// OnNotify, used to send messages.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="messageType">Message type.</param>
        protected void OnNotify(string message, MessageTypes messageType = MessageTypes.Receive)
        {
            if (messageType == MessageTypes.Receive)
            {
                DataReceived?.Invoke(this, message);
            }

            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));
        }

        /// <summary>
        /// Receives data from <see cref="Socket"/>.
        /// </summary>
        /// <param name="ar"><see cref="IAsyncResult"/> used to get the <see cref="TCPState"/>.</param>
        protected virtual void ReceiveCallback(IAsyncResult ar)
        {
            var state = ar.AsyncState as TCPState;

            var tcpClient = state.Client;

            try
            {
                var count = tcpClient.EndReceive(ar);
                state.BytesReceived += count;

                // we are still reading the size of the data
                if (state.MessageSize == -1)
                {
                    if (count == 0)
                    {
                        throw new ProtocolViolationException("The remote peer closed the connection while reading the message size.");
                    }

                    // we have received the entire message size information
                    if (state.BytesReceived == 4)
                    {
                        // read the size of the message
                        state.MessageSize = BitConverter.ToInt32(state.Buffer, 0);
                        if (state.MessageSize < 0)
                        {
                            throw new ProtocolViolationException("The remote peer sent a negative message size.");
                        }

                        // we should do some size validation here also (e.g. restrict incoming messages to x bytes long)
                        state.Buffer = new byte[state.MessageSize];

                        // reset the bytes received back to zero
                        // because we are now switching to reading the message body
                        state.BytesReceived = 0;
                    }

                    if (state.MessageSize != 0)
                    {
                        // we need more data – could be more of the message size information
                        // or it could be the message body.  The only time we won’t need to
                        // read more data is if the message size == 0
                        tcpClient.BeginReceive(
                            state.Buffer,
                            0,
                            state.Buffer.Length > TCPBufferSize ? TCPBufferSize : state.Buffer.Length,
                            SocketFlags.None,
                            ReceiveCallback,
                            state);
                    }
                }
                else
                {
                    // we are reading the body of the message
                    // we have the entire message
                    if (state.BytesRemaining == 0)
                    {
                        var data = Encoding.ASCII.GetString(state.Buffer);

                        state.MessageSize = -1;
                        state.BytesReceived = 0;
                        state.Buffer = new byte[4];

                        // BytesReceived => offset where data can be written
                        // Buffer.Length => how much data can be read into remaining buffer
                        tcpClient.BeginReceive(
                            state.Buffer,
                            state.BytesReceived,
                            state.Buffer.Length,
                            SocketFlags.None,
                            ReceiveCallback,
                            state);

                        // Console.WriteLine(data);
                        if (!data.Contains("\0"))
                        {
                            OnNotify(data);
                        }
                    }
                    else
                    {
                        // need more data.
                        tcpClient.BeginReceive(
                            state.Buffer,
                            state.BytesReceived,
                            state.BytesRemaining > TCPBufferSize ? TCPBufferSize : state.BytesRemaining,
                            SocketFlags.None,
                            ReceiveCallback,
                            state);

                        if (count == 0)
                        {
                            throw new ProtocolViolationException("The remote peer closed the connection before the entire message was received");
                        }
                    }
                }
            }
            catch (ProtocolViolationException e)
            {
                throw e;
            }
            catch (MemberAccessException e)
            {
                throw e;
            }
            catch (ObjectDisposedException e)
            {
                throw e;
            }
            catch (SocketException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Send the <see cref="CommunicationData"/> to the following <see cref="Socket"/>.
        /// </summary>
        /// <param name="data"><see cref="CommunicationData"/> to send to the <see cref="Socket"/>.</param>
        /// <param name="to"><see cref="Socket"/> to send the <see cref="CommunicationData"/> to.</param>
        protected void Send(CommunicationData data, ISocket to) // , Socket to = null)
        {
            var d = $"{Prepend}{data.ToString()}{Append}";

            OnNotify(d, MessageTypes.Transmit);

            IsSending = true;

            var package = PackageString(d);

            try
            {
                // send the package
                to.BeginSend(package);// , 0, package.Length, SocketFlags.None, null, null);
            }
            catch (SocketException e)
            {
                throw e;
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine($"Could not send {data}, socket closed");
            }

            IsSending = false;
        }
    }
}
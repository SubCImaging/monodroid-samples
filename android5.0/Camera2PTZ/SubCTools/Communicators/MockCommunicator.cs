// <copyright file="MockCommunicator.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
namespace SubCTools.Communicators
{
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Mock communicator for testing.
    /// </summary>
    public class MockCommunicator : ICommunicator
    {
        private bool isConnected;

        private bool isSending;

        /// <inheritdoc/>
        public event EventHandler<string> DataReceived;

        /// <inheritdoc/>
        public event EventHandler<bool> IsConnectedChanged;

        /// <inheritdoc/>
        public event EventHandler<bool> IsSendingChanged;

        /// <inheritdoc/>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <inheritdoc/>
        public CommunicatorAddress Address { get; set; }

        /// <inheritdoc/>
        public string Append { get; set; }

        /// <inheritdoc/>
        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    IsConnectedChanged?.Invoke(this, value);
                }
            }
        }

        /// <inheritdoc/>
        public bool IsSending
        {
            get => isSending;
            private set
            {
                if (isSending != value)
                {
                    isSending = value;
                    IsSendingChanged?.Invoke(this, value);
                }
            }
        }

        /// <inheritdoc/>
        public Func<string, string> Output { get; set; }

        /// <inheritdoc/>
        public string Prepend { get; set; }

        public MockCommunicator Receiver { get; set; }

        /// <inheritdoc/>
        public string Status { get; private set; }

        /// <inheritdoc/>
        public async Task<bool> ConnectAsync()
        {
            IsConnected = true;
            return IsConnected;
        }

        /// <inheritdoc/>
        public async Task<bool> ConnectAsync(CommunicatorAddress address)
        {
            IsConnected = true;
            return IsConnected;
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            IsConnected = false;
        }

        public void Receive(CommunicationData data)
        {
            DataReceived?.Invoke(this, data.ToString());
        }

        /// <inheritdoc/>
        public async Task SendAsync(CommunicationData data)
        {
            Receiver.Receive(data);
        }

        public async Task<string> SendSync(CommunicationData data)
        {
            Receiver.Receive(data);
            return string.Empty;
        }
    }
}
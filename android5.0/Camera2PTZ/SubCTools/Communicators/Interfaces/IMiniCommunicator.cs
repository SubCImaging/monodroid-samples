// <copyright file="IMiniCommunicator.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Messaging.Interfaces;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for a mini communicator.
    /// </summary>
    /// <typeparam name="T">Address type.</typeparam>
    /// <typeparam name="TTK">Type of data to send.</typeparam>
    /// <typeparam name="TK">Type of data to receive.</typeparam>
    public interface IMiniCommunicator<T, in TTK, TK> : INotifier, ISenderReceiver<CommunicationData>
    {
        /// <summary>
        /// Event to fire when the connection changes.
        /// </summary>
        event EventHandler<bool> IsConnectedChanged;

        /// <summary>
        /// Event to fire when is sending changes.
        /// </summary>
        event EventHandler<bool> IsSendingChanged;

        /// <summary>
        /// Gets or sets the address to connect.
        /// </summary>
        T Address { get; set; }

        /// <summary>
        /// Gets or sets the string to append to the data.
        /// </summary>
        string Append { get; set; }

        /// <summary>
        /// Gets a value indicating whether the communicator is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets a value indicating whether the communicator is sending.
        /// </summary>
        bool IsSending { get; }

        /// <summary>
        /// Gets or sets the output processing function.
        /// </summary>
        Func<string, string> Output { get; set; }

        /// <summary>
        /// Gets or sets the string to prepend to the data.
        /// </summary>
        string Prepend { get; set; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// Connect to the communicator.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Connect to the communicator with a given address.
        /// </summary>
        /// <param name="address">Address to connect.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<bool> ConnectAsync(T address);

        /// <summary>
        /// Disconnect from the communicator.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Send data sync to the communicator. It will receive data back htrough here.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<TK> SendSync(TTK data);
    }
}
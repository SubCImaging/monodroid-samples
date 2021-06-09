// <copyright file="ISenderReceiver.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for a communicator that only sends and receives.
    /// </summary>
    /// <typeparam name="TK">Type of data to send.</typeparam>
    public interface ISenderReceiver<in TK>
    {
        /// <summary>
        /// Event to fire when you've received data.
        /// </summary>
        event EventHandler<string> DataReceived;

        /// <summary>
        /// Send data async through the communicator.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendAsync(TK data);
    }
}
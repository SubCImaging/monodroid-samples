// <copyright file="IConnectable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using SubCTools.Communicators.EventArgsLib;
    using System;

    /// <summary>
    /// Simple communicator that can be only connected to.
    /// </summary>
    public interface IConnectable
    {
        /// <summary>
        /// Event to fire if the connection changes.
        /// </summary>
        event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        /// <summary>
        /// Gets a value indicating whether the communicator is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// COnnect to the communicator.
        /// </summary>
        /// <returns>True if successful.</returns>
        bool Connect();

        /// <summary>
        /// Disconnect from the communicator.
        /// </summary>
        void Disconnect();
    }
}
// <copyright file="ISerialPort.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using System;

    /// <summary>
    /// Interface for serial ports.
    /// </summary>
    public interface ISerialPort : IDisposable
    {
        /// <summary>
        /// Event to fire when data is received.
        /// </summary>
        event EventHandler DataReceived;

        /// <summary>
        /// Gets or sets the baud rate.
        /// </summary>
        int BaudRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Dtr is Enabled.
        /// </summary>
        bool DtrEnable { get; set; }

        /// <summary>
        /// Gets a value indicating whether the port is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets or sets the portname.
        /// </summary>
        string PortName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the RTS is enabled.
        /// </summary>
        bool RtsEnable { get; set; }

        /// <summary>
        /// Close the port.
        /// </summary>
        void Close();

        /// <summary>
        /// Discard what's in the buffer.
        /// </summary>
        void DiscardInBuffer();

        /// <summary>
        /// Open the port.
        /// </summary>
        void Open();

        /// <summary>
        /// Read the existing data.
        /// </summary>
        /// <returns>All the data present in the port.</returns>
        string ReadExisting();

        /// <summary>
        /// Write to the port.
        /// </summary>
        /// <param name="data">Data to write.</param>
        void Write(string data);
    }
}
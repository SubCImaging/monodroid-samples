// <copyright file="SubCSerialPort.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators
{
    using SubCTools.Communicators.Interfaces;
    using System;
    using System.IO.Ports;

    /// <summary>
    /// Class for using serial ports with an interface.
    /// </summary>
    public class SubCSerialPort : SerialPort, ISerialPort
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubCSerialPort"/> class.
        /// </summary>
        public SubCSerialPort()
        {
            base.DataReceived += (s, e) => DataReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Event to fire when data is received.
        /// </summary>
        public new event EventHandler DataReceived;
    }
}
//-----------------------------------------------------------------------
// <copyright file="TCPState.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Communicators
{
    using SubCTools.Communicators.Interfaces;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// <see cref="TCPState"/> class containing the <see cref="Client"/>,
    /// <see cref="Buffer"/> and <see cref="Message"/> information.
    /// </summary>
    public class TCPState
    {
        /// <summary>
        /// Gets or sets the Size of buffer.
        /// </summary>
        public static int BufferSize { get; set; } = 4;

        /// <summary>
        /// Gets or sets the Byte array of size <see cref="BufferSize"/>.
        /// </summary>
        public byte[] Buffer { get; set; } = new byte[BufferSize];

        /// <summary>
        /// Gets or sets the number of bytes that have been received.
        /// </summary>
        public int BytesReceived { get; set; }

        public int BytesRemaining => MessageSize - BytesReceived;

        /// <summary>
        /// Gets or sets the <see cref="Socket"/> Client to send data.
        /// </summary>
        public ISocket Client { get; set; }

        /// <summary>
        /// Gets the message to send.
        /// </summary>
        public StringBuilder Message { get; } = new StringBuilder();

        /// <summary>
        /// Gets or sets the Message Size.
        /// </summary>
        public int MessageSize { get; set; } = -1;
    }
}
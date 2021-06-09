// <copyright file="ISocket.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Interface for sockets.
    /// </summary>
    public interface ISocket
    {
        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Begin to receive on the socket.
        /// </summary>
        /// <param name="buffer">Buffer to copy dawta in to.</param>
        /// <param name="offset">Buffer offset.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="socketFlags">Socket flags to use.</param>
        /// <param name="receiveCallback">Delegate to call on callback.</param>
        /// <param name="state">State to pass through.</param>
        void BeginReceive(byte[] buffer, int offset, int bufferSize, SocketFlags socketFlags, AsyncCallback receiveCallback, TCPState state);

        /// <summary>
        /// Begin sending through the socket.
        /// </summary>
        /// <param name="buffer">Data to send.</param>
        /// <returns>Async result.</returns>
        IAsyncResult BeginSend(byte[] buffer);

        /// <summary>
        /// Disconnect from the socket.
        /// </summary>
        /// <param name="v">I have no idea.</param>
        void Disconnect(bool v);

        /// <summary>
        /// Stop receiving from the socket.
        /// </summary>
        /// <param name="ar">Async result.</param>
        /// <returns>Number of bytes.</returns>
        int EndReceive(IAsyncResult ar);
    }
}
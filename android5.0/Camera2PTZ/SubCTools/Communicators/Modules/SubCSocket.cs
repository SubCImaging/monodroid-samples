// <copyright file="SubCSocket.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Modules
{
    using SubCTools.Communicators.Interfaces;
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Socket implementation.
    /// </summary>
    public class SubCSocket : ISocket
    {
        private readonly Socket socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCSocket"/> class.
        /// </summary>
        /// <param name="socket">Socket to instantiate.</param>
        public SubCSocket(Socket socket)
        {
            this.socket = socket;
        }

        /// <inheritdoc/>
        public EndPoint RemoteEndPoint => socket.RemoteEndPoint;

        /// <inheritdoc/>
        public void BeginReceive(byte[] buffer, int v, int bufferSize, SocketFlags none, AsyncCallback receiveCallback, TCPState state)
        {
            socket.BeginReceive(buffer, v, bufferSize, none, receiveCallback, state);
        }

        /// <inheritdoc/>
        public IAsyncResult BeginSend(byte[] buffer)
        {
            return socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null);
        }

        /// <inheritdoc/>
        public void Disconnect(bool v)
        {
            socket?.Disconnect(v);
        }

        /// <inheritdoc/>
        public int EndReceive(IAsyncResult ar)
        {
            return socket.EndReceive(ar);
        }
    }
}
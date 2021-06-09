// <copyright file="ITcpClient.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for a tcp client.
    /// </summary>
    public interface ITcpClient
    {
        /// <summary>
        /// Gets the client socket.
        /// </summary>
        ISocket Client { get; }

        /// <summary>
        /// Close the client.
        /// </summary>
        void Close();

        /// <summary>
        /// Connect to the given ip address and port.
        /// </summary>
        /// <param name="address">Address to connect to.</param>
        /// <param name="port">Port to connect to.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task ConnectAsync(IPAddress address, int port);

        /// <summary>
        /// Start the client.
        /// </summary>
        /// <param name="family">Type of address family to start with.</param>
        void Start(AddressFamily family);
    }
}
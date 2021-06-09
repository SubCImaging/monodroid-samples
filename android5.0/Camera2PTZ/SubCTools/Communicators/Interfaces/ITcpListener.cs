// <copyright file="ITcpListener.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for a listener.
    /// </summary>
    public interface ITcpListener
    {
        /// <summary>
        /// Gets the Server socket.
        /// </summary>
        ISocket Server { get; }

        /// <summary>
        /// Start accepting clients.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ITcpClient> AcceptTcpClientAsync();

        /// <summary>
        /// Start the listener.
        /// </summary>
        void Start();

        /// <summary>
        /// Start the listener.
        /// </summary>
        /// <param name="address">Address to start on.</param>
        /// <param name="port">Port to start on.</param>
        void Start(IPAddress address, int port);

        /// <summary>
        /// Stop the listener.
        /// </summary>
        void Stop();
    }
}
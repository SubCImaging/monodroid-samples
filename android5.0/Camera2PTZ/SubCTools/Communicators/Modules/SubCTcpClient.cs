// <copyright file="SubCTcpClient.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Modules
{
    using SubCTools.Communicators.Interfaces;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Wrapper for MS.
    /// </summary>
    public class SubCTcpClient : ITcpClient
    {
        private TcpClient tcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTcpClient"/> class.
        /// </summary>
        public SubCTcpClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTcpClient"/> class.
        /// </summary>
        /// <param name="tcpClient">Base TCPClient to use.</param>
        public SubCTcpClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }

        /// <inheritdoc/>
        public ISocket Client => new SubCSocket(tcpClient.Client);

        /// <inheritdoc/>
        public void Close()
        {
            tcpClient.Close();
        }

        /// <inheritdoc/>
        public Task ConnectAsync(IPAddress address, int port)
        {
            return tcpClient.ConnectAsync(address, port);
        }

        /// <inheritdoc/>
        public void Start(AddressFamily family)
        {
            tcpClient = new TcpClient(family);
        }
    }
}
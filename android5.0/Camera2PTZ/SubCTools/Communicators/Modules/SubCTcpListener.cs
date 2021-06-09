// <copyright file="SubCTcpListener.cs" company="SubC Imaging">
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
    public class SubCTcpListener : ITcpListener
    {
        private TcpListener tcpListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTcpListener"/> class.
        /// </summary>
        public SubCTcpListener()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTcpListener"/> class.
        /// </summary>
        /// <param name="tcpListener">Listener to instanstiate.</param>
        public SubCTcpListener(TcpListener tcpListener)
        {
            this.tcpListener = tcpListener;
        }

        /// <inheritdoc/>
        public ISocket Server => new SubCSocket(tcpListener.Server);

        /// <inheritdoc/>
        public async Task<ITcpClient> AcceptTcpClientAsync()
        {
            return new SubCTcpClient(await tcpListener.AcceptTcpClientAsync());
        }

        /// <inheritdoc/>
        public void Start()
        {
            tcpListener.Start();
        }

        /// <inheritdoc/>
        public void Start(IPAddress address, int port)
        {
            tcpListener = new TcpListener(address, port);
            Start();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            tcpListener.Stop();
        }
    }
}
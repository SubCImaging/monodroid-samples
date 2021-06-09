// <copyright file="SubCTCPServer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators
{
    using Newtonsoft.Json;
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Communicators.Modules;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for new TCP Server.
    /// </summary>
    public class SubCTCPServer : SubCTCPBase, ICommunicator
    {
        private readonly List<ISocket> clients = new List<ISocket>();

        private readonly int connectionsLimit;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTCPServer"/> class.
        /// </summary>
        /// <param name="port">Port to open up server.</param>
        /// <param name="connectionsLimit">How many can connect.</param>
        public SubCTCPServer(int port, int connectionsLimit = 0)
           : this(port, new SubCTcpListener(), IPAddress.IPv6Any, connectionsLimit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTCPServer"/> class.
        /// </summary>
        /// <param name="port">Port to open up server.</param>
        /// <param name="connectionsLimit">How many can connect.</param>
        /// <param name="socketTypes">Socket type to use.</param>
        public SubCTCPServer(int port, IPAddress socketTypes, int connectionsLimit = 0)
            : this(port, new SubCTcpListener(), socketTypes, connectionsLimit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTCPServer"/> class.
        /// </summary>
        /// <param name="port">Port to open up server.</param>
        /// <param name="connectionsLimit">How many can connect.</param>
        /// <param name="socketTypes">Socket type to use.</param>
        /// <param name="tcpListener">Base listener for incoming data.</param>
        public SubCTCPServer(
            int port,
            ITcpListener tcpListener,
            IPAddress socketTypes,
            int connectionsLimit = 0)
        {
            this.Address = new EthernetAddress(socketTypes, port);

            this.tcpServer = tcpListener;

            this.connectionsLimit = connectionsLimit;

            // Make the listener listen for connections on any network device
            Connect(socketTypes, port);
        }

        /// <inheritdoc/>
        [JsonIgnore]
        public CommunicatorAddress Address { get; set; }

        /// <inheritdoc/>
        public string Status { get; }

        /// <summary>
        /// Start the server to the given address.
        /// </summary>
        /// <param name="address">IP Address to start server on.</param>
        /// <param name="port">Port to listen on.</param>
        /// <returns>True if the connection was sucessful, false otherwise.</returns>
        public bool Connect(IPAddress address, int port)
        {
            try
            {
                tcpServer.Start(address, port);
                ListenForNewClients();
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> ConnectAsync()
        {
            return await ConnectAsync(Address);
        }

        /// <inheritdoc/>
        public Task<bool> ConnectAsync(CommunicatorAddress address)
        {
            return Task.Run(() =>
{
    if (address == null)
    {
        return false;
    }

    var e = (EthernetAddress)address;
    return Connect(e.Address, e.Port);
});
        }

        /// <inheritdoc/>
        public Task DisconnectAsync()
        {
            return Task.Run(() =>
{
    tcpServer.Stop();
    tcpServer.Server.Disconnect(true);
});
        }

        /// <inheritdoc/>
        public async Task SendAsync(CommunicationData data)
        {
            foreach (var tcpClient in clients.ToList())
            {
                try
                {
                    await Task.Run(() => Send(data, tcpClient));
                }
                catch
                {
                    RemoveClient(tcpClient);
                }
            }
        }

        public Task<string> SendSync(CommunicationData data)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendSync(string data)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override void ReceiveCallback(IAsyncResult ar)
        {
            var tcpClient = (ar.AsyncState as TCPState).Client;

            try
            {
                base.ReceiveCallback(ar);
            }
            catch (MemberAccessException)
            {
                OnNotify("There was a member access error, please reconnect if this persists", MessageTypes.Error);
                RemoveClient(tcpClient);
            }
            catch (ProtocolViolationException e)
            {
                OnNotify(e.Message, MessageTypes.Error);
                RemoveClient(tcpClient);
            }
            catch
            {
                OnNotify("There was a transmission error, please reconnect if this persists", MessageTypes.Error);
                RemoveClient(tcpClient);
            }
        }

        private bool IsActive(Socket client)
        {
            var w = client.Poll(1000, SelectMode.SelectWrite);
            var r = client.Poll(1000, SelectMode.SelectRead);
            var e = client.Poll(1000, SelectMode.SelectError);
            return r;// e;
        }

        private void ListenForNewClients()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var tcpClient = await tcpServer.AcceptTcpClientAsync();

                    OnNotify($"{tcpClient.Client.RemoteEndPoint.ToString()} connected", MessageTypes.Error);
                    clients.Add(tcpClient.Client);
                    IsConnected = clients.Count > 0;

                    var state = new TCPState
                    {
                        Client = tcpClient.Client,
                    };

                    tcpClient.Client.BeginReceive(state.Buffer, 0, TCPState.BufferSize, SocketFlags.None, ReceiveCallback, state);
                }
            });
        }

        private void RemoveClient(ISocket tcpClient)
        {
            try
            {
                OnNotify(tcpClient?.RemoteEndPoint?.ToString() + " disconnected", MessageTypes.Debug);
            }
            catch
            {
            }

            try
            {
                clients.Remove(tcpClient);
                IsConnected = clients.Count > 0;
            }
            catch (Exception e)
            {
                OnNotify("Error removing client: " + tcpClient.RemoteEndPoint.ToString() + "\n" + e.Message, MessageTypes.Error);
            }
        }
    }
}
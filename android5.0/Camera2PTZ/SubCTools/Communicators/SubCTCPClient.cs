//-----------------------------------------------------------------------
// <copyright file="SubCTCPClient.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Communicators
{
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Communicators.Modules;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// Client for handling TCP communications.
    /// </summary>
    public class SubCTCPClient : SubCTCPBase, ICommunicator
    {
        private static readonly object Sync = new object();

        /// <summary>
        /// Timer to ping server to check for physical connection.
        /// </summary>
        private readonly Timer checkConnection = new Timer() { Interval = 1000 };

        private readonly ITcpClient client;

        /// <summary>
        /// Number of pings that have failed to reach the destination.
        /// </summary>
        private int pingsFailed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTCPClient"/> class.
        /// </summary>
        public SubCTCPClient()
            : this(new SubCTcpClient())
        {
            checkConnection.Elapsed += (s, e) => CheckConnection_Elapsed();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTCPClient"/> class.
        /// </summary>
        /// <param name="tcpClient">TCP Client type to instanstiate.</param>
        public SubCTCPClient(ITcpClient tcpClient)
        {
            this.client = tcpClient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTCPClient"/> class.
        /// </summary>
        public SubCTCPClient(TimeSpan checkConnectionInterval, TimeSpan checkConnectionTimeout, int checkConnectionMaxRetries)
            : this()
        {
            SetCheckConnectionInterval(checkConnectionInterval);
            this.CheckConnectionPingTimeout = checkConnectionTimeout;
            this.CheckConnectionPingRetries = checkConnectionMaxRetries;
        }

        /// <summary>
        /// Gets or sets the address the client uses to connect.
        /// </summary>
        public CommunicatorAddress Address { get; set; }

        public int CheckConnectionPingRetries { get; set; } = 3;

        public TimeSpan CheckConnectionPingTimeout { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets the connection status of the client.
        /// </summary>
        public string Status => IsConnected ? "Connected" : "Disconnected" + $": {Address}";

        /// <summary>
        /// Connect to specified address.
        /// </summary>
        /// <param name="address">Address containing IPAddress and Port to connect to.</param>
        /// <returns>True if successfully connected, false if connection failed.</returns>
        public bool Connect(EthernetAddress address)
        {
            return address == null ? false : Connect(address.Address, address.Port).Result;
        }

        /// <summary>
        /// Connect to specific ipaddress and port.
        /// </summary>
        /// <param name="address">IP Address of the server.</param>
        /// <param name="port">Port the server is listening on.</param>
        /// <returns>True if successfully connected, false if connection failed.</returns>
        public async Task<bool> Connect(IPAddress address, int port)
        {
            Address = new EthernetAddress(address, port);

            client.Start(address.AddressFamily);

            try
            {
                await client.ConnectAsync(address, port);
            }
            catch (SocketException)
            {
                // Console.WriteLine(e);
                return false;
            }

            OnNotify(Status, MessageTypes.Connection);

            try
            {
                var state = new TCPState() { Client = client.Client };
                client.Client.BeginReceive(state.Buffer, 0, TCPState.BufferSize, SocketFlags.None, ReceiveCallback, state);
                IsConnected = true;

                checkConnection.Start();
            }
            catch (SocketException)
            {
                return IsConnected = false;
            }

            return IsConnected;
        }

        /// <summary>
        /// Connect to the member address.
        /// </summary>
        /// <returns>True if successfully connected, false if connection failed.</returns>
        public async Task<bool> ConnectAsync()
        {
            return await ConnectAsync(Address);
        }

        /// <summary>
        /// Connect to the specified address.
        /// </summary>
        /// <param name="address">Address to connect to, can be specified to an EthernetAddress. Must contain Address and Port entries.</param>
        /// <returns>True if successfully connected, false if connection failed.</returns>
        public async Task<bool> ConnectAsync(CommunicatorAddress address)
        {
            return await Task.Run(() => Connect(IPAddress.Parse(address["Address"]), Convert.ToInt32(address["Port"])));
        }

        /// <summary>
        /// Disconnect from server.
        /// </summary>
        public void Disconnect()
        {
            checkConnection.Stop();

            IsConnected = false;

            try
            {
                client?.Client.Disconnect(false);
                client?.Close();
            }
            catch
            {
                // ignore
            }

            OnNotify(Status, MessageTypes.Connection);
        }

        /// <summary>
        /// Disconnect from server.
        /// </summary>
        /// <returns>Empty Task.</returns>
        public async Task DisconnectAsync()
        {
            await Task.Run(() => Disconnect());
        }

        /// <summary>
        /// Asynchronously send data to the server.
        /// </summary>
        /// <param name="data">Message to send to server.</param>
        /// <returns>Empty Task.</returns>
        public async Task SendAsync(CommunicationData data)
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                await Task.Run(() => Send(data, client.Client));
            }
            catch (SocketException)
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Send a message to the server, wait for a response before returning.
        /// </summary>
        /// <param name="data">Message, how long to wait, and pattern to match before returning.</param>
        /// <returns>Result of communication sent back.</returns>
        public async Task<string> SendSync(CommunicationData data)
        {
            using var appender = new DataAppender()
            {
                Pattern = data.Pattern,
                MasterTimeout = data.First().Item2,
                DataTimeout = data.First().Item2,
            };

            var tcs = new TaskCompletionSource<string>();

            var appenderHandler = new EventHandler<NotifyEventArgs>((s, e) =>
            {
                tcs.TrySetResult(e.Message);
            });

            appender.Notify += appenderHandler;

            var handler = new EventHandler<string>((s, e) =>
            {
                appender.Append(e);
            });

            DataReceived += handler;

            await SendAsync(data.First().Item1);
            appender.Start();
            var result = await tcs.Task;

            DataReceived -= handler;

            return result;
        }

        /// <summary>
        /// Send data synchronously. NOT IMPLEMENTED.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>Nothing.</returns>
        public Task<string> SendSync(string data)
        {
            throw new NotImplementedException();
        }

        public void SetCheckConnectionInterval(TimeSpan t)
        {
            checkConnection.Interval = t.TotalMilliseconds;
        }

        /// <summary>
        /// Socket's receive callback which is called when data is received.
        /// </summary>
        /// <param name="ar">Result of the callback.</param>
        protected override void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                base.ReceiveCallback(ar);
            }
            catch (MemberAccessException e)
            {
                Console.WriteLine(e.Message);

                // Removed Disconnect() on 04/24/18 because it was causing random disconnects.
                //  A objectdisposed exception was getting thrown in SubCTCPBase and throwing a memberaccessexception
                //  causing the disconnect here, not sure of the cause.
                Disconnect();
            }
            catch (ProtocolViolationException e)
            {
                Console.WriteLine(e);
                Disconnect();
            }
            catch (ObjectDisposedException e)
            {
                // this is seemingly harmless for the time being?
                Console.WriteLine(e);
                Disconnect(); // much more reliable since this line was added.
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
                Disconnect();
            }
            catch (Exception e)
            {
                Disconnect();
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Check the physical connection to the server with a ping.
        /// </summary>
        private async void CheckConnection_Elapsed()
        {
            // If any of the check-connection properties are zero don't perform the check.
            if (checkConnection.Interval == 0 || CheckConnectionPingTimeout == TimeSpan.Zero || CheckConnectionPingRetries <= 0)
            {
                return;
            }

            if (!await Ethernet.IsPresent(IPAddress.Parse(Address["Address"]), (int)CheckConnectionPingTimeout.TotalMilliseconds))
            {
                pingsFailed++;
                Console.WriteLine(pingsFailed + " Failed pings in a row");

                if (pingsFailed > CheckConnectionPingRetries)
                {
                    Disconnect();
                    //OnNotify("Ping failed, have you lost physical connection?", MessageTypes.Error);
                }
            }
            else
            {
                pingsFailed = 0;
            }
        }
    }
}
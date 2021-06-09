//-----------------------------------------------------------------------
// <copyright file="SubCUDP.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators
{
    using Newtonsoft.Json;
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for communicating using UDP.
    /// </summary>
    public class SubCUDP : ICommunicator
    {
        /// <summary>
        /// Object to sync to prevent multithreading issues.
        /// </summary>
        private static readonly object Sync = new object();

        /// <summary>
        /// NOT IMPLEMENTED.
        /// </summary>
        private readonly bool stopSync = false;

        /// <summary>
        /// Client responsible for communication.
        /// </summary>
        private UdpClient client;

        /// <summary>
        /// Endpoint of the connected client.
        /// </summary>
        private IPEndPoint endPoint;

        /// <summary>
        /// Value indicating whether the class is connected.
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// is the class sending data.
        /// </summary>
        private bool isSending;

        /// <summary>
        /// Is the class currently sending synchronous data.
        /// </summary>
        private bool isSendingSync;

        /// <summary>
        /// Port to listen to incoming messages.
        /// </summary>
        private int listeningPort;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCUDP"/> class, lets the system determine port to use.
        /// </summary>
        public SubCUDP()
            : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCUDP"/> class, with specified port.
        /// </summary>
        /// <param name="port">Port to listen on.</param>
        public SubCUDP(int port)
        {
            ListeningPort = port;
        }

        /// <summary>
        /// Event to alert when data has been received
        /// </summary>
        public event EventHandler<string> DataReceived;

        /// <summary>
        /// Event to alert when the is connected value changes
        /// </summary>
        public event EventHandler<bool> IsConnectedChanged;

        /// <summary>
        /// Event to alert when the is sending value changes
        /// </summary>
        public event EventHandler<bool> IsSendingChanged;

        /// <summary>
        /// Notification event
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Event to alert when the port has changed
        /// </summary>
        public event EventHandler<int> PortChanged;

        /// <summary>
        /// Gets or sets a value for the address the client will connect.
        /// </summary>
        public CommunicatorAddress Address { get; set; }

        /// <summary>
        /// Gets or sets a value for which data to append to all outgoing data.
        /// </summary>
        public string Append { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether or not the class is connected to another instance.
        /// </summary>
        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                if (isConnected == value)
                {
                    return;
                }

                isConnected = value;
                IsConnectedChanged?.Invoke(this, value);

                OnNotify(Status, MessageTypes.Connection);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the class is currently sending data.
        /// </summary>
        public bool IsSending
        {
            get => isSending;
            private set
            {
                if (isSending == value)
                {
                    return;
                }

                isSending = value;
                IsSendingChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Gets or sets a value of which port to listen to incoming data.
        /// </summary>
        public int ListeningPort
        {
            get => listeningPort;
            set
            {
                if (listeningPort == value)
                {
                    return;
                }

                listeningPort = value;

                try
                {
                    client = new UdpClient(listeningPort);
                    client.BeginReceive(OnReceive, null);
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.ToString());
                }
                catch (ArgumentOutOfRangeException oore)
                {
                    Console.WriteLine(oore.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets the value on how to process the output. E.g. you can set a function to remove a preceding character.
        /// </summary>
        [JsonIgnore]
        public Func<string, string> Output { get; set; } = (s) => s;

        /// <summary>
        /// Gets or sets a value for which data to prepend to all outgoing data.
        /// </summary>
        public string Prepend { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating the current status of the connected address.
        /// </summary>
        public string Status => Address + " is " + (IsConnected ? "connected" : "disconnected");

        /// <summary>
        /// Connect to the specified address.
        /// </summary>
        /// <param name="address">Specific address to connect to.</param>
        /// <returns>True if connected, false if connection was unsuccessful.</returns>
        public bool Connect(EthernetAddress address)
        {
            // if an address is not provided, you're acting like a server
            if (address == null)
            {
                return IsConnected = true;
            }

            Address = address;

            try
            {
                if (client == null)
                {
                    client = new UdpClient(ListeningPort);
                    client.BeginReceive(OnReceive, null);
                }

                endPoint = new IPEndPoint(address.Address, address.Port);
                return IsConnected = true;
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.ToString());
                OnNotify(e.ToString(), MessageTypes.Critical);
                return e.SocketErrorCode == SocketError.AddressAlreadyInUse;
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine(e.Message);
                OnNotify(e.ToString(), MessageTypes.Critical);
                return false;
            }
        }

        /// <summary>
        /// Connect to the set address.
        /// </summary>
        /// <returns>True if connected, false if connection was unsuccessful.</returns>
        public bool Connect()
        {
            if (Address == null)
            {
                return Connect(null);
            }

            var port = Convert.ToInt32(Address["Port"]);
            ListeningPort = port;
            var ip = Address["Address"];
            return Connect(new EthernetAddress(IPAddress.Parse(ip), port));
        }

        /// <summary>
        /// Connect to the address.
        /// </summary>
        /// <returns>True if connected, false if connection was unsuccessful.</returns>
        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() => Connect());
        }

        /// <summary>
        /// Connect to the specified address.
        /// </summary>
        /// <param name="address">Specific address to connect to.</param>
        /// <returns>True if connected, false if connection was unsuccessful.</returns>
        public async Task<bool> ConnectAsync(CommunicatorAddress address)
        {
            return await Task.Run(() =>
{
    Address = address;
    return Connect();
});
        }

        /// <summary>
        /// Disconnect from the connected client.
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;

            try
            {
                client?.Close();
                client?.Client.Close();
            }
            catch
            {
                // ignore
            }

            client = null;
        }

        /// <summary>
        /// Disconnect from connected client.
        /// </summary>
        /// <returns>An empty task.</returns>
        public async Task DisconnectAsync()
        {
            await Task.Run(() => Disconnect());
        }

        /// <summary>
        /// Read the incoming buffer.
        /// </summary>
        /// <returns>An empty task.</returns>
        public async Task ReceiveAsync()
        {
            OnNotify(await ReceiveSync());
        }

        /// <summary>
        /// Read the incoming buffer.
        /// </summary>
        /// <returns>The data from the incoming buffer.</returns>
        public async Task<string> ReceiveSync()
        {
            return await Task.Run(async () =>
                {
                    var result = new StringBuilder();
                    await Task.Delay(1000);
                    while (client.Available > 0)
                    {
                        result.Append(Encoding.ASCII.GetString(client.Receive(ref endPoint)));
                    }

                    return result.ToString();
                });
        }

        /// <summary>
        /// Send data to the client.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>True if the send was successful, false if it failed.</returns>
        public bool Send(string data)
        {
            return SendTo(data, endPoint);
        }

        /// <summary>
        /// Send the data to the connected client.
        /// </summary>
        /// <param name="data">Data to send to the client.</param>
        /// <returns>An empty task.</returns>
        public async Task SendAsync(CommunicationData data)
        {
            await SendAsync(data.First().Item1);
        }

        /// <summary>
        /// Send the data to the connected client.
        /// </summary>
        /// <param name="data">Data to send to the client.</param>
        /// <returns>An empty task.</returns>
        public async Task SendAsync(string data)
        {
            await Task.Run(() =>
{
    try
    {
        Send(data);
        return true;
    }
    catch
    {
        return false;
    }
});
        }

        /// <summary>
        /// Synchronously send data using the the UDP protocol.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task<string> SendSync(CommunicationData data) // => SendSync(data.First().Item1, (int)data.First().Item2.TotalMilliseconds);
        {
            var appender = new DataAppender() { Pattern = data.Pattern, MasterTimeout = data.First().Item2, DataTimeout = data.First().Item2 };

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
        /// Send data to a specific endpoint.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="sendTo">Client to send data to.</param>
        /// <returns>True if the send was successful, false if it failed.</returns>
        public bool SendTo(string data, IPEndPoint sendTo)
        {
            if ((sendTo?.Address?.ToString() ?? "0.0.0.0") == "0.0.0.0")
            {
                return false;
            }

            try
            {
                var send = $"{Prepend}{data}{Append}";

                lock (Sync)
                {
                    IsSending = true;
                    try
                    {
                        isSendingSync = false;
                        client.Send(Encoding.ASCII.GetBytes(send), send.Length, sendTo);
                    }
                    catch
                    {
                        IsSending = false;
                        return false;
                    }

                    IsSending = false;
                }
            }
            catch
            {
                OnNotify("There was an error sending data: " + data, MessageTypes.Critical);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Notify the message router that a new message was received.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="messageType">The type of message generated.</param>
        protected virtual void OnNotify(string message, MessageTypes messageType = MessageTypes.Information)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));

            if (messageType == MessageTypes.Receive)
            {
                DataReceived?.Invoke(this, message);
            }
        }

        /// <summary>
        /// Delegate called when the client receives data.
        /// </summary>
        /// <param name="ar">Result generated from the clients receive.</param>
        private void OnReceive(IAsyncResult ar)
        {
            if (isSendingSync)
            {
                return;
            }

            try
            {
                string message;

                lock (Sync)
                {
                    message = Encoding.ASCII.GetString(client.EndReceive(ar, ref endPoint));
                    message = message.TrimEnd('\0');
                }

                OnNotify(message, MessageTypes.Receive);
                client.BeginReceive(OnReceive, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                // ignore this exception, it's intended operation:
                // To cancel a pending call to the BeginConnect() method, close the Socket.
                // When the Close() method is called while an asynchronous operation is in progress,
                // the callback provided to the BeginConnect() method is called. A subsequent call
                // to the EndConnect(IAsyncResult) method will throw an ObjectDisposedException
                // to indicate that the operation has been cancelled.
            }
        }

        /// <summary>
        /// Delegate to call when the client has completed sending.
        /// </summary>
        /// <param name="ar">The send result.</param>
        private void OnSend(IAsyncResult ar)
        {
            try
            {
                client.EndSend(ar);
            }
            catch
            {
                // ignore
            }
        }
    }
}
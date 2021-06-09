//-----------------------------------------------------------------------
// <copyright file="SubCUdpSender.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators
{
    using SubCTools.Models;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Class to handle UDP communication.
    /// </summary>
    public class SubCUdpSender
    {
        /// <summary>
        /// Transmission timeout.
        /// </summary>
        private const int DefaultTimeout = 500;

        /// <summary>
        /// Calls <see cref="SendSync(string, EthernetAddress, int)"/> in a background task.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="address">The <see cref="EthernetAddress"/> to send the data.</param>
        /// <param name="timeout">The amount of time after which a receive call will time out.</param>
        /// <returns>Any data it receives.</returns>
        public static Task<string> SendSyncAsync(string data, EthernetAddress address, int timeout = DefaultTimeout)
        {
            return Task.Run(() => SendSync(data, address, timeout));
        }

        /// <summary>
        /// Synchronously send data over UDP.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="address">The <see cref="EthernetAddress"/> to send the data.</param>
        /// <param name="timeout">The amount of time after which a receive call will time out.</param>
        /// <returns>Any data it receives.</returns>
        public static string SendSync(string data, EthernetAddress address, int timeout = DefaultTimeout)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { ReceiveTimeout = timeout })
            {
                var endPoint = new IPEndPoint(address.Address, address.Port) as EndPoint;

                try
                {
                    socket.SendTo(Encoding.ASCII.GetBytes(data), endPoint);
                }
                catch (SocketException e)
                {
                    throw new SocketException(e.ErrorCode);
                }

                var buffer = new byte[1024];

                try
                {
                    var read = socket.ReceiveFrom(buffer, ref endPoint);
                    socket.Close();
                    return read != 0 ? Encoding.ASCII.GetString(buffer) : string.Empty;
                }
                catch (SocketException e)
                {
                    throw new SocketException(e.ErrorCode);
                }
            }
        }
    }
}
////    public class SubCUDPSender : CommunicatorBase, IUDPCommunicator
////    {
////        UdpClient sender;
////        IPEndPoint endPoint;

////        IList<IPEndPoint> receivers = new List<IPEndPoint>();

////        static bool sendingSync = false;
////        int port;

////        ~SubCUDPSender()
////        {
////            if (sender != null)
////            {
////                try
////                {
////                    sender.Close();
////                }
////                catch { }
////            }
////        }

////        public event EventHandler<DataSentToEventArgs> DataSentTo;
////        public event EventHandler<IntChangedEventArgs> PortChanged;

////        public override string Status
////        {
////            get
////            {
////                return Address + " is active on port " + Port;
////            }
////        }

////        public override Tuple<string, int> PackedAddress
////        {
////            get
////            {
////                return base.PackedAddress;
////            }

////            set
////            {
////                if (base.PackedAddress != value)
////                {
////                    Address = value.Item1;
////                    Port = value.Item2;
////                    base.PackedAddress = value;
////                }
////            }
////        }

////        /// <summary>
////        /// Address to send data
////        /// </summary>
////        public override string Address
////        {
////            get
////            {
////                return address;
////            }
////            set
////            {
////                IPAddress ipAddress;
////                if (IPAddress.TryParse(value, out ipAddress))
////                {
////                    address = ipAddress.ToString();
////                    OnAddressChanged(new StringChangedEventArgs(ipAddress.ToString()));
////                }
////            }
////        }

////        public int Port
////        {
////            get
////            {
////                return port; 
////            }
////            set
////            {
////                if (port != value && SubCTools.Helpers.Numbers.IsInt(value) && port != value)
////                {
////                    port = SubCTools.Helpers.Numbers.Clamp(value, 1, 61001);
////                    PortChanged?.Invoke(this, new IntChangedEventArgs(value));
////                }
////            }
////        }

////        public override bool Connect()
////        {
////            try
////            {
////                sender = new UdpClient();
////                Console.WriteLine("Address: {0} Port {1}", Address, Port);
////                endPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
////                //sender.BeginReceive(new AsyncCallback(OnReceive), null);
////                return base.Connect();
////            }
////            catch (System.Net.Sockets.SocketException e)
////            {
////                Console.WriteLine(e.ToString());
////                return false;
////            }
////        }

////        public override void Disconnect()
////        {
////            try
////            {
////                base.Disconnect();
////                sender.Close();
////            }
////            catch { }
////        }

////        /// <summary>
////        /// Synchronously send data to the client. Usage: SendSync("test", 1000).ContinueWith((t) => { Console.WriteLine(t.Result); }); You may need to sync to the UI context
////        /// </summary>
////        /// <param name="data">Data to send</param>
////        /// <param name="wait">How long to wait for a response</param>
////        /// <returns>A task with the received data</returns>
////        public override Task<string> SendSync(string data, int wait)
////        {
////            data = Prepend + data + Append;

////            return Task<string>.Factory.StartNew(() =>
////            {
////                //set the sending sync flag to true incase OnReceive is called
////                sendingSync = true;

////                try
////                {
////                    //close the client to prevent OnReceive from being activated
////                    sender.Close();
////                    //create a new UDP client without begin receive
////                    sender = new UdpClient();

////                    //lock on the sync in case the AsyncSender is using it
////                    lock (sync)
////                    {
////                        //Console.WriteLine("Sending sync: " + data);
////                        //send the data

////                        sender.Send(Encoding.ASCII.GetBytes(data), data.Length, endPoint);

////                        //Console.WriteLine("Data sent, waiting: " + wait + "ms");

////                        //wait to get a response
////                        Thread.Sleep(wait);
////                    }

////                    //get the result
////                    string result = "";

////                    while (sender.Available > 0)
////                    {
////                        result += Encoding.ASCII.GetString(sender.Receive(ref endPoint));
////                    }

////                    //Console.WriteLine("Result: " + result);

////                    //reset the sync flag in case the async sender wants to use it
////                    sendingSync = false;

////                    //start listening
////                    //sender.BeginReceive(new AsyncCallback(OnReceive), null);

////                    return result;
////                }
////                catch
////                {
////                    sendingSync = false;
////                    //start listening
////                    //sender.BeginReceive(new AsyncCallback(OnReceive), null);
////                    return "";
////                }
////            });

////        }

////        public override bool Send(string data)
////        {
////            try
////            {
////                SendTo(data, endPoint);
////                //OnNotify(data, MessageTypes.Transmit);
////                return true;
////            }
////            catch
////            {
////                return false;
////            }
////        }

////        public bool SendTo(string data, IPEndPoint sendTo)
////        {
////            data = Prepend + data + Append;

////            //Console.WriteLine("UDP Sending: <" + SubCTools.Helpers.Strings.StringToHex(data) + ">");

////            bool result;

////            try
////            {
////                lock (sync)
////                {
////                    try
////                    {
////                        sendingSync = false;
////                        sender.Send(Encoding.ASCII.GetBytes(data), data.Length, sendTo);
////                        result = true;
////                    }
////                    catch
////                    {
////                        result = false;
////                    }
////                }
////            }
////            catch
////            {
////                OnNotify("There was an error sending data: " + data, MessageTypes.Critical);
////                result = false;
////            }

////            return result;

////        }

////        public void SendTo(string data, string[] addressArgs)
////        {
////            if (!ValidateArgs(addressArgs))
////            {
////                return;
////            }

////            IPEndPoint sendTo = receivers.FirstOrDefault(c => c.Address.ToString() == addressArgs[0] &&
////                c.Port == Convert.ToInt32(addressArgs[1]));

////            if (sendTo != null)
////            {
////                SendTo(data, sendTo);
////            }
////            else
////            {
////                OnNotify("Could not send to: " + addressArgs[0], MessageTypes.Critical);
////            }
////        }

////        /// <summary>
////        /// Add an address with a port to the receiver list
////        /// </summary>
////        /// <param name="addressArgs"></param>
////        public void AddAddress(string[] addressArgs)
////        {
////            int port = Convert.ToInt32(addressArgs[1]);

////            IPAddress ipAddress;
////            if (IPAddress.TryParse(addressArgs[0], out ipAddress))
////            {
////                receivers.Add(new IPEndPoint(ipAddress, port));
////            }
////        }

////        public void RemoveAddress(string[] addressArgs)
////        {
////            if (!ValidateArgs(addressArgs))
////            {
////                return;
////            }

////            IPEndPoint client = receivers.FirstOrDefault(c => c.Address.ToString() == addressArgs[0] &&
////                c.Port == Convert.ToInt32(addressArgs[1]));

////            if (client != null)
////            {
////                receivers.Remove(client);
////            }
////            else
////            {
////                OnNotify("Address: " + addressArgs[0] + " does not exist. Could not remove.", MessageTypes.Information);
////            }
////        }

////        bool ValidateArgs(string[] addressArgs)
////        {
////            if (addressArgs.Length < 2)
////            {
////                throw new ArgumentException("Improper usage, needs an address and port.");
////            }

////            if (!SubCTools.Helpers.Numbers.IsInt(addressArgs[1]))
////            {
////                throw new ArgumentException("Improper usage, port must be an int.");
////            }

////            return true;
////        }

////        //void OnReceive(IAsyncResult ar)
////        //{
////        //    if (sendingSync)
////        //        return;

////        //    try
////        //    {
////        //        string message = "";

////        //        lock (sync)
////        //        {
////        //            message = Encoding.ASCII.GetString(sender.EndReceive(ar, ref endPoint));
////        //            message = message.TrimEnd('\0');
////        //            Console.WriteLine("Async message received: <" + message + ">");
////        //        }

////        //        Console.WriteLine("Notifying");
////        //        OnNotify(message, MessageTypes.Receive);
////        //        sender.BeginReceive(new AsyncCallback(OnReceive), null);
////        //    }
////        //    catch
////        //    {
////        //        //ignore this exception, it's intended operation:
////        //        //To cancel a pending call to the BeginConnect() method, close the Socket. 
////        //        //When the Close() method is called while an asynchronous operation is in progress, 
////        //        //the callback provided to the BeginConnect() method is called. A subsequent call 
////        //        //to the EndConnect(IAsyncResult) method will throw an ObjectDisposedException 
////        //        //to indicate that the operation has been cancelled.
////        //    }
////        //}

////        private void OnSend(IAsyncResult ar)
////        {
////            try
////            {
////                sender.EndSend(ar);
////            }
////            catch
////            { }
////        }
////    }
////}

//-----------------------------------------------------------------------
// <copyright file="SubCSerial.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators
{
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Front end to SerialPort for easy transmission of data over RS232 serial.
    /// </summary>
    public sealed class SubCSerial : ICommunicator, IDisposable
    {
        /// <summary>
        /// The default value for the data timeout.
        /// </summary>
        private const int DefaultDataTimeout = 100;

        /// <summary>
        /// The default value for the master timeout.
        /// </summary>
        private const int DefaultMasterTimeout = 250;

        /// <summary>
        /// Sync object to prevent multiple threads trying to read/write to the port.
        /// </summary>
        private static readonly object Sync = new object();

        /// <summary>
        /// Default data appending object to store data.
        /// </summary>
        private readonly DataAppender dataAppender;

        /// <summary>
        /// The invocation list for all the delegates to be executed when data is received.
        /// </summary>
        private readonly List<EventHandler> invocationList = new List<EventHandler>();

        /// <summary>
        /// Underlying serial port for reading/writing data.
        /// </summary>
        private readonly ISerialPort serialPort;

        /// <summary>
        /// Time in milliseconds that the appender will wait until it sends off it's appended data.
        /// </summary>
        private TimeSpan dataTimeout = TimeSpan.FromMilliseconds(DefaultDataTimeout);

        /// <summary>
        /// Enable data terminal read.
        /// </summary>
        private bool dtrEnable;

        /// <summary>
        /// True if serial port is connected.
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// True if the serial port is sending.
        /// </summary>
        private bool isSending;

        /// <summary>
        /// Value for letting class know if it's currently sending sync.
        /// </summary>
        private bool isSyncSending = false;

        /// <summary>
        /// Default time in milliseconds before the appender forces a new append.
        /// </summary>
        private TimeSpan masterTimeout = TimeSpan.FromMilliseconds(DefaultMasterTimeout);

        /// <summary>
        /// Enable Ready To Send.
        /// </summary>
        private bool rtsEnable;

        /// <summary>
        /// String to terminate the appender on.
        /// </summary>
        private string terminator = string.Empty;

        /// <summary>
        /// True if you're waiting for a new line before returning from data appender.
        /// </summary>
        private bool waitForNewLine;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCSerial"/> class. Creates it's own default DataAppender.
        /// </summary>
        public SubCSerial()
            : this(
                new DataAppender(
                    TimeSpan.FromMilliseconds(DefaultMasterTimeout),
                    TimeSpan.FromMilliseconds(DefaultDataTimeout)), new SubCSerialPort())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCSerial"/> class.
        /// </summary>
        /// <param name="dataAppender">Data appender to use to build commands.</param>
        public SubCSerial(DataAppender dataAppender)
            : this(dataAppender, new SubCSerialPort())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCSerial"/> class.
        /// </summary>
        /// <param name="dataAppender">Pre-configured data appending object.</param>
        /// <param name="serialPort">Base Serial port to use for unit testing.</param>
        public SubCSerial(DataAppender dataAppender, ISerialPort serialPort)
        {
            // serialPort = new SerialPort();
            this.serialPort = serialPort;

            this.dataAppender = dataAppender;
            this.dataAppender.Notify += DataAppender_Notify;
            this.dataAppender.Terminator = "\n";

            invocationList.Add(new EventHandler(AppendData));
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SubCSerial"/> class.
        /// </summary>
        ~SubCSerial()
        {
            serialPort?.Dispose();
        }

        /// <summary>
        /// Event to fire when data is received on the serial port
        /// </summary>
        public event EventHandler<string> DataReceived;

        /// <summary>
        /// Event to fire when the IsConnected property changes
        /// </summary>
        public event EventHandler<bool> IsConnectedChanged;

        /// <summary>
        /// Event to fire when the IsSending property changes
        /// </summary>
        public event EventHandler<bool> IsSendingChanged;

        /// <summary>
        /// Event to notify any incoming or outgoing messages
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Gets or sets Address which the serial object will connect.
        /// </summary>
        public SerialAddress Address { get; set; }

        /// <summary>
        /// Gets or sets Generic implementation of IMiniCommunicators address.
        /// </summary>
        CommunicatorAddress IMiniCommunicator<CommunicatorAddress, CommunicationData, string>.Address
        {
            get => Address;

            set
            {
                var port = value.ContainsKey(nameof(SerialAddress.PortDescription)) ? value[nameof(SerialAddress.PortDescription)] : string.Empty;
                var baud = value.ContainsKey(nameof(SerialAddress.BaudRate)) ? (int.TryParse(value[nameof(SerialAddress.BaudRate)], out var b) ? b : 0) : 0;
                Address = new SerialAddress(port, baud);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the class will append data to all the data sent.
        /// </summary>
        public string Append { get; set; } = "\n";

        /// <summary>
        /// Gets or sets Number of milliseconds the data appender will wait for incoming data.
        /// </summary>
        public TimeSpan DataTimeout
        {
            get => this.dataTimeout;

            set
            {
                if (this.dataTimeout != value)
                {
                    this.dataTimeout = value;
                    this.dataAppender.DataTimeout = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether data terminal ready.
        /// </summary>
        public bool DTREnable
        {
            get => dtrEnable;

            set
            {
                if (DTREnable != value)
                {
                    dtrEnable = value;
                    if (serialPort != null)
                    {
                        serialPort.DtrEnable = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the serial port is currently connected.
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
                IsConnectedChanged?.Invoke(this, this.isConnected);
                OnNotify(Status, MessageTypes.Connection);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the serial port is currently writing data to the port.
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
                IsSendingChanged?.Invoke(this, isSending);
            }
        }

        /// <summary>
        /// Gets or sets the Number of milliseconds the data appender will append data before forcing a send off and starting a new append.
        /// </summary>
        public TimeSpan MasterTimeout
        {
            get => this.masterTimeout;

            set
            {
                if (this.masterTimeout != value)
                {
                    this.masterTimeout = value;
                    this.dataAppender.MasterTimeout = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value on how to process the output. E.g. you can set a function to remove a preceding character.
        /// </summary>
        public Func<string, string> Output { get; set; } = (s) => s;

        /// <summary>
        /// Gets or sets the value to prepend to the beginning of the string being sent.
        /// </summary>
        public string Prepend { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether request to send is enabled.
        /// </summary>
        public bool RTSEnable
        {
            get => rtsEnable;

            set
            {
                if (rtsEnable != value)
                {
                    rtsEnable = value;
                    if (serialPort != null)
                    {
                        serialPort.RtsEnable = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the value to wait before allowing the send call to be made again.
        /// </summary>
        public TimeSpan SleepAfterSend { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets get the port name, is connected, and baud rate.
        /// </summary>
        public string Status => $"{Address?.ComPort ?? "Port"} is {(IsConnected ? "connected @ " + Address?.BaudRate + " baud" : "disconnected")}";

        /// <summary>
        /// Gets or sets String to determine end of line so data appender can send off data.
        /// </summary>
        public string Terminator
        {
            get => this.terminator;

            set
            {
                if (this.terminator != value)
                {
                    this.terminator = value;
                    this.dataAppender.Terminator = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to wait for a newline before starting a new data append.
        /// </summary>
        public bool WaitForNewLine
        {
            get => this.waitForNewLine;

            set
            {
                if (this.waitForNewLine != value)
                {
                    this.waitForNewLine = value;
                    this.dataAppender.Terminator = value ? Environment.NewLine : string.Empty;
                }
            }
        }

        /// <summary>
        /// Connect to the member address.
        /// </summary>
        /// <returns>True if connect is successful.</returns>
        public async Task<bool> ConnectAsync()
        {
            return await ConnectAsync(Address);
        }

        /// <summary>
        /// Connect to specified generic address.
        /// </summary>
        /// <param name="address">Address to connect.</param>
        /// <returns>True if connect was successful.</returns>
        public async Task<bool> ConnectAsync(CommunicatorAddress address)
        {
            return await ConnectAsync(new SerialAddress(address[nameof(SerialAddress.PortDescription)], Convert.ToInt32(address[nameof(SerialAddress.BaudRate)])));
        }

        /// <summary>
        /// Connect to specified SerialAddress address.
        /// </summary>
        /// <param name="address">Address to connect.</param>
        /// <returns>True if connect was successful.</returns>
        public async Task<bool> ConnectAsync(SerialAddress address)
        {
            if (string.IsNullOrEmpty(address?.ComPort))
            {
                return false;
            }

            // Disconnect if you're already open
            try
            {
                if (serialPort.IsOpen)
                {
                    await DisconnectAsync();
                }
            }
            catch (Exception exception)
            {
                OnNotify(exception.ToString(), MessageTypes.Critical);
                return false;
            }

            try
            {
                // Set the appropriate properties
                serialPort.PortName = address.ComPort;
                serialPort.BaudRate = address.BaudRate;
            }
            catch (Exception)
            {
                // ignored
            }

            // Set the member address
            Address = address;

            // Try to connect on a new thread to prevent the UI from stuttering
            await Task.Run(() =>
            {
                try
                {
                    lock (Sync)
                    {
                        serialPort.Open();
                    }

                    serialPort.DataReceived += SerialPort_DataReceived;
                }
                catch
                {
                    // ignored
                }
            });

            return IsConnected = serialPort?.IsOpen ?? false;
        }

        /// <summary>
        /// Disconnect from the currently connected port.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                serialPort.DataReceived -= SerialPort_DataReceived;

                if (this.dataAppender.IsStarted)
                {
                    this.dataAppender.Stop();
                }

                // if (!syncCompletionSource?.Task.IsCompleted ?? false)
                // {
                //    syncCompletionSource.TrySetResult(string.Empty);
                // }
                lock (Sync)
                {
                    this.serialPort.Close();
                }

                IsConnected = false;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Disconnect from the currently connected port.
        /// </summary>
        /// <returns>Empty Task.</returns>
        public async Task DisconnectAsync()
        {
            await Task.Run(() => Disconnect());
        }

        /// <summary>
        /// Dispose the serial port object.
        /// </summary>
        public void Dispose()
        {
            serialPort?.Dispose();
        }

        /// <summary>
        /// Send data on a separate thread.
        /// </summary>
        /// <param name="data">Data you wish to send.</param>
        /// <returns>Empty Task.</returns>
        public async Task SendAsync(CommunicationData data)
        {
            IsSending = true;

            // if (!syncCompletionSource?.Task.IsCompleted ?? false)
            // {
            //    syncCompletionSource.TrySetResult(string.Empty);
            // }
            dataAppender.Pattern = data.Pattern;

            foreach (var item in data)
            {
                Send(item.Item1);
                OnNotify(item.Item1 + Append, MessageTypes.Transmit);
                await Task.Delay(item.Item2);
            }

            IsSending = false;
        }

        /// <summary>
        /// Synchronously send data, wait for a response.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>Response to data.</returns>
        public async Task<string> SendSync(CommunicationData data)
        {
            if (IsSending || isSyncSending)
            {
                return string.Empty;
            }

            isSyncSending = true;

            using var appender = new DataAppender(data.First().Item2, data.First().Item2) { Pattern = data.Pattern };
            var syncCompletionSource = new TaskCompletionSource<string>();

            var toSend = data.First();

            var serialDataHandler = new EventHandler((s, e) =>
            {
                var rx = serialPort.ReadExisting();
                appender.Append(rx);
            });

            // clear the invocation list
            invocationList.Clear();
            invocationList.Add(serialDataHandler);

            var notifyHandler = new EventHandler<NotifyEventArgs>((s, e) =>
            {
                syncCompletionSource.TrySetResult(e.Message);
            });

            appender.Notify += notifyHandler;

            await SendAsync(toSend.Item1, true);
            appender.Start();
            var result = await syncCompletionSource.Task;

            // check to see if the port is open before discarding buffer
            if (serialPort?.IsOpen ?? false)
            {
                serialPort?.DiscardInBuffer();
            }

            appender.Notify -= notifyHandler;

            invocationList.Clear();

            invocationList.Add(new EventHandler(AppendData));

            isSyncSending = false;

            return result;
        }

        /// <summary>
        /// Send data, receive data in one call.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <returns>Data.</returns>
        public Task<string> SendSync(string data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Address of class.
        /// </summary>
        /// <returns>Friendly formatted address.</returns>
        public override string ToString()
        {
            return this.Address?.ComPort + "@" + this.Address?.BaudRate;
        }

        /// <summary>
        /// Delegate to call when to append data.
        /// </summary>
        /// <param name="sender">Object executing delegate.</param>
        /// <param name="e">Serial data that's been received.</param>
        private void AppendData(object sender, EventArgs e)
        {
            dataAppender.Append(serialPort.ReadExisting());
        }

        /// <summary>
        /// Event fired when the data appender has completed an append.
        /// </summary>
        /// <param name="sender">Object which fired event.</param>
        /// <param name="e">Notify Event Args.</param>
        private void DataAppender_Notify(object sender, NotifyEventArgs e)
        {
            OnNotify(Output(e.Message));
        }

        /// <summary>
        /// Send message to message router to be consumed by someone else.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <param name="messageType">Type of message, i.e. escalation.</param>
        private void OnNotify(string message, MessageTypes messageType = MessageTypes.Receive)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));

            if (messageType == MessageTypes.Receive)
            {
                DataReceived?.Invoke(this, message);
            }
        }

        /// <summary>
        /// Base implementation of sending data.
        /// </summary>
        /// <param name="data">Data to write.</param>
        private void Send(string data)
        {
            try
            {
                lock (Sync)
                {
                    if (serialPort.IsOpen)
                    {
                        serialPort.Write(Prepend + data + Append);

                        if (SleepAfterSend != TimeSpan.Zero)
                        {
                            Thread.Sleep(SleepAfterSend);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Send data on a separate thread.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="suppressIsSending">True to not fire the IsSending event.</param>
        /// <returns>Empty Task.</returns>
        private async Task SendAsync(string data, bool suppressIsSending = false)
        {
            await Task.Run(() =>
{
    if (!suppressIsSending)
    {
        IsSending = true;
    }

    Send(data);

    if (!suppressIsSending)
    {
        OnNotify(data, MessageTypes.Transmit);

        IsSending = false;
    }
});
        }

        /// <summary>
        /// Event fired by the serial port for receiving incoming data.
        /// </summary>
        /// <param name="sender">Object that fired event.</param>
        /// <param name="e">DataReceived Event Args.</param>
        private void SerialPort_DataReceived(object sender, EventArgs e)
        {
            foreach (var item in invocationList.ToList())
            {
                // invoke only if you're not null
                item?.Invoke(sender, e);
            }
        }
    }
}
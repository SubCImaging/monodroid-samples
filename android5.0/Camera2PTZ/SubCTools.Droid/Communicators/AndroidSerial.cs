//-----------------------------------------------------------------------
// <copyright file="SubCTeensy.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Communicators
{
    using Android.App;
    using Android.Content;
    using Android.Hardware.Usb;
    using Android.Widget;
    using Hoho.Android.UsbSerial.Driver;
    using Hoho.Android.UsbSerial.Util;
    using SubCTools.Communicators;
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class AndroidSerial : ICommunicator
    {
        /// <summary>
        /// Sync to lock
        /// </summary>
        private static readonly object Sync = new object();

        /// <summary>
        /// USed for permissions
        /// </summary>
        private readonly Activity activity;

        /// <summary>
        /// Grab serial device from USBManager
        /// </summary>
        private readonly UsbManager usbManager;

        /// <summary>
        /// For handling incoming information and appending it together
        /// </summary>
        private SerialAppender appender;

        /// <summary>
        /// Is the device connected
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// Serial port connected to
        /// </summary>
        private IUsbSerialPort port;

        /// <summary>
        /// Sending and receiving serial data on
        /// </summary>
        private SerialInputOutputManager serialIoManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AndroidSerial"/> class.
        /// </summary>
        /// <param name="activity">Activity used for permission handling</param>
        /// <param name="usbManager">Manager to grab device</param>
        public AndroidSerial(Activity activity, UsbManager usbManager)
        {
            this.activity = activity;
            this.usbManager = usbManager;

            appender = new SerialAppender()
            {
                Terminator = "\n"
            };
            appender.Notify += (s, e) => Appender_Notify(e);
        }

        /// <summary>
        /// Fire when data is received from serial port
        /// </summary>
        public event EventHandler<string> DataReceived;

        /// <summary>
        /// Fire when is connected changes
        /// </summary>
        public event EventHandler<bool> IsConnectedChanged;

        /// <summary>
        /// Fire when is sending changes
        /// </summary>
        public event EventHandler<bool> IsSendingChanged;

        /// <summary>
        /// Notify the message router with relevant data
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Gets or sets the address to connect
        /// </summary>
        public CommunicatorAddress Address
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the string to append to what's being sent
        /// </summary>
        public string Append { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the port is connected
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return isConnected;
            }

            private set
            {
                if (isConnected == value)
                {
                    return;
                }

                isConnected = value;
                IsConnectedChanged?.Invoke(this, isConnected);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the port is sending information
        /// </summary>
        public bool IsSending
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets or sets func to process output
        /// </summary>
        public Func<string, string> Output { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Gets or sets string to prepend to data sent
        /// </summary>
        public string Prepend { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Gets connection status
        /// </summary>
        public string Status => IsConnected ? "Connected" : "Disconnected";

        /// <summary>
        /// Connect to set Address
        /// </summary>
        /// <returns>True if connected</returns>
        public Task<bool> ConnectAsync() => ConnectAsync(Address);

        /// <summary>
        /// Connect to given address
        /// </summary>
        /// <param name="address">Address to connect to</param>
        /// <returns>True if connected</returns>
        public async Task<bool> ConnectAsync(CommunicatorAddress address)
        {
            var port = (address as AndroidCommunicatorAddress).Port;
            var baudRate = Convert.ToInt32(address[nameof(AndroidCommunicatorAddress.BaudRate)]);

            var ports = await GetPortsAsync();

            this.port = (from p in ports
                         where p.ToString() == port.ToString()
                         select p).FirstOrDefault();

            if (this.port == null)
            {
                return IsConnected = false;
            }

            serialIoManager = new SerialInputOutputManager(this.port)
            {
                BaudRate = baudRate,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
            };

            serialIoManager.DataReceived += (s, e) => SerialIoManager_DataReceived(e);

            try
            {
                if (usbManager.HasPermission(port.Driver.Device))
                {
                    serialIoManager.Open(usbManager);
                    return IsConnected = true;
                }

                SubCLogger.Instance.Write($"Get permission?\n", "AutoDetect.txt", DroidSystem.LogDirectory);
                var permissionGranted = await usbManager.RequestPermissionAsync(port.Driver.Device, activity.BaseContext);
                if (permissionGranted)
                {
                    serialIoManager.Open(usbManager);
                }
                else
                {
                    return IsConnected = false;
                }
            }
            catch (Java.IO.IOException e)
            {
#if DEBUG
                SubCLogger.Instance.Write($"Connect fail: {e}", directory: Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies) + "/Log/");
#endif
                return IsConnected = false;
            }

            return IsConnected = true;
        }

        /// <summary>
        /// Set is connected to false
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DisconnectAsync() => IsConnected = false;

        /// <summary>
        /// Get all the drivers from the USB manager
        /// </summary>
        /// <returns>Enumerable of serial drivers</returns>
        public async Task<IEnumerable<IUsbSerialDriver>> GetDriversAsync() => usbManager.FindAllDriversAsync();

        /// <summary>
        /// Get all the ports from all the drivers
        /// </summary>
        /// <returns>Enumerable of ports</returns>
        public async Task<IEnumerable<IUsbSerialPort>> GetPortsAsync() => (await GetDriversAsync()).SelectMany(d => d.Ports);

        /// <summary>
        /// Receive data async
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task ReceiveAsync()
        {
            throw new Exception();
        }

        public void Send(byte[] bytes)
        {
            try
            {
                port?.Write(bytes, 1000);
            }
            catch
            {
                // unable to write, is port disconnected?
            }
        }

        /// <summary>
        /// Send data to the connect port
        /// </summary>
        /// <param name="data">Data to send</param>
        public void Send(CommunicationData data)
        {
            var d = (data.FirstOrDefault()?.Item1 ?? string.Empty) + Append;

            var dataBytes = Encoding.ASCII.GetBytes(d);

            try
            {
                port?.Write(dataBytes, 1000);
            }
            catch
            {
                // unable to write, is port disconnected?
            }
        }

        /// <summary>
        /// Send data to the connect port
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendAsync(CommunicationData data)
        {
            Send(data);
        }

        /// <summary>
        /// Send data to the port and wait for a response to come back
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <returns>Response from teensy</returns>
        public async Task<string> SendSync(CommunicationData data)
        {
            var appender = new DataAppender() { Pattern = data.Pattern, MasterTimeout = data.First().Item2, DataTimeout = data.First().Item2 };

            var tcs = new TaskCompletionSource<string>();

            var appenderHandler = new EventHandler<NotifyEventArgs>((s, e) => tcs.TrySetResult(e.Message));

            appender.Notify += appenderHandler;

            var handler = new EventHandler<string>((s, e) => appender.Append(e));

            DataReceived += handler;

            await SendAsync(data.First().Item1);
            appender.Start();
            var result = await tcs.Task;

            DataReceived -= handler;

            return result;
        }

        /// <summary>
        /// Notification handler from appender
        /// </summary>
        /// <param name="e">Appended data</param>
        private void Appender_Notify(NotifyEventArgs e)
        {
            var message = e.Message;

            Task.Run(() =>
            {
                DataReceived?.Invoke(this, message);
                Notify?.Invoke(this, new NotifyEventArgs(message, MessageTypes.Receive));
            });
        }

        /// <summary>
        /// Serial data received handler
        /// </summary>
        /// <param name="e">Data that was received</param>
        private void SerialIoManager_DataReceived(SerialDataReceivedArgs e)
        {
            var data = Encoding.ASCII.GetString(e.Data);

            // serialio received data
            appender.Append(data);
        }
    }
}
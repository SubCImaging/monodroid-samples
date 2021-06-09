//-----------------------------------------------------------------------
// <copyright file="SubCTeensy.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.IO
{
    using SubCTools.Attributes;
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Droid.IO.AuxDevices;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Interfaces;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Communicates with the Teensy
    /// </summary>
    public class SubCTeensy : DroidBase, INotifiable
    {
        /// <summary>
        /// Character to prepend to messages to be echoed
        /// </summary>
        public const string TeensyEchoCharacter = "@";

        /// <summary>
        /// Character to prepend so that teensy passes the message through unchanged
        /// </summary>
        public const string TeensyPassthroughCharacter = ">";

        /// <summary>
        /// Character to prepend normal teensy commands with
        /// </summary>
        public const string TeensyStartCharacter = "~";

        /// <summary>
        /// Object that communicates of the serial ports on Android
        /// </summary>
        private readonly AndroidSerial serial;

        /// <summary>
        /// Listener for parsing teensy information
        /// </summary>
        private readonly TeensyListener teensyListener;

        /// <summary>
        /// Ambient temperature reading sensor
        /// </summary>
        private double ambientTemp;

        /// <summary>
        /// Version of the teensy firmware
        /// </summary>
        private string firmwareVersion = string.Empty;

        /// <summary>
        /// Humidity reading from sensor
        /// </summary>
        private double humidity;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCTeensy"/> class.
        /// </summary>
        /// <param name="serial">A serial communicator</param>
        /// <param name="interpreter">A command interpreter</param>
        /// <param name="settings">Settings service</param>
        /// <param name="auxInput">An aux device manager</param>
        /// <param name="teensyListener">A teensy listener</param>
        public SubCTeensy(
            AndroidSerial serial,
            ISettingsService settings)
            : base(settings)
        {
            this.serial = serial;
            teensyListener = new TeensyListener(serial);
            AuxManager = new AuxInputManager(serial, settings, teensyListener);

            if (serial.IsConnected)
            {
                GetTeensyState();
            }
            else
            {
                serial.IsConnectedChanged += (s, e) => GetTeensyState();
            }

            teensyListener.TeensyDataReceived += (s, e) => TeensyListener_InfoReceived(e);
            AuxManager.DeviceChanged += (s, e) => DeviceChanged?.Invoke(s, e);

            // create the expansion IO's
            ExpansionInput = new ExpansionInput(settings);
            ExpansionOutput = new ExpansionOutput(settings, serial, teensyListener);

            ExpansionPort = new ExpansionPort(serial);
        }

        /// <summary>
        /// Event to call when the device changes
        /// </summary>
        public event EventHandler<AuxDeviceArgs> DeviceChanged;

        public event EventHandler<string> FirmwareReceived;

        /// <summary>
        /// When teensy is looking for informaion that only it wants back
        /// </summary>
        public event EventHandler<string> WhisperReceived;

        /// <summary>
        /// Gets the ambient temperature reading from sensor
        /// </summary>
        [RemoteState]
        public double AmbientTemp
        {
            get => ambientTemp;
            private set
            {
                ambientTemp = value;
                OnNotify($"{nameof(AmbientTemp)}:{AmbientTemp}");
            }
        }

        /// <summary>
        /// Gets the Auxilliary output manager
        /// </summary>
        public AuxInputManager AuxManager { get; }

        /// <summary>
        /// Gets the expansion input
        /// </summary>
        public ExpansionInput ExpansionInput { get; }

        /// <summary>
        /// Gets the expansion output
        /// </summary>
        public ExpansionOutput ExpansionOutput { get; }

        public ExpansionPort ExpansionPort { get; }

        /// <summary>
        /// Gets the version of the teensy firmware
        /// </summary>
        [RemoteState]
        public string FirmwareVersion
        {
            get => firmwareVersion;
            private set
            {
                firmwareVersion = value;
                OnNotify($"{nameof(FirmwareVersion)}:{FirmwareVersion}");
                FirmwareReceived?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Gets the humidity reading from sensor
        /// </summary>
        [RemoteState]
        public double Humidity
        {
            get => humidity;
            private set
            {
                humidity = value;
                OnNotify($"{nameof(Humidity)}:{Humidity}");
            }
        }

        /// <summary>
        /// Load all properties from settings service
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();

            ExpansionInput.LoadSettings();
            ExpansionOutput.LoadSettings();

            Task.Run(async () =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                AuxManager.LoadSettings();
            });
        }

        /// <summary>
        /// Receives commands from rayfin that must be sent to the Teensy.  Splits these commands by line break.
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">event args</param>
        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            var data = e.Message;

            if (string.IsNullOrEmpty(data)) { return; }

            // in case you get multiple lines of data, split them on a new line and send one at a time
            var split = data.Split('\n');

            foreach (var item in split)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }

                var toSend = item;

                if (!item.StartsWith(TeensyPassthroughCharacter) && !item.StartsWith(TeensyStartCharacter) && !item.StartsWith(TeensyEchoCharacter))
                {
                    toSend = TeensyEchoCharacter + item;
                }

                Send(toSend);
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Trim the end of the data, and send to the teensy
        /// </summary>
        /// <param name="data">Data to send to teensy</param>
        public void Send(string data)
        {
            serial.Send(data.Trim());
        }

        /// <summary>
        /// Set the strobe output of the teensy
        /// </summary>
        /// <param name="output">Strobe output value from 0 - 100</param>
        public async void StrobeOutput(int output)
        {
            output = output.Clamp();
            await serial.SendAsync("~auxsetstrobeoutput:" + output);
        }

        /// <summary>
        /// Called when the connection to the Teensy changes
        /// </summary>
        private async void GetTeensyState()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            await serial.SendAsync(TeensyStartCharacter + "product print firmware");
            await serial.SendAsync(TeensyStartCharacter + "sensor print humidity");
            await serial.SendAsync(TeensyStartCharacter + "sensor print temp");

            // configure imu
            // Set IMU Flags
            await serial.SendAsync("~nmea set update imu:0000000");

            // gyro outputs at a resolution of 10Hz
            await serial.SendAsync("~NMEA set timer:01.000");

            // enable gyro output
            await serial.SendAsync("~nmea set update fusion:1");

            // flip the orientation for production
            // await serial.SendAsync("~nmea set imu axis remap:-y:-x:-z");
            // Removed as per Chad's conversation
            // await serial.SendAsync("~nmea set imu axis remap:-y:-x:+z");
        }

        /// <summary>
        /// Receives and parses communication from the Teensy.
        /// </summary>
        /// <param name="e">teensy info details being reported</param>
        private void TeensyListener_InfoReceived(TeensyInfo e)
        {
            if (e.Type == "&")
            {
                // We want back the result with a $ prepended to it
                WhisperReceived?.Invoke(this, e.Property + (!string.IsNullOrEmpty(e.Value) ? (":" + e.Value) : string.Empty));

                return;
            }

            if (e.Property == "productfirmware")
            {
                FirmwareVersion = Regex.Match(e.Value, @"(SubC Rayfin v\d+.\d+)").Groups[1].Value;
            }
            else if (e.Property == "sensorhumidity")
            {
                if (double.TryParse(e.Value, out var h))
                {
                    Humidity = h;
                }
            }
            else if (e.Property == "sensortemp")
            {
                if (double.TryParse(Regex.Match(e.Value, @"(\d+.\d+)").Groups[1].Value, out var a))
                {
                    AmbientTemp = a;
                }
            }
            else
            {
                if (DroidSystem.Instance.IsDebugging)
                {
                    OnNotify(e.Raw);
                }
            }
        }
    }
}
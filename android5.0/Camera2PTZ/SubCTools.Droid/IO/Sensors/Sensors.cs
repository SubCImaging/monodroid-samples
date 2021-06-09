// <copyright file="Sensors.cs" company="SubC Imaging">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.IO.Sensors
{
    using SubCTools.Attributes;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Class to get all the sensor information.
    /// </summary>
    public class Sensors : DroidBase
    {
        private readonly ICommunicator communicator;
        private double altitude;
        private double depth;
        private double heading;
        private bool isHorizontalOrientation;
        private double roll;
        private double tilt;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sensors" /> class.
        /// </summary>
        /// <param name="communicator">Communicator for generating all the sesnor data.</param>
        public Sensors(ICommunicator communicator, ISettingsService settings) : base(settings)
        {
            communicator.DataReceived += NmeaGenerator_DataReceived;
            this.communicator = communicator;

            communicator.SendAsync("~nmea print imu zero");
        }

        /// <summary>
        /// Event to fire when information is received.
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Gets the altitude if connected.
        /// </summary>
        [RemoteState]
        public double Altitude
        {
            get => altitude;
            private set
            {
                if (altitude == value)
                {
                    return;
                }

                altitude = value;
                OnNotify($"{nameof(Altitude)}:{Altitude}");
            }
        }

        /// <summary>
        /// Gets the depth reading.
        /// </summary>
        [RemoteState]
        public double Depth
        {
            get => depth;
            private set
            {
                if (depth == value)
                {
                    return;
                }

                depth = value;
                OnNotify($"{nameof(Depth)}:{Depth}");
            }
        }

        /// <summary>
        /// Gets a value indicating whether there's an active altitude sensor.
        /// </summary>
        [RemoteState]
        public bool HasAltitudeSensor { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there's an active depth sensor.
        /// </summary>
        [RemoteState]
        public bool HasDepthSensor { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there's an active heading sensor.
        /// </summary>
        [RemoteState]
        public bool HasHeading { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there's an active roll sensor.
        /// </summary>
        [RemoteState]
        public bool HasRoll { get; private set; }

        /// <summary>
        /// Gets a value indicating whether there's an active tilt sensor.
        /// </summary>
        [RemoteState]
        public bool HasTilt { get; private set; }

        /// <summary>
        /// Gets the heading reading.
        /// </summary>
        [RemoteState]
        public double Heading
        {
            get => heading;
            private set
            {
                if (heading == value)
                {
                    return;
                }

                heading = value;
                OnNotify($"{nameof(Heading)}:{Heading}");
            }
        }

        /// <summary>
        /// Gets the IMU Zero.
        /// </summary>
        [RemoteState]
        public string IMUZero { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the orientation is horizontal.
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool IsHorizontalOrientation
        {
            get => isHorizontalOrientation;
            set
            {
                Set(nameof(IsHorizontalOrientation), ref isHorizontalOrientation, value);
                OnNotify($"{nameof(IsHorizontalOrientation)}:{IsHorizontalOrientation}");
            }
        }

        /// <summary>
        /// Gets the roll value.
        /// </summary>
        [RemoteState]
        public double Roll
        {
            get => roll;
            private set
            {
                if (roll == value)
                {
                    return;
                }

                roll = value;
                OnNotify($"{nameof(Roll)}:{Roll}");
            }
        }

        /// <summary>
        /// Gets the tilt reading.
        /// </summary>
        [RemoteState]
        public double Tilt
        {
            get => tilt;
            private set
            {
                if (tilt == value)
                {
                    return;
                }

                tilt = value;
                OnNotify($"{nameof(Tilt)}:{Tilt}", MessageTypes.Information);
            }
        }

        /// <summary>
        /// Set the IMU to horizontal orientation.
        /// </summary>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        [RemoteCommand]
        public async Task HorizontalOrientation()
        {
            IsHorizontalOrientation = true;
            await communicator.SendAsync("~nmea set imu axis remap:-y:-x:+z");
        }

        public override void LoadSettings()
        {
            base.LoadSettings();

            if (IsHorizontalOrientation)
            {
                HorizontalOrientation();
            }
            else
            {
                VerticalOrientation();
            }
        }

        /// <summary>
        /// Reset the IMU zero point.
        /// </summary>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        [RemoteCommand]
        public async Task ResetIMU()
        {
            await communicator.SendAsync("~nmea reset imu zero");
        }

        /// <summary>
        /// Set the IMU to zero.
        /// </summary>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        [RemoteCommand]
        public async Task SetIMUZero()
        {
            await communicator.SendAsync("~nmea set imu zero");
        }

        /// <summary>
        /// Set the IMU to vertical orientation.
        /// </summary>
        /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
        [RemoteCommand]
        public async Task VerticalOrientation()
        {
            IsHorizontalOrientation = false;
            await communicator.SendAsync("~nmea set imu axis remap:-z:-x:-y");
        }

        private void NmeaGenerator_DataReceived(object sender, string e)
        {
            // get the nmea zero value @nmeaimuzero:1.00:0.00:0.00:0.00
            if (e.StartsWith("@nmeaimuzero"))
            {
                IMUZero = e.Replace("@nmeaimuzero:", string.Empty);
                return;
            }

            var data = e.Split(",");

            if (data.FirstOrDefault() == "$--SCF")
            {
                if (data.Length > 3 && double.TryParse(data[2], out var d))
                {
                    Depth = d;
                    HasDepthSensor = true;
                }

                if (data.Length > 5 && double.TryParse(data[4], out var h))
                {
                    Heading = h;
                    HasHeading = true;
                }

                if (data.Length > 7 && double.TryParse(data[6], out var r))
                {
                    HasRoll = true;
                    Roll = r;
                }

                if (data.Length > 8 && double.TryParse(data[7], out var t))
                {
                    HasTilt = true;
                    Tilt = t;
                }
            }
            else if (e.Contains("DBT,"))
            {
                if (double.TryParse(data[1], out var d))
                {
                    Altitude = d * 0.3048;
                    HasAltitudeSensor = true;
                }
            }
        }
    }
}
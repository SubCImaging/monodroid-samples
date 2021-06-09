//-----------------------------------------------------------------------
// <copyright file="PowerController.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;

    /// <summary>
    /// Handle shutdown communication with the teensy.
    /// </summary>
    public class PowerController : DroidBase
    {
        /// <summary>
        /// Shell for turning off the camera.
        /// </summary>
        private readonly IShell shell;

        /// <summary>
        /// Value for checking if the camera is powering down.
        /// </summary>
        private bool isPoweringDown;

        /// <summary>
        /// Is the cmare rebooting.
        /// </summary>
        private bool isRebooting;

        /// <summary>
        /// The time remaining before the camera shuts down.
        /// </summary>
        private TimeSpan shutdownTimeRemaining;

        /// <summary>
        /// Amount of time before the camera will shut down on a power loss.
        /// </summary>
        private TimeSpan timeToShutdown = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerController"/> class.
        /// </summary>
        /// <param name="settings">Save and load all settings.</param>
        /// <param name="shell">Shell used to turn off the camera.</param>
        public PowerController(
            ISettingsService settings,
            IShell shell)
            : base(settings)
        {
            ShutdownTimeRemaining = ShutdownTime;
            this.shell = shell;
            ConfigurePowerListener();
        }

        /// <summary>
        /// Event to fire when shutdown has been cancelled.
        /// </summary>
        public event EventHandler ShutdownCancelled;

        /// <summary>
        /// Event to fire when the camera is shutting down
        /// </summary>
        public event EventHandler ShuttingDown;

        /// <summary>
        /// Gets a value indicating whether the camera is powering down.
        /// </summary>
        public bool IsPoweringDown
        {
            get => isPoweringDown;
            private set
            {
                isPoweringDown = value;
                OnNotify($"{nameof(IsPoweringDown)}:{IsPoweringDown}");
            }
        }

        /// <summary>
        /// Gets a value indicating whether the camera is rebooting.
        /// </summary>
        public bool IsRebooting
        {
            get => isRebooting;
            private set
            {
                isRebooting = value;
                OnNotify($"{nameof(IsRebooting)}:{IsRebooting}");
            }
        }

        /// <summary>
        /// Gets or sets how long to wait before turning the power off.
        /// </summary>
        [Savable]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public TimeSpan ShutdownTime
        {
            get => timeToShutdown;
            set
            {
                if (Set(nameof(ShutdownTime), ref timeToShutdown, value))
                {
                    ShutdownTimeRemaining = value;
                }
            }
        }

        /// <summary>
        /// Gets the shutdown timer.
        /// </summary>
        public System.Timers.Timer ShutdownTimer { get; } = new System.Timers.Timer();

        /// <summary>
        /// Gets the amount of time remaining before the camera shuts down.
        /// </summary>
        public TimeSpan ShutdownTimeRemaining
        {
            get => shutdownTimeRemaining;
            private set
            {
                shutdownTimeRemaining = value;
                OnNotify($"{nameof(ShutdownTimeRemaining)}:{ShutdownTimeRemaining}");
            }
        }

        /// <summary>
        /// Cancels the shutdown sequence.
        /// </summary>
        [RemoteCommand]
        [Alias("CancelPowerOff")]
        public void CancelShutdown()
        {
            ShutdownTimer.Stop();
            ShutdownTimeRemaining = ShutdownTime;
            IsRebooting = false;
            IsPoweringDown = false;
            OnNotify("~powersetandroidack:1", MessageTypes.TeensyCommand);
            ShutdownCancelled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Initiates a hibernation period.
        /// </summary>
        /// <param name="timeOn">The time span to hibernate for.</param>
        [RemoteCommand]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public void Hibernate(TimeSpan timeOn)
        {
            if (timeOn < TimeSpan.FromMinutes(5))
            {
                OnNotify($"Minimum hibernate time is 5 minutes", MessageTypes.Error);
            }
            else
            {
                OnNotify($"~hibernate set time:" + timeOn.TotalSeconds, MessageTypes.TeensyCommand);
            }
        }

        /// <summary>
        /// Sets the Rayfin to shut down after 10 seconds.
        /// </summary>
        [RemoteCommand]
        [Alias("Shutdown")]
        public void PowerOff()
        {
            PowerOff(ShutdownTime);
        }

        /// <summary>
        /// Sets the Rayfin to shut down after a specified period of time.
        /// </summary>
        /// <param name="countdown">The duration of time to wait before shutdown.</param>
        [RemoteCommand]
        [Alias("Shutdown")]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public void PowerOff(TimeSpan countdown)
        {
            ShutdownTimer.Start();
            ShutdownTimeRemaining = countdown;
            OnNotify("~powersetandroidack:0", MessageTypes.TeensyCommand);
            IsPoweringDown = true;
        }

        /// <summary>
        /// Sets the Rayfin to restart after 10 seconds.
        /// </summary>
        [RemoteCommand]
        [Alias("Restart")]
        public void Reboot()
        {
            Reboot(ShutdownTime);
        }

        /// <summary>
        /// Sets the Rayfin to restart after a specified number of seconds.
        /// </summary>
        /// <param name="countdown">The number of seconds to wait before restarting.</param>
        [RemoteCommand]
        [Alias("Restart")]
        public void Reboot(int countdown)
        {
            Reboot(TimeSpan.FromSeconds(countdown));
        }

        /// <summary>
        /// Sets the Rayfin to restart after a specified period of time.
        /// </summary>
        /// <param name="countdown">The duration of time to wait before restarting.</param>
        [RemoteCommand]
        [Alias("Restart")]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public void Reboot(TimeSpan countdown)
        {
            IsRebooting = true;
            ShutdownTimer.Start();
            ShutdownTimeRemaining = countdown;
            IsPoweringDown = false;
        }

        /// <summary>
        /// Set up the shutdown timer listener.
        /// </summary>
        private void ConfigurePowerListener()
        {
            ShutdownTimer.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
            ShutdownTimer.Elapsed += (s, e) =>
            {
                if (ShutdownTimeRemaining.TotalSeconds > 1)
                {
                    ShutdownTimeRemaining -= TimeSpan.FromSeconds(1);
                }
                else
                {
                    ShutdownTimer.Stop();
                    ShuttingDown?.Invoke(this, EventArgs.Empty);

                    // TODO: Find some way to tell the communicator to disconnect (because serial remains connected)
                    shell.ShellSync("reboot" + (isPoweringDown ? " -p" : string.Empty));
                }
            };
        }
    }
}
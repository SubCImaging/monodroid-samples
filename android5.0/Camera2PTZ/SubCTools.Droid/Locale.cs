//-----------------------------------------------------------------------
// <copyright file="Locale.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Helpers
{
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A class to modify the clock in Android.
    /// </summary>
    public class Locale : DroidBase
    {
        /// <summary>
        /// The maximum number of minutes the clock can change before a restart is required.
        /// </summary>
        private const int MaxDrift = 5;

        /// <summary>
        /// The latest year that can be used(After this year the Android OS misbehaves).
        /// </summary>
        private const int MaxYear = 2035;

        /// <summary>
        /// Sync object so if the <see cref="NTPClockDrift" /> is currently being calculated it
        /// won't calculate it again.
        /// </summary>
        private static readonly object Sync = new object();

        /// <summary>
        /// Instance of the <see cref="Locale" /> singleton
        /// </summary>
        private static Lazy<Locale> instance = new Lazy<Locale>(() => new Locale());

        /// <summary>
        /// The threshold of time in milliseconds before the clock will auto sync.
        /// </summary>
        private readonly TimeSpan driftThreshold = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// The interval at which to poll the drift.
        /// </summary>
        private readonly TimeSpan driftUpdate = TimeSpan.FromSeconds(20);

        /// <summary>
        /// <see cref="bool" /> representing whether the clock uses NTP time or manual time.
        /// </summary>
        private bool autoTime;

        /// <summary>
        /// The <see cref="RayfinClockMode" /> of the Rayfin.
        /// </summary>
        private RayfinClockMode clockMode;

        /// <summary>
        /// The <see cref="ConnectionStatus" /> of the NTP Server.
        /// </summary>
        private ConnectionStatus ntpConnectionStatus;

        /// <summary>
        /// The default value for <see cref="Region" />
        /// </summary>
        private string region = "Canada";

        /// <summary>
        /// Prevents a default instance of the <see cref="Locale" /> class from being created.
        /// </summary>
        private Locale()
        {
            var scheduler = new ActionScheduler();

            scheduler.Add("UpdateDrift", driftUpdate, () => UpdateDrift());
            scheduler.Add("UpdateDate", TimeSpan.FromMinutes(1), () => OnNotify($"{nameof(SystemDateTime)}:{SystemDateTime}"));
            scheduler.Add("SyncClock", TimeSpan.FromMinutes(30), () => SyncClock(), true);

            InitializeState();
        }

        /// <summary>
        /// Gets an instance of the <see cref="Locale" /> class
        /// </summary>
        public static Locale Instance => instance.Value;

        /// <summary>
        /// Gets a value indicating whether the <see cref="RayfinClockMode" /> uses NTP time or
        /// manual time.
        /// </summary>
        [RemoteState]
        public bool AutoTime
        {
            get => autoTime;
            private set => Set(nameof(AutoTime), ref autoTime, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="RayfinClockMode" /> of the Rayfin.
        /// </summary>
        [RemoteState]
        public RayfinClockMode ClockMode
        {
            get => clockMode;
            set
            {
                if (Set(nameof(ClockMode), ref clockMode, value))
                {
                    OnNotify($"{nameof(ClockMode)}:{ClockMode}");
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed drift before a sync is attempted in milliseconds
        /// (clamped to 100ms - 1 day)
        /// </summary>
        [Savable]
        [RemoteState]
        public TimeSpan DriftThreshold
        {
            get => driftThreshold;
            set => value.Clamp(TimeSpan.FromMilliseconds(100), TimeSpan.FromDays(1));
        }

        /// <summary>
        /// Gets the drift of the system clock relative to the NTP server in seconds.
        /// </summary>
        [RemoteState]
        public float NTPClockDrift { get; private set; } = 0f;

        /// <summary>
        /// The <see cref="ConnectionStatus" /> of the <see cref="NTPServerAddress" />
        /// </summary>
        [RemoteState(true)]
        public ConnectionStatus NTPConnectionStatus
        {
            get => ntpConnectionStatus;
            set
            {
                if (Set(nameof(NTPConnectionStatus), ref ntpConnectionStatus, value))
                {
                    OnNotify($"{nameof(NTPConnectionStatus)}:{NTPConnectionStatus}");
                }
            }
        }

        /// <summary>
        /// Gets the current NTP server from the GPS configuration file.
        /// </summary>
        [RemoteState]
        public string NTPServerAddress { get; private set; }

        /// <summary>
        /// Gets or sets the current <see cref="Region" /> of the Rayfin.
        /// </summary>
        [Savable]
        [RemoteState]
        public string Region
        {
            get => region;
            set => Set(nameof(Region), ref region, value);
        }

        /// <summary>
        /// Returns the current system <see cref="DateTime" />.
        /// </summary>
        [RemoteState]
        public DateTime SystemDateTime => DateTime.Now;

        /// <summary>
        /// Polls the OS for the current timezone
        /// </summary>
        /// <returns> A <see cref="string" /> that represents the current persist.sys.timezone </returns>
        [RemoteState]
        public string TimeZone { get; private set; }

        /// <summary>
        /// Sets the NTP time server on the Rayfin.
        /// </summary>
        /// <param name="server"> The NTP time server. </param>
        [RemoteCommand]
        public void ConnectNTPServer(string server)
        {
            // sample server: time.izatcloud.net
            Task.Run(() =>
            {
                if (this.IsLocked())
                {
                    return;
                }

                lock (this)
                {
                    // try { NIST.GetNISTDateTime(server); } catch { CancelConnect(5); return; }

                    NTPConnectionStatus = ConnectionStatus.Connecting;
                    UpdateClockMode(RayfinClockMode.Auto);
                    NTPServerAddress = server;

                    try
                    {
                        UpdateNTPServer(server);
                        OnNotify("NTP server configured.", MessageTypes.Alert);
                    }
                    catch (PingException)
                    {
                        // The server is offline
                        NTPConnectionStatus = ConnectionStatus.Offline;
                        OnNotify($"Could not configure NTP server, server unreachable", MessageTypes.Alert);
                        return;
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        // Error handling
                        NTPConnectionStatus = ConnectionStatus.Offline;
                        OnNotify($"Could not configure NTP server, socket exception", MessageTypes.Alert);
                        return;
                    }
                    catch (System.Reflection.TargetInvocationException)
                    {
                        // Error handling
                        NTPConnectionStatus = ConnectionStatus.Offline;
                        OnNotify($"Could not configure NTP server, internal error", MessageTypes.Alert);
                        return;
                    }

                    OnNotify($"{nameof(NTPServerAddress)}:{NTPServerAddress}");
                    AutoTime = true;
                    DroidSystem.ShellSync("reboot");
                    SyncClock();
                }
            });
        }

        /// <summary>
        /// Sets the System Clock in the Rayfin
        /// </summary>
        /// <param name="dateValue"> the date and time to set </param>
        [PropertyConverter(typeof(StringToDateTime))]
        [RemoteCommand]
        [Alias("SystemTime", "SystemDateTime")]
        public void SetDateTime(DateTime dateValue)
        {
            if (dateValue.Year > MaxYear)
            {
                OnNotify($"You cannot set the date past the year {MaxYear}", MessageTypes.Error);
                return;
            }

            OnNotify($"{nameof(SystemDateTime)} set to {dateValue.ToString("yyyy/MM/dd HH\\:mm:ss")}.\n", MessageTypes.Alert);
            UpdateClockMode(RayfinClockMode.Manual);
            var utcToSet = dateValue + (DateTime.UtcNow - DateTime.Now);
            DroidSystem.ShellSync($"/system/subcimaging/bin/busybox date {utcToSet.ToString("MMddHHmmyyyy.ss")} && am broadcast -a android.intent.action.TIME_CHANGED");
            OnNotify($"{nameof(SystemDateTime)}:{SystemDateTime}");
            Thread.Sleep(1);
            DroidSystem.ShellSync("reboot");
        }

        /// <summary>
        /// Sets the Time Zone on the Rayfin
        /// </summary>
        /// <param name="timezone"> a string representation of the timezone to set </param>
        [RemoteCommand]
        [Alias("SystemTimeZone")]
        public void SetTimeZone(string timezone)
        {
            OnNotify($"{nameof(TimeZone)} set to: {timezone}.", MessageTypes.Alert);
            DroidSystem.ShellSync($@"setprop persist.sys.timezone {timezone} && am broadcast -a android.intent.action.TIMEZONE_CHANGED");
            OnNotify($"{nameof(SystemDateTime)}:{SystemDateTime}");
            OnNotify($"{nameof(TimeZone)}:{TimeZone}");
            DroidSystem.ShellSync("reboot");
        }

        /// <summary>
        /// Sets the Time Zone and System Clock on the Rayfin
        /// </summary>
        /// <param name="timezone"> a string representation of the timezone to set </param>
        /// <param name="dateValue"> the date and time to set </param>
        [PropertyConverter(typeof(StringToDateTime), "dateValue")]
        [RemoteCommand]
        public void SetTimeZoneAndDateTime(string timezone, DateTime dateValue)
        {
            if (dateValue.Year > MaxYear)
            {
                OnNotify($"You cannot set the date past the year {MaxYear}", MessageTypes.Error);
                return;
            }

            UpdateClockMode(RayfinClockMode.Manual);

            OnNotify($"{nameof(TimeZone)} set to: {timezone}.", MessageTypes.Alert);
            DroidSystem.ShellSync($@"setprop persist.sys.timezone {timezone} && am broadcast -a android.intent.action.TIMEZONE_CHANGED");
            OnNotify($"{nameof(TimeZone)}:{TimeZone}");

            OnNotify($"{nameof(SystemDateTime)} set to {dateValue.ToString("yyyy/MM/dd HH\\:mm:ss")}.\n", MessageTypes.Alert);
            var utcToSet = dateValue + (DateTime.UtcNow - DateTime.Now);
            DroidSystem.ShellSync($"/system/subcimaging/bin/busybox date {utcToSet.ToString("MMddHHmmyyyy.ss")} && am broadcast -a android.intent.action.TIME_CHANGED");
            OnNotify($"{nameof(SystemDateTime)}:{SystemDateTime}");

            Thread.Sleep(1);
            DroidSystem.ShellSync("reboot");
        }

        /// <summary>
        /// Attempts to sync the clock with the current NTP server.
        /// </summary>
        [RemoteCommand]
        public void SyncClock()
        {
            if (ClockMode != RayfinClockMode.Auto || NTPServerAddress == null)
            {
                return;
            }

            DroidSystem.ShellSync($@"timeout 5 /system/subcimaging/bin/busybox ntpd -Np {Dns.GetHostAddresses(NTPServerAddress).First()}");
            OnNotify($"Attempting to sync clock with {NTPServerAddress}");
            Task.Run(async () =>
            {
                await Task.Delay(5000);
                OnNotify($"{nameof(SystemDateTime)}:{SystemDateTime}");
            });
        }

        /// <summary>
        /// Updates the <see cref="ClockMode" /> and handles any syncing
        /// </summary>
        /// <param name="mode">
        /// The <see cref="RayfinClockMode" /> you wish to set the <see cref="ClockMode" /> to.
        /// (Auto or Manual)
        /// </param>
        [PropertyConverter(typeof(StringToRayfinClockMode))]
        [RemoteCommand]
        public void UpdateClockMode(RayfinClockMode mode)
        {
            if (mode == ClockMode)
            {
                return;
            }

            AutoTime = mode == RayfinClockMode.Auto;
            if (mode != RayfinClockMode.Auto)
            {
                NTPConnectionStatus = ConnectionStatus.Disabled;
            }

            DroidSystem.ShellSync($"settings put global auto_time {(AutoTime ? "1" : "0")}");
            ClockMode = mode;
            if (ClockMode == RayfinClockMode.Auto)
            {
                SyncClock();
            }
        }

        /// <summary>
        /// Sets the <see cref="NTPServerAddress" /> that the system polls for time.
        /// </summary>
        /// <param name="server"> The NTP time server </param>
        /// <exception cref="PingException"> Can't ping the server, connection failed. </exception>
        public void UpdateNTPServer(string server)
        {
            var ip = Dns.GetHostAddresses(server)[0];
            var online = new Ping().Send(ip).Status == IPStatus.Success;

            if (!online)
            {
                throw new PingException($"Could not find {server}");
            }

            NTPConnectionStatus = ConnectionStatus.Online;
            DroidSystem.ShellSync($"setprop persist.rayfin.ntp.server {server}");
            DroidSystem.ShellSync($"settings put global ntp_server {server}");
            DroidSystem.ShellSync($"sed -i \'/NTP_SERVER=/c\\NTP_SERVER={server}\' /system/etc/gps.conf");
        }

        /// <summary>
        /// Initializes the state so it is in sync with the OS on startup
        /// </summary>
        private void InitializeState()
        {
            AutoTime = DroidSystem.ShellSync("settings get global auto_time").TrimEnd() == "1" ? true : false;
            ClockMode = AutoTime ? RayfinClockMode.Auto : RayfinClockMode.Manual;
            UpdateClockMode(ClockMode);

            NTPServerAddress = DroidSystem.ShellSync(@"cat /system/etc/gps.conf | grep NTP_SERVER | cut -d '=' -f2")
                .TrimEnd();

            TimeZone = DroidSystem.ShellSync("getprop persist.sys.timezone").TrimEnd();
        }

        /// <summary>
        /// Updates the drift of the current clock relative to the NTP server
        /// </summary>
        private void UpdateDrift()
        {
            if (Sync.IsLocked() || ClockMode != RayfinClockMode.Auto)
            {
                return;
            }

            Task.Run(() =>
            {
                lock (Sync)
                {
                    var ip = Dns.GetHostAddresses(NTPServerAddress).First();

                    // Obtains server IP and gets all drift values from the test
                    var driftValues = DroidSystem.ShellSync($@"timeout 10 /system/subcimaging/bin/busybox ntpd -dnwp {ip} 2>&1 | cut -d ':' -f4 | cut -d ' ' -f1 | grep .").Split('\n');
                    var totalDrift = 0f;

                    foreach (string driftValue in driftValues)
                    {
                        if (float.TryParse(driftValue, out var result))
                        {
                            totalDrift += result;
                        }
                    }

                    if (driftValues.Length < 3)
                    {
                        NTPClockDrift = 0f;
                        NTPConnectionStatus = ConnectionStatus.Offline;
                        return;
                    }

                    NTPClockDrift = totalDrift / (driftValues.Length - 2);  // Averages all the driftvalues and multiplies by 1,000 to return the drift in seconds

                    var driftFromZero = NTPClockDrift >= 0 ? NTPClockDrift : NTPClockDrift * -1;

                    if (driftFromZero * 1000f > driftThreshold.Milliseconds) // If the drift is greater than the threshold re sync the clock
                    {
                        SyncClock();
                    }
                    NTPConnectionStatus = ConnectionStatus.Online;
                    OnNotify($"{nameof(NTPClockDrift)}:{NTPClockDrift}");
                }
            });
        }
    }
}
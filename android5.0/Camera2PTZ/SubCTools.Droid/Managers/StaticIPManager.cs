//-----------------------------------------------------------------------
// <copyright file="StaticIPManager.cs" company="SubC Imaging Ltd">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Managers
{
    using Android.OS;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class StaticIPManager : DroidBase
    {
        /// <summary>
        /// The max number of attempts to obtain a DHCP address,
        /// Sleeps for 1 second between each try, this will result
        /// in the same period of time that nmap uses before it
        /// times out.
        /// </summary>
        private const int MaxRetries = 8;

        /// <summary>
        /// The lock object for configuring static IP
        /// </summary>
        private static readonly object Sync = new object();

        /// <summary>
        /// The delay period between finishing configuration and
        /// resetting the network permissions
        /// </summary>
        private readonly TimeSpan networkRestartDelay = TimeSpan.FromSeconds(2);

        /// <summary>
        /// A broadcast address is a network address at which all devices connected
        /// to a multiple-access communications network are enabled to receive datagrams.
        /// A message sent to a broadcast address may be received by all network-attached
        /// hosts.  If no broadcast address is supplied it will default the x.x.x.255 x.x.x
        /// being the first 3 octets of the IP address provided
        /// </summary>
        private IPAddress broadcastAddress;

        /// <summary>
        /// A default gateway is the node in a computer network using the Internet
        /// Protocol Suite that serves as the forwarding host (router) to other
        /// networks when no other route specification matches the destination IP
        /// address of a packet. Don't worry if you don't have a gateway on your
        /// network, if it is under the same subnet as the computer running Rayfin
        /// Control it won't matter.  If no default gateway is provided it will
        /// default to x.x.x.1 x.x.x being the first 3 octets of the IP address
        /// provided.
        /// </summary>
        private IPAddress defaultGateway;

        /// <summary>
        /// A static Internet Protocol (IP) address (static IP address) is a permanent
        /// number assigned to a computer, speed and reliability are key advantages.
        /// </summary>
        private IPAddress staticAddress;

        /// <summary>
        /// A <see cref="bool"/> representing whether or not static
        /// IP is enabled for use.
        /// </summary>
        private bool staticEnabled = false;

        /// <summary>
        /// The routing prefix of an address is identified by the subnet mask, written
        /// in the same form used for IP addresses. For example, the subnet mask for a
        /// routing prefix that is composed of the most-significant 24 bits of an IPv4
        /// address is written as 255.255.255.0.
        /// </summary>
        private IPAddress subnetMask;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticIPManager"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="ISettingsService"/> file to use
        /// for saving / loading the settings</param>
        public StaticIPManager(ISettingsService settings)
            : base(settings)
        {
            //// LoadSettings();
            NetworkChange.NetworkAvailabilityChanged += Network_Changed;
            //// ConfigureStaticIP();
        }

        /// <summary>
        /// Gets a broadcast address is a network address at which all devices connected
        /// to a multiple-access communications network are enabled to receive datagrams.
        /// A message sent to a broadcast address may be received by all network-attached
        /// hosts.  If no broadcast address is supplied it will default the x.x.x.255 x.x.x
        /// being the first 3 octets of the IP address provided
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToIPAddress))]
        public IPAddress BroadcastAddress
        {
            get => broadcastAddress;
            set => Set(nameof(BroadcastAddress), ref broadcastAddress, value);
        }

        /// <summary>
        /// Gets a default gateway is the node in a computer network using the Internet
        /// Protocol Suite that serves as the forwarding host (router) to other
        /// networks when no other route specification matches the destination IP
        /// address of a packet. Don't worry if you don't have a gateway on your
        /// network, if it is under the same subnet as the computer running Rayfin
        /// Control it won't matter.  If no default gateway is provided it will
        /// default to x.x.x.1 x.x.x being the first 3 octets of the IP address
        /// provided.
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToIPAddress))]
        public IPAddress DefaultGateway
        {
            get => defaultGateway;
            set => Set(nameof(DefaultGateway), ref defaultGateway, value);
        }

        /// <summary>
        /// The network speed of the NIC inside the Rayfin
        /// </summary>
        [RemoteState]
        public int NetworkLinkSpeed { get; private set; }

        /// <summary>
        /// Gets a static Internet Protocol (IP) address (static IP address) is a permanent
        /// number assigned to a computer, speed and reliability are key advantages.
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToIPAddress))]
        public IPAddress StaticAddress
        {
            get => staticAddress;
            set => Set(nameof(StaticAddress), ref staticAddress, value);
        }

        /// <summary>
        /// Gets a value indicating whether or not static
        /// IP is enabled for use.
        /// </summary>
        [Savable]
        [RemoteState(true)]
        public bool StaticEnabled
        {
            get => staticEnabled;
            set => Set(nameof(StaticEnabled), ref staticEnabled, value);
        }

        /// <summary>
        /// Gets a routing prefix of an address is identified by the subnet mask, written
        /// in the same form used for IP addresses. For example, the subnet mask for a
        /// routing prefix that is composed of the most-significant 24 bits of an IPv4
        /// address is written as 255.255.255.0.
        /// </summary>
        [Savable]
        [RemoteState(true)]
        [PropertyConverter(typeof(StringToIPAddress))]
        public IPAddress SubnetMask
        {
            get => subnetMask;
            set => Set(nameof(SubnetMask), ref subnetMask, value);
        }

        /// <summary>
        /// Assign a new static IP address
        /// </summary>
        /// <param name="address">The address to assign as the IP address</param>
        /// <param name="gateway">The default gateway to use to route packets</param>
        /// <param name="mask">The subnet mask for the network</param>
        /// <param name="broadcast">The broadcast address for the network</param>
        [RemoteCommand]
        [PropertyConverter(typeof(StringToIPAddress))]
        public void AssignStaticIP(IPAddress address, IPAddress gateway, IPAddress mask, IPAddress broadcast)
        {
            StaticEnabled = true;
            Task.Run(async () =>
            {
                if (!await ConfigureStaticIP(address, gateway, mask, broadcast))
                {
                    OnNotify(this, new NotifyEventArgs("Failed to assign static IP", MessageTypes.Error));
                }
            });
        }

        /// <summary>
        /// Configures a static IP for use on a Rayfin
        /// </summary>
        /// <param name="address">The address to assign as the IP address</param>
        /// <param name="gateway">The default gateway to use to route packets</param>
        /// <param name="mask">The subnet mask for the network</param>
        /// <param name="broadcast">The broadcast address for the network</param>
        [RemoteCommand]
        [PropertyConverter(typeof(StringToIPAddress))]
        public async Task<bool> ConfigureStaticIP(IPAddress address, IPAddress gateway, IPAddress mask, IPAddress broadcast)
        {
            // We don't want this to run if there is a debugger attached since it will sever the connection
            if (Debugger.IsAttached)
            {
                OnNotify(this, new NotifyEventArgs("Cannot configure static IP when there is a debugger attached", MessageTypes.Error));
                UpdateNetworkLinkSpeed();
                return false;
            }

            // We don't want this to run if the camera is in the middle of an update since it will sever the connection
            if (DroidSystem.GetProp("rayfin.status").Equals("updating", StringComparison.InvariantCultureIgnoreCase))
            {
                OnNotify(this, new NotifyEventArgs("Cannot configure static IP when the camera is updating", MessageTypes.Error));
                UpdateNetworkLinkSpeed();
                return false;
            }

            if (Sync.IsLocked())
            {
                OnNotify(this, new NotifyEventArgs("Cannot configure static IP when already trying to configure static IP", MessageTypes.Error));
                return false;
            }

            lock (Sync)
            {
                WaitForAdbd();

                StaticAddress = address;
                DefaultGateway = gateway;
                SubnetMask = mask;
                BroadcastAddress = broadcast;

                // Flush all network addresses and reset NIC
                DroidSystem.ShellSync("ip addr flush eth0");
                DroidSystem.ShellSync("ifconfig eth0 down && ifconfig eth0 up");
                var attempts = 0;

                while (attempts < MaxRetries)
                {
                    // Attempt to get a DHCP address, if one
                    // can't be aquired after 8s timeout(Same
                    // as nmap).
                    if (NetworkHasDHCP())
                    {
                        ConfigureNetwork(false);
                        break;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    attempts++;
                }

                if (attempts >= MaxRetries)
                {
                    ConfigureNetwork(true);
                }

                // Restart samba, the ADB daemon, and the RTSP server
                RestartNetworkServices();
                Thread.Sleep(networkRestartDelay);

                // Unlock network for non root access
                DroidSystem.ShellSync("ip rule add from all lookup main pref 99");
                Thread.Sleep(networkRestartDelay);
                UpdateNetworkLinkSpeed();

                var ifconfig = DroidSystem.ShellSync($"ifconfig");
                var iproute = DroidSystem.ShellSync($"ip route");

                return ifconfig.Contains("eth0:static") && iproute.Contains("via");
            }
        }

        /// <summary>
        /// Configures a static IP for use on a Rayfin
        /// </summary>
        [RemoteCommand]
        public void ConfigureStaticIP()
        {
            Task.Run(() => ConfigureStaticIP(StaticAddress, DefaultGateway, SubnetMask, BroadcastAddress));
        }

        /// <summary>
        /// Disables the static IP
        /// </summary>
        [RemoteCommand]
        public void DisableStaticIP()
        {
            StaticEnabled = false;
            StaticAddress = null;
            Thread.Sleep(networkRestartDelay); // Wait for settings file to be written
            DroidSystem.ShellSync("reboot");
        }

        /// <summary>
        /// Checks all the network cards available and if any are a
        /// <see cref="NetworkInterfaceType.Ethernet"/> with the name
        /// of eth0 and have a <see cref="AddressFamily.InterNetwork"/>
        /// <see cref="IPAddress"/>
        /// </summary>
        /// <returns>A bool that represents whether or not an address is
        /// present on the main eth0 adapter</returns>
        private static bool NetworkHasDHCP() => (from n in NetworkInterface.GetAllNetworkInterfaces()
                                                 where n.Name.Equals("eth0")
                                                 where n.NetworkInterfaceType.Equals(NetworkInterfaceType.Ethernet)
                                                 from i in n.GetIPProperties().UnicastAddresses
                                                 where i.Address.AddressFamily.Equals(AddressFamily.InterNetwork)
                                                 select i).Any();

        /// <summary>
        /// Assigns a link local address to the Rayfin
        /// </summary>
        /// <param name="isMain">A <see cref="bool"/> representing whether or
        /// not to assign the link local address to the main address or to
        /// alias the main address with it.</param>
        private void AssignLinkLocalAddress(bool isMain)
        {
            var serial = DroidSystem.ShellSync("getprop rayfin.serial").TrimEnd();

            if (!string.IsNullOrEmpty(serial))
            {
                var ip = $"169.254.{serial.Substring(0, 3).TrimStart('0')}.{serial.Substring(3, 2).TrimStart('0')}";
                try
                {
                    var linkLocalAddress = IPAddress.Parse(ip);
                    DroidSystem.ShellSync($"ifconfig eth0{(isMain ? string.Empty : ":ll")} " +
                        $"{linkLocalAddress} netmask 255.255.0.0 broadcast 169.254.255.255");
                }
                catch (SystemException e) when (e is SocketException || e is FormatException)
                {
                    Console.WriteLine($"Error parsing ip {ip} : {e.Message}");
                }
            }
        }

        /// <summary>
        /// Configures the network to set up all addresses and
        /// restarts the permissions that are required
        /// </summary>
        private void ConfigureNetwork(bool isMain)
        {
            if (isMain)
            {
                DroidSystem.ShellSync("stop netd");
            }

            var staticValid = ValidateProperties();

            if (staticValid)
            {
                DroidSystem.ShellSync($"ifconfig eth0{(isMain ? string.Empty : ":static")} {StaticAddress} netmask {SubnetMask} broadcast {BroadcastAddress}");
                DroidSystem.ShellSync($"ip route add {StaticAddress} via {DefaultGateway}");
            }

            AssignLinkLocalAddress(!staticValid && isMain);

            if (isMain)
            {
                DroidSystem.ShellSync("start netd");
            }
        }

        /// <summary>
        /// An event that gets invoked when the network adapter is connected
        /// or disconnected.
        /// </summary>
        /// <param name="sender">The Sender</param>
        /// <param name="e">The <see cref="NetworkAvailabilityEventArgs"/></param>
        private void Network_Changed(object sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                ConfigureStaticIP();
            }
        }

        /// <summary>
        /// Restarts all the services that rely on the network
        /// Samba, ADB
        /// </summary>
        private void RestartNetworkServices()
        {
            // Restart Samba
            DroidSystem.ShellSync(@"/data/data/com.funkyfresh.samba/files/samba-rc stop " +
                @"&& sleep 1 && /data/data/com.funkyfresh.samba/files/samba-rc start");

            // Restart ADB Daemon
            DroidSystem.ShellSync("setprop ctl.restart adbd");

            // Restart RTSP server
            Task.Run(() => DroidSystem.ShellSync(@"/system/subcimaging/bin/node /system/subcimaging/bin/RtspServer.js"));
        }

        private void UpdateNetworkLinkSpeed()
        {
            Task.Run(() =>
            {
                var speedString = DroidSystem.ShellSync(@"cat /sys/class/net/eth0/speed").TrimEnd();

                if (int.TryParse(speedString, out int result))
                {
                    NetworkLinkSpeed = result;
                }
            });
        }

        /// <summary>
        /// Checks if any items of <see cref="StaticIP"/> are null,
        /// also verifies that <see cref="StaticEnabled"/> is set to
        /// <see cref="true"/>
        /// </summary>
        /// <returns>Returns a <see cref="bool"/> that shows if everything is valid
        /// before configuring the network adapter</returns>
        private bool ValidateProperties() =>
            !NullHelper.AreAnyNull(StaticAddress, DefaultGateway, SubnetMask, BroadcastAddress) && StaticEnabled;

        /// <summary>
        /// Wait for the OS to restart the adb daemon, if it doesn't restart after 4s timeout and continue anyway.
        /// </summary>
        private void WaitForAdbd()
        {
            try
            {
                if (Looper.MyLooper().Equals(Looper.MainLooper))
                {
                    throw new InvalidOperationException("DO NOT RUN ON MAIN THREAD!");
                }
            }
            catch (NullReferenceException)
            {
            }

            var attempts = 0;

            while (attempts < MaxRetries)
            {
                var listeningPorts = DroidSystem.ShellSync("netstat -an | grep \"LISTEN \"");

                if (listeningPorts.Contains("5555"))
                {
                    return;
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
                attempts++;
            }
        }
    }
}
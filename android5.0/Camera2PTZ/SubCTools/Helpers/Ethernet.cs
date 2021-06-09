//-----------------------------------------------------------------------
// <copyright file="Ethernet.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Helpers
{
    using SubCTools.Extensions;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Ethernet helper to do basic network operations.
    /// </summary>
    public static class Ethernet
    {
        /// <summary>
        /// Gets the active network address given the <see cref="AddressFamily"/>.
        /// </summary>
        /// <param name="family">The <see cref="AddressFamily"/> you are looking to get the active address for.</param>
        /// <returns></returns>
        public static IPAddress GetActiveIP(AddressFamily family = AddressFamily.InterNetwork)
        {
            using (var socket = new Socket(family, SocketType.Dgram, 0))
            {
                try
                {
                    socket.Connect("8.8.8.8", 65530);
                }
                catch (SocketException)
                {
                    return IPAddress.Loopback;
                }

                return (socket.LocalEndPoint as IPEndPoint).Address;
            }
        }

        /// <summary>
        /// Returns the IP Address of the system.
        /// </summary>
        /// <returns>A <see cref="string"/> representing the IP Address of the system.</returns>
        public static string GetIP()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList
.Where(i => !i.IsLoopback())
.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "IP Error";
        }

        /// <summary>
        /// Returns the IP Address of the system.
        /// </summary>
        /// <returns>A <see cref="string"/> representing the IP Address of the system.</returns>
        public static async Task<string> GetIPAsync()
        {
            return await Task.Run(() => GetIP());
        }

        /// <summary>
        /// Checks to see if the device has a active network connection
        /// and internet access.
        /// </summary>
        /// <param name="count">The number of attemps to get a <see cref="IPStatus.Success"/>.</param>
        /// <returns>A value indicating whether or not the device has internet access.</returns>
        public static bool IsOnline(int count = 3)
        {
            try
            {
                for (var i = 0; i < count; i++)
                {
                    if (new Ping().Send(System.Net.IPAddress.Parse("8.8.8.8"), 100).Status == IPStatus.Success)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (PingException)
            {
                return false;
            }
        }

        /// <summary>
        /// Pings a target <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="address">The target <see cref="IPAddress"/> to ping.</param>
        /// <param name="timeout">The maximum amount of time to wait before failing the ping.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public static async Task<bool> IsPresent(IPAddress address, int timeout = 100)
        {
            try
            {
                var pingSender = new Ping();
                var result = (await pingSender.SendPingAsync(address, timeout)).Status == IPStatus.Success;
                ((IDisposable)pingSender).Dispose();

                return result;
            }
            catch (PingException)
            {
                return false;
            }
        }
    }
}
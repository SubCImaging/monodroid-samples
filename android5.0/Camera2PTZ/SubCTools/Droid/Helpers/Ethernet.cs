// <copyright file="Ethernet.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Helpers
{
    using System.Net.NetworkInformation;
    using System.Net.Sockets;

    public class Ethernet
    {
        /// <summary>
        /// Get the IPv6 address of a Rayfin.
        /// </summary>
        /// <returns>IPv6 address as a string if found, empty string if not.</returns>
        public static string IPv6Address()
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.Name != "eth0")
                {
                    continue;
                }

                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addrInfo.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            return addrInfo.Address.ToString();
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}
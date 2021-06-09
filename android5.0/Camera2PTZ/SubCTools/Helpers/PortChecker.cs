// <copyright file="PortChecker.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using SubCTools.Interfaces;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Onject for checking tcp ports to ip addresses.
    /// </summary>
    public class PortChecker : IPortChecker
    {
        /// <summary>
        /// Check to see if a port is open at a given address.
        /// </summary>
        /// <param name="address">Address to connect.</param>
        /// <param name="port">Port to connect.</param>
        /// <returns>True if port is open, false otherwise.</returns>
        public bool IsPortOpen(IPAddress address, int port)
        {
            if (address == null)
            {
                return false;
            }

            var newClient = new TcpClient();
            var ar = newClient.BeginConnect(address.ToString(), port, null, null);
            var tcpOpen = ar.AsyncWaitHandle.WaitOne(1000, false);

            if (tcpOpen)
            {
                newClient.EndConnect(ar);
            }

            return tcpOpen;
        }
    }
}
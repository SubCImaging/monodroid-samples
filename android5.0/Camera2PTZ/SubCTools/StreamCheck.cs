// <copyright file="StreamCheck.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools
{
    using SubCTools.Attributes;
    using SubCTools.Helpers;
    using SubCTools.Interfaces;
    using SubCTools.Models;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for checking that the ports are open.
    /// </summary>
    public class StreamCheck
    {
        /// <summary>
        /// Signaling server port.
        /// </summary>
        public const int SignalingPort = 443;

        /// <summary>
        /// Signaling server IP.
        /// </summary>
        public const string SignalingServer = "40.117.185.108";

        /// <summary>
        /// Turn server port.
        /// </summary>
        public const int TurnPort = 3478;

        /// <summary>
        /// Turn server IP.
        /// </summary>
        public const string TurnServer = "3.230.64.120";

        private readonly IPortChecker portChecker;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamCheck"/> class.
        /// </summary>
        /// <param name="portChecker">The checker to use for the ports.</param>
        public StreamCheck(IPortChecker portChecker = null)
        {
            if (portChecker == null)
            {
                portChecker = new PortChecker();
            }

            this.portChecker = portChecker;
        }

        /// <summary>
        /// Gets a list of bitrates to send to the streaming service.
        /// </summary>
        [RemoteState]
        public static ObservableCollection<int> BitRates
        {
            get
            {
                var bitrates = new ObservableCollection<int>();
                for (var i = 6; i <= 10; i++)
                {
                    bitrates.Add((int)Math.Pow(2, i));
                }

                return bitrates;
            }
        }

        /// <summary>
        /// Gets the MAC address of the computer.
        /// </summary>
        [RemoteState]
        public static string GetMACAddress => (from nic in NetworkInterface.GetAllNetworkInterfaces()
                                               where nic.OperationalStatus == OperationalStatus.Up
                                               select nic.GetPhysicalAddress().ToString())
                        .FirstOrDefault();

        /// <summary>
        /// Gets a value indicating whether the status of connection to the signaling server.
        /// </summary>
        [RemoteState]
        public bool SignalingConnection => IsConnectionAvailable(SignalingServer, SignalingPort).Result;

        /// <summary>
        /// Gets a value indicating whether the status of connection to the turn server.
        /// </summary>
        [RemoteState]
        public bool TurnConnection => IsConnectionAvailable(TurnServer, TurnPort).Result;

        /// <summary>
        /// Gets the ports that are closed on the system.
        /// </summary>
        /// <param name="addresses">Ports to check.</param>
        /// <returns>Which ports you've passed in that are closed.</returns>
        public async Task<IEnumerable<EthernetAddress>> GetClosedConnectionsAsync(params EthernetAddress[] addresses)
        {
            var closedPorts = new List<EthernetAddress>();

            foreach (var address in addresses)
            {
                if (!await IsConnectionAvailable(address.Address, address.Port).ConfigureAwait(false))
                {
                    closedPorts.Add(address);
                }
            }

            return closedPorts;
        }

        /// <summary>
        /// Is the connection open to the address and port given.
        /// </summary>
        /// <param name="address">Address to check.</param>
        /// <param name="port">Port to check.</param>
        /// <returns>True if the connection is available.</returns>
        [RemoteCommand]
        public async Task<bool> IsConnectionAvailable(IPAddress address, int port)
        {
            return await Task.Run(() => portChecker.IsPortOpen(address, port)).ConfigureAwait(false);
        }

        /// <summary>
        /// Is the connection open to the address and port given.
        /// </summary>
        /// <param name="address">Address to check.</param>
        /// <param name="port">Port to check.</param>
        /// <returns>True if the connection is available.</returns>
        [RemoteCommand]
        public async Task<bool> IsConnectionAvailable(string address, int port)
        {
            return await Task.Run(() => portChecker.IsPortOpen(IPAddress.Parse(address), port)).ConfigureAwait(false);
        }

        /// <summary>
        /// Check to see if a port is open on the PC.
        /// </summary>
        /// <param name="port">Port number to check.</param>
        /// <returns>True if the port is open.</returns>
        [RemoteCommand]
        public async Task<bool> IsPortOpen(int port)
        {
            return await Task.Run(() => portChecker.IsPortOpen(IPAddress.Parse(TurnServer), port)).ConfigureAwait(false);
        }
    }
}
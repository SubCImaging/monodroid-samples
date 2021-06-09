// <copyright file="EthernetAddress.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Net;

    public class EthernetAddress : CommunicatorAddress
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EthernetAddress"/> class.
        /// </summary>
        /// <param name="info"></param>
        [JsonConstructor]
        public EthernetAddress(Dictionary<string, string> info)
            : this(IPAddress.Parse(info[nameof(Address)]), int.Parse(info[nameof(Port)]), info[nameof(PortDescription)])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EthernetAddress"/> class.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="portDescription"></param>
        public EthernetAddress(IPAddress address, int port, string portDescription = "")
        {
            Address = address;
            Port = port;
            PortDescription = string.IsNullOrEmpty(portDescription) ? ToString() : portDescription;

            Add(nameof(Address), Address.ToString());
            Add(nameof(Port), Port.ToString());
            Add(nameof(PortDescription), PortDescription);
        }

        [JsonProperty("address")]
        public IPAddress Address { get; }

        [JsonProperty("port")]
        public int Port { get; }

        [JsonProperty("portDescription")]
        public string PortDescription { get; } = string.Empty;

        // Review: Easier to parse and more consistent with SerialAddress
        // public override string ToString() => $"{Address}:{Port}";//Address + " on Port: " + Port;
    }
}
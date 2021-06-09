// <copyright file="SerialAddress.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class SerialAddress : CommunicatorAddress
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerialAddress"/> class.
        /// </summary>
        /// <param name="info"></param>
        [JsonConstructor]
        public SerialAddress(Dictionary<string, string> info)
            : this(info[nameof(PortDescription)], int.Parse(info[nameof(BaudRate)]))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialAddress"/> class.
        /// </summary>
        /// <param name="portDescription"></param>
        /// <param name="baudRate"></param>
        public SerialAddress(string portDescription, int baudRate)
        {
            PortDescription = portDescription;
            ComPort = Regex.Match(portDescription ?? string.Empty, @"com\d+", RegexOptions.IgnoreCase).Value;
            BaudRate = baudRate;

            Add(nameof(ComPort), ComPort);
            Add(nameof(BaudRate), BaudRate.ToString());
            Add(nameof(PortDescription), PortDescription);
        }

        public int BaudRate { get; }

        public string ComAddress => ComPort + " @ " + BaudRate;

        public string ComPort { get; } = string.Empty;

        public string PortDescription { get; } = string.Empty;

        // public override string ToString() => ComPort + " @ " + BaudRate;
    }
}
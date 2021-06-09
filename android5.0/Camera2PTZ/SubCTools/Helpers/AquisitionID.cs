// <copyright file="AquisitionID.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using SubCTools.Models;
    using System;
    using System.Net;

    /// <summary>
    /// Each acquisition device has a unique id.
    /// </summary>
    public class AquisitionID : Tuple<CommunicatorAddress, string>, IEquatable<AquisitionID>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AquisitionID"/> class.
        /// </summary>
        /// <param name="address">Address of the detected device.</param>
        /// <param name="name">Device name.</param>
        public AquisitionID(CommunicatorAddress address, string name)
            : this(address, name, "Rayfin")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AquisitionID"/> class.
        /// </summary>
        /// <param name="address">Address of the detected device.</param>
        /// <param name="name">Device name.</param>
        /// <param name="cameraType">Type of camera, e.g. Grenadier, Rayfin.</param>
        public AquisitionID(CommunicatorAddress address, string name, string cameraType)
            : base(address, name)
        {
            CameraType = cameraType;
        }

        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        public string Name => Item2;

        /// <summary>
        /// Gets the IP of the connected camera.
        /// </summary>
        public IPAddress IP => IPAddress.Parse(Item1["Address"]);

        /// <summary>
        /// Gets serial port camera was found on.
        /// </summary>
        public int Port => Convert.ToInt32(Item1["Port"]);

        /// <summary>
        /// Gets type of camera.
        /// </summary>
        public string CameraType { get; }

        /// <summary>
        /// Check to see if this id equals another.
        /// </summary>
        /// <param name="other">Other acquisition id to check against.</param>
        /// <returns>True if the name, IP address, and port are the same.</returns>
        public bool Equals(AquisitionID other)
        {
            return Name == other.Name
                       && IP.ToString() == other.IP.ToString()
                       && Port == other.Port;
        }

        /// <summary>
        /// Generate a hash code for the object.
        /// </summary>
        /// <returns>The combined hash codes of the name, ip, and port.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode() + IP.GetHashCode() + Port.GetHashCode();
        }
    }
}
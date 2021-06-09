//-----------------------------------------------------------------------
// <copyright file="CameraInfo.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using Newtonsoft.Json;
    using SubCTools.Converters.JsonConverters;
    using SubCTools.Enums;
    using SubCTools.Models;
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Class for holding all information around a camera.
    /// </summary>
    public class CameraInfo : INotifyPropertyChanged, IEquatable<CameraInfo>
    {
        private bool? isUpToDate;

        /// <summary>
        /// Event to fire when a property changes to update the UI
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the cameras type, e.g. Rayfin or Grenadier.
        /// </summary>
        public CameraType CameraType { get; set; }

        /// <summary>
        /// Gets or sets deployment environment.
        /// </summary>
        public DeploymentEnvironments DeploymentEnvironment { get; set; } = DeploymentEnvironments.Production;

        /// <summary>
        /// Gets or sets the cameras firmware version.
        /// </summary>
        [JsonConverter(typeof(SubCVersionConverter))]
        public Version FirmwareVersion { get; set; } = new Version("0.0");

        /// <summary>
        /// Gets or sets the IP address of the camera.
        /// </summary>
        [JsonConverter(typeof(EthernetAddressConverter))]
        public EthernetAddress IP { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether the camera is up to date.
        /// </summary>
        public bool? IsUpToDate
        {
            get => isUpToDate;
            set
            {
                if (isUpToDate == value)
                {
                    return;
                }

                isUpToDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUpToDate)));
            }
        }

        /// <summary>
        /// Gets or sets the camera name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the camera nickname.
        /// </summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>
        /// Gets the nick name if one is set, use the name otherwise.
        /// </summary>
        public string PreferredName => !string.IsNullOrEmpty(Nickname) ? Nickname : Name;

        /// <summary>
        /// Gets or sets the camera rom version.
        /// </summary>
        [JsonConverter(typeof(SubCVersionConverter))]
        public Version RomVersion { get; set; } = new Version("0.0");

        /// <summary>
        /// Gets or sets the camera tcp address.
        /// </summary>
        [JsonConverter(typeof(EthernetAddressConverter))]
        public EthernetAddress TCPAddress { get; set; }

        /// <summary>
        /// Gets or sets the camera udp address.
        /// </summary>
        [JsonConverter(typeof(EthernetAddressConverter))]
        public EthernetAddress UDPAddress { get; set; }

        /// <summary>
        /// Gets or sets the camera version.
        /// </summary>
        [JsonConverter(typeof(SubCVersionConverter))]
        public Version Version { get; set; } = new Version("0.0");

        /// <summary>
        /// Check to see if this id equals another.
        /// </summary>
        /// <param name="other">Other acquisition id to check against.</param>
        /// <returns>True if the name, IP address, and port are the same.</returns>
        public bool Equals(CameraInfo other)
        {
            return Name == other.Name;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Join("\n", new string[] { PreferredName, TCPAddress.ToString(), Version?.ToString(), RomVersion?.ToString(), FirmwareVersion?.ToString() });
        }
    }
}
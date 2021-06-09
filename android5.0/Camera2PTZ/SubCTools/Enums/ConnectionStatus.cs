// <copyright file="ConnectionStatus.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Enums
{
    /// <summary>
    /// Connection status of the NAS.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Connection is not enabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// Currently establishing a connection.
        /// </summary>
        Connecting,

        /// <summary>
        /// The connection is disconnecting.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// The connection is enabled and online.
        /// </summary>
        Online,

        /// <summary>
        /// The connection is enabled and offline.
        /// </summary>
        Offline,
    }
}

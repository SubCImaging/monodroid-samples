//-----------------------------------------------------------------------
// <copyright file="MessageTypes.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Messaging.Models
{
    using System;

    /// <summary>
    /// <see cref="enum"/> listing the different <see cref="MessageTypes"/>.
    /// </summary>
    [Flags]
    public enum MessageTypes
    {
        /// <summary>
        /// Default <see cref="MessageTypes"/>.
        /// </summary>
        Default = 1,

        /// <summary>
        /// Information <see cref="MessageTypes"/>.
        /// </summary>
        Information = 1 << 1,

        /// <summary>
        /// Critical <see cref="MessageTypes"/>.
        /// </summary>
        Critical = 1 << 2,

        /// <summary>
        /// Warning <see cref="MessageTypes"/>.
        /// </summary>
        Warning = 1 << 3,

        /// <summary>
        /// Debug <see cref="MessageTypes"/>.
        /// </summary>
        Debug = 1 << 4,

        /// <summary>
        /// Transmit <see cref="MessageTypes"/>.
        /// </summary>
        Transmit = 1 << 5,

        /// <summary>
        /// Receive <see cref="MessageTypes"/>.
        /// </summary>
        Receive = 1 << 6,

        /// <summary>
        /// SubCCommand <see cref="MessageTypes"/>.
        /// </summary>
        SubCCommand = 1 << 7,

        /// <summary>
        /// CameraState <see cref="MessageTypes"/>.
        /// </summary>
        CameraState = 1 << 8,

        /// <summary>
        /// Help <see cref="MessageTypes"/>.
        /// </summary>
        Help = 1 << 9,

        /// <summary>
        /// Connection <see cref="MessageTypes"/>.
        /// </summary>
        Connection = 1 << 10,

        /// <summary>
        /// Gauntlet <see cref="MessageTypes"/>.
        /// </summary>
        Gauntlet = 1 << 11,

        /// <summary>
        /// Error <see cref="MessageTypes"/>.
        /// </summary>
        Error = 1 << 12,

        /// <summary>
        /// RecordingTime <see cref="MessageTypes"/>.
        /// </summary>
        RecordingTime = 1 << 13,

        /// <summary>
        /// All <see cref="MessageTypes"/>.
        /// </summary>
        All = 1 << 14,

        /// <summary>
        /// CameraCommand <see cref="MessageTypes"/>.
        /// </summary>
        CameraCommand = 1 << 15,

        /// <summary>
        /// TeensyCommand <see cref="MessageTypes"/>.
        /// </summary>
        TeensyCommand = 1 << 16,

        /// <summary>
        /// SensorData <see cref="MessageTypes"/>.
        /// </summary>
        SensorData = 1 << 17,

        /// <summary>
        /// Alert <see cref="MessageTypes"/>.
        /// </summary>
        Alert = 1 << 18,

        /// <summary>
        /// Internal <see cref="MessageTypes"/>.
        /// </summary>
        Internal = 1 << 19,

        /// <summary>
        /// Command to route to aux port <see cref="MessageTypes"/>.
        /// </summary>
        Aux = 1 << 20,

        /// <summary>
        /// A message that is being routed to a new communicator.
        /// </summary>
        Routed = 1 << 21,
    }
}

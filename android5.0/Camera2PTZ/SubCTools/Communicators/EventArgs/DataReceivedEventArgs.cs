//-----------------------------------------------------------------------
// <copyright file="DataReceivedEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators.EventArgsLib
{
    using SubCTools.Messaging.Models;

    /// <summary>
    /// Data received event args.
    /// </summary>
    public class DataReceivedEventArgs : CommunicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="receivedFrom">address received from.</param>
        /// <param name="data">the data received.</param>
        public DataReceivedEventArgs(string receivedFrom, string data)
            : base(receivedFrom, data, MessageTypes.Receive)
        {
        }
    }
}
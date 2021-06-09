//-----------------------------------------------------------------------
// <copyright file="CommunicationEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators.EventArgsLib
{
    using SubCTools.Messaging.Models;

    /// <summary>
    /// Communication event arguments containing location, the data, and the type of data.
    /// </summary>
    public class CommunicationEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationEventArgs"/> class.
        /// </summary>
        /// <param name="communicatorAddress">the address.</param>
        /// <param name="data">the data.</param>
        /// <param name="dataType">the message type.</param>
        public CommunicationEventArgs(string communicatorAddress, string data, MessageTypes dataType)
        {
            this.CommunicatorAddress = communicatorAddress;
            this.Data = data;
            this.MyStatusType = dataType;
        }

        /// <summary>
        /// Gets the address of the one doing the communicating.
        /// </summary>
        public string CommunicatorAddress { get; private set; }

        /// <summary>
        /// Gets the data communicated.
        /// </summary>
        public string Data { get; private set; }

        /// <summary>
        /// Gets the type of communication.
        /// </summary>
        public MessageTypes MyStatusType { get; private set; }
    }
}
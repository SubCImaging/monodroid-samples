//-----------------------------------------------------------------------
// <copyright file="DataSentEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators.EventArgsLib
{
    using SubCTools.Messaging.Models;

    /// <summary>
    /// Data sent event args.
    /// </summary>
    public class DataSentEventArgs : CommunicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSentEventArgs"/> class.
        /// </summary>
        /// <param name="sentFrom">address sent from.</param>
        /// <param name="data">the data sent.</param>
        public DataSentEventArgs(string sentFrom, string data)
            : base(sentFrom, data, MessageTypes.Transmit)
        {
        }
    }
}
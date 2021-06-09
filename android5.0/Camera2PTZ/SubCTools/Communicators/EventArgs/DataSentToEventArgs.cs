//-----------------------------------------------------------------------
// <copyright file="DataSentToEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators.EventArgsLib
{
    using SubCTools.Messaging.Models;

    /// <summary>
    /// Data sent to event args.
    /// </summary>
    public class DataSentToEventArgs : CommunicationEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSentToEventArgs"/> class.
        /// </summary>
        /// <param name="sentFrom">from address.</param>
        /// <param name="sentTo">to address.</param>
        /// <param name="data">the data sent.</param>
        public DataSentToEventArgs(string sentFrom, string sentTo, string data)
            : base(sentFrom, data, MessageTypes.Transmit)
        {
            SentTo = sentTo;
        }

        /// <summary>
        /// Gets the 'sent to' address.
        /// </summary>
        public string SentTo
        {
            get;
            private set;
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="SendingEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators.EventArgsLib
{
    /// <summary>
    /// Sending event args.
    /// </summary>
    public class SendingEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendingEventArgs"/> class.
        /// </summary>
        /// <param name="sendingType">The method of sending.</param>
        /// <param name="isSending">true if sending.</param>
        public SendingEventArgs(SendingTypes sendingType, bool isSending)
        {
            SendingType = sendingType;
            IsSending = isSending;
        }

        /// <summary>
        /// Enumerable list of sending types {Sync, Async}.
        /// </summary>
        public enum SendingTypes
        {
            /// <summary>
            /// Sending synchronously
            /// </summary>
            Sync,

            /// <summary>
            /// Sending asynchronously
            /// </summary>
            Async,
        }

        /// <summary>
        /// Gets or sets a value indicating whether the communicator is sending.
        /// </summary>
        public bool IsSending
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the sending type.
        /// </summary>
        public SendingTypes SendingType
        {
            get;
            set;
        }
    }
}
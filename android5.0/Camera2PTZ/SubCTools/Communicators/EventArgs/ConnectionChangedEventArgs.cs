//-----------------------------------------------------------------------
// <copyright file="ConnectionChangedEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators.EventArgsLib
{
    /// <summary>
    /// Connection changed event args.
    /// </summary>
    public class ConnectionChangedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="address">the address.</param>
        /// <param name="isConnected">true if connected.</param>
        public ConnectionChangedEventArgs(string address, bool isConnected)
        {
            Address = address;
            IsConnected = isConnected;
        }

        /// <summary>
        /// Gets the address.
        /// </summary>
        public string Address
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the communicator is connected.
        /// </summary>
        public bool IsConnected
        {
            get;
            private set;
        }
    }
}
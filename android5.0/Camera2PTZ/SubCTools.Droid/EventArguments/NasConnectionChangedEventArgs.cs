//-----------------------------------------------------------------------
// <copyright file="NasConnectionChangedEventArgs.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.EventArguments
{
    using SubCTools.Enums;
    using System;

    /// <summary>
    /// Contains the connection status information for connection changed event
    /// </summary>
    public class NasConnectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NasConnectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="status">the new status</param>
        public NasConnectionChangedEventArgs(ConnectionStatus status)
        {
            ConnectionStatus = status;
        }

        /// <summary>
        /// Gets the new connection status
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; }
    }
}
//-----------------------------------------------------------------------
// <copyright file="SocketExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    using System.Net.Sockets;

    /// <summary>
    /// Extension class for the <see cref="Socket"/> object.
    /// </summary>
    public static class SocketExtensions
    {
        /// <summary>
        /// Returns a <see cref="bool"/> representing whether or not it is currently connected.
        /// </summary>
        /// <param name="socket"><see cref="Socket"/> to check connection.</param>
        /// <returns>A <see cref="bool"/> representing whether or not it is currently connected.</returns>
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}

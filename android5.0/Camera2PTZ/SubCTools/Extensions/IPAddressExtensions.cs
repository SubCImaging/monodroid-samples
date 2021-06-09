//-----------------------------------------------------------------------
// <copyright file="IPAddressExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Extensions
{
    using System.Net;

    public static class IPAddressExtensions
    {
        public static bool IsIPv4Multicast(this IPAddress ip)
        {
            var multicastBitMask = 14; // the 4 most significant bits '1110' means multicast address.
            return (ip.GetAddressBytes()[0] >> 4) == multicastBitMask;
        }

        public static bool IsLoopback(this IPAddress ip)
        {
            return ip == new IPAddress(new byte[] { 127, 0, 0, 1 });
        }
    }
}

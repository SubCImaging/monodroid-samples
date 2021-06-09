//-----------------------------------------------------------------------
// <copyright file="AddressCompare.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Models
{
    using System;

    /// <summary>
    /// Compares two <see cref="Tuple(EthernetAddress, string)"/>.
    /// </summary>
    public class AddressCompare : IEquatable<Tuple<EthernetAddress, string>>
    {
        /// <summary>
        /// Compares two <see cref="Tuple(EthernetAddress, string)"/> and returns binary <see cref="int"/> representing the outcome.
        /// </summary>
        /// <param name="x"><see cref="Tuple"/> 1.</param>
        /// <param name="y"><see cref="Tuple"/> 2.</param>
        /// <returns>Binary value, 1 for True, 0 for False.</returns>
        public int Compare(Tuple<EthernetAddress, string> x, Tuple<EthernetAddress, string> y)
        {
            return x.Item1.Address.ToString() == y.Item1.Address.ToString() && x.Item1.Port == y.Item1.Port && x.Item2 == y.Item2 ? 1 : 0;
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <param name="other"><see cref="string"/> input.</param>
        /// <returns>NotImplementedException (Not Implemented).</returns>
        public bool Equals(Tuple<EthernetAddress, string> other)
        {
            throw new NotImplementedException();
        }
    }
}

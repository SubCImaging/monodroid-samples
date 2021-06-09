//-----------------------------------------------------------------------
// <copyright file="TupleExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    using System.Collections;
    using System.Linq;

    /// <summary>
    /// A collection of extension methods for tuples.
    /// </summary>
    public static class TupleExtensions
    {
        /// <summary>
        /// Gets the list of items in a tuple.
        /// </summary>
        /// <param name="tuple">The tuple to get the items from.</param>
        /// <returns>A <see cref="IEnumerable"/> of items in a tuple.</returns>
        public static IEnumerable GetItems(this object tuple)
        {
            return tuple.GetType()
.GetProperties()
.Select(property => property.GetValue(tuple));
        }
    }
}

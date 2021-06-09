// <copyright file="AquisitionNameComparer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System.Collections.Generic;

    public class AquisitionNameComparer : IEqualityComparer<AquisitionID>
    {
        /// <inheritdoc/>
        public bool Equals(AquisitionID id1, AquisitionID id2)
        {
            return id1.Name == id2.Name;
        }

        /// <inheritdoc/>
        public int GetHashCode(AquisitionID id)
        {
            return id.Name.GetHashCode();
        }
    }
}
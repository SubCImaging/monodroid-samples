// <copyright file="AquisitionIDComparer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System.Collections.Generic;

    public class AquisitionIDComparer : IEqualityComparer<AquisitionID>
    {
        /// <inheritdoc/>
        public bool Equals(AquisitionID x, AquisitionID y)
        {
            return x.Equals(y);
        }

        /// <inheritdoc/>
        public int GetHashCode(AquisitionID obj)
        {
            return obj.GetHashCode();
        }
    }
}
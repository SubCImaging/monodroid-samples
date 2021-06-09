// <copyright file="CameraInfoComparer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helper
{
    using SubCTools.Droid;
    using System.Collections.Generic;

    public class CameraInfoComparer : IEqualityComparer<CameraInfo>
    {
        /// <inheritdoc/>
        public bool Equals(CameraInfo x, CameraInfo y)
        {
            return x.Equals(y);
        }

        /// <inheritdoc/>
        public int GetHashCode(CameraInfo obj)
        {
            return obj.GetHashCode();
        }
    }
}
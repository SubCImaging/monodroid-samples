//-----------------------------------------------------------------------
// <copyright file="IResettable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Interfaces
{
    /// <summary>
    /// An <see cref="interface"/> used to reset a classes state to the default.
    /// </summary>
    public interface IResettable
    {
        /// <summary>
        /// Reset method to reset a classes state to the default.
        /// </summary>
        void Reset();
    }
}

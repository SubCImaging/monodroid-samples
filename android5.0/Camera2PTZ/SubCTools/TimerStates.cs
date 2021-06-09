//-----------------------------------------------------------------------
// <copyright file="TimerStates.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools
{
    /// <summary>
    /// States that a time can be in.
    /// </summary>
    public enum TimerStates
    {
        /// <summary>
        /// Stopped state
        /// </summary>
        Stopped,

        /// <summary>
        /// Started state
        /// </summary>
        Started,

        /// <summary>
        /// Paused state
        /// </summary>
        Paused,
    }
}

//-----------------------------------------------------------------------
// <copyright file="RecordingStates.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Enums
{
    /// <summary>
    /// <see cref="enum"/> representing various states used in recording.
    /// </summary>
    public enum RecordingStates
    {
        /// <summary>
        /// Indicates the recording has stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// Indicates the device is currently recording.
        /// </summary>
        Recording,

        /// <summary>
        /// Indicates the recording is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Indicates a transient state.
        /// </summary>
        Transient,
    }
}

// <copyright file="IRecorder.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid
{
    using System;
    using System.Threading.Tasks;

    public interface IRecorder
    {
        event EventHandler<RecordedFile> VideoRecorded;

        event EventHandler<string> Started;

        // event EventHandler<string> RecordingStopped;
        TimeSpan CurrentTime { get; }

        string Directory { get; set; }

        bool? IsRecording { get; }

        string VideoName { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task StartRecording();

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task StartRecording(string path);

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task StopRecording();
    }
}
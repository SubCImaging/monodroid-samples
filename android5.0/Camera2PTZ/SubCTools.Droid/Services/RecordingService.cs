// <copyright file="RecordingService.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;

    /// <summary>
    /// Responsible for performing the recording actions on the camera.
    /// </summary>
    public class RecordingService
    {
        /// <summary>
        /// Start recording.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public void StartRecording()
        {
            // await Task.Delay(TimeSpan.FromMilliseconds(500));

            var i = new Random().Next(0, 3);

            if (i == 0)
            {
                throw new Exception("Failed to start");
            }
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task StopRecording()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            var i = new Random().Next(0, 3);

            if (i == 0)
            {
                throw new Exception("Failed to stop");
            }
        }
    }
}
// <copyright file="StillService.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Services
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Responsible for interacting with the camera to take stills.
    /// </summary>
    public class StillService
    {
        /// <summary>
        /// Take a picture.
        /// </summary>
        /// <returns>Name of the still.</returns>
        public Task<string> TakePictureAsync()
        {
            return Task.FromResult($"/stills/picture{new Random().Next(0, 1000)}.jpg");
        }
    }
}
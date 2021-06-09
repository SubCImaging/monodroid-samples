// <copyright file="IZeroConfDetector.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using SubCTools.Droid;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IZeroConfDetector
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IEnumerable<CameraInfo>> GetCamerasAsync();
    }
}
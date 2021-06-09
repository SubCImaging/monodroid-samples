// <copyright file="IStartable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;
    using System.Threading.Tasks;

    public interface IStartable
    {
        event EventHandler<bool?> IsStartedChanged;

        bool? IsStarted { get; }

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task StartAsync();

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task StopAsync();
    }
}

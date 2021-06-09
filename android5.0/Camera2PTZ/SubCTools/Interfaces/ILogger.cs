// <copyright file="ILogger.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System.IO;
    using System.Threading.Tasks;

    public interface ILogger
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task LogAsync(string data);

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="file"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task LogAsync(string data, FileInfo file);
    }
}
// <copyright file="IMatcher.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;

    public interface IMatcher : INotifyPropertyChanged
    {
        event EventHandler<string> MatchUpdated;

        string HeaderToMatch { get; set; }

        string Format { get; set; }

        string LatestMatch { get; }

        string Parse(string data);

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task ParseAsync(string data);
    }
}

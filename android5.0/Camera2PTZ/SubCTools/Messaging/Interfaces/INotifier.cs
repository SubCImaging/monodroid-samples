// <copyright file="INotifier.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Messaging.Interfaces
{
    using SubCTools.Messaging.Models;
    using System;

    /// <summary>
    /// An interface for an object that notifies.
    /// </summary>
    public interface INotifier
    {
        /// <summary>
        /// The notify event
        /// </summary>
        event EventHandler<NotifyEventArgs> Notify;
    }
}

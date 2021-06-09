//-----------------------------------------------------------------------
// <copyright file="IMessageService.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Messaging.Interfaces
{
    using SubCTools.Messaging.Models;
    using System;

    /// <summary>
    /// Interface for <see cref="IMessageService"/> which contains events for messages.
    /// </summary>
    internal interface IMessageService
    {
        /// <summary>
        /// MessageReceived event.
        /// </summary>
        event EventHandler<MessageEventArgs> MessageReceived;

        /// <summary>
        /// InformationReceived event.
        /// </summary>
        event EventHandler<MessageEventArgs> InformationReceived;

        /// <summary>
        /// WarningReceived event.
        /// </summary>
        event EventHandler<MessageEventArgs> WarningReceived;

        /// <summary>
        /// CriticalReceived event.
        /// </summary>
        event EventHandler<MessageEventArgs> CriticalReceived;
    }
}

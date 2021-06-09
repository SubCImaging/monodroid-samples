// <copyright file="DebugLogger.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid
{
    using SubCTools.Interfaces;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;

    public class DebugLogger : INotifiable
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="logger"></param>
        public DebugLogger(ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            // TODO: Delete the file once it gets so big
            // SubCLogger.Instance.Write(DateTime.Now + ": " + e.Message, "Debug.log", DroidSystem.LogDirectory);
            logger.LogAsync(DateTime.Now + ": " + e.Message);
        }
    }
}
// <copyright file="NotifyEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Messaging.Models
{
    using System;

    public class NotifyEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyEventArgs"/> class.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="messageType"></param>
        public NotifyEventArgs(object notification, MessageTypes messageType)
            : this(
                 notification,
                 messageType,
                 null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyEventArgs"/> class.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="messageType"></param>
        /// <param name="typeToNotify"></param>
        public NotifyEventArgs(

            // object sender, 
            object notification,
            MessageTypes messageType,
            Type typeToNotify)
        {
            // Sender = sender;
            Notification = notification;
            MessageType = messageType;
            TypeToNotify = typeToNotify;
        }

        public object Notification { get; }

        public string Message => Notification?.ToString() ?? string.Empty;

        public MessageTypes MessageType { get; }

        public Type TypeToNotify { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Message;
        }
    }
}

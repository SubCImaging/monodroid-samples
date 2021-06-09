// <copyright file="MessageEventArgs.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Messaging.Models
{
    public class MessageEventArgs : System.EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEventArgs"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        public MessageEventArgs(string message, MessageTypes messageType)
        {
            this.Message = message;
            this.MessageType = messageType;
        }

        public string Message { get; private set; }

        public MessageTypes MessageType { get; private set; }

    }
}

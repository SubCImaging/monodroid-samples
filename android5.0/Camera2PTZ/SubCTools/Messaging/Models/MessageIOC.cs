//-----------------------------------------------------------------------
// <copyright file="MessageIOC.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Messaging.Models
{
    using SubCTools.Extensions;
    using SubCTools.Messaging.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class for holding all the notifiable objects that can be called.
    /// </summary>
    public class MessageIOC
    {
        /// <summary>
        /// Lazy instance of the singleton.
        /// </summary>
        private static readonly Lazy<MessageIOC> instance = new Lazy<MessageIOC>(() => new MessageIOC());

        /// <summary>
        /// Object that can be used to lock on to to prevent threading issues.
        /// </summary>
        private readonly object sync = new object();

        /// <summary>
        /// Prevents a default instance of the <see cref="MessageIOC"/> class from being created.
        /// </summary>
        private MessageIOC()
        {
        }

        /// <summary>
        /// Gets a value indicating the message inversion container instance.
        /// </summary>
        public static MessageIOC Instance => instance.Value;

        /// <summary>
        /// Gets a value of notifiable objects that are to be notified when a message is received.
        /// </summary>
        public IDictionary<MessageTypes, Dictionary<INotifiable, Type>> Notifiables { get; } = new Dictionary<MessageTypes, Dictionary<INotifiable, Type>>();

        /// <summary>
        /// Add notifiable to be called when message type is received.
        /// </summary>
        /// <param name="messageType">Type of message being sent.</param>
        /// <param name="notifiable">Notifier to notify message.</param>
        /// <param name="fromType">Only listen to messages that are sent from a specific from type.</param>
        public void Add(MessageTypes messageType, INotifiable notifiable, Type fromType = null)
        {
            Add(messageType, notifiable, fromType != null ? new Type[] { fromType } : new Type[] { null });
        }

        /// <summary>
        /// Add notifiable to be called when message type is received.
        /// </summary>
        /// <param name="messageType">Type of message being sent.</param>
        /// <param name="notifiable">Notifier to notify message.</param>
        /// <param name="fromTypes">Only listen to messages that are sent from a specific from types.</param>
        public void Add(MessageTypes messageType, INotifiable notifiable, IEnumerable<Type> fromTypes)
        {
            foreach (MessageTypes item in Enum.GetValues(typeof(MessageTypes)))
            {
                if (messageType.HasFlag(item))
                {
                    lock (sync)
                    {
                        foreach (var fromType in fromTypes)
                        {
                            if (Notifiables.ContainsKey(item))
                            {
                                Notifiables[item].Update(notifiable, fromType);
                            }
                            else
                            {
                                Notifiables.Add(item, new Dictionary<INotifiable, Type>() { { notifiable, fromType } });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove the notifiable object from the list to be called when the specified message type is received.
        /// </summary>
        /// <param name="messageType">Message type that is received.</param>
        /// <param name="notifiable">Object to remove from being called.</param>
        public void Remove(MessageTypes messageType, INotifiable notifiable)
        {
            foreach (MessageTypes item in Enum.GetValues(typeof(MessageTypes)))
            {
                if (!messageType.HasFlag(item))
                {
                    continue;
                }

                lock (sync)
                {
                    if (Notifiables.ContainsKey(item))
                    {
                        Notifiables[item].Remove(notifiable);
                    }
                }
            }
        }

        /// <summary>
        /// Notify all the objects in the list who are registered to listen to a specific message type.
        /// </summary>
        /// <param name="sender">Object executing the delegate.</param>
        /// <param name="e">The notification event arguments.</param>
        public void Notify(object sender, NotifyEventArgs e)
        {
            // loop through all the enum values
            foreach (MessageTypes messageType in Enum.GetValues(typeof(MessageTypes)))
            {
                // check the message type to see if it has the flag, this is because of bitwise enums
                // if it has the flag, check to see if anyone is listening for it
                if (e.MessageType.HasFlag(messageType)
                    && Notifiables.ContainsKey(messageType))
                {
                    // go through all the ones who want to hear from this specific message type
                    foreach (var notifiable in Notifiables[messageType].ToList())
                    {
                        // just continue if you're listening to a specific type and this is not it
                        if (notifiable.Value != null
                            && !notifiable.Value.IsAssignableFrom(sender.GetType()))
                        {
                            continue;
                        }

                        // check to see if you've specified what type you want to communicate to 
                        if (e.TypeToNotify != null)
                        {
                            // keep moving if it's not assignable from the specified type
                            if (!notifiable.Key.GetType().IsAssignableFrom(e.TypeToNotify))
                            {
                                continue;
                            }
                        }

                        // don't notify yourself
                        if (notifiable.Key == sender)
                        {
                            continue;
                        }

                        // send the message
                        notifiable.Key.ReceiveNotification(sender, e);
                    }
                }
            }
        }
    }
}

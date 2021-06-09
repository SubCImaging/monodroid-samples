//-----------------------------------------------------------------------
// <copyright file="MessageRouter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Messaging.Models
{
    using SubCTools.Messaging.Interfaces;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class for routing messages between classes.
    /// </summary>
    public class MessageRouter : IMessageRouter
    {
        /// <summary>
        /// Lazy instance for the singleton.
        /// </summary>
        private static readonly Lazy<MessageRouter> instance = new Lazy<MessageRouter>(() => new MessageRouter());

        /// <summary>
        /// Prevents a default instance of the <see cref="MessageRouter"/> class from being created.
        /// </summary>
        private MessageRouter()
        {
        }

        /// <summary>
        /// Gets a value indicating the singleton instance.
        /// </summary>
        public static MessageRouter Instance => instance.Value;

        /// <summary>
        /// Gets a value indicating the list of notifiers registered.
        /// </summary>
        public IList<INotifier> Notifiers { get; } = new List<INotifier>();

        /// <summary>
        /// Add a notifier class to the message router.
        /// </summary>
        /// <param name="notifier">The class to register.</param>
        // public void Add(INotifier notifier)
        // {
        //    Notifiers.Add(notifier);
        //    notifier.Notify += Notifier_Notify;
        // }

        /// <summary>
        /// Add multiple notifiers to route messages from
        /// </summary>
        /// <param name="notifiers">Collection of notifiers you wish to add</param>
        public void Add(params INotifier[] notifiers)
        {
            foreach (var notifier in notifiers)
            {
                // Add(item);
                Notifiers.Add(notifier);
                notifier.Notify += Notifier_Notify;
            }
        }

        /// <summary>
        /// Determine if the Notifier is already in the message router.
        /// </summary>
        /// <param name="notifier">The notifier to test.</param>
        /// <returns>True if the notifier is already contained, false otherwise.</returns>
        public bool Contains(INotifier notifier)
        {
            foreach (var thisNotifier in Notifiers)
            {
                if (notifier == thisNotifier)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove the class from the message router.
        /// </summary>
        /// <param name="notifier">Class to unregister.</param>
        public void Remove(INotifier notifier)
        {
            notifier.Notify -= Notifier_Notify;
            Notifiers.Remove(notifier);
        }

        /// <summary>
        /// Route the message to the message inversion container.
        /// </summary>
        /// <param name="sender">Object executing the delegate.</param>
        /// <param name="e">Arguments for notification.</param>
        private void Notifier_Notify(object sender, NotifyEventArgs e)
        {
            MessageIOC.Instance.Notify(sender, e);
        }
    }
}
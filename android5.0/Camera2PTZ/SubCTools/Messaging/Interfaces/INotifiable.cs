//-----------------------------------------------------------------------
// <copyright file="INotifiable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Messaging.Interfaces
{
    using SubCTools.Messaging.Models;

    /// <summary>
    /// Classes that can be notified with information.
    /// </summary>
    public interface INotifiable
    {
        /// <summary>
        /// Recieve notification from a <see cref="object"/> sender.
        /// </summary>
        /// <param name="sender">The <see cref="object"/> sender that sent the notification.</param>
        /// <param name="e">The <see cref="NotifyEventArgs"/> message that the sender sent.</param>
        void ReceiveNotification(object sender, NotifyEventArgs e);
    }
}

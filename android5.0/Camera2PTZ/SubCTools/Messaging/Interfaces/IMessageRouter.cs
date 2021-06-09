//-----------------------------------------------------------------------
// <copyright file="IMessageRouter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Messaging.Interfaces
{
    public interface IMessageRouter
    {
        void Add(params INotifier[] notifiers);

        void Remove(INotifier notifier);
    }
}
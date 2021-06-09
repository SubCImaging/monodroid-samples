// <copyright file="ILogable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;

    public interface ILoggable : IStartable
    {
        event EventHandler<string> LogAdded;
    }
}

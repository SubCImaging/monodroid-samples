// <copyright file="LoggableContainer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;
    using System.Collections.Generic;

    public interface ILogDataContainer
    {
        IEnumerable<ILogData> LogDataContainer { get; }

        event EventHandler<ILogData> LogChanged;

        event EventHandler<ILogData> LogAdded;
    }
}

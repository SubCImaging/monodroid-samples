// <copyright file="IPowerListener.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid.Interfaces
{
    using System;

    public interface IPowerListener
    {
        event EventHandler<bool> IsPowerConnectedChanged;
    }
}
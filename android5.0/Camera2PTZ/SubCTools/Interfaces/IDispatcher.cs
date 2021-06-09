// <copyright file="IDispatcher.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;

    public interface IDispatcher
    {
        void Invoke(Action action);
    }
}

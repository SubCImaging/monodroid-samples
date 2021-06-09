// <copyright file="ILoader.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings.Interfaces
{
    using SubCTools.Interfaces;
    using System;

    public interface IDynamicLoader<T, K> : ILoader<T, K>
    {
        event EventHandler RepositoryChanged;
    }
}

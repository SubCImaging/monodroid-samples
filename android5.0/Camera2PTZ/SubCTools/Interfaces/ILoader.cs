// <copyright file="ILoader.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    public interface ILoader<T, K>
    {
        T Load(K key);
    }
}

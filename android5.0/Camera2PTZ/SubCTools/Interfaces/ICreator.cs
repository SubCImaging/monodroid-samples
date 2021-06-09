// <copyright file="ICreator.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    public interface ICreator<T>
    {
        void Create(T entry);
    }
}

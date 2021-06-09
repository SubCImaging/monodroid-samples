// <copyright file="IError.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;

    public interface IError
    {
        event EventHandler<string> Error;
    }
}

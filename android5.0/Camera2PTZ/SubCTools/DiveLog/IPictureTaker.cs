// <copyright file="IPictureTaker.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid
{
    using System;

    public interface IPictureTaker
    {
        event EventHandler<string> PictureTaken;

        string Directory { get; set; }

        string PictureName { get; set; }
    }
}
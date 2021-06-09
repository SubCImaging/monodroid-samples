// <copyright file="IFileCRUD.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
using System.IO;

namespace SubCTools.Settings.Interfaces
{
    public interface IFileCRUD : ICRUD
    {
        FileInfo File { get; }
    }
}
// <copyright file="IFileWriter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using System;

    public interface IFileWriter
    {
        string LastWrittenFile { get; }

        event EventHandler<string> LastWrittenFileChanged;
    }
}

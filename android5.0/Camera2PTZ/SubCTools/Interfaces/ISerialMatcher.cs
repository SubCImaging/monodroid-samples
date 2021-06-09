// <copyright file="ISerialMatcher.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using SubCTools.Communicators.Interfaces;

    public interface ISerialMatcher : IConnectable, IMatcher
    {
        string ComPort { get; set; }
    }
}

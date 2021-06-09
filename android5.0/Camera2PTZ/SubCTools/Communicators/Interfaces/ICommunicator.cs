// <copyright file="ICommunicator.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators.Interfaces
{
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Models;

    /// <summary>
    /// Interface for communicators.
    /// </summary>
    public interface ICommunicator : ICommunicator<CommunicatorAddress>
    {
    }

    /// <summary>
    /// Interface for specific types of communicators.
    /// </summary>
    /// <typeparam name="T">Type of communicator to create.</typeparam>
    public interface ICommunicator<T> : IMiniCommunicator<T, CommunicationData, string>
    {
    }
}
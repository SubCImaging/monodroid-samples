//-----------------------------------------------------------------------
// <copyright file="IDataRouter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Interfaces
{
    using SubCTools.Messaging.Interfaces;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for a DataRouter. Is both a notifiable and a notifier.
    /// </summary>
    public interface IDataRouter : INotifier, INotifiable
    {
        /// <summary>
        /// Gets or sets a unique identifier for distinguishing this object in the settings file/db.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Gets a value indicating whether is connected.
        /// </summary>
        bool? IsConnected { get; }

        /// <summary>
        /// Gets the readiness status of the router.
        /// </summary>
        string Status { get; }

        /// <summary>
        /// Connect to the communicator or data file.
        /// </summary>
        /// <returns>task of bool.</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the communicator.
        /// </summary>
        /// <returns>task.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// The Route method.
        /// </summary>
        /// <param name="data">Data to route.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task Route(string data);
    }
}
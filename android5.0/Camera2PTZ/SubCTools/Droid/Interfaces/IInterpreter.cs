//-----------------------------------------------------------------------
// <copyright file="IInterpreter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Interfaces
{
    using SubCTools.Messaging.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// An interface for interpreters.
    /// </summary>
    public interface IInterpreter
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IEnumerable<NotifyEventArgs>> InterpretSync(string item);
    }
}

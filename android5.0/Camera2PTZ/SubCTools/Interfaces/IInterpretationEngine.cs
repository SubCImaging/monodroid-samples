// <copyright file="IInterpretationEngine.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Interfaces
{
    using SubCTools.Models;
    using System.Threading.Tasks;

    public interface IInterpretationEngine
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<InterpretationResult> Interpret(string input);

        /// <summary>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<InterpretationResult> InterpretSync(string input);
    }
}
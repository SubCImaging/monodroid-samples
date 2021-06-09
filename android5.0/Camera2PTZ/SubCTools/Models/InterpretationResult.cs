// <copyright file="InterpretationResult.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Models
{
    public class InterpretationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterpretationResult"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="couldInterpret"></param>
        public InterpretationResult(string message, bool couldInterpret = false)
        {
            Message = message;
            CouldInterpret = couldInterpret;
        }

        public bool CouldInterpret { get; }

        public string Message { get; }
    }
}

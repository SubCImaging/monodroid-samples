//-----------------------------------------------------------------------
// <copyright file="MinimumPercentageNegativeException.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Scott Maher</author>
//-----------------------------------------------------------------------
namespace SubCTools.Exceptions
{
    using System;

    /// <summary>
    /// Instance of MinimumPercentageNegativeException.
    /// </summary>
    public class MinimumPercentageNegativeException : ArgumentOutOfRangeException
    {
        /// <summary>
        /// Create a new instance of the MinimumPercentageNegativeException class.
        /// </summary>
        /// <param name="paramName">Name of the given parameter.</param>
        /// <param name="actualValue">Actual value of the argument.</param>
        /// <param name="message">Specified error message.</param>
        public MinimumPercentageNegativeException(string paramName, object actualValue, string message) : base(paramName, actualValue, message) { }
    }
}

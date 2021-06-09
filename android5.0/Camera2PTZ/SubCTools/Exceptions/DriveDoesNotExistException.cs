// <copyright file="DriveDoesNotExistException.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Exceptions
{
    using System;

    public class DriveDoesNotExistException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DriveDoesNotExistException"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public DriveDoesNotExistException(string message, Exception innerException = null) : base(message, innerException) { }
    }
}

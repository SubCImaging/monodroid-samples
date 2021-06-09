//-----------------------------------------------------------------------
// <copyright file="RemoteCommand.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Attributes
{
    using System;

    /// <summary>
    /// RemoteCommand class used for sending / recieving commands remotely to cameras.
    /// </summary>
    public class RemoteCommand : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteCommand"/> class attribute.
        /// </summary>
        /// <param name="prefix">String to prepend to method name.</param>
        /// <param name="ovrride">If ovride is true, it will only execute this method if there are two with the same name.</param>
        /// <param name="hidden">If hidden is true the command will not appear in GetCommands or in the online API.</param>
        /// <param name="classNameRequired">If classNameRequired is true the command will only be called if the class name is prepended. ie Class.Command:Parameter.</param>
        public RemoteCommand(string prefix = "", bool ovrride = false, bool hidden = false, bool classNameRequired = false)
        {
            Prefix = prefix;
            Override = ovrride;
            Hidden = hidden;
            ClassNameRequired = classNameRequired;
        }

        /// <summary>
        /// Gets a value indicating whether a class name is required.
        /// </summary>
        public bool ClassNameRequired { get; }

        /// <summary>
        /// Gets a value indicating whether you want the <see cref="RemoteCommand"/> to be hidden from API and GetCommands.
        /// </summary>
        public bool Hidden { get; }

        /// <summary>
        /// Gets a value indicating whether you want to override any existing commands with this one.
        /// </summary>
        public bool Override { get; }

        /// <summary>
        /// Gets the value of the Prefix.
        /// </summary>
        public string Prefix { get; }
    }
}
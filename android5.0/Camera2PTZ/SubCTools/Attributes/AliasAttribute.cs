//-----------------------------------------------------------------------
// <copyright file="AliasAttribute.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Attributes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Define a list of keywords that will be considered aliases.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class AliasAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AliasAttribute"/> class.
        /// </summary>
        /// <param name="aliases">A list of keywords to consider aliases.</param>
        public AliasAttribute(params string[] aliases)
        {
            Aliases = aliases;
        }

        /// <summary>
        /// Gets An enumerable list of alias names.
        /// </summary>
        public IEnumerable<string> Aliases { get; }
    }
}
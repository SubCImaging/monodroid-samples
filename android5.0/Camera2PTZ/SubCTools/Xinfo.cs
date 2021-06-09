//-----------------------------------------------------------------------
// <copyright file="Xinfo.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools
{
    using System.Collections.Generic;

    /// <summary>
    /// An object representation of a Node in an XML document.
    /// </summary>
    public class XInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XInfo"/> class. This XInfo will have an empty <see cref="Name"/> property.
        /// </summary>
        /// <param name="value">The value inside the XML node this XInfo represends.</param>
        /// <param name="attributes">Attributes in the header the node definition. </param>
        public XInfo(string value, Dictionary<string, string> attributes)
            : this(string.Empty, value, attributes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the XML node.</param>
        /// <param name="value">The value inside the XML node this XInfo represents.</param>
        /// <param name="attributes">Attributes in the header of the node definition.</param>
        public XInfo(string name, string value, Dictionary<string, string> attributes)
        {
            Name = name;
            Value = value;
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the attributes of the XML node this XInfo represents.
        /// </summary>
        public Dictionary<string, string> Attributes { get; }

        /// <summary>
        /// Gets the Name of the XML node this XInfo represents.
        /// </summary>
        public string Name { get; } = string.Empty;

        /// <summary>
        /// Gets the Value of the XML node this XInfo represents.
        /// </summary>
        public string Value { get; } = string.Empty;
    }
}
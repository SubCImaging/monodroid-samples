//-----------------------------------------------------------------------
// <copyright file="SqlInfo.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.SQLite
{
    using global::SQLite;

    /// <summary>
    /// Class for containing sql info.
    /// </summary>
    public class SqlInfo
    {
        /// <summary>
        /// Gets or sets the attributes of the XML node this XInfo represents.
        /// </summary>
        public string Attributes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Name of the XML node this XInfo represents.
        /// </summary>
        [PrimaryKey]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Value of the XML node this XInfo represents.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
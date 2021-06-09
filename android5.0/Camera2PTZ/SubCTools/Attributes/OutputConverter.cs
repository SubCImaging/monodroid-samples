//-----------------------------------------------------------------------
// <copyright file="OutputConverter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Attributes
{
    using System;

    /// <summary>
    /// Special type of property converter.
    /// </summary>
    public class OutputConverter : PropertyConverterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutputConverter"/> class.
        /// </summary>
        /// <param name="converter">Type of converter.</param>
        public OutputConverter(Type converter)
            : base(converter)
        {
        }
    }
}
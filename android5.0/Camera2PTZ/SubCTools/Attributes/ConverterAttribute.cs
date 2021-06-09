//-----------------------------------------------------------------------
// <copyright file="ConverterAttribute.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Attributes
{
    using SubCTools.Interfaces;
    using System;

    /// <summary>
    /// Attribute that specifies a converter that the property setter can use to convert its value before attempting to set the property.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class ConverterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConverterAttribute"/> class.
        /// </summary>
        /// <param name="converter">The converter class to use.  Must implement IConverter.</param>
        public ConverterAttribute(Type converter)
        {
            try
            {
                Converter = (IConvert)Activator.CreateInstance(converter);
            }
            catch
            {
                throw new InvalidCastException("Class must implement IConvert");
            }
        }

        /// <summary>
        /// Gets the converter.
        /// </summary>
        public IConvert Converter { get; }
    }
}
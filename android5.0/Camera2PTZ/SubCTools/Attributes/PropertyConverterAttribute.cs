//-----------------------------------------------------------------------
// <copyright file="PropertyConverterAttribute.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Attributes
{
    using SubCTools.Interfaces;
    using System;

    /// <summary>
    /// Property converter attribute for interpreter.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class PropertyConverterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyConverterAttribute"/> class.
        /// </summary>
        /// <param name="converter">The converter class to use, must implement IPropertyConverter.</param>
        public PropertyConverterAttribute(Type converter)
            : this(converter, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyConverterAttribute"/> class.
        /// </summary>
        /// <param name="converter">Type of converter to create.</param>
        /// <param name="argumentName">The specific name of the argument to target.</param>
        public PropertyConverterAttribute(Type converter, string argumentName)
        {
            ArgumentName = argumentName;

            try
            {
                Converter = (IPropertyConverter)Activator.CreateInstance(converter);
            }
            catch
            {
                throw new InvalidCastException("Class must implement IPropertyConverter");
            }
        }

        /// <summary>
        /// Gets the specific argument name to target.
        /// </summary>
        public string ArgumentName { get; }

        /// <summary>
        /// Gets the conversion implementation of the converter.
        /// </summary>
        public IPropertyConverter Converter { get; }
    }
}
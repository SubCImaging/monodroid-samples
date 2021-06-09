//-----------------------------------------------------------------------
// <copyright file="PropertyExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    using SubCTools.Attributes;
    using System;
    using System.Reflection;

    /// <summary>
    /// A class that handles extensions for properties.
    /// </summary>
    public static class PropertyExtensions
    {
        /// <summary>
        /// A <see cref="bool"/> representing whether or not the
        /// <see cref="PropertyInfo"/> contains a specific attribute.
        /// </summary>
        /// <typeparam name="T">The type of attribute to check for.</typeparam>
        /// <param name="property">The property to check.</param>
        /// <returns><see cref="bool"/> representing whether or not the
        /// <see cref="PropertyInfo"/> contains a specific attribute.</returns>
        /// <remarks>DEPRECATED USE <see cref="HasAttribute"/>.</remarks>
        public static bool ContainsAttribute<T>(this PropertyInfo property) where T : Attribute
        {
            return property.HasAttribute<T>();
        }

        /// <summary>
        /// Get the converted value of the property if it has a PropertyConverter.
        /// </summary>
        /// <param name="property">Property Info to get value from.</param>
        /// <param name="owner">Object that owns the property to get value.</param>
        /// <returns>Converted value if has a converter and try convert succeeds, unconverted value otherwise.</returns>
        public static object GetConvertedValue(this PropertyInfo property, object owner)
        {
            var value = property.GetValue(owner);

            if (property.HasAttribute<PropertyConverterAttribute>())
            {
                var converter = property.GetCustomAttribute<PropertyConverterAttribute>();
                if (converter.Converter.TryConvertBack(value, out var v))
                {
                    value = v;
                }
            }

            return value;
        }
    }
}
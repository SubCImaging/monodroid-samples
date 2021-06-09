//-----------------------------------------------------------------------
// <copyright file="Saveable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class Savable : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Savable"/> class.
        /// </summary>
        public Savable()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Savable"/> class.
        /// </summary>
        /// <param name="saveAsName"></param>
        public Savable(string saveAsName)
        {
            SaveAsName = saveAsName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Savable"/> class.
        /// </summary>
        /// <param name="castingType"></param>
        public Savable(Type castingType)
        {
            CastingType = castingType;
        }

        /// <summary>
        /// Create a new instance of the savable attribute.
        /// </summary>
        /// <param name="order">The order in which to load properties. Normal(2) by default, anything lower will load first.</param>
        public Savable(LoadingOrder order)
        {
            Order = order;
        }

        public Type CastingType
        {
            get;
        }
= null;

        /// <summary>
        /// Gets an empty string.
        /// </summary>
        public string SaveAsName { get; } = string.Empty;

        public LoadingOrder Order { get; } = LoadingOrder.Normal;
    }

    /// <summary>
    /// The order in which to load properties. Normal(2) by default, anything lower will load first.
    /// </summary>
    public enum LoadingOrder
    {
        /// <summary>
        /// First Item to load
        /// </summary>
        First = 0,

        /// <summary>
        /// High Priority item to load.
        /// </summary>
        High = 1,

        /// <summary>
        /// Normal Priority item to load.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Low Priority item to load.
        /// </summary>
        Low = 3,

        /// <summary>
        /// Last item to load.
        /// </summary>
        Last = 4,
    }
}

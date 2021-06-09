// <copyright file="ReactiveAttribute.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;

    public class ReactiveAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveAttribute"/> class.
        /// </summary>
        public ReactiveAttribute()
            : this(string.Empty)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReactiveAttribute"/> class.
        /// </summary>
        /// <param name="name"></param>
        public ReactiveAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}

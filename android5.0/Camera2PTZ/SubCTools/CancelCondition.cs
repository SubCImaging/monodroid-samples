//-----------------------------------------------------------------------
// <copyright file="CancelCondition.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Helpers
{
    using SubCTools.Enums;
    using System;

    /// <summary>
    /// Base cancel condition class.
    /// </summary>
    public abstract class CancelCondition
    {
        /// <summary>
        /// Gets or sets either == or !=.
        /// </summary>
        public abstract ComparisonOperators CancelOp { get; set; }

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        public abstract string CancelWhenProp { get; set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// Used to evaluate the condition.
        /// </summary>
        /// <param name="cancelWhenObj">Object to evaluate.</param>
        /// <returns>True when evaluates.</returns>
        public abstract bool Evaluate(object cancelWhenObj);
    }
}
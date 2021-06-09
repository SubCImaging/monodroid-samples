//-----------------------------------------------------------------------
// <copyright file="CancelCondition{T}.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Helpers
{
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using System;
    using System.Reflection;

    /// <summary>
    /// Cancel conditions with a generic.
    /// </summary>
    /// <typeparam name="T">Type of cancel condition.</typeparam>
    public class CancelCondition<T> : CancelCondition
        where T : IComparable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CancelCondition{T}"/> class.
        /// </summary>
        /// <param name="cancelWhenProp">The cancel when property.</param>
        /// <param name="comparisonOperator">The comparison operator.</param>
        /// <param name="cancelValue">The cancel value.</param>
        public CancelCondition(string cancelWhenProp, ComparisonOperators comparisonOperator, T cancelValue)
        {
            CancelWhenProp = cancelWhenProp;
            CancelOp = comparisonOperator;
            CancelValue = cancelValue;
        }

        /// <summary>
        /// Gets or sets either == or !=.
        /// </summary>
        public override ComparisonOperators CancelOp { get; set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public T CancelValue { get; set; }

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        public override string CancelWhenProp { get; set; }

        /// <summary>
        /// Gets the type T.
        /// </summary>
        public override Type Type => CancelValue.GetType();

        /// <summary>
        /// Used to evaluate the condition.
        /// </summary>
        /// <param name="cancelWhenObj">Cancel when object.</param>
        /// <returns>True when evaluated correctly.</returns>
        public override bool Evaluate(object cancelWhenObj)
        {
            var cancelWhenProp = cancelWhenObj.GetType().GetProperty(CancelWhenProp, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (CancelWhenProp == null)
            {
                return false;
            }

            T value;

            if (typeof(T) == typeof(string))
            {
                value = (T)(object)cancelWhenProp.GetValue(cancelWhenObj).ToString();
            }
            else
            {
                value = (T)cancelWhenProp.GetValue(cancelWhenObj);
            }

            if (value.GetType() == Type)
            {
                return Compare(CancelOp, value, CancelValue);
            }
            else
            {
                return Compare(CancelOp, value.ToString(), CancelValue.ToString());
            }
        }

        /// <summary>
        /// ToString override.
        /// </summary>
        /// <returns>The string.</returns>
        public override string ToString()
        {
            return $"'{CancelWhenProp}' {CancelOp.GetDescription()} {CancelValue}";
        }

        private static bool Compare<TK>(ComparisonOperators op, TK left, TK right)
            where TK : IComparable<TK>
        {
            if (left == null || right == null)
            {
                throw new ArgumentException("Invalid operand (null): {0}", (left == null) ? "left" : "right");
            }

            switch (op)
            {
                case ComparisonOperators.LessThan: return left.CompareTo(right) < 0;
                case ComparisonOperators.GreaterThan: return left.CompareTo(right) > 0;
                case ComparisonOperators.LessThanOrEqual: return left.CompareTo(right) <= 0;
                case ComparisonOperators.GreaterThanOrEqual: return left.CompareTo(right) >= 0;
                case ComparisonOperators.EqualTo: return left.Equals(right);
                case ComparisonOperators.NotEqualTo: return !left.Equals(right);
                default: throw new ArgumentException("Invalid comparison operator: {0}", op.ToString());
            }
        }
    }
}
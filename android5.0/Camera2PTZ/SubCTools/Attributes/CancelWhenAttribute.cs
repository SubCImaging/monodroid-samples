// <copyright file="CancelWhenAttribute.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Attributes
{
    using SubCTools.Droid.Helpers;
    using SubCTools.Enums;
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// An attribute for specifying when to cancel a command when specific conditions are met.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class CancelWhenAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CancelWhenAttribute"/> class with a variable number of conditions.
        /// Each condition is specified by three <see cref="Params"/> of type string, ComparissonOperator and a value in that order
        /// If the first is not a string, the second is not a comparrisonOperator, or the third is not an int, double, bool, or string and exception will be thrown.
        /// If the number of <see cref="Params"/> is not a multiple of three an exception will be thrown.
        /// </summary>
        /// <param name="paramsList">The parameters.</param>
        public CancelWhenAttribute(params object[] paramsList)
        {
            Type[] acceptedTypes = { typeof(string), typeof(double), typeof(int), typeof(bool) };
            if (paramsList.Count() % 3 != 0)
            {
                if ((paramsList.Count() - 1) % 3 == 0 && paramsList.Last().GetType() == typeof(string))
                {
                    CustomMessage = (string)paramsList.Last();
                }
                else
                {
                    throw new ArgumentException("Parameters must be supplied in sets of three (Or with one trailing string parameter containing the message text)");
                }
            }

            var i = 0;
            while (i < paramsList.Count() - 1)
            {
                if (paramsList[i].GetType() == typeof(string)
                    && paramsList[i + 1].GetType() == typeof(ComparisonOperators)
                    && acceptedTypes.Contains(paramsList[i + 2].GetType()))
                {
                    if (paramsList[i + 2].GetType() == typeof(string))
                    {
                        CancelConditions.Add(new CancelCondition<string>((string)paramsList[i], (ComparisonOperators)paramsList[i + 1], (string)paramsList[i + 2]));
                    }
                    else if (paramsList[i + 2].GetType() == typeof(double))
                    {
                        CancelConditions.Add(new CancelCondition<double>((string)paramsList[i], (ComparisonOperators)paramsList[i + 1], (double)paramsList[i + 2]));
                    }
                    else if (paramsList[i + 2].GetType() == typeof(int))
                    {
                        CancelConditions.Add(new CancelCondition<int>((string)paramsList[i], (ComparisonOperators)paramsList[i + 1], (int)paramsList[i + 2]));
                    }
                    else if (paramsList[i + 2].GetType() == typeof(bool))
                    {
                        CancelConditions.Add(new CancelCondition<bool>((string)paramsList[i], (ComparisonOperators)paramsList[i + 1], (bool)paramsList[i + 2]));
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid parameter type");
                }

                i += 3;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelWhenAttribute"/> class.
        /// </summary>
        /// <param name="cancelWhenProp">The property to assess.</param>
        /// <param name="op">The comparison operator.</param>
        /// <param name="cancelValue">The value to compare against.</param>
        /// <param name="customMessage">A custom message to return if command cancelled.</param>
        public CancelWhenAttribute(string cancelWhenProp, ComparisonOperators op, string cancelValue, string customMessage = null)
                    : this(new CancelCondition<string>(cancelWhenProp, op, cancelValue), customMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelWhenAttribute"/> class.
        /// </summary>
        /// <param name="cancelWhenProp"></param>
        /// <param name="op"></param>
        /// <param name="cancelValue"></param>
        /// <param name="customMessage"></param>
        public CancelWhenAttribute(string cancelWhenProp, ComparisonOperators op, double cancelValue, string customMessage = null)
                    : this(new CancelCondition<double>(cancelWhenProp, op, cancelValue), customMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelWhenAttribute"/> class.
        /// </summary>
        /// <param name="cancelWhenProp"></param>
        /// <param name="op"></param>
        /// <param name="cancelValue"></param>
        /// <param name="customMessage"></param>
        public CancelWhenAttribute(string cancelWhenProp, ComparisonOperators op, int cancelValue, string customMessage = null)
                    : this(new CancelCondition<int>(cancelWhenProp, op, cancelValue), customMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelWhenAttribute"/> class.
        /// </summary>
        /// <param name="cancelWhenProp"></param>
        /// <param name="cancelValue"></param>
        /// <param name="customMessage"></param>
        public CancelWhenAttribute(string cancelWhenProp, bool cancelValue, string customMessage = null)
                    : this(new CancelCondition<bool>(cancelWhenProp, ComparisonOperators.EqualTo, cancelValue), customMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelWhenAttribute"/> class.
        /// </summary>
        /// <param name="cancelWhenProp"></param>
        /// <param name="op"></param>
        /// <param name="cancelValue"></param>
        /// <param name="customMessage"></param>
        public CancelWhenAttribute(string cancelWhenProp, ComparisonOperators op, bool cancelValue, string customMessage)
              : this(new CancelCondition<bool>(cancelWhenProp, op, cancelValue), customMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelWhenAttribute"/> class.
        /// </summary>
        /// <param name="cancelWhenProp1"></param>
        /// <param name="op1"></param>
        /// <param name="cancelValue1"></param>
        /// <param name="cancelWhenProp2"></param>
        /// <param name="op2"></param>
        /// <param name="cancelValue2"></param>
        /// <param name="customMessage"></param>
        public CancelWhenAttribute(string cancelWhenProp1, ComparisonOperators op1, bool cancelValue1, string cancelWhenProp2, ComparisonOperators op2, bool cancelValue2, string customMessage)
              : this(new object[] { cancelWhenProp1, op1, cancelValue1, cancelWhenProp2, op2, cancelValue2, customMessage })
        {
        }

        private CancelWhenAttribute(CancelCondition condition, string customMessage = null)
                                    : this(new Collection<CancelCondition> { condition }, customMessage)
        {
        }

        private CancelWhenAttribute(Collection<CancelCondition> conditions, string customMessage = null)
        {
            CancelConditions = conditions;
            CustomMessage = customMessage;
        }

        /// <summary>
        /// Gets or sets the condition(s) in which to cancel.
        /// </summary>
        public Collection<CancelCondition> CancelConditions { get; set; } = new Collection<CancelCondition>();

        public string CustomMessage { get; set; }
    }
}
//-----------------------------------------------------------------------
// <copyright file="ComparisonOperators.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------

namespace SubCTools.Enums
{
    using System.ComponentModel;

    public enum ComparisonOperators
    {
        [Description("is")]
        EqualTo,

        [Description("is not")]
        NotEqualTo,

        [Description("<")]
        LessThan,

        [Description(">")]
        GreaterThan,

        [Description("<=")]
        LessThanOrEqual,

        [Description(">=")]
        GreaterThanOrEqual,
    }
}

// <copyright file="PrimaryKey.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Attributes
{
    using System;

    /// <summary>
    /// Primary key attribute for use with CRUD.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public class PrimaryKey : Attribute
    {
    }
}
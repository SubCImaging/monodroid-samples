// <copyright file="Definition.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

//namespace SubCTools.Models
//{
//    using SubCTools.DiveLog;
//    using System;
//    using System.Collections.Generic;

//    /// <summary>
//    /// Class for holding definitions.
//    /// </summary>
//    public class Definition
//    {
//        /// <summary>
//        /// Initializes a new instance of the <see cref="Definition"/> class.
//        /// </summary>
//        public Definition()
//            : this(
//                  "$-SUBC",
//                  new Dictionary<string, Type>
//                {
//                        { "DateTime", typeof(DateTime) },
//                        { "Altitude(m)", typeof(double) },
//                        { "Depth(m)", typeof(double) },
//                        { "Northing", typeof(double) },
//                        { "Easting", typeof(double) },
//                        { "Speed(knts)", typeof(double) },
//                        { "KP", typeof(double) },
//                        { "DCC", typeof(double) },
//                        { "Heading", typeof(double) },
//                })
//        {
//        }

//        /// <summary>
//        /// Initializes a new instance of the <see cref="Definition"/> class.
//        /// </summary>
//        /// <param name="header">Header of the definition.</param>
//        /// <param name="titles">Titles for all the cells in the csv.</param>
//        public Definition(string header, Dictionary<string, Type> titles)
//        {
//            if (string.IsNullOrEmpty(header))
//            {
//                throw new System.ArgumentException(nameof(header));
//            }

//            if (titles is null)
//            {
//                throw new System.ArgumentNullException(nameof(titles));
//            }

//            Header = header;
//            Titles = titles;
//        }

//        /// <summary>
//        /// Gets the header of the definition.
//        /// </summary>
//        [PrimaryKey]
//        public string Header { get; }

//        /// <summary>
//        /// Gets the titles of the csv string.
//        /// </summary>
//        public Dictionary<string, Type> Titles { get; }
//    }
//}
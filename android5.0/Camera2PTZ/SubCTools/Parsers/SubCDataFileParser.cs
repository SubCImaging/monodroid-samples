//-----------------------------------------------------------------------
// <copyright file="SubCDataFileParser.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Parsers
{
    using SubCTools.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public class SubCDataFileParser : IParser
    {
        private DateTime lastTime = DateTime.MinValue;

        /// <inheritdoc/>
        public object Parse(string data)
        {
            return ParseTuple(data);
        }

        public (DateTime, IList<string>) ParseTuple(string data)
        {
            var fields = data.Split(',').Select(d => d.Trim()).ToList();

            if (fields.Count <= 1)
            {
                return default;
            }

            for (var i = 0; i < 2; i++)
            {
                if (DateTime.TryParseExact(
                     fields[i],
                     @"yyyy-MM-dd HH:mm:ss.fff",
                     new DateTimeFormatInfo(),
                     DateTimeStyles.None,
                     out var t))
                {
                    lastTime = t;
                    fields.RemoveAt(i);
                    return (t, fields);
                }
            }

            return (lastTime, fields);
        }
    }
}

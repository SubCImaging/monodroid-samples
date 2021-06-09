//-----------------------------------------------------------------------
// <copyright file="NmeaDefinition.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Models
{
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Helpers;
    using System.Collections.Generic;

    public class NmeaDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NmeaDefinition"/> class.
        /// </summary>
        /// <param name="header"></param>
        public NmeaDefinition(string header)
            : this(header, new List<string>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NmeaDefinition"/> class.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="fieldNames"></param>
        [JsonConstructor]
        public NmeaDefinition(string header, List<string> fieldNames)
        {
            Header = header;
            FieldNames = fieldNames;
        }

        public List<string> FieldNames { get; } = new List<string>();

        [PrimaryKey]
        public string Header { get; set; } = string.Empty;

        public void SetFieldNames(IEnumerable<string> names)
        {
            FieldNames.SetElementsTo(names);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Header:{Header}, \nFields:{string.Join(",", FieldNames)}";
        }
    }
}
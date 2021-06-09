// <copyright file="SubCVersionConverter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Converters.JsonConverters
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    public class SubCVersionConverter : VersionConverter
    {
        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (Version.TryParse(reader.Value.ToString(), out var v))
            {
                return v;
            }
            else
            {
                return default(Version);
            }
        }
    }
}

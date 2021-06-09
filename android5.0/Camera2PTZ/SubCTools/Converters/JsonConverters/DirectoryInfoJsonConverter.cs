//-----------------------------------------------------------------------
// <copyright file="DirectoryInfoJsonConverter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Converters.JsonConverters
{
    using Newtonsoft.Json;
    using System;
    using System.IO;

    public class DirectoryInfoJsonConverter : JsonConverter<DirectoryInfo>
    {
        /// <inheritdoc/>
        public override DirectoryInfo ReadJson(JsonReader reader, Type objectType, DirectoryInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value is string s)
            {
                return new DirectoryInfo(s);
            }

            throw new ArgumentOutOfRangeException(nameof(reader));
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, DirectoryInfo value, JsonSerializer serializer)
        {
            writer.WriteValue(value.FullName);
        }
    }
}

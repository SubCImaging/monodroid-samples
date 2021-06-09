// <copyright file="EthernetAddressConverter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Converters.JsonConverters
{
    using Newtonsoft.Json;
    using SubCTools.Models;
    using System;
    using System.Linq;
    using System.Net;

    public class EthernetAddressConverter : JsonConverter<EthernetAddress>
    {
        /// <inheritdoc/>
        public override EthernetAddress ReadJson(JsonReader reader, Type objectType, EthernetAddress existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var v = reader.Value.ToString().Split(':');

            if (v.Count() > 0)
            {
                if (IPAddress.TryParse(v[0], out var ip))
                {
                    return new EthernetAddress(ip, int.Parse(v[1]));
                }
            }

            // just return localhost to prevent null exceptions thrown all over the place
            return new EthernetAddress(IPAddress.Parse("127.0.0.1"), 8888);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, EthernetAddress value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
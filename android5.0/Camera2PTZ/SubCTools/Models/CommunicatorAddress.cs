// <copyright file="CommunicatorAddress.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Models
{
    using System.Collections.Generic;
    using System.Linq;

    public class CommunicatorAddress : Dictionary<string, string>
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return "{" + string.Join(",", this.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
        }
    }
}
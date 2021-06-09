// <copyright file="GenericJsonConvert.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Converters.JsonConverters
{
    using Newtonsoft.Json;
    using SubCTools.Interfaces;

    public class GenericJsonConvert : IPropertyConverter
    {
        /// <inheritdoc/>
        public string Format => string.Empty;

        /// <inheritdoc/>
        public bool TryConvert(object data, out object value)
        {
            value = null;

            try
            {
                value = JsonConvert.SerializeObject(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public bool TryConvertBack(object data, out object value)
        {
            value = null;

            try
            {
                value = JsonConvert.SerializeObject(data);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
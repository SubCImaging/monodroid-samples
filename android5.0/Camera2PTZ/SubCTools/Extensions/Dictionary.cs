//-----------------------------------------------------------------------
// <copyright file="Dictionary.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Extensions
{
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="Dictionary"/> extension class.
    /// </summary>
    public static class Dictionary
    {
        /// <summary>
        /// If the entry is not found already this adds it, otherwise it updates the value.
        /// </summary>
        /// <typeparam name="T">The type of the key.</typeparam>
        /// <typeparam name="K">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to update.</param>
        /// <param name="key">The key to search for.</param>
        /// <param name="value">The value to add/replace. </param>
        public static void Update<T, K>(this IDictionary<T, K> dictionary, T key, K value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
            else
            {
                dictionary[key] = value;
            }
        }

        /// <summary>
        /// Compares the full contents of two <see cref="Dictionary{TKey, TValue}"/>s.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dict1">The <see cref="Dictionary{TKey, TValue}"/> to compare.</param>
        /// <param name="dict2">The <see cref="Dictionary{TKey, TValue}"/> to compare against.</param>
        /// <returns></returns>
        public static bool Compare<TKey, TValue>(
    this Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
        {
            if (dict1 == dict2)
            {
                return true;
            }

            if ((dict1 == null) || (dict2 == null))
            {
                return false;
            }

            if (dict1.Count != dict2.Count)
            {
                return false;
            }

            var valueComparer = EqualityComparer<TValue>.Default;

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var value2))
                {
                    return false;
                }

                if (!valueComparer.Equals(kvp.Value, value2))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
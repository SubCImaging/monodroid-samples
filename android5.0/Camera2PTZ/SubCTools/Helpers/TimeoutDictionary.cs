//-----------------------------------------------------------------------
// <copyright file="TimeoutDictionary.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Helpers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A <see cref="Dictionary{TKey, TValue}"/> that removes keys after a set
    /// <see cref="TimeSpan"/> after they are added.
    /// </summary>
    /// <typeparam name="TKey">The <see cref="typeof"/> key.</typeparam>
    /// <typeparam name="TValue">The <see cref="typeof"/> value.</typeparam>
    public class TimeoutDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        /// <summary>
        /// The <see cref="TimeSpan"/> to wait before removing the key from
        /// the <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        private TimeSpan timeout;

        /// <summary>
        /// The <see cref="ActionScheduler"/> that handles removing the keys.
        /// </summary>
        private readonly ActionScheduler scheduler = new ActionScheduler();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="timeout">The <see cref="TimeSpan"/> to wait before removing the key from
        /// the <see cref="Dictionary{TKey, TValue}"/>.</param>
        public TimeoutDictionary(TimeSpan timeout)
        {
            this.timeout = timeout;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        /// <exception cref="ArgumentNullException">key is null.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the System.Collections.Generic.Dictionary`2.</exception>
        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            scheduler.AddOnce(timeout, () => Remove(key), key.ToString());
        }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The <see cref="TKey"/> to remove.</param>
        public new void Remove(TKey key)
        {
            base.Remove(key);
            scheduler.Remove(key.ToString());
        }
    }
}

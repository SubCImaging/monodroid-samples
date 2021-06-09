//-----------------------------------------------------------------------
// <copyright file="ActionScheduler.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Helpers
{
    using SubCTools.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Timers;

    /// <summary>
    /// A class for adding multiple <see cref="Action"/>s at different intervals.
    /// </summary>
    public class ActionScheduler
    {
        /// <summary>
        /// A <see cref="Dictionary{String, Timer}"/> that contains the timers that are running.
        /// </summary>
        private readonly Dictionary<string, Timer> timers = new Dictionary<string, Timer>();

        /// <summary>
        /// Adds a <see cref="Action"/> to the <see cref="ActionScheduler"/>
        /// If a <see cref="Action"/> with the same <see cref="id"/> already exists
        /// it will update the existing <see cref="Action"/> and <see cref="interval"/>.
        /// </summary>
        /// <param name="id">The name of the <see cref="Action"/>.</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to invoke the <see cref="Action"/>.</param>
        /// <param name="action">The <see cref="Action"/>.</param>
        /// <param name="executeFirst"> A <see cref="bool"/> representing whether or not to
        /// invoke the <see cref="Action"/> when it is added to the <see cref="ActionScheduler"/>.</param>
        /// <param name="loop">Whether or not to rerun the <see cref="Action"/> at the given
        /// <see cref="TimeSpan"/> interval.</param>
        public void Add(string id, TimeSpan interval, Action action, bool executeFirst = false, bool loop = true)
        {
            var timer = new Timer(interval.TotalMilliseconds)
            {
                AutoReset = loop,
            };
            timer.Elapsed += (s, e) =>
            {
                action.Invoke();
                if (timer.AutoReset == false)
                {
                    timers.Remove(id);
                }
            };

            timer.Start();
            timers.Update(id, timer);

            if (executeFirst)
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Adds a <see cref="Action"/> to the <see cref="ActionScheduler"/>
        /// If a <see cref="Action"/> with the same <see cref="id"/> already exists
        /// it will update the existing <see cref="Action"/> and <see cref="interval"/>.
        /// </summary>
        /// <param name="interval">The <see cref="TimeSpan"/> to invoke the <see cref="Action"/>.</param>
        /// <param name="action">The <see cref="Action"/>.</param>
        /// <param name="id">The id to store the action as.</param>
        public void AddOnce(TimeSpan interval, Action action, string id = null)
        {
            id = id ?? Guid.NewGuid().ToString();
            Add(id, interval, action, false, false);
        }

        /// <summary>
        /// Removes the <see cref="Action"/> and assosiated <see cref="Timer"/> with
        /// the given <see cref="id"/>.
        /// </summary>
        /// <param name="id">The name of the <see cref="Action"/>.</param>
        public void Remove(string id)
        {
            if (timers.TryGetValue(id, out var val))
            {
                val.Dispose();
                timers.Remove(id);
            }
        }
    }
}

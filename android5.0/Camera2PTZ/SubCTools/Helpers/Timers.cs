// <copyright file="Timers.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public static class Timers
    {
        /// <summary>
        /// Create a process timeout so it doesn't run indefinitely.
        /// </summary>
        /// <param name="tcs">The completion source to set when the timeout elapses.</param>
        /// <param name="span">How long the timeout should run.</param>
        /// <returns>Configured timer.</returns>
        public static System.Timers.Timer CreateTimeout<T>(TaskCompletionSource<T> tcs, TimeSpan span, T defaultValue = default(T))
        {
            // maximum length of time to wait for focus to converge
            var timeout = new System.Timers.Timer(span.TotalMilliseconds) { AutoReset = false };

            timeout.Elapsed += (s, e) =>
            {
                tcs.TrySetResult(defaultValue);
                tcs = new TaskCompletionSource<T>(defaultValue);
            };

            timeout.Start();

            return timeout;
        }

        /// <summary>
        /// Retry an action until the timeout elapses.
        /// </summary>
        /// <param name="action">The action to retry.</param>
        /// <param name="timeout">The amount of time to keep retrying before giving up.</param>
        public static void RetryWithTimeout(Action action, TimeSpan timeout)
        {
            var time = Stopwatch.StartNew();
            while (time.Elapsed < timeout)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException e)
                {
                    // access error
                    if (e.HResult != -2147024864)
                    {
                        throw;
                    }
                }
            }

            throw new TimeoutException("Failed perform action within allotted time.");
        }
    }
}
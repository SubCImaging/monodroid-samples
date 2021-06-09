//-----------------------------------------------------------------------
// <copyright file="PrecisionStopwatch.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools
{
    using System;

    /// <summary>
    /// Stopwatch that uses the system's date and time to measure time periods.
    /// </summary>
    public class PrecisionStopwatch
    {
        /// <summary>
        /// Compounding time that the watch is paused.
        /// </summary>
        private TimeSpan pauseDuration;

        /// <summary>
        /// Time that the watch is last paused.
        /// </summary>
        private DateTime? pauseTime;

        /// <summary>
        /// Time that the stopwatch starts.
        /// </summary>
        private DateTime? startTime;

        /// <summary>
        /// Gets total recording duration.
        /// </summary>
        public TimeSpan Duration => startTime == null ? TimeSpan.Zero : DateTime.Now.Subtract(startTime ?? DateTime.Now).Subtract(pauseDuration);

        /// <summary>
        /// Gets a value indicating whether the stop watch is started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the value of the TimerState to see whether it's started, stopped, or paused.
        /// </summary>
        public TimerStates State { get; private set; }

        /// <summary>
        /// Pause timer.
        /// </summary>
        public void Pause()
        {
            if (!IsStarted)
            {
                return;
            }

            State = TimerStates.Paused;
            pauseTime = DateTime.Now;
        }

        /// <summary>
        /// Start the stop watch at zero.
        /// </summary>
        public void Start()
        {
            Start(TimeSpan.Zero);
        }

        /// <summary>
        /// Stop timer.
        /// </summary>
        public void Stop()
        {
            IsStarted = false;
            State = TimerStates.Stopped;
            Reset();
        }

        /// <summary>
        /// Reset all the variables back to 0.
        /// </summary>
        private void Reset()
        {
            pauseTime = null;
            pauseDuration = TimeSpan.Zero;
        }

        /// <summary>
        /// Start the stopwatch with a specified offset.
        /// </summary>
        /// <param name="offset">Time which to offset the watch.</param>
        private void Start(TimeSpan offset)
        {
            IsStarted = true;
            State = TimerStates.Started;

            if (pauseTime != null)
            {
                // append the pause duration to know the cumlitive pause time
                pauseDuration += DateTime.Now.Subtract(pauseTime.Value);
                pauseTime = null;
            }
            else
            {
                Reset();
                startTime = DateTime.Now.Add(offset);
            }
        }
    }
}
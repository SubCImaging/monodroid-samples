//-----------------------------------------------------------------------
// <copyright file="PrecisionTimer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools
{
    using System;
    using System.Timers;

    /// <summary>
    /// Timer which ticks within a given interval to prevent drifting.
    /// </summary>
    public class PrecisionTimer
    {
        /// <summary>
        /// Timer to base the ticks from.
        /// </summary>
        private readonly Timer timer = new Timer();

        /// <summary>
        /// Stopwatch used to ensure that ticks happen when they're supposed to.
        /// </summary>
        private readonly PrecisionStopwatch stopwatch = new PrecisionStopwatch();

        /// <summary>
        /// The interval to tick at, your resolution.
        /// </summary>
        private readonly TimeSpan interval;

        /// <summary>
        /// The time since the last tick.
        /// </summary>
        private TimeSpan lastTick;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecisionTimer"/> class. With a resolution of 100 milliseconds.
        /// </summary>
        public PrecisionTimer()
            : this(TimeSpan.FromMilliseconds(100))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecisionTimer"/> class. With a defined resolution.
        /// </summary>
        /// <param name="resolution">The resolution of the timer, always be +- resolution.</param>
        public PrecisionTimer(TimeSpan resolution)
            : this(resolution, TimeSpan.FromSeconds(1))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecisionTimer"/> class. With a defined resolution and interval.
        /// </summary>
        /// <param name="resolution">The resolution of the timer, always be +- resolution.</param>
        /// <param name="interval">How often to fire the tick event.</param>
        public PrecisionTimer(TimeSpan resolution, TimeSpan interval)
        {
            this.interval = interval;
            timer.Interval = resolution.TotalMilliseconds;
            timer.Elapsed += Timer_Elapsed;
        }

        /// <summary>
        /// Event that fires every interval
        /// </summary>
        public event EventHandler Tick;

        /// <summary>
        /// Gets the value of how long the timer has been running.
        /// </summary>
        public TimeSpan Duration => stopwatch.Duration;

        /// <summary>
        /// Gets a value indicating whether gets the value whether the timer is started.
        /// </summary>
        public bool IsStarted => stopwatch.IsStarted;

        /// <summary>
        /// Gets a value indicating whether deprecated, <see cref="IsStarted"/>.
        /// </summary>
        public bool IsEnabled => timer.Enabled;

        /// <summary>
        /// Gets the value of the state of the timer.
        /// </summary>
        public TimerStates State => stopwatch.State;

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            timer.Start();
            stopwatch.Start();
            Tick?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            timer.Stop();
            stopwatch.Stop();
            lastTick = TimeSpan.Zero;
        }

        /// <summary>
        /// Pauses the timer.
        /// </summary>
        public void Pause()
        {
            timer.Stop();
            stopwatch.Pause();
        }

        /// <summary>
        /// Method called when the timer ticks.
        /// </summary>
        /// <param name="sender">Timer that executed the method.</param>
        /// <param name="e">Event args of the elapsed time.</param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var difference = Duration - lastTick;

            if (difference > interval)
            {
                lastTick = Duration;

                // TODO: Floor to the interval rather than a whole number
                // eg. if the interval is 500ms and we have 3.561 seconds, we can 3.5, not 3
                lastTick = TimeSpan.FromSeconds(Math.Floor(lastTick.TotalSeconds));

                Tick?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

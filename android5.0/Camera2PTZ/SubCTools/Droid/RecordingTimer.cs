// <copyright file="RecordingTimer.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid
{
    using System;
    using System.Timers;

    public class RecordingTimer
    {
        private readonly Timer tickTimer = new Timer(1000);
        private int completedSplits;
        private TimeSpan maxDuration;
        private int elapsedSeconds;

        public RecordingTimer(TimeSpan maxDuration)
        {
            this.maxDuration = maxDuration;
            tickTimer.Elapsed += (s, e) =>
            {
                elapsedSeconds++;
                Tick?.Invoke(s, e);

                if (TimeSpan.FromSeconds(elapsedSeconds) == maxDuration)
                {
                    tickTimer.Stop();

                    completedSplits++;
                    Split?.Invoke(s, e);
                    elapsedSeconds = 0;
                }
            };
        }

        public event EventHandler Tick;

        public event EventHandler Split;

        public TimeSpan RecordingDuration
        {
            get
            {
                var time = TimeSpan.FromTicks(maxDuration.Ticks * completedSplits) + TimeSpan.FromSeconds(elapsedSeconds);
                return time;
            }
        }

        public void Start()
        {
            tickTimer.Start();
        }

        public void Reset()
        {
            completedSplits = 0;
            tickTimer.Stop();
        }
    }
}
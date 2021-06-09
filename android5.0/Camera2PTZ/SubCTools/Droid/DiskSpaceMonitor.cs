//-----------------------------------------------------------------------
// <copyright file="DiskSpaceMonitor.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using SubCTools.Droid.Interfaces;
    using System;
    using System.Timers;

    /// <summary>
    /// A monitor to make sure that file sizes are growing when recording.
    /// </summary>
    public class DiskSpaceMonitor
    {
        /// <summary>
        /// Monitor for checking the file systems size.
        /// </summary>
        private readonly IDiskSpaceMonitor diskSpaceMonitor;

        /// <summary>
        /// Used to check on disk space.
        /// </summary>
        private readonly Timer timer = new Timer();

        /// <summary>
        /// Number of bytes remaining the last check.
        /// </summary>
        private double previousDiskBytes = 0;

        /// <summary>
        /// Number of warnings received since file growtn.
        /// </summary>
        private int warningCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskSpaceMonitor"/> class.
        /// </summary>
        /// <param name="diskSpaceMonitor">Monitor for checking the file systems size.</param>
        public DiskSpaceMonitor(IDiskSpaceMonitor diskSpaceMonitor)
            : this(diskSpaceMonitor, TimeSpan.FromSeconds(5))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskSpaceMonitor"/> class.
        /// </summary>
        /// <param name="diskSpaceMonitor">Monitor for checking the file systems size.</param>
        /// <param name="timerInterval">How often to check disk space.</param>
        public DiskSpaceMonitor(IDiskSpaceMonitor diskSpaceMonitor, TimeSpan timerInterval)
        {
            this.diskSpaceMonitor = diskSpaceMonitor;
            timer.Interval = timerInterval.TotalMilliseconds;
            timer.Elapsed += (s, e) => CheckRecordingFileStatusTimer_Elapsed();
        }

        /// <summary>
        /// Event to fire when the diskSpaceMonitor hasn't changed and you've received the max number of warnings.
        /// </summary>
        public event EventHandler Error;

        /// <summary>
        /// Event to fire when the disk space hasn't changed.
        /// </summary>
        public event EventHandler Warning;

        /// <summary>
        /// Gets or sets the maximum number of warnings to receive before firing the Error event.
        /// </summary>
        public int MaxWarnings { get; set; } = 1;

        /// <summary>
        /// Start monitoring the disk space.
        /// </summary>
        public void Start()
        {
            timer.Start();
        }

        /// <summary>
        /// Stop monitoring the disk space.
        /// </summary>
        public void Stop()
        {
            timer.Stop();
        }

        /// <summary>
        /// Check the disk space remaining.
        /// </summary>
        private void CheckRecordingFileStatusTimer_Elapsed()
        {
            // the disk space changed, reset and return
            if (previousDiskBytes != diskSpaceMonitor.GetDiskSpaceRemaining())
            {
                previousDiskBytes = diskSpaceMonitor.GetDiskSpaceRemaining();
                warningCount = 0;
                return;
            }

            // there was no change in the disk space
            warningCount++;

            if (warningCount == MaxWarnings)
            {
                Warning?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Error?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
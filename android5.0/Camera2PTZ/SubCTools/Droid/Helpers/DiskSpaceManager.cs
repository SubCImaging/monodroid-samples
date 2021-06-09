//-----------------------------------------------------------------------
// <copyright file="DiskSpaceManager.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Helpers
{
    using SubCTools.Attributes;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// Monitors the disk space used on the android system.
    /// </summary>
    public class DiskSpaceManager : INotifier, IDiskSpaceMonitor
    {
        /// <summary>
        /// The lower threshold before the <see cref="DiskSpaceManager"/> reports that the disk space is low.
        /// </summary>
        private const long LowSpaceThreshold = 2_000_000_000;

        /// <summary>
        /// Timer for reporting the disk space remaining at a regular interval.
        /// </summary>
        private readonly Timer spaceTimer = new Timer();

        private readonly IDiskSpaceMonitor diskSpaceMonitor;

        /// <summary>
        /// true if the disk space is below the <see cref="LowSpaceThreshold"/>.
        /// </summary>
        private bool isDiskSpaceLow;

        /// <summary>
        /// number of GB of disk space available.
        /// </summary>
        private double diskSpaceRemainingGB = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskSpaceManager"/> class.
        /// </summary>
        public DiskSpaceManager(IDiskSpaceMonitor diskSpaceMonitor)
        {
            spaceTimer.Interval = 30000;
            spaceTimer.Elapsed += (s, e) => NotifyDiskSpaceRemaining();
            spaceTimer.Start();
            this.diskSpaceMonitor = diskSpaceMonitor;
        }

        /// <summary>
        /// Low disk space event
        /// </summary>
        public event EventHandler LowDiskSpace;

        /// <summary>
        /// Notify event
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Gets a value indicating whether the disk space is low.
        /// </summary>
        [RemoteState(true)]
        public bool IsDiskSpaceLow
        {
            get => isDiskSpaceLow;

            private set
            {
                if (isDiskSpaceLow != value)
                {
                    isDiskSpaceLow = value;
                    OnNotify($"{nameof(IsDiskSpaceLow)}:{IsDiskSpaceLow}");
                }

                if (isDiskSpaceLow)
                {
                    LowDiskSpace?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the amount of disk space remaining in bytes.
        /// </summary>
        [RemoteState(true)]
        public double DiskSpaceRemaining
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the amount of disk space remaining in GB.
        /// </summary>
        [RemoteState]
        public double DiskSpaceRemainingGB => SubCTools.Helpers.Numbers.BytesToGB(DiskSpaceRemaining);

        /// <summary>
        /// <see cref="RemoteCommand"/> to call for the disk space remaining to be updated.
        /// </summary>
        [RemoteCommand]
        public void NotifyDiskSpaceRemaining()
        {
            NotifyDiskSpaceRemaining(false);
        }

        /// <summary>
        /// Querys the file system to determine the amount of free space, sets the property, and notifies subscribers.
        /// </summary>
        /// <param name="update">if true this method notifies the <see cref="DiskSpaceRemainingGB"/>.</param>
        [RemoteCommand]
        public void NotifyDiskSpaceRemaining(bool update)
        {
            Task.Run(() =>
            {
                var free = GetDiskSpaceRemaining();
                DiskSpaceRemaining = free - LowSpaceThreshold;
                if (DiskSpaceRemaining < 0)
                {
                    DiskSpaceRemaining = 0;
                }

                IsDiskSpaceLow = free < LowSpaceThreshold;

                if (diskSpaceRemainingGB != Math.Round(DiskSpaceRemainingGB, 1) || update)
                {
                    OnNotify($"{nameof(DiskSpaceRemainingGB)}:{DiskSpaceRemainingGB}");
                    diskSpaceRemainingGB = Math.Round(DiskSpaceRemainingGB, 1);
                }
            });
        }

        /// <summary>
        /// Raises the <see cref="Notify"/> event.
        /// </summary>
        /// <param name="message">the message to notify.</param>
        /// <param name="messageType">the message type.</param>
        private void OnNotify(string message, MessageTypes messageType = MessageTypes.Information)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));
        }

        /// <inheritdoc/>
        public double GetDiskSpaceRemaining()
        {
            return diskSpaceMonitor.GetDiskSpaceRemaining();
        }
    }
}
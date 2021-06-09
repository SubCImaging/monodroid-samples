//-----------------------------------------------------------------------
// <copyright file="DriveExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Extensions
{
    using SubCTools.Exceptions;
    using System;
    using System.IO;
    using System.Linq;

    public static class DriveExtensions
    {
        /// <summary>
        /// Returns true if the drive has more than the minimum percentage requested, false otherwise.
        /// </summary>
        /// <param name="drive">The drive to test.</param>
        /// <param name="minimumPercentage">The threshold to test for.</param>
        /// <returns>True if there is enough space left, false otherwise. </returns>
        /// <exception cref="MinimumPercentageNegativeException">Throws when the minimum percentage is negative.</exception>
        public static bool CanRecord(this DriveInfo drive, double minimumPercentage = 0.01)
        {
            if (minimumPercentage < 0)
            {
                throw new MinimumPercentageNegativeException(nameof(minimumPercentage), minimumPercentage, "The minimum percentage cannot be negative");
            }

            return drive.PercentageSpaceFree() > minimumPercentage;
        }

        /// <summary>
        /// Determines whether or not the specified drive exists.
        /// </summary>
        /// <param name="drive">The drive to test.</param>
        /// <returns>True if the drive exists, false otherwise.</returns>
        public static bool Exists(this DriveInfo drive)
        {
            return DriveInfo.GetDrives().Any(d => d.Name.ToLower() == (drive?.Name.ToLower() ?? string.Empty));
        }

        /// <summary>
        /// Returns the percentage space left as a decimal between 0 and 1.
        /// </summary>
        /// <param name="drive">The drive to test.</param>
        /// <returns>A decimal between 0 and 1 representing the available space remaining. </returns>
        /// <exception cref="DriveDoesNotExistException">Throws when the drive does not exist.</exception>
        public static double PercentageSpaceFree(this DriveInfo drive)
        {
            if (!drive.Exists())
            {
                throw new DriveDoesNotExistException($"Drive {drive.Name} does not exist");
            }

            return Math.Round((double)drive.TotalFreeSpace / drive.TotalSize, 2);
        }
    }
}
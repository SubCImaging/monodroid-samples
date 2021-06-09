// <copyright file="Drives.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.IO;

    public static class Drives
    {
        public static double DrivePercentRemaining(string drive)
        {
            return DrivePercentRemaining(new DriveInfo(drive));
        }

        public static double DrivePercentRemaining(DriveInfo drive)
        {
            return Math.Round((double)drive.TotalFreeSpace / drive.TotalSize, 2);
        }

        public static bool IsValidDrive(string drive)
        {
            return IsValidDrive(new DriveInfo(drive));
        }

        public static bool IsValidDrive(DriveInfo di)
        {

            return di.IsReady && di.Name != @"C:\" && di.DriveType != DriveType.CDRom && di.AvailableFreeSpace > 0; // ||


            // || (di.DriveType == DriveType.Fixed &&
            // di.DriveType != DriveType.Removable &&
            // di.DriveType != DriveType.Network) ||
        }
    }
}

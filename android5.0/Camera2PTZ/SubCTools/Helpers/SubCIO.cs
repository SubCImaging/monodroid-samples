// <copyright file="SubCIO.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class SubCIO : INotifier
    {
        private static readonly Lazy<SubCIO> instance = new Lazy<SubCIO>(() => new SubCIO());

        private SubCIO()
        {

        }

        /// <inheritdoc/>
        public event EventHandler<NotifyEventArgs> Notify;

        public static SubCIO Instance => instance.Value;

        private void OnNotify(string message, MessageTypes messageType)
        {
            if (Notify != null)
            {
                Notify(this, new NotifyEventArgs(message, messageType));
            }
        }

        /// <summary>
        /// Get all the files in the directory with the supplied search options.
        /// </summary>
        /// <param name="directory">Directory to search.</param>
        /// <param name="searchPattern">Patterns separated by a |. Ie. *.mpg|*.mp4.</param>
        /// <returns>A list of files that meet pattern.</returns>
        // public IList<FileInfo> FindFiles(string directory, string searchPattern = "")
        // {
        //    return FindFiles(new DirectoryInfo(path))
        // }



        /// <summary>
        /// Delete oldest files from the specified drive until the available free space is greater than or equal to the specified minimum space
        /// </summary>
        /// <param name="files">An unsorted list of files to delete from</param>
        /// <param name="drive">Drive to check available free space</param>
        /// <param name="minimumPercentage">Minimum amount of required space</param>
        /// <param name="isDescending">Is the list sorted in descending order</param>
        /// <returns>Whether the minimum space was achieved</returns>
        public bool DeleteOldFiles(IList<FileInfo> files, DriveInfo drive, double minimumPercentage, bool isDescending = false)
        {
            IList<FileInfo> sortedFiles;

            if (!isDescending)
            {
                OnNotify("Files are not sorted, sorting...", MessageTypes.Information);
                sortedFiles = files.OrderByDescending(a => a.LastWriteTime).ToList();
            }
            else
            {
                OnNotify("Files are already sorted", MessageTypes.Information);
                sortedFiles = files;
            }

            for (var i = sortedFiles.Count - 1; i >= 0; i--)
            {
                try
                {
                    OnNotify("Deleting " + sortedFiles[i].FullName, MessageTypes.Information);

                    try
                    {
                        File.Delete(sortedFiles[i].FullName);
                    }
                    catch
                    {
                        OnNotify("Could not delete file: " + sortedFiles[i].FullName + " It may be open, or no longer exist.", MessageTypes.Information);
                    }

                    sortedFiles.RemoveAt(i);

                    OnNotify("Available space: " + drive.AvailableFreeSpace + " vs. Minimum req: " + minimumPercentage, MessageTypes.Information);

                    // continue through the list until the drive has at least the minimum supplied space
                    if (SubCTools.Helpers.Drives.DrivePercentRemaining(drive) >= minimumPercentage)
                    {
                        OnNotify("We've got enough space, returning", MessageTypes.Information);
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        /// <summary>
        /// Delete the oldest files in the list until their combined size is equal to the amount to clear.
        /// </summary>
        /// <param name="files">List of files to delete from.</param>
        /// <param name="amountToClear">Amount of space to clear in GB.</param>
        /// <param name="isDescending">Whether the list is in descending order according to last write time.</param>
        /// <returns></returns>
        public bool DeleteOldestFiles(IList<FileInfo> files, long amountToClear, bool isDescending = false)
        {
            long amountDeleted = 0;

            // sort the files if they aren't already
            IList<FileInfo> sortedFiles;
            if (!isDescending)
            {
                OnNotify("Files are not sorted, sorting...", MessageTypes.Information);
                sortedFiles = files.OrderByDescending(a => a.LastWriteTime).ToList();
            }
            else
            {
                OnNotify("Files are already sorted", MessageTypes.Information);
                sortedFiles = files;
            }

            for (var i = sortedFiles.Count - 1; i >= 0; i--)
            {
                try
                {
                    OnNotify("Deleting " + sortedFiles[i].FullName, MessageTypes.Information);

                    try
                    {
                        amountDeleted += sortedFiles[i].Length;
                        File.Delete(sortedFiles[i].FullName);
                    }
                    catch
                    {
                        OnNotify("Could not delete file: " + sortedFiles[i].FullName + " It may be open, or no longer exist.", MessageTypes.Information);
                    }

                    sortedFiles.RemoveAt(i);

                    // if you've cleared enough space, return
                    // if (SubCTools.Helpers.Numbers.BytesToGB(amountDeleted) >= amountToClear)
                    if (amountDeleted >= amountToClear)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        // bool MinimumSpaceReached()
    }
}

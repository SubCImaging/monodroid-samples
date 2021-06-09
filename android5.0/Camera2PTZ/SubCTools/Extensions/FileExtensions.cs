//-----------------------------------------------------------------------
// <copyright file="FileExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------
namespace SubCTools.Extensions
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// An extension class for FileInfo.
    /// </summary>
    public static class FileExtensions
    {
        public static FileStream GetFileStream(this FileInfo file, TimeSpan timeout)
        {
            var timer = new Stopwatch();
            timer.Start();
            while (timer.Elapsed < timeout)
            {
                try
                {
                    return new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
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

            throw new TimeoutException($"Failed to get a write handle to {file.FullName} within {timeout.TotalMilliseconds}ms.");
        }

        public static bool IsFileWritable(this FileInfo file)
        {
            try
            {
                var fs = new FileStream(file.FullName, FileMode.Append, FileAccess.Write);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a filename to determine if the file exists already.  If so, and overwrite is false, a new filename with the next running index is returned.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> object to be parsed.</param>
        /// <param name="overwrite">True if you do not wish to get the next running indexed filename.</param>
        /// <returns>A new <see cref="FileInfo"/> with the resulting filename.</returns>
        public static FileInfo Parse(this FileInfo file, bool overwrite = false)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FullName);
            var fileExt = Path.GetExtension(file.FullName);

            var parsedDirectory = Helpers.FilesFolders.Parse(file.Directory);

            var parsedName = Helpers.FilesFolders.ParseFilename(fileName);

            return Helpers.FilesFolders.FileWithSeqNum(parsedDirectory.FullName, parsedName, fileExt, overwrite);
        }
    }
}
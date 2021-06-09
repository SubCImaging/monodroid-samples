// <copyright file="Zip.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Helper class for <see cref="System.IO.Compression"/>.
    /// </summary>
    public static class Zip
    {
        /// <summary>
        /// Safe extract method that extracts a <see cref="ZipFile"/> into a target directory <see cref="extractTo"/> and deletes the target directory first if you tell it to.
        /// </summary>
        /// <param name="zipFile">The zip file to extract.</param>
        /// <param name="extractTo">The target directory to extract to.</param>
        /// <param name="deleteIfExists">If true deletes <see cref="extractTo"/> before extracting.</param>
        /// <returns>True if the zip file was extracted successfully.</returns>
        public static bool UnzipFile(FileInfo zipFile, DirectoryInfo extractTo, bool deleteIfExists = false)
        {
            extractTo.Refresh();
            if (extractTo.Exists)
            {
                if (deleteIfExists)
                {
                    extractTo.Delete(true);
                }
                else
                {
                    return false;
                }
            }

            if (zipFile.Exists)
            {
                try
                {
                    var zip = ZipFile.OpenRead(zipFile.FullName);
                    extractTo.Create();
                    zip.ExtractToDirectory(extractTo.FullName);
                }
                catch (Exception)
                {
                    Console.WriteLine("There was a problem extracting the file");
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}

// <copyright file="DirectoryInfoExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Extensions
{
    using System;
    using System.IO;
    using static SubCTools.Helpers.Paths;

    public static class DirectoryInfoExtensions
    {
        private const int MaxLength = 100;

        public static DirectoryInfo RemoveIllegalPathCharacters(this DirectoryInfo dir, FileSystem fs, bool containsTags = false)
        {
            return new DirectoryInfo(dir.FullName.RemoveIllegalPathCharacters(fs, containsTags));
        }

        public static DirectoryInfo RemoveIllegalPathCharacters(this DirectoryInfo dir, bool containsTags = false)
        {
            return new DirectoryInfo(dir.FullName.RemoveIllegalPathCharacters(containsTags));
        }

        public static DirectoryInfo LimitDirectoryLength(this DirectoryInfo dir, int length = MaxLength)
        {
            var path = dir.FullName;
            path = path.Substring(0, Math.Min(length, path.Length));
            return new DirectoryInfo(path);
        }
    }
}

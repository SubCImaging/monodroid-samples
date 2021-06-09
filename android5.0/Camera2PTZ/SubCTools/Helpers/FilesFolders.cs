// <copyright file="FilesFolders.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class FilesFolders
    {
        /// <summary>
        /// Rename existing file with Backup prefix, create a new file with supplied name in it's place. If a backup file exists, delete it.
        /// </summary>
        /// <param name="fileName">FileName to create a backup of.</param>
        public static void CreateBackup(string fileName)
        {
            Create(fileName, "Backup");

            File.Create(fileName);
        }

        /// <summary>
        /// Rename existing file with Corrupt prefix, create a new file with supplied name in it's place. If a corrupt file exists, delete it.
        /// </summary>
        /// <param name="fileName">FileName to create a backup of.</param>
        public static void CreateCorrupt(string fileName)
        {
            Create(fileName, "Corrupt");
        }

        /// <summary>
        /// Returns a directory that is not full and outputs the seqNum of that directory.
        /// </summary>
        /// <param name="parsedDirectory">The parsed base directory name without sequence numbers.</param>
        /// <param name="seqNum">Output parameter to return the seqNum after it has been determined.</param>
        /// <param name="startingSeqNum">The seqNum to start at. (ie. if you've been taking pictures in /Pics_2 pass in '2' so that the software doesn't have to enumerate and count the files in /Pics and /Pics_1 every time!).</param>
        /// <param name="maxFiles">The maximum number of files you want there to be in the directory.  Negative value means unlimited.</param>
        /// <returns>The indexed directory that has been confirmed not full, thus good to use.</returns>
        public static DirectoryInfo DirectoryWithSeqNum(DirectoryInfo parsedDirectory, out int seqNum, int startingSeqNum = 0, int maxFiles = -1)
        {
            var baseDir = new DirectoryInfo(DirectoryNameWithSeqNum(parsedDirectory.FullName, startingSeqNum));
            seqNum = startingSeqNum;

            if (maxFiles < 0)
            {
            }
            else if (!Directory.Exists(baseDir.FullName))
            {
            }
            else
            {
                seqNum = startingSeqNum;

                if (maxFiles <= 0)
                {
                    return parsedDirectory;
                }

                while (Directory.Exists(DirectoryNameWithSeqNum(parsedDirectory.FullName, seqNum)))
                {
                    seqNum++;
                }

                if (!IsDirectoryFull(DirectoryNameWithSeqNum(parsedDirectory.FullName, seqNum - 1), maxFiles))
                {
                    seqNum--;
                }

                return new DirectoryInfo(DirectoryNameWithSeqNum(parsedDirectory.FullName, seqNum));

                // if (IsDirectoryFull(baseDir.FullName, maxFiles))
                // {
                //    var dirName = DirectoryNameWithSeqNum(baseDir.Parent.FullName, parsedDirectory, out seqNum, startingSeqNum, maxFiles);
                //    return new DirectoryInfo(Path.Combine(baseDir.Parent.FullName, dirName));
                // }
            }

            return baseDir;
        }

        /// <summary>
        /// Get a file with a running number if the file already exists. E.g. C:\Newfile.txt becomes C:\Newfile_1.txt.
        /// </summary>
        /// <param name="parsedFilename">Filename that has already been parsed.</param>
        /// <param name="parsedDirectory">Directory that has already been parsed.</param>
        /// <param name="fileExt">File extension.</param>
        /// <param name="overwrite">Overwrite the existing file.</param>
        /// <returns>New file info with the new path.</returns>
        public static FileInfo FileWithSeqNum(string parsedDirectory, string parsedFilename, string fileExt, bool overwrite = false)
        {
            if (!fileExt.StartsWith("."))
            {
                fileExt = "." + fileExt;
            }

            if (overwrite || !FileExists(parsedDirectory, parsedFilename, fileExt))
            {
                return new FileInfo(Path.Combine(parsedDirectory, parsedFilename + fileExt));
            }
            else
            {
                return new FileInfo(Path.Combine(parsedDirectory, FileNameWithSeqNum(parsedDirectory, parsedFilename, fileExt)));
            }
        }

        /// <summary>
        /// Get all the files in the directory with the supplied search options.
        /// </summary>
        /// <param name="directory">Directory to search.</param>
        /// <param name="searchPattern">Patterns separated by a |. Ie. *.mpg|*.mp4.</param>
        /// <returns>A list of files that meet pattern.</returns>
        public static IEnumerable<FileInfo> FindFiles(DirectoryInfo directory, string searchPattern = "")
        {
            var files = new List<FileInfo>();

            if (!directory.Exists)
            {
                return files;
            }

            // DirectoryInfo di = new DirectoryInfo(directory);
            if (searchPattern != string.Empty)
            {
                var searchPatterns = searchPattern.Split('|');

                foreach (var pattern in searchPatterns)
                {
                    files.AddRange(directory.GetFiles(pattern));
                }
            }
            else
            {
                files.AddRange(directory.GetFiles());
            }

            return files;
        }

        /// <summary>
        /// Get all the directories in the specified path.
        /// </summary>
        /// <param name="path">Starting path.</param>
        /// <returns>List of directories.</returns>
        public static IEnumerable<string> GetAllDirectories(string path)
        {
            var directories = new List<string>();

            foreach (var dir in Directory.GetDirectories(path))
            {
                try
                {
                    // add the directory
                    directories.Add(dir);

                    // add all the sub directories
                    directories.AddRange(GetAllDirectories(dir));
                }
                catch
                {
                    // ignore
                }
            }

            return directories;
        }

        /// <summary>
        /// Recursively search all the directories and sub directories for files.
        /// </summary>
        /// <param name="startingDirectory">Directory to start the search.</param>
        /// <param name="searchPattern">Patterns separated by a |. Ie. *.mpg|*.mp4.</param>
        /// <returns>List of files that match the search pattern.</returns>
        public static IEnumerable<FileInfo> GetAllFiles(DirectoryInfo startingDirectory, string searchPattern = "", bool isRecursive = true, string exclude = "")
        {
            var files = new List<FileInfo>();

            files.AddRange(FindFiles(startingDirectory, searchPattern));

            if (isRecursive)
            {
                foreach (var dir in startingDirectory.GetDirectories())
                {
                    try
                    {
                        files.AddRange(GetAllFiles(dir, searchPattern));
                    }
                    catch
                    {
                    }
                }
            }

            FileInfo fi;
            if (!string.IsNullOrWhiteSpace(exclude) && (fi = files.FirstOrDefault(f => f.FullName != exclude)) != null)
            {
                files.Remove(fi);
            }

            return files;
        }

        /// <summary>
        /// Calculates the total amount of disk space used in a directory.
        /// </summary>
        /// <param name="folder">the directory to check.</param>
        /// <param name="files">Output parameter, the list of files.</param>
        /// <returns></returns>
        public static long GetFolderSize(DirectoryInfo folder, out List<FileInfo> files)
        {
            long folderSize = 0;

            files = new List<FileInfo>();

            files.AddRange(FindFiles(folder));

            try
            {
                foreach (var file in files)
                {
                    folderSize += file.Length;
                }
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("Unable to calculate folder size: {0}", e.Message);
            }

            return folderSize;
        }

        public static string GetTag(string toReplace)
        {
            switch (toReplace)
            {
                case "${YEAR}":
                case "${Year}":
                case "${year}":
                case "${yyyy}":
                case "${YYYY}":
                    return DateTime.Now.Year.ToString("0000");

                case "${yy}":
                case "${YY}":
                    return DateTime.Now.Year.ToString("00").Substring(2);

                case "${mon}":
                case "${MM}":
                case "${month}":
                case "${Month}":
                case "${MONTH}":
                    return DateTime.Now.Month.ToString("00");

                case "${day}":
                case "${DAY}":
                case "${Day}":
                case "${dd}":
                case "${DD}":
                    return DateTime.Now.Day.ToString("00");

                case "${hr}":
                case "${hh}":
                case "${hour}":
                case "${Hour}":
                case "${HOUR}":
                    return DateTime.Now.Hour.ToString("00");

                case "${min}":
                case "${mm}":
                case "${minute}":
                case "${Minute}":
                case "${MINUTE}":
                    return DateTime.Now.Minute.ToString("00");

                case "${sec}":
                case "${Sec}":
                case "${SEC}":
                case "${ss}":
                case "${second}":
                case "${Second}":
                case "${SECOND}":
                    return DateTime.Now.Second.ToString("00");

                case "${ms}":
                case "${ff}":
                case "${MS}":
                    return DateTime.Now.Millisecond.ToString("00");

                case "${fff}":
                    return DateTime.Now.Millisecond.ToString("000");

                case "${ffff}":
                    return DateTime.Now.Millisecond.ToString("0000");

                default:
                    return toReplace;
            }
        }

        /// <summary>
        /// Get any invalid characters from the path, check to see if the drive exists.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>Valid result.</returns>
        [Obsolete("Use IsIllegalPath instead.  Once no occurrances of this method exist please delete it")]
        public static bool IsPathValid(string path, bool ContainsTags = false)
        {
            if (path == null || string.IsNullOrEmpty(path))
            {
                return false;
            }

            var isValid = path.IsLegalPath(Paths.ThisFileSystem(), ContainsTags);

            var drive = Path.GetPathRoot(path);
            var dr = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == drive);
            var foundDrive = dr != null;
            return isValid && foundDrive;
        }

        /// <summary>
        /// Check to see if a path it a valid rooted local path.
        /// </summary>
        /// <param name="pathString">Path to check.</param>
        /// <returns>True if path is valid, and is on a rooted local drive.</returns>
        public static bool IsPathValidRootedLocal(string pathString)
        {
            if (string.IsNullOrEmpty(pathString))
            {
                return false;
            }

            var isValidUri = Uri.TryCreate(pathString, UriKind.Absolute, out var pathUri);
            return isValidUri && pathUri != null && pathUri.IsLoopback;
        }

        /// <summary>
        /// Check the filename to make sure there are no illegal chars.
        /// </summary>
        /// <param name="filename">The file name to check.</param>
        /// <returns>Whether the filename is valid or not.</returns>
        [Obsolete("Use IsLegalFileOrFolderName instead.  Once no occurrances of this method exist please delete it")]
        public static bool IsValidFilename(string filename, bool ContainsTags = false)
        {
            return filename.IsLegalFileOrFolderName(Paths.ThisFileSystem(), ContainsTags);
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static string MakeRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
            {
                throw new ArgumentNullException(nameof(fromPath));
            }

            if (string.IsNullOrEmpty(toPath))
            {
                throw new ArgumentNullException(nameof(toPath));
            }

            Uri fromUri = new Uri(fromPath), toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            } // path can't be made relative.

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.ToUpperInvariant() == "FILE")
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        /// <summary>
        /// Replace tags in directory and remove any invalid characters.
        /// </summary>
        /// <param name="directory">Directory to parse.</param>
        /// <returns>Parsed directory.</returns>
        public static string Parse(this string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return string.Empty;
            }

            return directory.ReplaceTags().RemoveIllegalPathCharacters();
        }

        /// <summary>
        /// Replace tags in directory and remove any invalid characters.
        /// </summary>
        /// <param name="directory">Directory to parse.</param>
        /// <param name="tags">Tags to replace.</param>
        /// <returns>Parsed directory.</returns>
        public static string Parse(this string directory, Dictionary<string, string> tags)
        {
            return directory.ReplaceTags(tags).RemoveIllegalPathCharacters();
        }

        public static DirectoryInfo Parse(this DirectoryInfo directory)
        {
            return directory == null ? null : new DirectoryInfo(Parse(directory.FullName));
        }

        public static DirectoryInfo Parse(this DirectoryInfo directory, Dictionary<string, string> tags)
        {
            return directory == null ? null : new DirectoryInfo(Parse(directory.FullName, tags));
        }

        public static DirectoryInfo ParseDirectoryAddSeqNum(this DirectoryInfo dir, out int lastDirectoryIndex, int startingDirectoryIndex = 0, int maxFiles = -1)
        {
            return DirectoryWithSeqNum(dir.Parse(), out lastDirectoryIndex, startingDirectoryIndex, maxFiles);
        }

        /// <summary>
        /// Parses a file name and adds a sequence number if required.
        /// </summary>
        /// <param name="file">File to parse and add sequence number.</param>
        /// <returns>Parsed file with sequence number.</returns>
        public static FileInfo ParseFileAddSeqNum(this FileInfo file)
        {
            var f = file.ParseFileInfo();
            return FileWithSeqNum(f.Directory.FullName, Path.GetFileNameWithoutExtension(f.Name), f.Extension);
        }

        /// <summary>
        /// Parses a file name and adds a sequence number if required.
        /// </summary>
        /// <param name="file">File to parse and add sequence number.</param>
        /// <returns>Parsed file with sequence number.</returns>
        public static FileInfo ParseFileAddSeqNum(this FileInfo file, Dictionary<string, string> tags)
        {
            var f = file.ParseFileInfo(tags);
            return FileWithSeqNum(f.Directory.FullName, Path.GetFileNameWithoutExtension(f.Name), f.Extension);
        }

        public static FileInfo ParseFileInfo(this FileInfo file)
        {
            return new FileInfo(Path.Combine(file.Directory.Parse().FullName, file.Name.ParseFilename()));
        }

        public static FileInfo ParseFileInfo(this FileInfo file, Dictionary<string, string> tags)
        {
            return new FileInfo(Path.Combine(file.Directory.Parse(tags).FullName, file.Name.ParseFilename(tags)));
        }

        /// <summary>
        /// Replace all the tags and remove all the bad characters from the filename.
        /// </summary>
        /// <param name="filename">Name of file to parse.</param>
        /// <returns>Filename with tags inserted and bad characters removed.</returns>
        public static string ParseFilename(this string filename)
        {
            return filename.ReplaceTags().RemoveIllegalFileOrFolderCharacters();
        }

        /// <summary>
        /// Replace all the tags and remove all the bad characters from the filename.
        /// </summary>
        /// <param name="filename">Name of file to parse.</param>
        /// <param name="tags">Tags you also wish to replace.</param>
        /// <returns>Filename with tags inserted and bad characters removed.</returns>
        public static string ParseFilename(this string filename, Dictionary<string, string> tags)
        {
            return filename.ReplaceTags(tags).RemoveIllegalFileOrFolderCharacters();
        }

        /// <summary>
        /// Print the file.
        /// </summary>
        /// <param name="value">the file handle.</param>
        public static void Print(this FileInfo value)
        {
            if (!value.Exists)
            {
                throw new FileNotFoundException("File doesn't exist");
            }

            var p = new Process();
            p.StartInfo.FileName = value.FullName;
            p.StartInfo.Verb = "Print";
            p.Start();
        }

        [Obsolete("Use RemoveIllegalFileOrFolderCharacters instead.  Once no occurrances of this method exist please delete it")]
        public static string RemoveBadChars(this string fileName, bool ContainsTags = false)
        {
            return fileName.RemoveIllegalFileOrFolderCharacters(Paths.ThisFileSystem(), ContainsTags);
        }

        [Obsolete("Use RemoveIllegalPathCharacters instead.  Once no occurrances of this method exist please delete it")]
        public static DirectoryInfo RemoveBadChars(this DirectoryInfo directory, bool ContainsTags = false)
        {
            if (directory == null)
            {
                return null;
            }

            var dir = new StringBuilder(directory.FullName);

            return new DirectoryInfo(dir.ToString().RemoveIllegalPathCharacters(ContainsTags));
        }

        /// <summary>
        /// Remove invalid characters from a string.
        /// </summary>
        /// <param name="path">Path to remove characters from.</param>
        /// <param name="isDirectory">True if path is a directory, false if it's a file.</param>
        /// <returns>Clean path.</returns>
        public static string RemoveInvalidCharacters(this string path, bool isDirectory = false)
        {
            return isDirectory ? path.RemoveIllegalPathCharacters() : path.RemoveIllegalFileOrFolderCharacters();
        }

        [Obsolete("Use RemoveIllegalPathCharacters instead.  Once no occurrances of this method exist please delete it")]
        public static DirectoryInfo RemoveInvalidDirectoryCharacters(this DirectoryInfo dir, bool ContainsTags = false)
        {
            return new DirectoryInfo(dir.ToString().RemoveInvalidDirectoryCharacters(ContainsTags));
        }

        /// <summary>
        /// Remove all the invalid characters from a directory.
        /// </summary>
        /// <param name="directory">Directorty to remove characters from.</param>
        /// <returns>Clean directory.</returns>
        [Obsolete("Use RemoveIllegalPathCharacters instead.  Once no occurrances of this method exist please delete it")]
        public static string RemoveInvalidDirectoryCharacters(this string directory, bool ContainsTags = false)
        {
            var builder = new StringBuilder(directory);
            builder = builder.Replace("../", string.Empty).Replace(@"..\", string.Empty);

            return builder.ToString().RemoveIllegalPathCharacters(ContainsTags);
        }

        /// <summary>
        /// Remove all the invalid characters from the filename.
        /// </summary>
        /// <param name="filename">Filename to remove invalid characters from.</param>
        /// <returns>File with all invalid characters removed.</returns>
        [Obsolete("Use RemoveIllegalFileOrFolderCharacters instead.  Once no occurrances of this method exist please delete it")]
        public static string RemoveInvalidFilenameCharacters(this string filename, bool ContainsTags = false)
        {
            return filename.RemoveBadChars(ContainsTags);
        }

        /// <summary>
        /// Replace all the tags in a path. E.g. ${yyyy} => 2017.
        /// </summary>
        /// <param name="path">Path to replace tags in.</param>
        /// <returns>Path with all tags replaced.</returns>
        public static string ReplaceTags(this string path)
        {
            var matches = Regex.Matches(path ?? string.Empty, @"\${(\w+})");
            foreach (Match m in matches)
            {
                path = path.Replace(m.Value, GetTag(m.Value));
            }

            return path;
        }

        /// <summary>
        /// Replace all the standard tags, along with any additional ones supplied.
        /// </summary>
        /// <param name="path">Path to modify.</param>
        /// <param name="tags">Dictionary of tags to replace.</param>
        /// <returns>Path with all tags replaced with desired values.</returns>
        public static string ReplaceTags(this string path, Dictionary<string, string> tags)
        {
            path = path.ReplaceTags();

            foreach (var item in tags)
            {
                path = path.Replace(item.Key, item.Value);
            }

            return path;
        }

        /// <summary>
        /// Create a copy of a file with a prefix.  ie corrupt_settings.xml.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="prefix"></param>
        private static void Create(string fileName, string prefix)
        {
            var path = Path.GetDirectoryName(Path.GetFullPath(fileName));
            var sourceFile = Path.GetFullPath(fileName);

            var destFile = Path.Combine(path, prefix + " " + Path.GetFileName(fileName));
            File.Copy(sourceFile, destFile, true);
        }

        /// <summary>
        /// Check to see if the directory is full, if it is then increment and append the seqNum until a non-full directory is found.
        /// </summary>
        /// <param name="parentDirectory">The parent directory of the one you are indexing.</param>
        /// <param name="directory">The directory you are indexing.</param>
        /// <param name="seqNum">Output parameter to return the seqNum after it has been determined.</param>
        /// <param name="startingSeqNum">The seqNum to start at. (ie. if you've been taking pictures in /Pics_2 pass in '2' so that the software doesn't have to enumerate and count the files in /Pics and /Pics_1 every time!).</param>
        /// <param name="maxFiles">The maximum number of files you want there to be in the directory.  Negative value means unlimited.</param>
        /// <returns>The indexed directory name that has been confirmed not full, thus good to use.</returns>
        private static string DirectoryNameWithSeqNum(string directory, int seqNum)
        {
            return (seqNum <= 0) ? directory : directory + "_" + seqNum;
        }

        /// <summary>
        /// Returns true of the specified file exists.
        /// </summary>
        /// <param name="filename">The base filename.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="fileExt">The file extension.</param>
        /// <returns>true if the file exists, false otherwise.</returns>
        private static bool FileExists(string directory, string filename, string fileExt)
        {
            return File.Exists(Path.Combine(directory, filename + fileExt));
        }

        /// <summary>
        /// Check to see if any files exist with the same name, append an underscore and number if it does.
        /// </summary>
        /// <param name="filename">Filename to check if exists.</param>
        /// <param name="seqNum">A reference to a sequence number to increase.</param>
        /// <returns>Filename with appended sequence number.</returns>
        private static string FileNameWithSeqNum(string directory, string filename, string fileExt)
        {
            var seqNum = NextAvailableSeqNum(directory, filename, fileExt);

            if (seqNum <= 0)
            {
                return filename + fileExt;
            }
            else
            {
                return $"{filename}_{seqNum}{fileExt}";
            }
        }

        /// <summary>
        /// Finds the first available sequence number for a specified filename in a specified directory.
        /// </summary>
        /// <param name="filename">The base filename.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="fileExt">The file extension.</param>
        /// <returns>true if the file exists, false otherwise.</returns>
        private static int FirstAvailableSeqNum(string directory, string filename, string fileExt)
        {
            if (!Directory.Exists(directory))
            {
                return -1;
            }

            var regex = new Regex($@"^.*_(\d+)\{fileExt}$");
            return FirstMissingNumber(Directory.GetFiles(directory, filename + "_*" + fileExt, SearchOption.TopDirectoryOnly)
                .Where(f => regex.IsMatch(f))
                .Select(f => Convert.ToInt32(regex.Match(f).Groups[1].Value))
                .OrderBy(x => x)
                .ToArray());
        }

        /// <summary>
        /// Binary search algorithm for finding the first missing number (starting at 1) in a sorted array of integers.
        /// </summary>
        /// <param name="list">an array of integers.</param>
        /// <returns>The first missing number (starting at 1) from the array.</returns>
        private static int FirstMissingNumber(int[] list)
        {
            var indexMin = 0;
            var index = list.Count();
            var indexMax = index;

            while (index != indexMin)
            {
                if (list[index - 1] == index)
                {
                    indexMin = index;
                }
                else
                {
                    indexMax = index;
                }

                index = (int)Math.Floor((indexMax + indexMin) / 2d);
            }

            return index + 1;
        }

        /// <summary>
        /// Enumerates the files in a directory to determine if the number of files is >= the maximum number specified.
        /// </summary>
        /// <param name="directory">The directory to check.</param>
        /// <param name="maxFiles">The maximum number of files to allow.</param>
        /// <returns>True if the directory contains at least as many files as the maximum.</returns>
        private static bool IsDirectoryFull(string directory, int maxFiles)
        {
            if (maxFiles < 0 || !Directory.Exists(directory))
            {
                return false;
            }

            var e = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
            return e.Count() >= maxFiles;
        }

        /// <summary>
        /// Finds the next available sequence number for a specified filename in a specified directory using binary search algorithm.
        /// </summary>
        /// <param name="filename">The base filename.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="fileExt">The file extension.</param>
        /// <returns>true if the file exists, false otherwise.</returns>
        private static int NextAvailableSeqNum(string directory, string filename, string fileExt)
        {
            if (!Directory.Exists(directory))
            {
                return -1;
            }

            var seqNum = 1;
            var lbound = 0;
            var ubound = -1;

            // establish bounds
            while (ubound < lbound)
            {
                setBounds();
                seqNum *= 2;
            }

            while ((seqNum = (int)Math.Ceiling((lbound + ubound) / 2d)) != ubound)
            {
                setBounds();
            }

            return seqNum;

            /// <summary>
            /// local method to update the bounds based on existence of file
            /// </summary>
            void setBounds()
            {
                if (FileExists(directory, filename + "_" + seqNum, fileExt))
                {
                    lbound = seqNum;
                }
                else
                {
                    ubound = seqNum;
                }
            }
        }
    }
}
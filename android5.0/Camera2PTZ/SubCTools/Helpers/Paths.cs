//-----------------------------------------------------------------------
// <copyright file="Paths.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Helpers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// This class handles all path and filename illegal character checking for both Windows and Unix.
    /// Since Path.GetInvalidPathChars and Path.GetInvalidFileNameChars only work in windows we have to create lists of illegal characters manually so that this code functions identically on both systems.
    /// </summary>
    public static class Paths
    {
        /// <summary>
        /// The illegal keyboard characters that unix disallows in file and folder names.
        /// </summary>
        private const string IllegalFilenameCharsUnix = "/";

        /// <summary>
        /// The illegal keyboard characters that windows disallows in file and folder names.
        /// </summary>
        private const string IllegalFilenameCharsWindows = "\"<>|:*?/\\";

        /// <summary>
        /// The illegal keyboard characters that unix disallows in paths.
        /// </summary>
        private const string IllegalPathCharsUnix = "";

        /// <summary>
        /// The illegal keyboard characters that windows disallows in paths.
        /// </summary>
        private const string IllegalPathCharsWindows = "\"<>|/";

        /// <summary>
        /// The characters that, if present, requires the path to be encased in quotes.
        /// </summary>
        private const string QuotedCharacters = "|&;<>()$`\"'*?[#~=% \t\n";

        /// <summary>
        /// These are characters that we at SubC would just rather not worry about.
        /// </summary>
        private const string SubCRejectedChars = "&$?*{}"; // removed ~ because Coabis needed it

        /// <summary>
        /// A regular expression that matches our tags, such as ${yyyy}.
        /// </summary>
        private const string TagPattern = @"(\${(?:\w+)})"; // group[1] returns the tag-type characters

        /// <summary>
        /// The unix escape characters.  These characters can cause the OS to parse and expand text following them even if they are encased in quotes.  For that reason we disallow them entirely.
        /// </summary>
        private const string UnixEscapes = @"$`\";

        /// <summary>
        /// A regular expression that matches any legal unix path.
        /// </summary>
        private const string UnixPathPattern = @"^\/?([^$`/\\]+\/)*[^$`/\\]+\/?$";

        /// <summary>
        /// A regular expression that matches any string of legal characters in a windows file path.
        /// </summary>
        private const string WinLegalPattern = "[^\\\"<>|:*?/\\\\]";

        /// <summary>
        /// The special control characters represented by ASCII and unicode characters 0-31.
        /// </summary>
        private static readonly List<char> CtrlChars = System.Linq.Enumerable.Range(0, 31).Select(c => (char)c).ToList();

        /// <summary>
        /// A regular expression that matches any legal windows path.
        /// </summary>
        private static readonly string WindowsPathPattern = $@"^(\\\\\?\\)?(([a-zA-Z]:|\.|\.\.|)\\)?({WinLegalPattern}+\\)*{WinLegalPattern}+$";

        /// <summary>
        /// an enum that specifies a file system or group of file systems to perform operations based upon.
        /// </summary>
        public enum FileSystem
        {
            /// <summary>
            /// perform operations based upon compliance to both file systems
            /// </summary>
            Any,

            /// <summary>
            /// perform operations based upon compliance to the windows file system
            /// </summary>
            Windows,

            /// <summary>
            /// perform operations based upon compliance to the unix file system
            /// </summary>
            Unix,
        }

        /// <summary>
        /// Checks whether a string qualifies as a legal file or folder name for the specified file system.
        /// </summary>
        /// <param name="name">the string to check.</param>
        /// <param name="fs">the file system to check for.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>true if the string is a legal file or folder name for the specified file system.</returns>
        public static bool IsLegalFileOrFolderName(this string name, FileSystem fs = FileSystem.Any, bool containsTags = false)
        {
            if (containsTags)
            {
                name = name.ReplaceTags();
            }

            var illegal = new List<char>(CtrlChars)
            {
                SubCRejectedChars,
            };
            if (fs == FileSystem.Any || fs == FileSystem.Windows)
            {
                illegal.Add(IllegalFilenameCharsWindows);
            }

            if (fs == FileSystem.Any || fs == FileSystem.Unix)
            {
                illegal.Add(IllegalFilenameCharsUnix + UnixEscapes);
            }

            var isLegal = !name.Contains(illegal);

            ////if (isLegal && name.Contains(QuotedCharacters))
            ////{
            ////    // Give a warning that the string must be quoted
            ////}

            return isLegal;
        }

        /// <summary>
        /// This method checks a string for file path legality.
        /// </summary>
        /// <param name="name">the string to check.</param>
        /// <param name="fs">the file system to check for.  Supplying FileSystem.All will result in an exception.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>true if the path string is a legal path.</returns>
        public static bool IsLegalPath(this string name, FileSystem fs, bool containsTags = false)
        {
            if (fs == FileSystem.Any)
            {
                fs = ThisFileSystem();
            }

            if (containsTags)
            {
                name = name.ReplaceTags();
            }

            // Create a list of the illegal characters to check for
            string pathPattern;
            var illegal = new List<char>(CtrlChars)
            {
                SubCRejectedChars,
            };

            // TODO: if FileSystem.All check for windows then convert to unix and check for that too.
            if (fs == FileSystem.Windows)
            {
                illegal.Add(IllegalPathCharsWindows);
                pathPattern = WindowsPathPattern;
            }
            else
            {
                illegal.Add(IllegalPathCharsUnix + UnixEscapes);
                pathPattern = UnixPathPattern;
            }

            var isLegal = !name.Contains(illegal);

            // ensure the string conforms the the appropriate format for the file system
            if (isLegal)
            {
                var match = Regex.Match(name, pathPattern);
                isLegal = match.Success;
            }

            ////if (isLegal && name.Contains(QuotedCharacters))
            ////{
            ////    // Give a warning that the string must be quoted
            ////}

            return isLegal;
        }

        /// <summary>
        /// Checks whether the given path string contains characters that would cause the path to require surrounding quotes in order for it to properly parse.
        /// </summary>
        /// <param name="name">The path string to convert.</param>
        /// <param name="fs">the file system to check for.  Supplying FileSystem.All will result in an exception.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>A boolean specifying whether or not the quotes are required.</returns>
        public static bool IsQuotesRequired(this string name, FileSystem fs, bool containsTags = false)
        {
            if (containsTags)
            {
                name = name.ReplaceTags();
            }

            var result = name.Contains<char>(QuotedCharacters);
            if (!result && (fs == FileSystem.Any || fs == FileSystem.Windows))
            {
                result = name.Contains('/');
            }

            if (!result && (fs == FileSystem.Any || fs == FileSystem.Unix))
            {
                result = name.Contains('\\');
            }

            return result;
        }

        /// <summary>
        /// Removes illegal characters from a filename or foldername string, determine own file system.
        /// </summary>
        /// <param name="name">the string to check.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>The modified name.</returns>
        public static string RemoveIllegalFileOrFolderCharacters(this string name, bool containsTags = false)
        {
            return name.RemoveIllegalFileOrFolderCharacters(ThisFileSystem(), containsTags);
        }

        /// <summary>
        /// Removes illegal characters from a filename or foldername string, specific file system.
        /// </summary>
        /// <param name="name">the string to check.</param>
        /// <param name="fs">the file system to check for.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>The modified name.</returns>
        public static string RemoveIllegalFileOrFolderCharacters(this string name, FileSystem fs, bool containsTags = false)
        {
            if (!IsLegalFileOrFolderName(name, fs, containsTags))
            {
                var illegal = new List<char>(CtrlChars)
                {
                    SubCRejectedChars,
                };

                if (fs == FileSystem.Any || fs == FileSystem.Windows)
                {
                    illegal.Add(IllegalFilenameCharsWindows);
                }

                if (fs == FileSystem.Any || fs == FileSystem.Unix)
                {
                    illegal.Add(IllegalFilenameCharsUnix + UnixEscapes);
                }

                name = name.RemoveChars(illegal, containsTags);
            }

            return name;
        }

        /// <summary>
        /// Removes illegal characters from a path string, determine own file system.
        /// </summary>
        /// <param name="name">the string to check.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>The modified path string.</returns>
        public static string RemoveIllegalPathCharacters(this string name, bool containsTags = false)
        {
            return name.RemoveIllegalPathCharacters(ThisFileSystem(), containsTags);
        }

        /// <summary>
        /// Removes illegal characters from a path string, specific file system.
        /// </summary>
        /// <param name="name">the string to check.</param>
        /// <param name="fs">the file system to check for.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>The modified path string.</returns>
        public static string RemoveIllegalPathCharacters(this string name, FileSystem fs, bool containsTags = false)
        {
            if (!name.IsLegalPath(fs, containsTags))
            {
                var illegal = new List<char>(CtrlChars)
                {
                    SubCRejectedChars,
                };

                if (fs == FileSystem.Unix)
                {
                    illegal.Add(IllegalPathCharsUnix + UnixEscapes);
                }
                else
                {
                    illegal.Add(IllegalPathCharsWindows);
                }

                name = name.RemoveChars(illegal, containsTags);
            }

            return name;
        }

        /// <summary>
        /// Method that detects which operating system this application is running on.
        /// </summary>
        /// <returns>the file system enum of the current OS.</returns>
        public static FileSystem ThisFileSystem()
        {
            return (Path.DirectorySeparatorChar == '\\') ? Paths.FileSystem.Windows : Paths.FileSystem.Unix;
        }

        /// <summary>
        /// Tries to convert the given path string to a unix path.
        /// </summary>
        /// <param name="name">The path string to convert.</param>
        /// <param name="path">The converted path.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>A unix path compliant string.</returns>
        public static bool TryConvertToUnixPath(this string name, out string path, bool containsTags = false)
        {
            if (name.IsLegalPath(FileSystem.Unix, containsTags))
            {
                path = name;
                return true;
            }
            else if (name.IsLegalPath(FileSystem.Windows, containsTags))
            {
                foreach (Match item in Regex.Match(name, @"^(\\\\\?\\)").Captures)
                {
                    name = name.Replace(item.ToString(), string.Empty);
                }

                foreach (Match item in Regex.Match(name, @"^[a-zA-Z]:\\").Captures)
                {
                    name = name.Replace(item.ToString(), string.Empty);
                }

                path = name.Replace('\\', '/');
                return true;
            }
            else
            {
                path = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Tries to convert the given path string to a windows path.
        /// </summary>
        /// <param name="name">The path string to convert.</param>
        /// <param name="path">The converted path.</param>
        /// <param name="containsTags">set to true if the string contains SubC tags and you want them to pass.</param>
        /// <returns>A windows path compliant string.</returns>
        public static bool TryConvertToWindowsPath(this string name, out string path, bool containsTags = false)
        {
            if (name.IsLegalPath(FileSystem.Windows, containsTags))
            {
                path = name;
                return true;
            }
            else if (name.IsLegalPath(FileSystem.Unix, containsTags))
            {
                path = name.Replace('/', '\\');
                return true;
            }
            else
            {
                path = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Removes the specified characters from a string.
        /// </summary>
        /// <param name="s">the string to operate on.</param>
        /// <param name="charsToRemove">the characters to remove.</param>
        /// <returns>the string with the characters removed.</returns>
        private static string RemoveChars(this string s, List<char> charsToRemove)
        {
            foreach (var c in charsToRemove)
            {
                foreach (Match item in Regex.Matches(s, $@"\{c.ToString()}"))
                {
                    if (item.ToString() != string.Empty)
                    {
                        s = s.Replace(item.ToString(), string.Empty);
                    }
                }
            }

            return s;
        }

        /// <summary>
        /// Removes the specified characters from the parts of a string not matching a tag.
        /// </summary>
        /// <param name="s">the string to operate on.</param>
        /// <param name="charsToRemove">the characters to remove.</param>
        /// <param name="containsTags">true if the string contains tags that must be ignored.</param>
        /// <returns>the string with the characters removed.</returns>
        private static string RemoveChars(this string s, List<char> charsToRemove, bool containsTags)
        {
            if (containsTags)
            {
                var splits = Regex.Split(s, TagPattern);
                for (var index = 0; index < splits.Length; index++)
                {
                    if (Regex.Matches(splits[index], TagPattern).Count == 0)
                    {
                        splits[index] = splits[index].RemoveChars(charsToRemove);
                    }
                }

                s = string.Join(string.Empty, splits);
            }
            else
            {
                s = s.RemoveChars(charsToRemove);
            }

            return s;
        }
    }
}
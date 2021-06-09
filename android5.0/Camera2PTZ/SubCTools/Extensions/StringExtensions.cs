//-----------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------
namespace SubCTools.Extensions
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using SubCTools.Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Extensions for the string class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Check to see if string is a valid JSON object.
        /// </summary>
        /// <param name="strInput">Input to check.</param>
        /// <returns>True if string is JSON.</returns>
        public static bool IsValidJson(this string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) // For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    // Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) // some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Parses the string as a path and returns the name of the last directory found in that path <see cref="str"/>.
        /// </summary>
        /// <param name="str">a path string.</param>
        /// <returns>the name of the last directory in <see cref="str"/>.</returns>
        public static string ParseDirectory(this string str)
        {
            var ret = string.Empty;
            try
            {
                ret = new FileInfo(str).Directory.Name;
            }
            catch (Exception e) when (
                e is ArgumentNullException ||
                e is ArgumentException ||
                e is PathTooLongException ||
                e is NotSupportedException)
            {
            }

            return ret;
        }

        /// <summary>
        /// Removes characters that are illegal in a path on the current file system from the string.
        /// </summary>
        /// <param name="str">the string.</param>
        /// <returns>A string that is clear of illegal characters.  Note that this still may not be a valid path format.</returns>
        [Obsolete("Use RemoveIllegalPathCharacters instead.  Once no occurrances of this method exist please delete it")]
        public static string RemoveIllegalDirectoryChars(this string str)
        {
            return str.RemoveIllegalPathCharacters(Paths.ThisFileSystem());
        }

        ////public static string RemoveNonTransmitable(this string s)
        ////{
        ////    var pattern = @"[^a-zA-Z0-9_\- .]";
        ////    var groupPattern = @"\${\w+}";
        ////    var tagMa
        ////}

        /// <summary>
        /// Calls replace only on a specific part of the string specified by the rangeStart and rangeLength parameters.
        /// </summary>
        /// <param name="input">the string.</param>
        /// <param name="oldValue">the pattern to match.</param>
        /// <param name="newValue">the string litteral to replace pattern with.</param>
        /// <param name="rangeStart">the start of the section to search.  If negative this will count back from the end of the string.</param>
        /// <param name="rangeLength">the length of the section to search.  If ommitted this will search from rangeStart to the end of the string.</param>
        /// <returns></returns>
        public static string ReplaceInRange(this string input, string oldValue, string newValue, int rangeStart, int rangeLength = 0)
        {
            if (input.Length == 0)
            {
                return string.Empty;
            }

            rangeStart = (input.Length + rangeStart) % input.Length;
            if (rangeStart < 0)
            {
                rangeStart = 0;
            }

            if (rangeStart + rangeLength > input.Length || rangeLength == 0)
            {
                rangeLength = input.Length - rangeStart;
            }

            return input.Substring(0, rangeStart) +
                input.Substring(rangeStart, rangeLength).Replace(oldValue, newValue) +
                input.Substring(rangeStart + rangeLength);
        }

        /// <summary>
        /// Takes a string and divides it up into chunks of the specified <see cref="maxChunkSize"/>.
        /// </summary>
        /// <param name="str">The string to chunk.</param>
        /// <param name="maxChunkSize">The max size of each chunk.</param>
        /// <returns>IEnumerable of chunks.</returns>
        public static IEnumerable<string> ChunksUpto(this string str, int maxChunkSize)
        {
            for (var i = 0; i < str.Length; i += maxChunkSize)
            {
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
            }
        }

        /// <summary>
        /// Overload for split that allows passage of <see cref="separator"/>s as a string rather than character array.
        /// </summary>
        /// <param name="splitee">the string.</param>
        /// <param name="separator">the separators.</param>
        /// <param name="options">string split options.</param>
        /// <returns>an IEnumerable of strings, the results of the split.</returns>
        public static string[] Split(this string splitee, string separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return splitee.Split(separator.ToCharArray(), options);
        }

        /// <summary>
        /// Splits the string using a func to specify delimiters.
        /// </summary>
        /// <param name="str">the string.</param>
        /// <param name="controller">the func.</param>
        /// <returns>an IEnumerable of strings, the results of the split.</returns>
        public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
        {
            var nextPiece = 0;
            for (var c = 0; c < str.Length; c++)
            {
                if (!controller(str[c]))
                {
                    continue;
                }

                yield return str.Substring(nextPiece, c - nextPiece);
                nextPiece = c + 1;
            }

            yield return str.Substring(nextPiece);
        }

        /// <summary>
        /// Trims all whitespace as well as the specified characters.
        /// </summary>
        /// <param name="input">the string to trim.</param>
        /// <param name="c">the character to trim.</param>
        /// <returns></returns>
        public static string TrimAnd(this string input, char[] c)
        {
            var temp = string.Empty;
            while (input != temp)
            {
                temp = input;
                input = input.Trim().Trim(c);
            }

            return input;
        }

        /// <summary>
        /// Trims all whitespace as well as the specified character.
        /// </summary>
        /// <param name="input">the string to trim.</param>
        /// <param name="c">the character to trim.</param>
        /// <returns></returns>
        public static string TrimAnd(this string input, char c)
        {
            return input.TrimAnd(new char[] { c });
        }

        /// <summary>
        /// Trims one layer of parentheses off both ends of the string if they exist and match the <see cref="quote"/> character.
        /// </summary>
        /// <param name="input">the string.</param>
        /// <param name="quote">the character to trim.</param>
        /// <returns>the trimmed string.  If the parenthesis are absent from either end string is unchanged.</returns>
        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) &&
                (input[0] == quote) &&
                (input[input.Length - 1] == quote))
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }

        /// <summary>
        /// Tries to parse a filename out of a path string.
        /// </summary>
        /// <param name="filepath">a string containing a path.</param>
        /// <param name="filename">the filename within the <see cref="filepath"/>.</param>
        /// <returns>true if successfull.</returns>
        public static bool TryParseFilename(this string filepath, out string filename)
        {
            filename = filepath;

            FileInfo fi;
            try
            {
                fi = new FileInfo(filepath);
            }
            catch
            {
                return false;
            }

            filename = fi.Name;
            return true;
        }

        public static bool TryParseIPAddress(this string ipString, out IPAddress address)
        {
            var regex = new Regex(@"^(.+):\d+$");
            var match = regex.Match(ipString);

            if (match.Success)
            {
                return IPAddress.TryParse(match.Groups[1].Value, out address);
            }
            else
            {
                return IPAddress.TryParse(ipString, out address);
            }
        }
    }
}
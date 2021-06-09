// <copyright file="FTP.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    public static class FTP
    {
        internal const string Password = "C}U?V0m(j~mF";
        internal const string Username = "software@ftp.subcimaging.com";

        /// <summary>
        /// Get the highest versioned file or folder at the given directory.
        /// </summary>
        /// <param name="versionPattern">Pattern used to match the versions.</param>
        /// <param name="url">FTP Directory to look in.</param>
        /// <returns>A string with the latest name, and version, default (string, Version) if nothing is found.</returns>
        public static (string latestName, Version version) GetHighestVersion(string versionPattern, Uri url)
        {
            return GetHighestVersion(Username, Password, versionPattern, url);
        }

        /// <summary>
        /// Get the highest versioned file or folder at the given directory.
        /// </summary>
        /// <param name="username">Username to log in to FTP.</param>
        /// <param name="password">Password to log in to FTP.</param>
        /// <param name="versionPattern">Pattern used to match the versions.</param>
        /// <param name="url">FTP Directory to look in.</param>
        /// <returns>A string with the latest name, and version, default (string, Version) if nothing is found.</returns>
        public static (string latestName, Version version) GetHighestVersion(string username, string password, string versionPattern, Uri url)
        {
            return (from path in GetWebDirectoryContents(url, username, password)
                    let match = Regex.Match(path, versionPattern)
                    where match.Success
                    let version = new Version(match.Groups.Count > 1 ? match.Groups[1].Value : match.Value)
                    select (path, version)).OrderByDescending(f => f.version).FirstOrDefault();
        }

        /// <summary>
        /// Append the given file location to the ftp address.
        /// </summary>
        /// <param name="location">Location of the file inside the Rayfin folder.</param>
        /// <returns>FTP address prepended to the location.</returns>
        public static string GetRayfinFTPString(string location)
        {
            return $@"ftp://ftp.subcimaging.com/Rayfin/{location}";
        }

        /// <summary>
        /// Append the given file location to the ftp address.
        /// </summary>
        /// <param name="location">Location of the file inside the Rayfin folder.</param>
        /// <returns>FTP address prepended to the location.</returns>
        public static Uri GetRayfinFTPUri(string location)
        {
            return new Uri(GetRayfinFTPString(location));
        }

        /// <summary>
        /// Append the given file location to the ftp address.
        /// </summary>
        /// <returns>FTP address prepended to the location.</returns>
        public static Uri GetRayfinFTPUri()
        {
            return GetRayfinFTPUri(string.Empty);
        }

        /// <summary>
        /// Get a list of the web directories files and folders.
        /// </summary>
        /// <param name="url">Url where to search.</param>
        /// <returns>List of directory contents.</returns>
        public static IEnumerable<string> GetWebDirectoryContents(Uri url)
        {
            return GetWebDirectoryContents(url, Username, Password);
        }

        /// <summary>
        /// Get a list of the web directories files and folders.
        /// </summary>
        /// <param name="url">Url where to search.</param>
        /// <param name="username">Uesrname to log in.</param>
        /// <param name="password">Password to log in.</param>
        /// <returns>List of directory contents.</returns>
        public static IEnumerable<string> GetWebDirectoryContents(Uri url, string username, string password)
        {
            if (url == null)
            {
                throw new ArgumentNullException("Url must not be null");
            }

            var ftpRequest = (FtpWebRequest)WebRequest.Create(url);
            ftpRequest.Credentials = new NetworkCredential(username, password);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;

            var directories = new List<string>();

            try
            {
                using (var response = (FtpWebResponse)ftpRequest.GetResponse())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var line = streamReader.ReadLine();
                        while (!string.IsNullOrEmpty(line))
                        {
                            directories.Add(line);
                            line = streamReader.ReadLine();
                        }

                        streamReader.Close();
                    }
                }
            }
            catch (WebException)
            {
                Console.WriteLine("Could not connect to FTP");
            }

            return directories;
        }
    }
}
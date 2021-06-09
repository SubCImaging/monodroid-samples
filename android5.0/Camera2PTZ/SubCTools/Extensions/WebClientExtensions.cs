//-----------------------------------------------------------------------
// <copyright file="WebClientExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Extensions
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    public static class WebClientExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="client"></param>
        /// <param name="address"></param>
        /// <param name="filename"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task DownloadAsync(this WebClient client, Uri address, string filename)
        {
            await DownloadAsync(client, address, new FileInfo(filename));
        }

        /// <summary>
        /// Download a file from the address.
        /// </summary>
        /// <param name="client">Client to use for the download.</param>
        /// <param name="address">Address to download from.</param>
        /// <param name="filename">Local file to save to.</param>
        /// <returns>Empty task.</returns>
        public static async Task DownloadAsync(this WebClient client, Uri address, FileInfo filename)
        {
            var tcs = new TaskCompletionSource<bool>();

            var handler = new System.ComponentModel.AsyncCompletedEventHandler((s, ev) =>
            {
                tcs.SetResult(true);
            });

            client.DownloadFileCompleted += handler;

            if (!filename.Directory.Exists)
            {
                filename.Directory.Create();
            }

            client.DownloadFileAsync(address, filename.FullName);

            await tcs.Task;

            client.DownloadFileCompleted -= handler;
        }

        /// <summary>
        /// Download a file from FTP.
        /// </summary>
        /// <param name="downloadPath">Path of file on FTP.</param>
        /// <param name="output">Where to output the file.</param>
        /// <param name="progressChangedHandler">What to do with the progress as it updates.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task DownloadFileAsync(
            string downloadPath,
            FileInfo output,
            Action<double> progressChangedHandler = null)
        {
            await DownloadFile(Helpers.FTP.Username, Helpers.FTP.Password, downloadPath, output, progressChangedHandler);
        }

        /// <summary>
        /// Download a file from FTP.
        /// </summary>
        /// <param name="username">Username to log in.</param>
        /// <param name="password">Password for login.</param>
        /// <param name="downloadPath">Path of file on FTP.</param>
        /// <param name="output">Where to output the file.</param>
        /// <param name="progressChangedHandler">What to do with the progress as it updates.</param>
        public static async Task DownloadFile(
            string username,
            string password,
            string downloadPath,
            FileInfo output,
            Action<double> progressChangedHandler = null)
        {
            using (var client = new WebClient())
            {
                client.Credentials = new NetworkCredential(username, password);

                double fileSize;

                // get the file size
                var request = (FtpWebRequest)WebRequest.Create(downloadPath);
                request.Credentials = client.Credentials;
                request.Method = WebRequestMethods.Ftp.GetFileSize;

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    fileSize = response.ContentLength;
                    response.Close();
                }

                if (progressChangedHandler != null)
                {
                    client.DownloadProgressChanged += (s, e) => progressChangedHandler(e.BytesReceived / fileSize * 100);
                }

                await client.DownloadAsync(new Uri(downloadPath), output.FullName);
            }
        }
    }
}
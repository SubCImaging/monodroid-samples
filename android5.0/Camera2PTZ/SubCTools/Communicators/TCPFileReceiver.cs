// <copyright file="TCPFileReceiver.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators
{
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.IO;
    using System.Text;

    public struct TCPData
    {
        public byte[] Buffer { get; set; }

        public int BytesRead { get; set; }
    }

    public class TCPFileReceiver : INotifier
    {
        private readonly SubCTCPServer server;
        private long fileLength, bytesReceived = 0;
        private string fileExtension = "jpg";
        private string path;
        private string filename = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="TCPFileReceiver"/> class.
        /// </summary>
        /// <param name="port"></param>
        public TCPFileReceiver(int port)
        {
            server = new SubCTCPServer(port);

            // server.TCPDataReceived += DataReceived;
        }

        /// <inheritdoc/>
        public event EventHandler<NotifyEventArgs> Notify;

        public event EventHandler<string> FileNameChanged;

        public string Filename
        {
            get => filename;
            set
            {
                if (filename == value)
                {
                    return;
                }

                filename = value;
                FileNameChanged?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether save the incoming file with it's name.
        /// </summary>
        public bool UseIncomingFilename { get; set; } = true;

        public DirectoryInfo Directory { get; set; }

        private void DataReceived(object sender, TCPData data)
        {
            var buffer = data.Buffer;
            var bytesRead = data.BytesRead;

            if (IsFileHeader(buffer) || bytesRead <= 0)
            {
                return;
            }

            bytesReceived += bytesRead;

            var writer = !File.Exists(path) ? new BinaryWriter(File.Open(path, FileMode.Create)) : new BinaryWriter(File.Open(path, FileMode.Append));

            writer.Write(buffer, 0, bytesRead);
            writer.Flush();
            writer.Close();

            var progress = (int)Math.Floor((double)bytesReceived / fileLength * 100);
            OnNotify($"Progress:{progress}");

            // Console.WriteLine("Progress: " + progress);
            if (bytesReceived == fileLength)
            {
                OnNotify($"File transfer: {path} complete");
            }

        }

        /// <summary>
        /// Check the received data to see if it is a file header.
        /// </summary>
        /// <param name="bytes">Data to parse.</param>
        /// <returns>True is the data sent is a file header.</returns>
        private bool IsFileHeader(byte[] bytes)
        {
            var rxText = Encoding.ASCII.GetString(bytes).TrimEnd('\0');

            // it's a file header if it starts with StartTransfer
            if (!rxText.StartsWith("StartTransfer"))
            {
                return false;
            }

            Console.WriteLine(rxText);

            // split the information apart
            var split = rxText.Split(':');

            // get the file length if you can
            if (split.Length > 1)
            {
                try
                {
                    fileLength = Convert.ToInt64(split[1]);
                }
                catch
                {
                    // file length was not a proper number
                    OnNotify("File length is incorrect format");
                }
            }

            var parentDirectory = string.Empty;

            // get the file extension
            if (split.Length > 2)
            {
                var filename = split[2];
                var ext = Path.GetExtension(filename);

                if (fileExtension != null && fileExtension != ext)
                {
                    fileExtension = ext;
                }

                if (UseIncomingFilename)
                {
                    parentDirectory = Path.GetDirectoryName(filename) + @"\";
                    Filename = Path.GetFileNameWithoutExtension(filename);// Filename.TrimEnd(ext);//Path.GetFileNameWithoutExtension(split[2]);
                }
            }

            // get the file name
            // if (split.Length > 3
            //    && UseIncomingFilename)
            // {
            //    Filename = split[3];
            // }

            // create the path variable
            path = Helpers.FilesFolders.FileWithSeqNum(Directory + "\\" + parentDirectory, Helpers.FilesFolders.ParseFilename(Filename), fileExtension).FullName;

            var d = Path.GetDirectoryName(path);

            if (!System.IO.Directory.Exists(d))
            {
                System.IO.Directory.CreateDirectory(d);
            }

            // reset the amount of bytes received
            bytesReceived = 0;

            OnNotify("Transfer started");
            return true;
        }

        private void OnNotify(string message, MessageTypes messageType = MessageTypes.Information)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));
        }
    }
}

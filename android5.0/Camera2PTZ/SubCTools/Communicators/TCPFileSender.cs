// <copyright file="TCPFileSender.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Communicators
{
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    public class PackedFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackedFile"/> class.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="parentDirectory"></param>
        public PackedFile(FileInfo file, string parentDirectory = "")
        {
            File = file;
            ParentDirectory = parentDirectory;
        }

        public FileInfo File { get; }

        public string ParentDirectory { get; }
    }

    public class TCPFileSender : INotifier
    {
        public static int BufferSize = 1024 * 1024 * 5;

        private readonly TcpClient client = new TcpClient();
        private readonly ConcurrentQueue<PackedFile> transferQueue = new ConcurrentQueue<PackedFile>();
        private bool isConnected;

        private bool isTransferring = false;

        public event EventHandler<FileInfo> FileSent;

        public event EventHandler<bool> IsConnectedChanged;

        /// <inheritdoc/>
        public event EventHandler<NotifyEventArgs> Notify;

        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (isConnected == value)
                {
                    return;
                }

                isConnected = value;
                IsConnectedChanged?.Invoke(this, value);
            }
        }

        public bool Connect(EthernetAddress address)
        {
            // try
            // {
            //    client.Connect(address.Address, address.Port);
            //    IsConnected = true;
            // }
            // catch
            // {
            //    // could not connect
            //    IsConnected = false;
            // }
            var result = client.BeginConnect(address.Address, address.Port, null, null);
            IsConnected = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

            // client.EndConnect(null);
            return IsConnected;
        }

        public void Disconnect()
        {
            client.Client.Disconnect(false);
            IsConnected = false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="file"></param>
        /// <param name="parentPath"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<bool> SendFileAsync(FileInfo file, string parentPath = "")
        {
            if (!IsConnected)
            {
                return false;
            }

            transferQueue.Enqueue(new PackedFile(file, parentPath));

            TransferQueue();

            return true;

            // return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="files"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<bool> SendFilesAsync(IEnumerable<PackedFile> files)// FileInfo[] files, string parentPath = "") => await Task.Run(() =>
        {
            foreach (var item in files)
            {
                transferQueue.Enqueue(item);
            }

            // for (int i = 0; i < files.Length; i++)
            // {
            // OnNotify($"Transmitting file {i + 1}/{files.Length}: {files[i].Name}");
            // if (!await SendFileAsync(files[i], parentPath))
            // {
            //    return false;
            // }

            // await Task.Delay(250);
            // }
            TransferQueue();

            return true;
        }// );

        private void OnNotify(string message)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, MessageTypes.Information));
        }

        private async Task<bool> SendFileAsync(string fileName)
        {
            return await Task.Run(async () =>
{
    var readBytes = 0;

    var buffer = new byte[BufferSize];

    using (var stream = new FileStream(fileName, FileMode.Open))
    {
        do
        {
            stream.Flush();
            readBytes = stream.Read(buffer, 0, BufferSize);

            try
            {
                await WriteAsync(buffer, readBytes);
            }
            catch (Exception e)
            {
                // send failed
                Console.WriteLine(e);
                return false;
            }
        }
        while (readBytes > 0);
    }

    return true;
});
        }

        private async Task<bool> SendFileInfoAsync(FileInfo file, string parentPath = "")
        {
            var fileLength = file.Length;

            var fileName = !string.IsNullOrEmpty(parentPath) ? SubCTools.Helpers.FilesFolders.MakeRelativePath(parentPath, file.FullName) : file.Name;

            var fileInfo = $"StartTransfer:{fileLength}:{fileName}";

            try
            {
                await WriteAsync(Encoding.ASCII.GetBytes(fileInfo), fileInfo.Length);
            }
            catch (Exception e)
            {
                // send failed
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        private async Task TransferQueue()
        {
            await Task.Run(async () =>
{
    if (isTransferring)
    {
        return;
    }

    while (transferQueue.Count > 0)
    {
        isTransferring = true;

        // keep trying to dequeue
        if (!transferQueue.TryDequeue(out var file))
        {
            await Task.Delay(10);
            continue;
        }

        // OnNotify($"Transmitting file {i + 1}/{files.Length}: {files[i].Name}");
        var parentPath = file.ParentDirectory;
        var f = file.File;
        if (await SendFileInfoAsync(f, parentPath))
        {
            if (await SendFileAsync(f.FullName))
            {
                FileSent?.Invoke(this, f);
                OnNotify($"{f.Name} is finished transmitting");
            }
        }

        await Task.Delay(250);
    }

    isTransferring = false;
});
        }

        private async Task WriteAsync(byte[] data, int length)
        {
            await client.GetStream().WriteAsync(data, 0, length);
        }
    }
}
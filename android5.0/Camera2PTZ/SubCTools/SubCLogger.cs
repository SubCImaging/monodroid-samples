// <copyright file="SubCLogger.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools
{
    using SubCTools.Interfaces;
    using System;
    using System.IO;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A singleton class that records text data to files.
    /// </summary>
    public class SubCLogger : ILogger, IDisposable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "Property overrides")]
        private static readonly Lazy<SubCLogger> instance = new Lazy<SubCLogger>(() => new SubCLogger());

        private readonly object sync = new object();
        private string fileName = string.Empty;
        private StreamWriter streamWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCLogger"/> class.
        /// Private constructor, do not call and access the <see cref="Instance"/> property instead.
        /// </summary>
        private SubCLogger()
        {
        }

        /// <summary>
        /// Gets the created instance of this singleton class.
        /// </summary>
        public static SubCLogger Instance => instance.Value;

        /// <summary>
        /// Gets or sets the path to the file to be written.
        /// </summary>
        public string Filename
        {
            get => fileName;
            set => fileName = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets a value which indicates if the file should be kept open after writing or not.
        /// </summary>
        public bool KeepOpen { get; set; }

        /// <summary>
        /// Closes the current file.
        /// </summary>
        public void Close()
        {
            try
            {
                lock (sync)
                {
                    streamWriter?.Close();
                    streamWriter = null;
                }
            }
            catch (EncoderFallbackException)
            {
                Console.WriteLine("The current encoding does not support displaying half of a Unicode surrogate pair");
            }
        }

        /// <summary>
        /// Close resources for garbage collection.
        /// </summary>
        public void Dispose()
        {
            if (streamWriter != null)
            {
                streamWriter.Close();
                streamWriter.Dispose();
            }
        }

        /// <summary>
        /// Writes data to a file synchronously.
        /// </summary>
        /// <param name="data">The data to write.</param>
        public void Log(string data)
        {
            Write(data);
        }

        /// <summary>
        /// Writes data to a file asynchronously, calls <see cref="Log(string)"/>.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <returns>Task.</returns>
        public async Task LogAsync(string data)
        {
            await Task.Run(() => Log(data));
        }

        /// <summary>
        /// Writes data to a file asynchronously, calls <see cref="Log(string)"/>.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="file">File to write data.</param>
        /// <returns>Task.</returns>
        public async Task LogAsync(string data, FileInfo file)
        {
            await Task.Run(() => Write(data, file.Name, file.DirectoryName));
        }

        /// <summary>
        /// Write text to a file, make sure the file is not already open.
        /// </summary>
        /// <param name="text">Text to write to the file.</param>
        /// <param name="filename">File to write the text.</param>
        /// <param name="directory">Working directory that contains the filename.</param>
        /// <returns>True if the text was written, false otherwise.</returns>
        public bool Write(string text, string filename = "log.csv", string directory = "./log/")
        {
            lock (sync)
            {
                try
                {
                    if (filename is null)
                    {
                        filename = "log.csv";
                    }

                    // if you haven't specified a MyFilename or a filename set a default
                    if (Filename == string.Empty && filename == "log.csv")
                    {
                        filename = $"log_{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}.csv";
                    }
                    else if (Filename != string.Empty && filename == "log.csv")
                    {
                        // you've specified a MyFilename and no filename
                        Filename += Filename.EndsWith(".csv") ? string.Empty : ".csv";
                        filename = Filename;
                        directory = string.Empty;
                    }

                    // otherwise you've specified a filename
                    if (directory != string.Empty)
                    {
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                    }

                    try
                    {
                        filename = Path.Combine(directory, filename);
                    }
                    catch
                    {
                        throw new Exception("Invalid filename");
                    }

                    var fi = new FileInfo(filename);

                    // if the file exists and its locked, return false
                    if (File.Exists(filename)
                        && !KeepOpen
                        && !CanAccessFile(fi))
                    {
                        return false;
                    }

                    // create the file if it doesn't exist, otherwise append text to it
                    if (!File.Exists(filename))
                    {
                        streamWriter = new StreamWriter(filename);
                    }
                    else
                    {
                        if (streamWriter == null)
                        {
                            streamWriter = File.AppendText(filename);
                        }
                    }

                    streamWriter.WriteLine(text);

                    if (!KeepOpen)
                    {
                        streamWriter.Close();
                        streamWriter = null;
                    }

                    return true;
                }

                // A lot of things can go wrong here, but only catch explicitly what we expect,
                // never catch(Exception)! If it's unexpected let another thing handle it
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Cannot Log, Access to file is Denied");
                    return false;
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Cannot Log, Path to file is null");
                    return false;
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Cannot Log, Path to file is empty or invalid");
                    return false;
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine("Cannot Log, Path to file is of invalid format");
                    return false;
                }
                catch (PathTooLongException)
                {
                    Console.WriteLine("Cannot Log, Path to file exceeds the 248 character limit");
                    return false;
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine("Cannot Log, The specified directory does not exist");
                    return false;
                }
                catch (IOException)
                {
                    Console.WriteLine("Cannot Log, Path includes invalid syntax");
                    return false;
                }
                catch (SecurityException)
                {
                    Console.WriteLine("Cannot Log, Access to the file is denied");
                    return false;
                }
            }
        }

        /// <summary>
        /// Wrapper method for <see cref="Dispose"/>.
        /// </summary>
        /// <param name="dispose">Do nothing if false.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                Dispose();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Returns true if the file is not locked and is available for accessing, false otherwise.
        /// </summary>
        /// <param name="file"><see cref="FileInfo"/> to check access.</param>
        /// <returns>False if file cannot be accessed, True otherwise.</returns>
        private bool CanAccessFile(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException e)
            {
                Console.WriteLine(e);

                // the file is unavailable because it is:
                // still being written to
                // or being processed by another thread
                // or does not exist (has already been processed)
                return false;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            // file is not locked
            return true;
        }
    }
}
////-----------------------------------------------------------------------
//// <copyright file="RayfinShell.cs" company="SubC Imaging Ltd">
////     Copyright (c) SubC Imaging. All rights reserved.
//// </copyright>
//// <author>Aaron Watson</author>
////-----------------------------------------------------------------------

//namespace SubCTools.Droid
//{
//    using Java.IO;
//    using Java.Lang;
//    using System;
//    using System.IO;
//    using System.Text;
//    using System.Threading;
//    using System.Threading.Tasks;

//    /// <summary>
//    /// Class that handles all shell calls in the Rayfin.
//    /// </summary>
//    public static class RayfinShell
//    {
//        /// <summary>
//        /// The max period of time that a parallel shell call can take before timing out.
//        /// </summary>
//        private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(5);

//        /// <summary>
//        /// The maximum number of cycles to let a synchronous command run for before timing out.
//        /// </summary>
//        private static readonly int MaxWaitCycles = (int)MaxWait.TotalMilliseconds / 5;

//        /// <summary>
//        /// The lock object used to retain order of the asynchronous shell calls.
//        /// </summary>
//        private static readonly object ShellAsyncObject = new object();

//        /// <summary>
//        /// The lock object for all the synchronous shell calls.
//        /// </summary>
//        private static readonly object ShellSyncObject = new object();

//        /// <summary>
//        /// The <see cref="Process"/> that holds the interactive shell for synchronous commands.
//        /// </summary>
//        private static Process process;

//        /// <summary>
//        /// The error <see cref="Stream"/>
//        /// </summary>
//        private static Stream errorStream;

//        /// <summary>
//        /// The output <see cref="Stream"/>
//        /// </summary>
//        private static Stream inputStream;

//        /// <summary>
//        /// The input <see cref="Stream"/>
//        /// </summary>
//        private static Stream outputStream;

//        /// <summary>
//        /// Initializes static members of the <see cref="RayfinShell"/> class.
//        /// </summary>
//        static RayfinShell()
//        {
//            process = Runtime.GetRuntime().Exec("su");
//            var instance = Guid.NewGuid();
//            SubCLogger.Instance.Write($"{DateTime.Now}:  Application instance {instance}", "NewShell.log", @"/storage/emulated/0/Logs/");
//            SubCLogger.Instance.Write($"{DateTime.Now}:  Application instance {instance}", "NewShellTotal.log", @"/storage/emulated/0/Logs/");
//        }

//        /// <summary>
//        /// Makes a synchronous shell call and returns the output, will time out after <see cref="MaxWait"/> has been reached.
//        /// </summary>
//        /// <param name="command">The command to execute.</param>
//        /// <param name="appendStdErr">Whether or not you want to append stderr stream to the output.</param>
//        /// <returns>The output <see cref="Stream"/></returns>
//        public static string ShellSync(string command, bool appendStdErr = false)
//        {
//            lock (ShellSyncObject)
//            {
//                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

//                // Linking streams
//                inputStream = process.OutputStream;
//                errorStream = process.ErrorStream;
//                outputStream = process.InputStream;

//                var time = 0L;

//                var bytes = Encoding.UTF8.GetBytes(command + '\n');
//                inputStream.Write(bytes, 0, bytes.Length);
//                inputStream.Flush();

//                var bufferedReader = new BufferedReader(new InputStreamReader(outputStream));

//                var stdOutLog = new System.Text.StringBuilder();
//                var stdErrLog = new System.Text.StringBuilder();
//                var line = string.Empty;

//                var timeSpentWaiting = 0;

//                // If the buffered reader isn't ready sleep for  
//                // 5ms and check again, timeout if it takes longer 
//                // than MaxWaitCycles
//                while (!bufferedReader.Ready() && timeSpentWaiting < MaxWaitCycles)
//                {
//                    System.Threading.Thread.Sleep(5);
//                    timeSpentWaiting += 5;
//                }

//                if (timeSpentWaiting >= MaxWaitCycles)
//                {
//                    // As long as there is data available get it from the buffered reader
//                    while (bufferedReader.Ready() && (line = bufferedReader.ReadLine()) != null)
//                    {
//                        stdOutLog.Append(line);
//                    }

//                    // If it received nothing from the buffered reader log the error and restart the shell, it's probably hanging.
//                    if (!(stdOutLog.ToString() == string.Empty))
//                    {
//                        SubCLogger.Instance.Write($"{DateTime.Now}:  Command {command} timed out", "Error.log", @"/storage/emulated/0/Logs/");

//                        process.Destroy();
//                        process = Runtime.GetRuntime().Exec("su");

//                        return string.Empty;
//                    }

//                    time = stopwatch.ElapsedMilliseconds;

//                    // If the call took more than 100ms log the warning that it should be moved off the main thread.
//                    if (time > 100)
//                    {
//                        SubCLogger.Instance.Write($"{DateTime.Now}:  Command {command} took {time}ms, it should be moved off the main thread", "Warning.log", @"/storage/emulated/0/Logs/");
//                    }

//                    return stdOutLog.ToString();
//                }

//                while (bufferedReader.Ready() && (line = bufferedReader.ReadLine()) != null)
//                {
//                    stdOutLog.Append(line);
//                }

//                time = stopwatch.ElapsedMilliseconds;

//                // If the call took more than 100ms log the warning that it should be moved off the main thread.
//                if (time > 100)
//                {
//                    SubCLogger.Instance.Write($"{DateTime.Now}:  Command {command} took {time}ms, it should be moved off the main thread", "Warning.log", @"/storage/emulated/0/Logs/");
//                }

//                return stdOutLog.ToString();
//            }
//        }

//        /// <summary>
//        /// Runs a shell command asynchronously on a seperate thread.
//        /// Retains order of all commands passed in so they will be executed one at a time in order.
//        /// </summary>
//        /// <param name="command">The command to execute.</param>
//        //public static void ShellAsync(string command) => Task.Run(() =>
//        //{
//        //    lock (ShellAsyncObject)
//        //    {
//        //        Runtime.GetRuntime().Exec(new[] { "su", "-c", command });
//        //    }
//        //});

//        /// <summary>
//        /// Runs a parallel synchronous shell command.
//        /// Do not run on the main thread, this will cause a <see cref="System.Exception"/> to be thrown.
//        /// </summary>
//        /// <param name="command">The command to execute.</param>
//        /// <param name="bypassThreadCheck">Bypasses the check for the main thread, only safe for initialization logic.</param>
//        /// <returns>The stdout <see cref="Stream"/></returns>
//        public static string ShellSync(string command, bool bypassThreadCheck = false)
//        {
//            if (!bypassThreadCheck && Android.OS.Looper.MyLooper() == Android.OS.Looper.MainLooper)
//            {
//                SubCLogger.Instance.Write($"{DateTime.Now}:  Parallel command {command} attempted to run on main thread.", "Error.log", @"/storage/emulated/0/Logs/");
//                throw new System.Exception($"RayfinShell:  Attempted to call {command} on main thread.");
//            }

//            using (var backgroundProcess = Runtime.GetRuntime().Exec("su"))
//            {
//                var input = backgroundProcess.OutputStream;
//                var error = backgroundProcess.ErrorStream;
//                var output = backgroundProcess.InputStream;

//                var bytes = Encoding.UTF8.GetBytes(command + '\n');
//                input.Write(bytes, 0, bytes.Length);
//                input.Flush();
//                bytes = Encoding.UTF8.GetBytes("exit\n");
//                input.Write(bytes, 0, bytes.Length);
//                input.Flush();
//                input.Close();

//                var bufferedReader = new BufferedReader(new InputStreamReader(output));

//                var stdOutLog = new System.Text.StringBuilder();
//                var stdErrLog = new System.Text.StringBuilder();
//                var line = string.Empty;

//                var timer = new System.Timers.Timer();

//                timer.Interval = MaxWait.TotalMilliseconds;
//                timer.Elapsed += (s, e) =>
//                {
//                    backgroundProcess.Destroy();
//                    SubCLogger.Instance.Write($"{DateTime.Now}:  Parallel command {command} timed out.", "Error.log", @"/storage/emulated/0/Logs/");
//                };
//                timer.Start();
//                backgroundProcess.WaitFor();
//                timer.Stop();

//                // As long as there is data available get it from the buffered reader
//                while (bufferedReader.Ready() && (line = bufferedReader.ReadLine()) != null)
//                {
//                    stdOutLog.Append(line);
//                }

//                return stdOutLog.ToString();

//            }
//        }
//    }
//}
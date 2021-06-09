// <copyright file="SystemCommandHelper.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System.Diagnostics;

    public static class SystemCommandHelper
    {
        public static void ResetProcessInfo(ProcessStartInfo cmdProcessInfo)
        {
            cmdProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmdProcessInfo.FileName = "cmd.exe";
            cmdProcessInfo.RedirectStandardOutput = true;
            cmdProcessInfo.RedirectStandardInput = true;
            cmdProcessInfo.UseShellExecute = false;
            cmdProcessInfo.CreateNoWindow = true;
            cmdProcessInfo.Verb = "runas";
        }

        public static void CloseAllProcesses(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                processes[0].CloseMainWindow();
            }
        }

        /// <summary>
        /// Kills all processes matching the given <see cref="processName"/> (except this one).
        /// </summary>
        /// <param name="processName">The process name to kill.</param>
        /// <param name="matchExact">If true, kills only processes that match the name exactly.  Otherwise kill processes that contain the name.</param>
        /// <exception cref="System.ComponentModel.Win32Exception">Throws if the process cannot be killed.</exception>
        public static void KillAllProcesses(string processName, bool matchExact = true)
        {
            processName = processName.ToLower();

            foreach (var process in Process.GetProcesses())
            {
                if (((matchExact && process.ProcessName.ToLower().Equals(processName)) || (!matchExact && process.ProcessName.ToLower().Contains(processName))) &&
                    process.Id != Process.GetCurrentProcess().Id)
                {
                    process.Kill();
                }
            }
        }

        public static void RunProcess(string args, ref ProcessStartInfo info)
        {
            info.Arguments = args;
            var process = Process.Start(info);
            process.WaitForExit();
        }
    }
}

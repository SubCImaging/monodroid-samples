namespace SubCTools.Droid.Extensions
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class DirectoryInfoExtensions
    {
        public static void CreateIfMissing(this DirectoryInfo directory)
        {
            if (directory.Exists)
            {
                return;
            }

            var baseDirectory = directory.FullName.Contains(DroidSystem.BaseDirectory)
                ? DroidSystem.BaseDirectory
                : directory.FullName.Contains(DroidSystem.InternalDirectory)
                    ? DroidSystem.InternalDirectory
                    : directory.FullName.Contains(DroidSystem.SwapDirectory) ? DroidSystem.SwapDirectory : string.Empty;

            if (string.IsNullOrEmpty(baseDirectory))
            {
                throw new ArgumentException($"{directory} must have either DroidSystem.BaseDirectory or DroidSystem.InternalDirectory as it's root");
            }

            directory.CreateIfMissing(new DirectoryInfo(baseDirectory));
        }

        /// <summary>
        /// Create the requested directory and all parent directories
        /// </summary>
        /// <param name="directory"></param>
        /// <exception cref="Java.Lang.Exception">Could not generate directory</exception>
        public static void CreateIfMissing(this DirectoryInfo directory, DirectoryInfo baseDirectory)
        {
            if (directory.Exists)
            {
                return;
            }

            try
            {
                DroidSystem.ShellSync($"mkdir -p \"{directory}\"");
                SetFullPermissions(directory, baseDirectory);
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
                System.Diagnostics.Debug.WriteLine($"Could not generate directory {directory}");
            }
        }

        /// <summary>
        /// Sets the permissions for a directory with a given permission number.
        /// </summary>
        /// <param name="directory">this</param>
        /// <param name="permissionString">Permission number</param>
        public static void SetFullPermissions(this DirectoryInfo directory, DirectoryInfo baseDirectory)
        {
            while (directory.Parent.FullName != baseDirectory.FullName)
            {
                directory = directory.Parent;
            }

            try
            {
                DroidSystem.ShellSync($"chmod -R 777 \"{directory}\"");
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
                System.Diagnostics.Debug.WriteLine($"Could not set permissions on {directory}/n{e.Message}");
            }
        }
    }
}
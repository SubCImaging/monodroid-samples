// <copyright file="Memory.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Diagnostics;

    public class Memory
    {
        /// <summary>
        /// Get the amount of free memory left on the system.
        /// </summary>
        /// <returns>Amount of free memory in bytes.</returns>
        public static long GetFreeMemory()
        {
            return Process.GetCurrentProcess().PrivateMemorySize64 / 1024;
        }

        public static void ReleaseComObject(params object[] objs)
        {
            foreach (var o in objs)
            {
                if (o != null)
                {
                    try
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(o);
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (NullReferenceException)
                    {
                    }
                }
            }
        }
    }
}

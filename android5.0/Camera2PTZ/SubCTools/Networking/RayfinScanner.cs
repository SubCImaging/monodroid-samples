// <copyright file="RayfinScanner.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public static class RayfinScanner
    {
        private static List<string> BaseIPs;
        private static readonly int StartIP = 1;
        private static readonly int StopIP = 255;
        private static string ip;

        private static readonly int Timeout = 100;
        private static int nFound = 0;
        private static List<IPAddress> rayfins = new List<IPAddress>();
        private static readonly object LockObj = new object();

        static RayfinScanner()
        {
            BaseIPs = GetLocalBaseAddress();
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<List<IPAddress>> GetRayfins()
        {
            rayfins = new List<IPAddress>();
            await Task.Run(() => RunPingSweep());
            return rayfins;
        }

        private static void RunPingSweep()
        {
            BaseIPs = GetLocalBaseAddress();
            var ma = new ManualResetEvent(true);
            var stopWatch = new Stopwatch();
            TimeSpan ts;

            ma.Reset();
            nFound = 0;

            var tasks = new List<Task>();

            stopWatch.Start();

            for (var i = StartIP; i <= StopIP; i++)
            {
                foreach (var baseIP in BaseIPs)
                {
                    ip = baseIP + i.ToString();

                    var p = new Ping();
                    var task = PingAndUpdateAsync(p, ip);
                    tasks.Add(task);
                }
            }

            Task.WhenAll(tasks).ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    stopWatch.Stop();
                    ts = stopWatch.Elapsed;

                    // Console.WriteLine(nFound.ToString() + " Rayfins found! Elapsed time: " + ts.ToString(), "Asynchronous");
                    ma.Set();
                }
                else
                {
                    Thread.Sleep(100);
                }

            });
            ma.WaitOne();
        }

        private static List<string> GetLocalBaseAddress()
        {
            var output = new List<string>();

            foreach (var netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netif.Description.ToLower().Contains("loopback") || // Skip obvious adapters that don't need to be checked
                    netif.Description.ToLower().Contains("virtual") ||
                    netif.Description.ToLower().Contains("vm"))
                {
                    continue;
                }

                var properties = netif.GetIPProperties();

                foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var bytes = unicast.Address.GetAddressBytes();

                        if (bytes.First().Equals(169) || bytes.First().Equals(127))
                        {
                            continue;
                        }

                        output.Add($"{bytes[0]}.{bytes[1]}.{bytes[2]}.");
                    }
                }
            }

            return output;
        }

        private static async Task PingAndUpdateAsync(Ping ping, string ip)
        {
            var reply = await ping.SendPingAsync(ip, Timeout);

            if (reply.Status == IPStatus.Success)
            {
                var mac = getMacByIp(ip);
                if (mac.StartsWith("58-FC-DB"))
                {
                    lock (LockObj)
                    {
                        rayfins.Add(IPAddress.Parse(ip));
                        nFound++;
                    }
                }
            }
        }

        private static string getMacByIp(string ip)
        {
            var macIpPairs = GetAllMacAddressesAndIppairs();
            var index = macIpPairs.FindIndex(x => x.IpAddress == ip);
            if (index >= 0)
            {
                return macIpPairs[index].MacAddress.ToUpper();
            }
            else
            {
                return null;
            }
        }

        private static List<MacIpPair> GetAllMacAddressesAndIppairs()
        {
            var mip = new List<MacIpPair>();
            var pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = "arp";
            pProcess.StartInfo.Arguments = "-a ";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();
            var cmdOutput = pProcess.StandardOutput.ReadToEnd();
            var pattern = @"(?<ip>([0-9]{1,3}\.?){4})\s*(?<mac>([a-f0-9]{2}-?){6})";

            foreach (Match m in Regex.Matches(cmdOutput, pattern, RegexOptions.IgnoreCase))
            {
                mip.Add(new MacIpPair()
                {
                    MacAddress = m.Groups["mac"].Value,
                    IpAddress = m.Groups["ip"].Value,
                });
            }

            return mip;
        }

        private struct MacIpPair
        {
            public string MacAddress;
            public string IpAddress;
        }
    }
}

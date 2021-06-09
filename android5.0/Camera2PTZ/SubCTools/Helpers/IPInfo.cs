// <copyright file="IPInfo.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    public class IPInfo
    {
        private string hostName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPInfo"/> class.
        /// </summary>
        /// <param name="macAddress"></param>
        /// <param name="ipAddress"></param>
        public IPInfo(string macAddress, string ipAddress)
        {
            this.MacAddress = macAddress;
            this.IPAddress = ipAddress;
        }

        public string HostName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.hostName))
                {
                    return this.hostName;
                }

                try
                {
                    // Retrieve the "Host Name" for this IP Address. This is the "Name" of the machine.
                    this.hostName = Dns.GetHostEntry(this.IPAddress).HostName;
                }
                catch
                {
                    this.hostName = string.Empty;
                }

                return this.hostName;
            }
        }

        public string IPAddress { get; private set; }

        public string MacAddress { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{HostName} {IPAddress} {MacAddress}";
        }

        public static IEnumerable<string> GetAllIPs()
        {
            return from a in GetARPResult().Split(new char[] { '\n', '\r' })
                   where !string.IsNullOrEmpty(a) && !a.Contains("static") && a.Contains("58-fc")
                   let match = Regex.Match(a, @"\b(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\b")
                   where match.Success
                   select match.Value;
        }

        /// <summary>
        /// This runs the "arp" utility in Windows to retrieve all the MAC / IP Address entries.
        /// </summary>
        /// <returns></returns>
        public static string GetARPResult()
        {
            Process p = null;
            var output = string.Empty;

            try
            {
                p = Process.Start(new ProcessStartInfo("arp", "-a")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                });

                output = p.StandardOutput.ReadToEnd();

                p.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("IPInfo: Error Retrieving 'arp -a' Results", ex);
            }
            finally
            {
                p?.Close();
            }

            return output;
        }

        /// <summary>
        /// Retrieves the IPInfo for the machine on the local network with the specified MAC Address.
        /// </summary>
        /// <param name="macAddress">The MAC Address of the IPInfo to retrieve.</param>
        /// <returns></returns>
        public static IPInfo GetIPInfo(string macAddress)
        {
            var ipinfo = (from ip in IPInfo.GetIPInfo()
                          where ip.MacAddress.ToLowerInvariant() == macAddress.ToLowerInvariant()
                          select ip).FirstOrDefault();

            return ipinfo;
        }

        /// <summary>
        /// Retrieves the IPInfo for All machines on the local network.
        /// </summary>
        /// <returns></returns>
        public static List<IPInfo> GetIPInfo()
        {
            try
            {
                var list = new List<IPInfo>();

                foreach (var arp in GetARPResult().Split(new char[] { '\n', '\r' }))
                {
                    // Parse out all the MAC / IP Address combinations
                    if (!string.IsNullOrEmpty(arp))
                    {
                        var pieces = (from piece in arp.Split(new char[] { ' ', '\t' })
                                      where !string.IsNullOrEmpty(piece)
                                      select piece).ToArray();
                        if (pieces.Length == 3)
                        {
                            list.Add(new IPInfo(pieces[1], pieces[0]));
                        }
                    }
                }

                // Return list of IPInfo objects containing MAC / IP Address combinations
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("IPInfo: Error Parsing 'arp -a' results", ex);
            }
        }
    }
}
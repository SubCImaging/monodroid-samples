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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace SubCTools.Droid.Helpers
{
    public static class NetworkTools
    {
        public static IPAddress StaticIP => FetchStaticIP();

        public static void AssignStaticIp(IPAddress staticIP)
        {
            // var input = DroidSystem.ShellSync("ifconfig eth0");

            // DroidSystem.ShellSync($"ip addr add {staticIP.ToString()} dev eth0");
            // DroidSystem.ShellSync($"ip addr del {ParseIP(input)} dev eth0");
        }

        public static string ParseIP(string input)
        {
            //Regex ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            Regex ip = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            MatchCollection result = ip.Matches(input);
            return result.Count > 0 ? result[0].ToString() : string.Empty;
        }

        /// <summary>
        /// Returns the static IP assigned to the rayfin.
        /// </summary>
        /// <returns>Static IP on Rayfin;NULL if there is no static IP set</returns>
        public static IPAddress FetchStaticIP()
        {
            if (File.Exists("/data/local/tmp/eth0conf"))
            {
                var staticIP = new IPAddress(0);
                if (IPAddress.TryParse(NetworkTools.ParseIP(DroidSystem.ShellSync("cat /data/local/tmp/eth0conf")), out staticIP))
                {
                    return staticIP;
                }
            }
            return null;
        }

        public static bool GetResolvedConnecionIPAddress(string serverNameOrURL,
                   out IPAddress resolvedIPAddress)
        {
            bool isResolved = false;
            IPHostEntry hostEntry = null;
            IPAddress resolvIP = null;
            try
            {
                if (!IPAddress.TryParse(serverNameOrURL, out resolvIP))
                {
                    hostEntry = Dns.GetHostEntry(serverNameOrURL);

                    if (hostEntry != null && hostEntry.AddressList != null
                                 && hostEntry.AddressList.Length > 0)
                    {
                        if (hostEntry.AddressList.Length == 1)
                        {
                            resolvIP = hostEntry.AddressList[0];
                            isResolved = true;
                        }
                        else
                        {
                            foreach (IPAddress var in hostEntry.AddressList)
                            {
                                if (var.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    resolvIP = var;
                                    isResolved = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    isResolved = true;
                }
            }
            catch (Exception)
            {
                isResolved = false;
                resolvIP = null;
            }
            finally
            {
                resolvedIPAddress = resolvIP;
            }

            return isResolved;
        }

        public static string ValidateIP(string ip)
        {
            return IPAddress.TryParse(ip, out IPAddress output) ? ip : string.Empty;
        }
    }
}
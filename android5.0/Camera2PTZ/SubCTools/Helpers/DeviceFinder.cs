// <copyright file="DeviceFinder.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
//    public static class DeviceFinder
//    {
//        public static IEnumerable<CameraInfo> GetDevices() => GetDevices(new IPAddress[] { });

// /// <summary>
//        /// Get all the devices on the network
//        /// </summary>
//        /// <param name="ignoreAddresses">List of addresses to ignore</param>
//        /// <returns>A list of available cameras to connect to</returns>
//        public static IEnumerable<CameraInfo> GetDevices(IEnumerable<IPAddress> ignoreAddresses) //=> //Task.Run(() =>
//        {
//            var results = new ConcurrentBag<CameraInfo>();

// // run through all the ips in parallel
//            Parallel.ForEach(IPInfo.GetAllIPs(), (ipAddress) =>
//             {
//                 var ip = IPAddress.Parse(ipAddress);

// // ignore the ip if it's multicast or has 255 at the end, 3 will show up for each otherwise
//                 if (ipAddress.EndsWith("255") || IPAddress.Parse(ipAddress).IsIPv4Multicast() || ignoreAddresses.Contains(ip))
//                 {
//                     return;
//                 }

// try
//                 {
//                     var address = new EthernetAddress(ip, 8890);

// // see if you get a response from the name
//                     var name = SubCUdpSender.SendSync("name", address).TrimEnd('\0');

// try
//                     {
//                         // if it's a later version, it will have camera infor
//                         var cameraInfo = SubCUdpSender.SendSync("camerainfo", address).TrimEnd('\0');
//                         var result = Newtonsoft.Json.JsonConvert.DeserializeObject<CameraInfo>(cameraInfo);
//                         results.Add(result);
//                     }
//                     catch
//                     {
//                         // reverse compatibility if the Json fails
//                         if (!string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"\b\w+rayfin\w+\b", RegexOptions.IgnoreCase))
//                         {
//                             // set the default to Rayfin for reverse compatibiliity
//                             var cameraType = "Rayfin";

// try
//                             {
//                                 // if you're a later version, it will have the camera type
//                                 cameraType = SubCUdpSender.SendSync("cameratype", address).TrimEnd('\0');
//                             }
//                             catch
//                             {
//                             }

// cameraType = string.IsNullOrEmpty(cameraType) ? "Rayfin" : cameraType;

// results.Add(new CameraInfo { TCPAddress = new EthernetAddress(address.Address, 8888), Name = name, CameraType = cameraType });
//                         }
//                     }
//                 }
//                 catch
//                 {
//                     // no response was received from UDP, it timed out and threw an exception
//                 }
//             });

// return results;
//        }

// public static async Task<IEnumerable<CameraInfo>> GetDevicesAsync(IEnumerable<IPAddress> ignoreAddresses) => await Task.Run(() => GetDevices(ignoreAddresses));

// // });
//    }
// }
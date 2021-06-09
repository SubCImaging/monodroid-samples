//-----------------------------------------------------------------------
// <copyright file="SubCServiceDiscovery.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using Android.App;
    using Android.Content;
    using Android.Net.Nsd;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Newtonsoft.Json;
    using SubCTools.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;

    public class SubCServiceDiscovery
    {
        private readonly NsdManager nsdManager;
        private RegistrationListener listener;

        /// <summary>
        /// A flag to limit the number of NSD servers registered to 1 maximum
        /// </summary>
        private bool serviceRegistered = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCServiceDiscovery"/> class.
        /// </summary>
        /// <param name="nsdManager">Manager for initializing the NSD service</param>
        /// <param name="info">Camera info to send to connection</param>
        public SubCServiceDiscovery(NsdManager nsdManager, CameraInfo info)
        {
            this.nsdManager = nsdManager;
            Info = info;
        }

        public CameraInfo Info { get; private set; }

        /// <summary>
        /// Register the <see cref="Info"/> to the NSD Service/>
        /// </summary>
        public void Register()
        {
            Register(Info);
        }

        /// <summary>
        /// Start the service discovery
        /// </summary>
        public void Register(CameraInfo info)
        {
            //SubCLogger.Instance.Write($"\nIP:{info.TCPAddress}", "AttemptedNSD.log", DroidSystem.LogDirectory);

            if (serviceRegistered)
            {
                //SubCLogger.Instance.Write($" -server already registered {nsdManager.Handle}", "AttemptedNSD.log", DroidSystem.LogDirectory);
                return;
            }

            if (info.TCPAddress.Address.IsLoopback())
            {
                //SubCLogger.Instance.Write($" -cannot register loopback", "AttemptedNSD.log", DroidSystem.LogDirectory);
                return;
            }

            //SubCLogger.Instance.Write($" -attempting to register...", "AttemptedNSD.log", DroidSystem.LogDirectory);
            Info = info;

            var serviceInfo = new NsdServiceInfo
            {
                ServiceName = Info.Name,
                ServiceType = "_SubCRayfin._tcp",
                Port = 8890
            };

            // first need to serialize the camera info object in to a json string
            // then need to deserialize it in to a dictionary so we can loop through it and set the attributes
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(info));

            // service info doesn't accept a json string as a value
            foreach (var item in json)
            {
                serviceInfo.SetAttribute(item.Key, item.Value);
            }

            listener = new RegistrationListener();

            nsdManager.RegisterService(serviceInfo, NsdProtocol.DnsSd, listener);
            serviceRegistered = true;
            //SubCLogger.Instance.Write($" -registered", "AttemptedNSD.log", DroidSystem.LogDirectory);
        }

        /// <summary>
        /// Updates the NSD info with the updated <see cref="info"/>
        /// </summary>
        /// <param name="info"></param>
        public void UpdateCameraInfo(CameraInfo info)
        {
            nsdManager.UnregisterService(listener);
            serviceRegistered = false;

            Register(info);
        }

        private class RegistrationListener : Java.Lang.Object, NsdManager.IRegistrationListener
        {
            public void OnRegistrationFailed(NsdServiceInfo serviceInfo, [GeneratedEnum] NsdFailure errorCode)
            {
            }

            public void OnServiceRegistered(NsdServiceInfo serviceInfo)
            {
                Console.WriteLine(serviceInfo.ServiceName);
            }

            public void OnServiceUnregistered(NsdServiceInfo serviceInfo)
            {
            }

            public void OnUnregistrationFailed(NsdServiceInfo serviceInfo, [GeneratedEnum] NsdFailure errorCode)
            {
            }
        }
    }
}
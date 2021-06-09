//-----------------------------------------------------------------------
// <copyright file="Nas.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid
{
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.Droid.Attributes;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.EventArguments;
    using SubCTools.Droid.Helpers;
    using SubCTools.Enums;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// A class written to enable writing to network attached storage.
    /// </summary>
    public class Nas : DroidBase
    {
        /// <summary>
        /// The sync object for the lock that makes sure the IP is set before connecting.
        /// </summary>
        private static readonly object IPSync = new object();

        /// <summary>
        /// The location to mount the server in the system.
        /// </summary>
        private const string InternalMountPoint = "/mnt/nas";

        /// <summary>
        /// The timeout before a connection test fails.  Must follow the format below.
        /// </summary>
        private const string ConnectionTestTimeout = "0.25s";

        /// <summary>
        /// The maximum number of consecutive failures before changing <see cref="nasStatus"/>
        /// to <see cref="ConnectionStatus.Offline"/>
        /// </summary>
        private const int MaxConsecutiveFailures = 3;

        /// <summary>
        /// The number of consecutive failed pings
        /// </summary>
        private int consecutiveFailures = 0;

        /// <summary>
        /// The state the camera gets in when the connection is broken.
        /// </summary>
        private bool deadConnection = false;

        /// <summary>
        /// A <see cref="bool"/> representing whether or not the NAS is currently connected.
        /// </summary>
        private bool nasConnected;

        /// <summary>
        /// The current <see cref="ConnectionStatus"/> of the NAS.
        /// </summary>
        private ConnectionStatus nasStatus = ConnectionStatus.Disabled;

        /// <summary>
        /// A <see cref="bool"/> representing whether or not the NAS is currently enabled.
        /// </summary>
        private bool nasEnabled;

        /// <summary>
        /// The <see cref="IPAddress"/> of the NAS you wish to connect to.
        /// </summary>
        private IPAddress nasIP;

        /// <summary>
        /// The path relative to the <see cref="IPAddress"/> to mount.
        /// </summary>
        private string nasPath;

        /// <summary>
        /// The <see cref="Timer"/> that manages pinging the server to check for connection dropouts.
        /// </summary>
        private Timer serverPingTimer = new Timer();

        /// <summary>
        /// Initializes a new instance of the <see cref="Nas"/> class.
        /// </summary>
        public Nas(ISettingsService settings) :
            base(settings)
        {
            serverPingTimer.AutoReset = true;
            serverPingTimer.Interval = TimeSpan.FromSeconds(2).TotalMilliseconds;
            serverPingTimer.Elapsed += (s, e) => Ping_Server();
            serverPingTimer.Start();

            LoadSettings();

            if (NasEnabled)
            {
                ConnectNAS();
            }
            else
            {
                SetStatus(ConnectionStatus.Disabled);
            }
        }

        /// <summary>
        /// The <see cref="EventHandler"/> that triggers when the connection to the NAS changes.
        /// </summary>
        public event EventHandler<bool> ConnectionChanged;

        /// <summary>
        /// Gets a value indicating whether or not the NAS is currently connected.
        /// </summary>
        [RemoteState]
        public bool NasConnected
        {
            get => nasConnected;
            private set => Set(nameof(NasConnected), ref nasConnected, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the NAS is currently enabled.
        /// </summary>
        [Savable]
        [RemoteState]
        public bool NasEnabled
        {
            get => nasEnabled;
            set
            {
                if (value != NasEnabled)
                {
                    Set(nameof(NasEnabled), ref nasEnabled, value);
                    OnNotify($"{nameof(NasEnabled)}:{NasEnabled}");
                }
            }
        }

        /// <summary>
        /// Gets the current <see cref="ConnectionStatus"/> of the NAS.
        /// </summary>
        [RemoteState]
        public ConnectionStatus NasStatus
        {
            get => nasStatus;
            private set => nasStatus = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="IPAddress"/> of the NAS you wish to connect to.
        /// </summary>
        [Savable]
        [RemoteState]
        [PropertyConverter(typeof(StringToIPAddress))]
        public IPAddress NasIP
        {
            get => nasIP;
            set => Set(nameof(NasIP), ref nasIP, value);
        }

        /// <summary>
        /// Gets or sets the path relative to the <see cref="IPAddress"/> to mount.
        /// </summary>
        [Savable]
        [RemoteState]
        public string NasPath
        {
            get => nasPath;
            set => Set(nameof(NasPath), ref nasPath, value);
        }

        //// [RemoteState]
        //// public string NasProtocol
        //// {
        ////     get => nasProtocol;
        ////     set => Set(nameof(NasProtocol), ref nasProtocol, value);
        //// }

        //// [RemoteState]
        //// public string Password
        //// {
        ////     get => password;
        ////     set => Set(nameof(Password), ref password, value);
        //// }

        //// [RemoteState]
        //// public string Username
        //// {
        ////     get => username;
        ////     set => Set(nameof(Username), ref username, value);
        //// }

        /// <summary>
        /// <see cref="RemoteCommand"/> to connect to the NAS.
        /// </summary>
        [RemoteCommand]
        public void ConnectNAS()
        {
            lock (IPSync)
            {
                Task.Run(() =>
                {
                    CheckConnection();
                    var currentmount = DroidSystem.ShellSync(@"/system/subcimaging/bin/busybox mount | grep nfs | sed -n '1!p' | /system/subcimaging/bin/busybox awk '{print $1;}'");
                    try
                    {
                        var currentIP = currentmount.Split(':')[0];
                        var currentPath = currentmount.Split(':')[1];

                        if (deadConnection && (!(currentIP == NasIP.ToString()) || !(currentPath == NasPath)))
                        {
                            OnNotify("Could not connect, connection to old server still active.  If you want to connect to a new server you will have to restart the Rayfin.", Messaging.Models.MessageTypes.Error);
                            SubCLogger.Instance.Write($"{DateTime.Now}:  Could not connect, current NFS server dead", "nas.log", @"/storage/emulated/0/Logs/");
                            return;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                    }

                    if (NasIP == null)
                    {
                        OnNotify("Could not connect, invalid IP", Messaging.Models.MessageTypes.Error);
                        SubCLogger.Instance.Write($"{DateTime.Now}:  Could not connect, invalid IP", "nas.log", @"/storage/emulated/0/Logs/");
                        SetStatus(ConnectionStatus.Disabled);
                        return;
                    }

                    SetStatus(ConnectionStatus.Connecting);

                    // var protocol = (nasProtocol == "0") ? "SMB" : "NFS";
                    // var mountString = $@"/system/subcimaging/bin/mount_server.sh {protocol} {nasIP} {nasPath} {InternalMountPoint}";
                    // mountString += $" {Username} {Password}";
                    var mountString = $@"/system/subcimaging/bin/busybox mount -o nolock,rw,hard,intr,vers=3 -t nfs {NasIP.ToString()}:{NasPath} /mnt/nas";

                    DroidSystem.ShellSync("umount -f -l /mnt/nas");                        // Force unmount the directory if one is already mounted.
                    var mount = DroidSystem.ShellSync($@"timeout {ConnectionTestTimeout} df {InternalMountPoint} | tail -n 1 | cut -d' ' -f1").TrimEnd();
                    var sambaInUse = mount != "tmpfs";
                    if (sambaInUse)
                    {
                        SubCLogger.Instance.Write($"{DateTime.Now}:  Unmounting Samba", "nas.log", @"/storage/emulated/0/Logs/");
                        DroidSystem.ShellSync("/data/data/com.funkyfresh.samba/files/samba-rc stop");
                        DroidSystem.ShellSync("umount -f -l /mnt/nas");
                    }
                    if (mount == "tmpfs")
                    {
                        SubCLogger.Instance.Write($"{DateTime.Now}:  Temp filesystem detected, clearing directories.", "nas.log", @"/storage/emulated/0/Logs/");
                        DroidSystem.ShellSync("rm -r /mnt/nas");                                // Clear any contents that may have been auto-generated by SubCCam if it is confirmed not mounted.
                    }

                    DroidSystem.ShellSync("mkdir -p /mnt/nas");                          // Make the mount directory if it doesn't already exist.
                    DroidSystem.ShellSync($"timeout 3s {mountString}");                                       // Mount the server.
                    if (sambaInUse)
                    {
                        SubCLogger.Instance.Write($"{DateTime.Now}:  Restarting Samba", "nas.log", @"/storage/emulated/0/Logs/");
                        DroidSystem.ShellSync("/data/data/com.funkyfresh.samba/files/samba-rc start");
                    }

                    if (CheckConnection() && CheckServerConfiguration())
                    {
                        SubCLogger.Instance.Write($"{DateTime.Now}:  Connected {NasIP}", "nas.log", @"/storage/emulated/0/Logs/");
                        DroidSystem.SetProp("persist.rayfin.nas.enabled", "1");
                        DroidSystem.Instance.SetStorageType(nameof(RayfinStorageType.NAS));
                        NasEnabled = true;
                        NasConnected = true;
                        SetStatus(ConnectionStatus.Online);
                    }
                    else if (!CheckConnection())
                    {
                        OnNotify("There has been an error connecting to the network attached storage.  E:301", Messaging.Models.MessageTypes.Error);
                        SubCLogger.Instance.Write($"{DateTime.Now}:  Connection failed  E:301", "nas.log", @"/storage/emulated/0/Logs/");
                        SetStatus(ConnectionStatus.Disabled);
                        NasEnabled = false;
                    }
                    else
                    {
                        OnNotify("There has been an error connecting to the network attached storage.  E:308", Messaging.Models.MessageTypes.Error);
                        SubCLogger.Instance.Write($"{DateTime.Now}:  Connection failed  E:308", "nas.log", @"/storage/emulated/0/Logs/");
                        SetStatus(ConnectionStatus.Disabled);
                        NasEnabled = false;
                    }
                });
            }
        }

        /// <summary>
        /// <see cref="RemoteCommand"/> to disconnect from the NAS.
        /// </summary>
        [RemoteCommand]
        [CancelWhen(nameof(Camera.RecordingHandler.IsRecording), true)]
        public void DisconnectNAS()
        {
            Task.Run(() =>
            {
                SubCLogger.Instance.Write($"{DateTime.Now}:  Disconnecting {NasIP}", "nas.log", @"/storage/emulated/0/Logs/");
                SetStatus(ConnectionStatus.Disconnecting);
                DroidSystem.ShellSync(@"setprop persist.rayfin.nas.enabled 0");
                DroidSystem.Instance.SetStorageType(nameof(RayfinStorageType.SD));
                DroidSystem.ShellSync($"umount -f -l {InternalMountPoint}");
                NasConnected = false;
                SetStatus(ConnectionStatus.Disabled);
                NasEnabled = false;
                SubCLogger.Instance.Write($"{DateTime.Now}:  Disconnected", "nas.log", @"/storage/emulated/0/Logs/");
            });
        }

        /// <summary>
        /// <see cref="RemoteCommand"/> to set the NAS <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ip">The <see cref="IPAddress"/> to set as the current <see cref="NasIP"/></param>
        [RemoteCommand]
        public void SetNasIP(string ip)
        {
            lock (IPSync)
            {
                if (ip == string.Empty || ip.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }

                if (!IPAddress.TryParse(ip, out IPAddress addr))
                {
                    SubCLogger.Instance.Write($"{DateTime.Now}:  Could not parse {ip} as a valid IP address", "nas.log", @"/storage/emulated/0/Logs/");
                    OnNotify($"There was a problem setting the {nameof(NasIP)}", Messaging.Models.MessageTypes.Error);
                    return;
                }
                else
                {
                    NasIP = addr;
                }
            }
        }

        /// <summary>
        /// Checks the connection to the NAS.
        /// </summary>
        /// <returns>A value representing whether or not the NAS is currently connected.</returns>
        private bool CheckConnection()
        {
            var mount = DroidSystem.ShellSync($@"timeout {ConnectionTestTimeout} df {InternalMountPoint} | tail -n 1 | cut -d' ' -f1").TrimEnd();
            if (mount.Contains((NasIP ?? IPAddress.None).ToString()) && mount.Contains(NasPath))
            {
                deadConnection = false;
                return true;
            }
            else if (mount.Contains("tmpfs"))
            {
                deadConnection = false;
                return false;
            }
            else
            {
                deadConnection = true;
                return false;
            }
        }

        /// <summary>
        /// Checks the connection to ensure that the Rayfin can write
        /// and read the share as well as change permissions on its files
        /// </summary>
        /// <returns></returns>
        private bool CheckServerConfiguration()
        {
            if (!CheckConnection())
            {
                return false;
            }

            var guid = Guid.NewGuid();
            var file = $"/mnt/nas/{guid.ToString()}";
            DroidSystem.ShellSync($"echo {guid.ToString()} > {file}");
            var read = DroidSystem.ShellSync($"cat {file}").TrimEnd();
            if (guid.ToString() == read == false)
            {
                return finished(false);
            }

            DroidSystem.ShellSync($"chmod 777 {file}");
            var perm = DroidSystem.ShellSync($"ls -l {file}");
            if (perm.Contains("rwxrwxrwx"))
            {
                return finished(true);
            }

            return finished(false);

            bool finished(bool status)
            {
                DroidSystem.ShellSync($"rm {file}");
                return status;
            }
        }

        /// <summary>
        /// Sets the <see cref="ConnectionStatus"/> and notifies the change.
        /// </summary>
        /// <param name="connectionStatus">The new <see cref="ConnectionStatus"/></param>
        private void SetStatus(ConnectionStatus connectionStatus)
        {
            if (NasStatus == connectionStatus)
            {
                return;
            }

            ConnectionChanged?.Invoke(this, connectionStatus == ConnectionStatus.Online);
            NasStatus = connectionStatus;
            OnNotify($"{nameof(NasStatus)}:{NasStatus}");
            OnNotify("NotifyDiskSpaceRemaining:true", Messaging.Models.MessageTypes.Internal);
            SubCLogger.Instance.Write($"{DateTime.Now}:  Status change => {connectionStatus}", "nas.log", @"/storage/emulated/0/Logs/");
        }

        /// <summary>
        /// Check the status of the NAS and updates <see cref="NasConnected"/>.
        /// </summary>
        private void Ping_Server()
        {
            Task.Run(() =>
            {
                if (!NasEnabled)
                {
                    return;
                }

                if (CheckConnection())
                {
                    if (!NasConnected && consecutiveFailures >= MaxConsecutiveFailures)
                    {
                        OnNotify("The connection to the network attached storage has been reconnected, switching to network attached storage.", Messaging.Models.MessageTypes.Error);
                        DroidSystem.Instance.SetStorageType(nameof(RayfinStorageType.NAS));  // If NAS connection is re-established let the user know, update RCS and switch the storagetype to NAS.
                        SetStatus(ConnectionStatus.Online);
                    }

                    consecutiveFailures = 0;

                    if (!NasConnected)
                    {
                        NasConnected = true;
                        OnNotify($"{nameof(NasConnected)}:{NasConnected}");
                    }
                }
                else
                {
                    if (NasStatus == ConnectionStatus.Online && !CheckConnection())
                    {
                        if (++consecutiveFailures == MaxConsecutiveFailures)
                        {
                            OnNotify("The connection to the network attached storage has been interrupted, switching to internal storage.  Please check the camera's connection speed ('System Info' tab) to determine if your network can support the bitrate you are recording at.  (Displayed connection speed of the camera may not reflect actual limitations of entire network)", Messaging.Models.MessageTypes.Error);
                            SubCLogger.Instance.Write($"{DateTime.Now}:  Connection Lost {NasIP}", "nas.log", @"/storage/emulated/0/Logs/");
                            DroidSystem.Instance.SetStorageType(nameof(RayfinStorageType.SD));
                            SetStatus(ConnectionStatus.Offline);
                            return;
                        }
                    }

                    if (NasConnected)
                    {
                        NasConnected = false;
                        OnNotify($"{nameof(NasConnected)}:{NasConnected}");
                    }
                }
            });
        }
    }
}
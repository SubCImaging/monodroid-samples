//-----------------------------------------------------------------------
// <copyright file="DroidSystem.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid
{
    using Android.App;
    using Java.IO;
    using Java.Lang;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.Droid.Enums;
    using SubCTools.Droid.Extensions;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.IO;
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// A class that helps interface with the Android OS in the Rayfin.
    /// </summary>
    public class DroidSystem : INotifier
    {
        /// <summary>
        /// The default timezone to use if one isn't specified.
        /// </summary>
        private const string DefaultTimeZone = @"Etc/GMT";

        /// <summary>
        /// The latest year that can be used(After this year the Android OS misbehaves).
        /// </summary>
        private const int MaxYear = 2035;

        /// <summary>
        /// The location of the file that contains the static IP    TODO:  This should be moved to a system prop persist.rayfin.static.address
        /// </summary>
        private const string StaticIPLocation = @"/data/local/tmp/eth0conf";

        /// <summary>
        /// The location of the SD card in the system.
        /// </summary>
        private const string StorageLocation = "/mnt/expand";

        /// <summary>
        /// The <see cref="ObservableCollection"/> of <see cref="DroidSystem"/> instances.
        /// </summary>
        private static readonly Lazy<DroidSystem> instance = new Lazy<DroidSystem>(() => new DroidSystem());

        /// <summary>
        /// The network sync object for restarting the network services
        /// </summary>
        private static readonly object networkSync = new object();

        /// <summary>
        /// The throttling governor to use when the temperature limit is reached.
        /// </summary>
        private static readonly Governor throttlingGovernor = Governor.PowerSave;

        /// <summary>
        /// The current governor being used on the CPU in Android.
        /// </summary>
        private static Governor currentGovernor;

        /// <summary>
        /// A place to store the storagePoint rather than calling it repeatedly
        /// </summary>
        private static string storagePoint;

        /// <summary>
        /// The warning range for temp, if the limit is reached the throttling govorner is enabled.
        /// </summary>
        private static Range<Integer> TempWarningRange = new Range<Integer>(new Android.Util.Range(53, 68));

        /// <summary>
        /// <see cref="Timer"/> that handles the tempature checking.
        /// </summary>
        private readonly Timer checkDiagTimer = new Timer { AutoReset = true };

        /// <summary>
        /// The configured camera type
        /// </summary>
        private CameraType cameraType;

        /// <summary>
        /// The highest tempature recorded.
        /// </summary>
        private double highestTemp = 0;

        /// <summary>
        /// A <see cref="bool"/> representing whether or not the app is in debug mode.
        /// </summary>
        private bool isDebugging;

        /// <summary>
        /// The last recorded tempature
        /// </summary>
        private int lastRecordedTemperature;

        /// <summary>
        /// Various fields
        /// </summary>
        private string linkLocalAddress;

        /// <summary>
        /// Prevents a default instance of the <see cref="DroidSystem"/> class from being created.
        /// </summary>
        private DroidSystem()
        {
            checkDiagTimer.Interval = TimeSpan.FromSeconds(29).TotalMilliseconds;
            checkDiagTimer.Elapsed += RefreshState;
            checkDiagTimer.Start();

            if (!DoesDriveExist())
            {
                SetStorageType("Internal");
            }

            storagePoint = GetStoragePoint().ToString();

            System.Console.WriteLine(BaseDirectory);

            new DirectoryInfo(LogDirectory).CreateIfMissing();

            new DirectoryInfo(ScriptDirectory).CreateIfMissing();

            CreateMediaDirectories();

            InitializeState();

            //var tempLogger = new TemperatureLogger(new Shell());

            SetPerformanceMode();

            UpdateNetworkLinkSpeed();

            Task.Run(() =>
            {
                ShellSync("mkdir -p /data/local/tmp/swap");
                ShellSync("chmod 777 /data/local/tmp/swap");
                var isRunning = ShellSync("ps | grep python") != string.Empty;

                if (!isRunning)
                {
                    ShellSync(@"python3 /system/subcimaging/python/transfer_data.py > /data/local/tmp/python_log 2>&1 &");
                }
            });

            //NetworkChange.NetworkAvailabilityChanged += (s, e) =>
            //{
            //    if (!e.IsAvailable)
            //    {
            //        return;
            //    }

            //    Task.Run(() =>
            //    {
            //        lock (networkSync)
            //        {
            //            var networkRestartDelay = TimeSpan.FromSeconds(2);

            //            Thread.Sleep((long)networkRestartDelay.TotalMilliseconds);

            //            // Restart samba and the ADB daemon
            //            // Restart Samba
            //            ShellSync(@"/data/data/com.funkyfresh.samba/files/samba-rc stop " +
            //                @"&& sleep 1 && /data/data/com.funkyfresh.samba/files/samba-rc start");

            //            // Restart ADB Daemon
            //            ShellSync("setprop ctl.restart adbd");
            //            Thread.Sleep((long)networkRestartDelay.TotalMilliseconds);

            //            // Unlock network for non root access
            //            // DroidSystem.ShellSync("ip rule add from all lookup main pref 99");
            //            Thread.Sleep((long)networkRestartDelay.TotalMilliseconds);

            //            UpdateNetworkLinkSpeed();
            //        }
            //    });
            //};
        }

        /// <summary>
        /// Notify event args
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// The event that fires when the static ip changes.
        /// </summary>
        public event EventHandler StaticIPChanged;

        /// <summary>
        /// The base directory to store the media in.
        /// </summary>
        [RemoteState]
        public static string BaseDirectory => (StorageType == RayfinStorageType.NAS) ? @"/mnt/nas" : (StorageType == RayfinStorageType.SD) ? $@"{StorageLocation}/" + storagePoint : InternalDirectory;

        /// <summary>
        /// Gets the directory to store the NMEA data in.
        /// </summary>
        public static DirectoryInfo DataDirectory { get; private set; }

        /// <summary>
        /// A file transfer script
        /// </summary>
        public static string FileTransferScript => @"/system/subcimaging/bin/file_created.sh";

        /// <summary>
        /// A instance of the <see cref="DroidSystem"/> class.
        /// </summary>
        public static DroidSystem Instance => instance.Value;

        /// <summary>
        /// Gets the Internal storage location.
        /// </summary>
        public static string InternalDirectory { get; } = Android.OS.Environment.GetExternalStoragePublicDirectory(string.Empty).AbsolutePath;

        /// <summary>
        /// Gets the Rayfins lens type
        /// 0=LiquidOptics, 1=UltraOptics, 2=ZoomLens
        /// </summary>
        [RemoteState(true)]
        public static LensType LensType { get; private set; } = LensType.LiquidOptics;

        /// <summary>
        /// Gets the directory to store the logs in.
        /// </summary>
        public static string LogDirectory { get; } = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(string.Empty).AbsolutePath, "Logs");

        /// <summary>
        /// Gets the base directory to store the scripts in.
        /// </summary>
        public static string ScriptDirectory { get; } = Path.Combine(InternalDirectory, "Scripts");

        /// <summary>
        /// Gets or sets the <see cref="RayfinStorageType"/> to save media to.
        /// Internal, SD, or NAS
        /// </summary>
        [Savable]
        [RemoteState]
        public static RayfinStorageType StorageType { get; set; }

        public static string SwapDirectory => @"/data/local/tmp/swap";

        /// <summary>
        /// Gets the configured camera type
        /// </summary>
        [RemoteState]
        public CameraType CameraType => cameraType;

        /// <summary>
        /// Gets cPUTemp
        /// </summary>
        [RemoteState]
        public double CPUTemp { get; private set; }

        /// <summary>
        /// Gets the DHCP Address, which is also the location of samba server.
        /// </summary>
        [RemoteState]
        public string DHCPAddress { get; private set; }

        /// <summary>
        /// Gets the serial number for the Rayfin
        /// </summary>
        [RemoteState]
        public string HostName { get; private set; }

        /// <summary>
        /// Gets the current IP from <see cref="Ethernet.GetIP"/>
        /// </summary>
        [RemoteState]
        public string IP { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the app is debugging.
        /// </summary>
        [RemoteState(true)]
        public bool IsDebugging
        {
            get => isDebugging;
            private set => isDebugging = value;
        }

        /// <summary>
        /// Gets the local link address assosiated with the rayfin.
        /// </summary>
        public string LinkLocalAddress { get; private set; }

        /// <summary>
        /// Gets the MAC Address of eth0.
        /// </summary>
        [RemoteState]
        public string MAC { get; private set; }

        /// <summary>
        /// Gets the current link speed of the network adapter in mbps
        /// </summary>
        [RemoteState]
        public int NetworkLinkSpeed { get; private set; }

        /// <summary>
        /// Gets the Rayfins product ID
        /// </summary>
        [RemoteState]
        public string ProductID { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the ROM version of the Rayfin
        /// </summary>
        [RemoteState]
        public string RomVersion { get; private set; }

        /// <summary>
        /// Gets the serial number of the Rayfin
        /// </summary>
        [RemoteState]
        public string SerialNumber { get; private set; }

        /// <summary>
        /// Gets the static IP address that is currently assigned to the Rayfin
        /// </summary>
        // public string StaticIPAddress => (RomVersion == "1.0") ? NetworkTools.StaticIP?.ToString() ?? string.Empty : NetworkTools.ValidateIP(DroidSystem.ShellSync("ifconfig eth0:static | grep \"inet addr\" | cut -c21-36").Split(' ')[0]);

        /// <summary>
        /// Gets the 128-bit AES encryption key for the storage mount.
        /// </summary>
        [RemoteState]
        public string StorageKey { get; private set; }

        /// <summary>
        /// Gets the current system TimeZone.
        /// </summary>
        [RemoteState]
        public string TimeZone { get; private set; }

        /// <summary>
        /// Gets a comma seperated string containing all the available timezones.
        /// </summary>
        /// <returns>A comma seperated string containing all the available timezones.</returns>
        public string TimeZones => string.Join(",", Android.Icu.Util.TimeZone.GetAvailableIDs());

        /// <summary>
        /// Gets the Hostname of the device.
        /// </summary>
        [RemoteState]
        public string UnitID => HostName;

        /// <summary>
        /// Gets version of the software
        /// </summary>
        [RemoteState]
        public string Version { get; } = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0).VersionName;

        /// <summary>
        /// Gets the video mode for the composite video output
        /// </summary>
        [RemoteState]
        public string VideoMode { get; private set; }

        /// <summary>
        /// Creates the media directories if they don't exist.
        /// </summary>
        public static void CreateMediaDirectories()
        {
            var stillsDirectory = new DirectoryInfo(System.IO.Path.Combine(BaseDirectory, "Stills"));
            var videosDirectory = new DirectoryInfo(System.IO.Path.Combine(BaseDirectory, "Videos"));
            DataDirectory = new DirectoryInfo(Path.Combine(BaseDirectory, "Data"));

            stillsDirectory.CreateIfMissing();
            videosDirectory.CreateIfMissing();
            DataDirectory.CreateIfMissing();
        }

        /// <summary>
        /// Checks if there are any drives mounted at <see cref="StorageLocation"/>
        /// </summary>
        /// <returns>A <see cref="bool"/> representing whether or not the number of drives is greater than 0.</returns>
        public static bool DoesDriveExist()
        {
            var drive = DroidSystem.ShellSync($@"ls {StorageLocation}");
            return !string.IsNullOrEmpty(drive);
        }

        /// <summary>
        /// Gets the Android system property named <see cref="prop"/>.
        /// </summary>
        /// <param name="prop">The name of the system property you want to get.</param>
        /// <returns>The system property you want.</returns>
        public static string GetProp(string prop) => DroidSystem.ShellSync($"getprop {prop}").TrimEnd();

        /// <summary>
        /// Gets the 128-bit AES encryption key for the storage mount.
        /// </summary>
        /// <returns>The 128-bit AES encryption key for the storage mount.</returns>
        [RemoteCommand]
        public static string GetStorageKey()
        {
            try
            {
                var keyLocation = DroidSystem.ShellSync("ls /data/misc/vold -Art | tail -n 1").TrimEnd();
                return DroidSystem.ShellSync($@"od -t x1 /data/misc/vold/{keyLocation} | cut -c9-55");
            }
            catch (Java.Lang.Exception e)
            {
                e.PrintStackTrace();
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the total amount of heap available in MB
        /// </summary>
        /// <returns>The total amount of heap available in MB</returns>
        public static long GetUsedMemory()
        {
            var runtime = Runtime.GetRuntime();
            var usedMemInMB = (runtime.TotalMemory() - runtime.FreeMemory()) / 1048576L;
            var maxHeapSizeInMB = runtime.MaxMemory() / 1048576L;
            var availHeapSizeInMB = maxHeapSizeInMB - usedMemInMB;
            return availHeapSizeInMB;
        }

        /// <summary>
        /// Sets the CPU governor in Android
        /// </summary>
        /// <param name="governor">the governator</param>
        public static void SetGovernor(Governor governor)
        {
            currentGovernor = governor;
            SubCLogger.Instance.Write($"{DateTime.Now}:  Setting governor to {governor}", "CPU_Governor.log", @"/storage/emulated/0/Logs/");
            for (int i = 0; i < 4; i++)
            {
                DroidSystem.ShellSync($"echo {governor.ToString().ToLower()} > /sys/devices/system/cpu/cpu{i}/cpufreq/scaling_governor");
            }
        }

        /// <summary>
        /// Sets the Android system property named <see cref="prop"/>
        /// </summary>
        /// <param name="prop">The name of the property you want to set.</param>
        /// <param name="value">The value you want to pass into the setter.</param>
        public static string SetProp(string prop, string value) => DroidSystem.ShellSync($"setprop {prop} \"{value}\"");

        /// <summary>
        /// Runs a shell command on the Rayfin.
        /// </summary>
        /// <param name="command">The command you wish to execute.</param>
        /// <param name="timeout">The maximum time the command is allowed to run before timing out.</param>
        /// <returns>Anything that comes from stdout</returns>
        public static string ShellSync(string command, int timeout = 0)
        {
            try
            {
                // Run the command
                var log = new System.Text.StringBuilder();
                var process = Runtime.GetRuntime().Exec(new[] { "su", "-c", command });
                var bufferedReader = new BufferedReader(
                new InputStreamReader(process.InputStream));

                // Grab the results
                if (timeout > 0)
                {
                    process.Wait(timeout);
                    return string.Empty;
                }

                string line;

                while ((line = bufferedReader.ReadLine()) != null)
                {
                    log.AppendLine(line);
                }
                return log.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Runs a shell command on the Rayfin.
        /// </summary>
        /// <param name="process">The process to use for the command.</param>
        /// <param name="command">The command you wish to execute.</param>
        /// <param name="timeout">The maximum time the command is allowed to run before timing out.</param>
        public static void ShellSync(ref Process process, string command, int timeout = 0)
        {
            try
            {
                process.Destroy();
                process.Dispose();
            }
            catch (NullReferenceException)
            {
                // Process is null.
            }

            try
            {
                // Run the command
                var log = new System.Text.StringBuilder();
                process = Runtime.GetRuntime().Exec(new[] { "su", "-c", command });
                var bufferedReader = new BufferedReader(
                        new InputStreamReader(process.InputStream));

                // Grab the results
                if (timeout > 0)
                {
                    process.Wait(timeout);
                    return;
                }

                string line;
                while ((line = bufferedReader.ReadLine()) != null)
                {
                    log.AppendLine(line);
                }

                ShellSync(ref process, command);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Runs chmod on a file using the current user.  (NON-ROOT)
        /// </summary>
        /// <param name="permissions">The permissions to use, 644, 777, etc.</param>
        /// <param name="fileLocation">The location of the file to change.</param>
        public static void UserChmod(int permissions, string fileLocation) =>
            Runtime.GetRuntime().Exec(new string[]
            {
                "chmod", permissions.ToString(), fileLocation
            });

        /// <summary>
        /// Clears the Rayfin Settings and restarts the camera.
        /// </summary>
        //[RemoteCommand]
        //[Alias("DeleteSettings", "clearsettings")]
        //public void ClearSettings()
        //{
        //    Shell("rm -r /storage/emulated/0/Settings/*");
        //    OnNotify("Settings cleared!  Rayfin will now restart.", MessageTypes.Alert);
        //    ResetTimeZone();
        //    Shell("reboot");
        //}

        /// <summary>
        /// Gets the CPU usage and returns it as a 4 element array.
        /// </summary>
        /// <returns>Array with 4 elements: user, system, idle and other cpu usage in percentage.</returns>
        public int[] CpuUsage()
        {
            var tempString = ExecuteTop();

            tempString = tempString.Replace(",", string.Empty);
            tempString = tempString.Replace("User", string.Empty);
            tempString = tempString.Replace("System", string.Empty);
            tempString = tempString.Replace("IOW", string.Empty);
            tempString = tempString.Replace("IRQ", string.Empty);
            tempString = tempString.Replace("%", string.Empty);
            for (int i = 0; i < 10; i++)
            {
                tempString = tempString.Replace("  ", " ");
            }

            tempString = tempString.Trim();
            var myString = tempString.Split(' ');
            int[] cpuUsageAsInt = new int[myString.Length];
            for (int i = 0; i < myString.Length; i++)
            {
                myString[i] = myString[i].Trim();
                cpuUsageAsInt[i] = int.Parse(myString[i]);
            }

            return cpuUsageAsInt;
        }

        /// <summary>
        /// Disables debug mode.
        /// </summary>
        [RemoteCommand]
        public void DisableDebugging()
        {
            IsDebugging = false;
        }

        /// <summary>
        /// Enables debug mode.
        /// </summary>
        [RemoteCommand]
        public void EnableDebugging()
        {
            IsDebugging = true;
        }

        /// <summary>
        /// Deletes all files from the Media folder
        /// </summary>
        [RemoteCommand]
        public void FormatMedia()
        {
            Shell("rm -r " + BaseDirectory);
        }

        /// <summary>
        /// Formats the internal SD card.
        /// </summary>
        //[RemoteCommand]
        //public void Format()
        //{
        //    Task.Run(async () =>
        //    {
        //        DroidSystem.ShellSync(@"sm partition $(sm list-disks) private");
        //        OnNotify($"Rayfin has been formatted\nRestarting now", MessageTypes.Alert);
        //        await Task.Delay(10_000);
        //        DroidSystem.ShellSync(@"reboot");
        //    });
        //}

        /// <summary>
        /// Gets the available heap size in MB.
        /// </summary>
        /// <returns>The available heap size in MB.</returns>
        [RemoteCommand]
        public string GetAvailableMemory() => GetUsedMemory().ToString();

        /// <summary>
        /// Gets the CPU usage.
        /// </summary>
        /// <returns>A comma seperated string containing the output from <see cref="CpuUsage"/></returns>
        [RemoteCommand]
        public string GetCPUUsage() => string.Join(", ", CpuUsage());

        /// <summary>
        /// Gets the hostname of the Rayfin
        /// </summary>
        /// <returns>The hostname of the Rayfin</returns>
        [RemoteCommand]
        public string GetHostName() => DroidSystem.ShellSync($"getprop net.hostname");

        /// <summary>
        /// Gets the Mac Address of the eth0 interface.
        /// </summary>
        /// <returns>The Mac Address of the eth0 interface.</returns>
        [RemoteCommand]
        public string GetMACAddress() => DroidSystem.ShellSync($"cat /sys/class/net/eth0/address");

        /// <summary>
        /// Set the camera to performance mode for max performance
        /// </summary>
        public void SetPerformanceMode()
        {
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                SetGovernor(SubCTools.Droid.Enums.Governor.Performance);
                for (int i = 0; i < 4; i++)
                {
                    ShellSync($"echo {(i > 1 ? "2150400" : "1593600 ")} > /sys/devices/system/cpu/cpu{i}/cpufreq/scaling_max_freq");
                    ShellSync($"echo 1228800  > /sys/devices/system/cpu/cpu{i}/cpufreq/scaling_min_freq");
                }
            });
        }

        /// <summary>
        /// Sets the <see cref="RayfinStorageType"/> to use to save media.
        /// </summary>
        /// <param name="value">A <see cref="string"/> containing a <see cref="RayfinStorageType"/> you wish to use.</param>
        public void SetStorageType(string value)
        {
            if (System.Enum.TryParse(value, true, out RayfinStorageType type))
            {
                StorageType = type;
            }
        }

        /// <summary>
        /// Sets the System Clock in the Rayfin
        /// </summary>
        /// <param name="timezone">a string representation of the timezone to set</param>
        [RemoteCommand]
        [Alias("SystemTimeZone", "TimeZone")]
        public void SetTimeZone(string timezone)
        {
            OnNotify($"{nameof(TimeZone)} set to: {timezone}.\nSystem will now restart", MessageTypes.Alert);
            DroidSystem.ShellSync($@"setprop persist.sys.timezone {timezone}");
            Shell("reboot");
        }

        /// <summary>
        /// Sends a Shell command
        /// </summary>
        /// <param name="command">The command to execute.</param>
        [RemoteCommand(hidden: true)]
        public void Shell(string command)
        {
            OnNotify(DroidSystem.ShellSync(command));
        }

        /// <summary>
        /// Taps the screen in a given location (x,y)
        /// </summary>
        /// <param name="x">The x location on the screen.</param>
        /// <param name="y">The y location on the screen.</param>
        public void Tap(int x, int y)
        {
            Shell($"input tap {x} {y}");
        }

        /// <summary>
        /// Gets the tempature of a given thermal_zone
        /// </summary>
        /// <param name="zone">The thermal zone you want to check.</param>
        /// <returns>The tempature of a given thermal_zone</returns>
        public string ThermalZoneTemperature(string zone)
        {
            var temperature = DroidSystem.ShellSync($"cat sys/class/thermal/thermal_zone{zone}/temp");
            //SubCLogger.Instance.Write(temperature + "," + DateTime.Now, "temperature.csv", LogDirectory);
            return temperature;
        }

        /// <summary>
        /// Gets the <see cref="Guid"/> that represents the SD card in the Rayfin.
        /// </summary>
        /// <returns>The <see cref="Guid"/> that represents the SD card in the Rayfin.</returns>
        private static Guid GetStoragePoint()
        {
            var folders = DroidSystem.ShellSync($@"ls {StorageLocation}").Split('\n');

            foreach (string folder in folders)
            {
                if (Guid.TryParse(folder, out Guid result))
                {
                    return result;
                }
            }

            throw new System.IO.IOException("Could not find storage mount");
        }

        /// <summary>
        /// Executes the Top command to get CPU usage.  TODO:  This can probably be replaced by <see cref="DroidSystem.ShellSync(string, int)"/>
        /// </summary>
        /// <returns>CPU usage</returns>
        private string ExecuteTop()
        {
            Process p = null;
            BufferedReader i = null;
            string returnString = null;
            try
            {
                p = Runtime.GetRuntime().Exec("top -n 1");
                i = new BufferedReader(new InputStreamReader(p.InputStream));
                while (string.IsNullOrEmpty(returnString))
                {
                    returnString = i.ReadLine();
                }
            }
            catch
            {
                // Log.e("executeTop", "error in getting first line of top");
                // e.printStackTrace();
            }
            finally
            {
                try
                {
                    i.Close();
                    p.Destroy();
                }
                catch
                {
                    // Log.e("executeTop",
                    //        "error in closing and destroying top process");
                    // e.printStackTrace();
                }
            }

            return returnString;
        }

        /// <summary>
        /// Gets OS state.
        /// </summary>
        private void InitializeState()
        {
            Task.Run(() =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                MAC = GetMACAddress();
                HostName = DroidSystem.ShellSync("getprop net.hostname");
                var romString = DroidSystem.ShellSync("getprop rayfin.rom.version").TrimEnd();
                var match = Regex.Match(romString, @".?(\d+\.\d+(\.\d+)?)");
                StorageKey = GetStorageKey();
                TimeZone = Android.Icu.Util.TimeZone.Default.DisplayName;
                IP = SubCTools.Helpers.Ethernet.GetIP();
                RomVersion = match.Success ? match.Groups[1].ToString() : "1.0";
                SerialNumber = DroidSystem.ShellSync("getprop rayfin.serial");
                VideoMode = DroidSystem.ShellSync("getprop rayfin.video.mode");
                // linkLocalAddress = (RomVersion == "1.0") ? string.Empty : NetworkTools.ValidateIP(DroidSystem.ShellSync("ifconfig eth0:ll | grep \"inet addr\" | cut -c21-36").Split(' ')[0]);
                ProductID = ShellSync("getprop rayfin.product.id");
                var lensType = ShellSync("getprop rayfin.lens").TrimEnd();

                if (!string.IsNullOrEmpty(lensType) && System.Enum.TryParse(lensType, out LensType type))
                {
                    LensType = type;
                }

                cameraType = RayfinLicense.FetchCameraType();
            });
        }

        private void OnNotify(string message, MessageTypes messageType = MessageTypes.Information)
        {
            Notify?.Invoke(this, new NotifyEventArgs(message, messageType));
        }

        /// <summary>
        /// Checks the tempature of the CPU to see if it has changed since the last measurement.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/></param>
        private void RefreshState(object sender, ElapsedEventArgs e)
        {
            CPUTemp = Convert.ToDouble(ThermalZoneTemperature("6"));

            if (Convert.ToInt32(CPUTemp) != lastRecordedTemperature)
            {
                lastRecordedTemperature = Convert.ToInt32(CPUTemp);
                OnNotify($"{nameof(CPUTemp)}:{CPUTemp}");
            }

            if ((CPUTemp / 10) > (int)TempWarningRange.Upper && currentGovernor != throttlingGovernor)
            {
                SetGovernor(throttlingGovernor);
            }
            else if ((CPUTemp / 10) < (int)TempWarningRange.Lower && currentGovernor != Governor.Performance)
            {
                SetGovernor(Governor.Performance);
            }

            // DHCPAddress = (RomVersion == "1.0") ? string.Empty : NetworkTools.ValidateIP(DroidSystem.ShellSync("ifconfig eth0 | grep \"inet addr\" | cut -c21-36").Split(' ')[0]);
        }

        /// <summary>
        /// Sets the timezone to the default.
        /// </summary>
        private void ResetTimeZone() => DroidSystem.ShellSync($@"setprop persist.sys.timezone {DefaultTimeZone}");

        /// <summary>
        /// Evaluate the network link speed
        /// </summary>
        private void UpdateNetworkLinkSpeed()
        {
            Task.Run(() =>
            {
                var speedString = ShellSync(@"cat /sys/class/net/eth0/speed").TrimEnd();

                if (int.TryParse(speedString, out int result))
                {
                    NetworkLinkSpeed = result;
                }
            });
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="GrenadierManager.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Managers
{
    using Android.App;
    using Android.Content;
    using Android.Content.Res;
    using Android.Hardware.Camera2;
    using Android.Hardware.Usb;
    using Android.Net.Nsd;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Communicators;
    using SubCTools.Converters.JsonConverters;
    using SubCTools.Droid.Camera;
    using SubCTools.Droid.Converters;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Droid.IO.Sensors;
    using SubCTools.Droid.Listeners;
    using SubCTools.Droid.Rtsp;
    using SubCTools.Enums;
    using SubCTools.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Timers;

    public class GrenadierManager
    {
        /// <summary>
        /// Matches our semantic version
        /// </summary>
        private const string VersionPattern = @"(.*\s)?v?(\d+\.\d+(\.\d+)?)[\s-]?(\w+)?";

        private readonly Activity activity;

        private readonly Sensors gyro;

        /// <summary>
        /// Preview for displaying live video
        /// </summary>
        private readonly AutoFitTextureView preview;

        private EthernetManager ethernetManager;

        private IPAddress ipAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="GrenadierManager"/> class.
        /// </summary>
        /// <param name="activity">Activty used to pass to some object that require it</param>
        /// <param name="context">Context used to pass to some object that require it</param>
        /// <param name="preview">Preview to display video</param>
        /// <param name="cameraManager">Camera manager to retrieve camera device</param>
        /// <param name="rtspServer">The RTSP server that will manager the network stream</param>
        public GrenadierManager(
            Activity activity,
            Context context,
            AutoFitTextureView preview,
            CameraManager cameraManager,
            RtspServer rtspServer)
        {
            this.activity = activity;
            this.preview = preview;
            CameraManager = cameraManager;
            ParseVersion(DroidSystem.Instance.Version, out var version, out var environment);

            var assets = activity.Assets;
            Features = new FeatureToggler(assets.Open("FeatureToggles.xml"), environment);

            // create the sewttings data base to be used for the entire application
            Settings = new SqlSettings(context, Path.Combine(
                        Android.OS.Environment.GetExternalStoragePublicDirectory(string.Empty).AbsolutePath,
                        "Settings",
                        "Settings.db"));

            // power controller to stop recording and turn off the leds
            PowerController = new PowerController(Settings, new Shell());

            // create the interpreter
            Interpreter = new CommandInterpreter();

            // set up serial comms
            SerialManager = new SerialManager(
                activity,
                new DeviceListener(activity),
                activity.GetSystemService(Android.Content.Context.UsbService) as UsbManager,
                Interpreter,
                Settings,
                PowerController);

            var thermalManager = new ThermalManager(new SubCGPIO(62));

            var videoController = new VideoController(Settings);
            videoController.LoadSettings();

            var powerListener = new PowerListener(context);

            // configure gyro
            var serial = SerialManager.Serial;
            gyro = new Sensors(serial, Settings);

            Interpreter.Register(gyro);
            MessageRouter.Instance.Add(gyro);

            // register the odd objects
            Interpreter.Register(
                DroidSystem.Instance,
                PowerController,
                Locale.Instance,
                rtspServer,
                videoController,
                SerialManager,
                powerListener);

            MessageRouter.Instance.Add(
                DroidSystem.Instance,
                Interpreter,
                rtspServer,
                videoController,
                Locale.Instance,
                SerialManager,
                powerListener);

            SerialManager.FirmwareReceived += SerialManager_FirmwareReceived;

            MessageIOC.Instance.Add(MessageTypes.CameraCommand | MessageTypes.Internal, Interpreter);
            MessageIOC.Instance.Add(MessageTypes.Information, thermalManager);

            Locale.Instance.LoadSettings();

            var ipTimer = new Timer() { Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };
            ipTimer.Elapsed += IpTimer_Elapsed;
            ipTimer.Start();

            CreateCamera();
        }

        /// <summary>
        /// Gets base camera object to perform camera functions
        /// </summary>
        public Grenadier Camera { get; private set; }

        /// <summary>
        /// Gets the camera info
        /// </summary>
        public CameraInfo CameraInfo { get; private set; }

        /// <summary>
        /// Gets camera manager to retrieve camera device
        /// </summary>
        public CameraManager CameraManager { get; }

        /// <summary>
        /// Gets an object that's used to evaluate feature toggles
        /// </summary>
        public FeatureToggler Features { get; }

        /// <summary>
        /// Gets command interpreter to take incoming data and excute
        /// </summary>
        public CommandInterpreter Interpreter { get; }

        /// <summary>
        /// Gets power controller to shut things down when power is lost
        /// </summary>
        public PowerController PowerController { get; }

        /// <summary>
        /// Gets the serial manager
        /// </summary>
        public SerialManager SerialManager { get; }

        /// <summary>
        /// Gets settings service to hold all settings to save
        /// </summary>
        public ISettingsService Settings { get; }

        /// <summary>
        /// Gets Static IP manager to manage assigning static IP
        /// </summary>
        public StaticIPManager StaticIPManager { get; private set; }

        /// <summary>
        /// Update the camera info on the service discovery object when the ip changes
        /// </summary>
        public void UpdateCameraInfo()
        {
            var ip = SubCTools.Helpers.Ethernet.GetIP();
            if (ipAddress.ToString() != (ip ?? string.Empty))
            {
                if (IPAddress.TryParse(ip, out var i))
                {
                    ipAddress = i;
                    ethernetManager.UpdateCameraInfo(GetCameraInfo());
                }
            }
        }

        /// <summary>
        /// Create the camera object
        /// </summary>
        protected virtual void CreateCamera()
        {
            Camera = new Grenadier(CameraManager, preview, Settings);
            Camera.Initialized += (s, e) => Camera_Initialized();
        }

        /// <summary>
        /// Register all the camera components with the interpreter
        /// </summary>
        private void Camera_Initialized()
        {
            // take care of all the ethernet communication registration
            // var ethernetManager = new EthernetManager(activity.GetSystemService(Context.NsdService) as NsdManager, Settings);
            StaticIPManager = new StaticIPManager(Settings);
            StaticIPManager.LoadSettings();
            StaticIPManager.ConfigureStaticIP();

            Interpreter.Register(
                Camera,
                Camera.Lens,
                Camera.ExposureSettings,
                StaticIPManager,
                this);

            MessageRouter.Instance.Add(
                StaticIPManager,
                Camera,
                Camera.Lens,
                Camera.ExposureSettings);

            // if the camera's initialized and you have the teensy fimware, start the ethernet manager
            // otherwise wait until you get the teensy firmware
            if (!string.IsNullOrEmpty(SerialManager.Teensy.FirmwareVersion))
            {
                CreateEthernetManager();
            }
        }

        private void CreateEthernetManager()
        {
            if (ethernetManager != null)
            {
                return;
            }

            ipAddress = IPAddress.Parse(DroidSystem.Instance.IP);

            CameraInfo = GetCameraInfo();

            // take care of all the ethernet communication registration
            ethernetManager = new EthernetManager(activity.GetSystemService(Context.NsdService) as NsdManager, Settings, CameraInfo, Features);
        }

        /// <summary>
        /// Construct the camera info based on all the camera properties
        /// </summary>
        /// <returns>New camera info</returns>
        private CameraInfo GetCameraInfo()
        {
            ParseVersion(DroidSystem.Instance.Version, out var version, out var environment);
            return new CameraInfo
            {
                Name = DroidSystem.ShellSync("getprop net.hostname").TrimEnd(),
                Nickname = Camera.Nickname,
                FirmwareVersion = new Version(Regex.Match(SerialManager.Teensy.FirmwareVersion, @"v(\d+.\d+)").Groups[1].Value),
                RomVersion = new Version(DroidSystem.Instance.RomVersion),
                CameraType = DroidSystem.Instance.CameraType,
                Version = version,
                DeploymentEnvironment = environment,
                TCPAddress = new EthernetAddress(IPAddress.Parse(DroidSystem.Instance.IP), 8888),
                UDPAddress = new EthernetAddress(IPAddress.Parse(DroidSystem.Instance.IP), 8887)
            };
        }

        private void IpTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateCameraInfo();
        }

        private bool ParseVersion(string versionString, out Version version, out DeploymentEnvironments environment)
        {
            version = new Version();
            environment = DeploymentEnvironments.Production;

            var match = Regex.Match(versionString, VersionPattern);
            if (!match.Success)
            {
                environment = DeploymentEnvironments.Dev;
                return false;
            }

            if (!Version.TryParse(match.Groups[2].Value, out version))
            {
                return false;
            }

            var suffix = match.Groups[4].Value.ToLower();

            if (suffix == "a")
            {
                environment = DeploymentEnvironments.Alpha;
            }
            else if (suffix == "b")
            {
                environment = DeploymentEnvironments.Beta;
            }
            else if (suffix == "rc")
            {
                environment = DeploymentEnvironments.ReleaseCandidate;
            }
            else if (match.Groups[3].Value == string.Empty)
            {
                environment = DeploymentEnvironments.Dev;
            }
            else
            {
                environment = DeploymentEnvironments.Production;
            }

            return true;
        }

        private void SerialManager_FirmwareReceived(object sender, string e)
        {
            gyro.LoadSettings();

            // wait until the camera is initialized before creating the ethernet manager when you get the teensy firmware
            if (Camera.IsInitialized)
            {
                CreateEthernetManager();
            }
        }
    }

    public class Logger : ILogger
    {
        public async Task LogAsync(string data)
        {
            SubCLogger.Instance.Write(data, directory: Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies) + "/Log/");
        }

        public async Task LogAsync(string data, FileInfo file)
        {
            SubCLogger.Instance.Write(data, file.Name, file.DirectoryName);
        }
    }

    public class Shell : IShell
    {
        public string ShellSync(string command) => DroidSystem.ShellSync(command);
    }
}
namespace SubCTools.Droid.Managers
{
    using Android.App;
    using Android.Content;
    using Android.Hardware.Usb;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Droid.Helpers;
    using SubCTools.Droid.IO;
    using SubCTools.Droid.IO.AuxDevices;
    using SubCTools.Droid.Lenses;
    using SubCTools.Droid.Listeners;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    /// <summary>
    /// Hawndle all the serial set up
    /// </summary>
    public class SerialManager : INotifier
    {
        /// <summary>
        /// Low level serial communication and connection
        /// </summary>

        /// <summary>
        /// The USB product ID for the Teensy 3.2 in the Rayfin
        /// </summary>
        private const int Teensy32ProductID = 1144;

        /// <summary>
        /// The USB vendor ID for the Teensy 3.2 in the Rayfin
        /// </summary>
        private const int Teensy32VendorID = 5824;

        /// <summary>
        /// The system property that handles setting the teensy mode.
        /// </summary>
        private const string TeensyModeKey = "rayfin.teensy.mode";

        /// <summary>
        /// Device listener to connect to the usb serial connection
        /// </summary>
        private readonly DeviceListener deviceListener;

        /// <summary>
        /// Interpreter for doing sync commands from teensy
        /// </summary>
        private readonly CommandInterpreter interpreter;

        private readonly ISettingsService settings;
        private bool skateWasOn = false;

        public SerialManager(
            Activity activity,
            DeviceListener deviceListener,
            UsbManager usbManager,
            CommandInterpreter interpreter,
            ISettingsService settings,
            PowerController powerController)
        {
            this.interpreter = interpreter;
            this.settings = settings;

            // listen to the devices that are attached and detached, connect to serial when attached
            this.deviceListener = deviceListener;

            var teensyTimer = new Timer()
            {
                AutoReset = true,
                Interval = 4700
            };

            teensyTimer.Elapsed += (s, e) => Check_TeensyMode();

            teensyTimer.Start();

            Serial = new AndroidSerial(activity, usbManager) { Append = "\n" };

            deviceListener.DeviceAttached += (s, e) =>
            {
                if (e.ProductName?.ToLower().Contains("serial") == true)
                {
                    ConnectSerial();
                }
            };

            deviceListener.DeviceDetached += DeviceListener_DeviceDetached;

            LensController = new AndroidSerial(activity, usbManager) { Append = "\n" };

            // get all the information about the teensy
            Teensy = new SubCTeensy(
                Serial,
                settings);

            Serial.IsConnectedChanged += Serial_IsConnectedChanged;

            // listen for when the aux devices changes so you can add and remove them from the message router and interpreter
            Teensy.DeviceChanged += AuxDevices_DeviceChanged;

            Teensy.WhisperReceived += Teensy_WhisperReceived;

            MessageRouter.Instance.Add(Teensy, Teensy.AuxManager, Teensy.ExpansionInput, Teensy.ExpansionOutput);

            MessageIOC.Instance.Add(MessageTypes.TeensyCommand | MessageTypes.Alert, Teensy);

            interpreter.Register(
                Teensy,
                Teensy.AuxManager,
                Teensy.ExpansionInput,
                Teensy.ExpansionOutput,
                Teensy.ExpansionPort);

            Teensy.FirmwareReceived += (s, e) => FirmwareReceived?.Invoke(this, e);

            powerController.ShuttingDown += PowerController_ShuttingDown;
            powerController.ShutdownCancelled += PowerController_ShutDownCancelled;
            // try to connect to serial right away in case it's already been detected
            ConnectSerial();
        }

        public event EventHandler<string> FirmwareReceived;

        public event EventHandler<NotifyEventArgs> Notify;

        public AndroidSerial LensController { get; private set; }
        public AndroidSerial Serial { get; private set; }
        public SubCTeensy Teensy { get; }

        /// <summary>
        /// Turns the breaker off
        /// </summary>
        [RemoteCommand]
        public void BOff()
        {
            Notify?.Invoke(this, new NotifyEventArgs("BreakerOff", MessageTypes.CameraCommand));
        }

        private void AuxDevices_DeviceChanged(object sender, AuxDeviceArgs e)
        {
            // add and remove the aux devices as they change
            if (e.OldDevice != null)
            {
                MessageRouter.Instance.Remove(e.OldDevice);
                interpreter.Unregister(e.OldDevice);

                // the controller needs to be added to the message ioc
                if (e.OldDevice is INotifiable n)
                {
                    MessageIOC.Instance.Remove(MessageTypes.Information | MessageTypes.Aux, n);
                }
            }
            if (e.NewDevice != null)
            {
                MessageRouter.Instance.Add(e.NewDevice);
                interpreter.Register(e.NewDevice);

                if (e.NewDevice is INotifiable n)
                {
                    MessageIOC.Instance.Add(MessageTypes.Information | MessageTypes.Aux, n);
                }
            }
        }

        /// <summary>
        /// Checks the system prop to get the mode of the teensy, if it
        /// is set to bootloader reboot the teensy into bootloader mode
        /// </summary>
        private void Check_TeensyMode()
        {
            var teensyMode = DroidSystem.GetProp(TeensyModeKey);

            if (Serial.IsConnected && teensyMode.Equals("Bootloader", StringComparison.InvariantCultureIgnoreCase)) //TODO: Find a way to check if the teensy is already in bootloader mode?
            {
                DroidSystem.SetProp(TeensyModeKey, "Firmware");
                Serial.SendAsync("~product reset device");
            }
        }

        /// <summary>
        /// Connect to the serial device when t's detected
        /// </summary>
        private async void ConnectSerial()
        {
            var ports = await Serial.GetPortsAsync();

            if (!((ports?.Any() ?? false) && !Serial.IsConnected))
            {
                return;
            }

            await Serial.ConnectAsync(new AndroidCommunicatorAddress(ports.FirstOrDefault(), 6000000));
        }

        /// <summary>
        /// Disconnect from the serial device if it ever becomes detached
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceListener_DeviceDetached(object sender, UsbDevice e)
        {
            //if ((e.ProductId.Equals(Teensy32ProductID) && e.VendorId.Equals(Teensy32VendorID)) == false ||
            //    e.ProductName?.ToLower().Contains("serial") == false)
            //{
            //    return;
            //}

            if (e.VendorId.Equals(Teensy32VendorID))
            {
                Serial.DisconnectAsync();
            }
        }

        private void PowerController_ShutDownCancelled(object sender, EventArgs e)
        {
            //if (skateWasOn)
            //{
            Notify?.Invoke(this, new NotifyEventArgs("LoadSkateSettings", MessageTypes.CameraCommand));
            //}
        }

        /// <summary>
        /// Turn off the lamp on any connected leds and set the strobe output to zero when the camera is shutting down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerController_ShuttingDown(object sender, EventArgs e)
        {
            Notify?.Invoke(this, new NotifyEventArgs("BreakerOff", MessageTypes.CameraCommand));
            Notify?.Invoke(this, new NotifyEventArgs("LampPower:0", MessageTypes.CameraCommand));
            //var skate = Teensy.AuxManager.AuxDevices.Where(d => d.Value.GetType() == typeof(Skate)).FirstOrDefault().Value;
            //if (skate != null)
            //{
            //    if (skateWasOn = ((Skate)skate).IsOn)
            //    {
            //        Notify?.Invoke(this, new NotifyEventArgs("LaserOff", MessageTypes.CameraCommand));
            //    }
            //}

            Teensy.StrobeOutput(0);
        }

        private void Serial_IsConnectedChanged(object sender, bool e)
        {
            Teensy.LoadSettings();
        }

        private async void Teensy_WhisperReceived(object sender, string e)
        {
            var result = await interpreter.Interpret(e);

            if (string.IsNullOrEmpty(result.Message))
            {
                return;
            }

            var data = SubCTeensy.TeensyStartCharacter + result.Message;
            Teensy.Send(data);
        }
    }
}
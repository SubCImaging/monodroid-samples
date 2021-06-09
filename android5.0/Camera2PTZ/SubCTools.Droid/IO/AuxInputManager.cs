namespace SubCTools.Droid.IO
{
    using SubCTools.Attributes;
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Droid.IO.AuxDevices;
    using SubCTools.Extensions;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AuxInputManager : DroidBase
    {
        private const string AquoreaMk3 = "Aquorea Mk3";

        /// <summary>
        /// Controller tag
        /// </summary>
        private const string Controller = nameof(Controller);

        private const string KongsbergPanTilt = "Kongsberg Pan Tilt";

        private const string MantaRay = "Manta Ray";
        private const string NoDevice = "No Device";
        private const string PanTiltEmulator = "Pan-Tilt Emulator";
        private const string RS232 = nameof(RS232);

        private const string RS485 = nameof(RS485);

        private const string Skate = nameof(Skate);
        private const string SkateMk2 = "Skate Mk2";
        private readonly int detectDelay = 1000;

        /// <summary>
        /// Serial to send information to and from
        /// </summary>
        private readonly AndroidSerial serial;

        /// <summary>
        /// The teensy listener for getting aux messages
        /// </summary>
        private readonly TeensyListener teensyListener;

        public AuxInputManager(
                    AndroidSerial serial,
            ISettingsService settings,
            TeensyListener teensyListener)
            : base(settings)
        {
            this.serial = serial;
            this.teensyListener = teensyListener;
        }

        /// <summary>
        /// Event to call when the device changes
        /// </summary>
        public event EventHandler<AuxDeviceArgs> DeviceChanged;

        /// <summary>
        /// Gets aux devices that are in use
        /// </summary>
        public Dictionary<int, AuxDevice> AuxDevices { get; }
            = new Dictionary<int, AuxDevice>();

        /// <summary>
        /// Gets the list of configured aux inputs
        /// </summary>
        public Dictionary<int, AuxInput> AuxInputs { get; }
            = new Dictionary<int, AuxInput>() { { 0, new AuxInput { Input = 0, Device = NoDevice } }, { 1, new AuxInput { Input = 1, Device = NoDevice } } };

        /// <summary>
        /// Set each aux port to default no device
        /// </summary>
        public async void AllToDefault()
        {
            for (int i = 0; i < 2; i++)
            {
                await ToDefault(i);
                await Task.Delay(TimeSpan.FromMilliseconds(detectDelay));
            }
        }

        /// <summary>
        /// Get the aux status
        /// </summary>
        /// <param name="input"> Aux input to get property </param>
        [RemoteCommand]
        [Alias("AuxStatus", "GetAuxStatus")]
        public string GetStatus(int input)
            => AuxInputs.ContainsKey(input) ? AuxInputs[input].ToString() : string.Empty;

        /// <summary>
        /// Load the aux devices from the settings file
        /// </summary>
        public override async void LoadSettings()
        {
            base.LoadSettings();

            // load the aux settings if they were previously configured
            var auxSettings = Settings.LoadAll().Where(s => s.Name.StartsWith("Aux"));

            if (auxSettings.Any())
            {
                foreach (var item in auxSettings)
                {
                    if (!item.Attributes.ContainsKey("input"))
                    {
                        continue;
                    }

                    var auxInput = new AuxInput()
                    {
                        Input = int.Parse(item.Attributes["input"]),
                        Device = item.Attributes[nameof(AuxInput.Device).ToLower()],
                        Standard = item.Attributes[nameof(AuxInput.Standard).ToLower()],
                        Baud = int.Parse(item.Attributes[nameof(AuxInput.Baud).ToLower()])
                    };

                    // don't overwrite a detected device when you load from settings no need to load
                    // NoDevice, it's already set to default
                    if (AuxInputs[auxInput.Input].Device == NoDevice
                        && auxInput.Device != NoDevice)
                    {
                        AuxInputs.Update(auxInput.Input, auxInput);
                        await ConfigureInput(auxInput);
                    }
                }
            }
        }

        /// <summary>
        /// Set the baud rate of the input
        /// </summary>
        /// <param name="input"> Aux input to set baud </param>
        /// <param name="baud"> Baud rate </param>
        [RemoteCommand]
        [Alias("DriverBaud", "Baud")]
        public void SetBaud(int input, int baud)
        {
            if (baud != 9600
                && baud != 115200)
            {
                OnNotify($"{baud} is invalid. Please select a valid baud.", MessageTypes.Error);
                return;
            }

            var auxInfo = AuxInputs.ContainsKey(input) ? AuxInputs[input] : new AuxInput();//AuxInputs.First(a => a.Input == input);
            auxInfo.Baud = baud;

            Settings.Update("Aux" + input, attributes: new Dictionary<string, string>()
            {
                { nameof(AuxInput.Input).ToLower(), input.ToString()},
                { nameof(AuxInput.Device).ToLower(), auxInfo.Device},
                { nameof(AuxInput.Standard).ToLower(), auxInfo.Standard},
                { nameof(AuxInput.Baud).ToLower(), baud.ToString()}
            });

            OnNotify($"Baud{input}:{baud}");

            Set(input, "driverbaud", baud.ToString());
        }

        /// <summary>
        /// Set the teensy aux input to the corresponding given device
        /// </summary>
        /// <param name="input"> Input to configure </param>
        /// <param name="device"> Device to set </param>
        [RemoteCommand]
        [Alias("Device")]
        public async Task SetDevice(int input, string device)
        {
            // restrict bad input devices
            if (device != Controller
                && device != AquoreaMk3
                && device != NoDevice
                && device != "ROS Pan and Tilt"
                && device != PanTiltEmulator
                && device != Skate
                && device != SkateMk2
                && device != KongsbergPanTilt
                && device != "Manta Ray")
            {
                OnNotify($"{device} is invalid. Please select a valid device.", MessageTypes.Error);
                return;
            }

            // get the device from the list to get it's information and to update its device
            var auxInfo = AuxInputs.ContainsKey(input) ? AuxInputs[input] : new AuxInput();
            auxInfo.Device = device;

            if (device == Skate)
            {
                auxInfo.Standard = RS232;
                auxInfo.Baud = 115200;
            }
            else if (device == SkateMk2)
            {
                auxInfo.Standard = RS485;
                auxInfo.Baud = 9600;
            }
            else if (device == AquoreaMk3)
            {
                auxInfo.Standard = RS485;
                auxInfo.Baud = 115200;
            }
            else if (device == KongsbergPanTilt)
            {
                auxInfo.Standard = RS485;
                auxInfo.Baud = 9600;
            }

            // update the settings file with the new information
            Settings.Update(
                "Aux" + input,
                attributes: new Dictionary<string, string>()
                {
                    { nameof(AuxInput.Input).ToLower(), input.ToString() },
                    { nameof(AuxInput.Device).ToLower(), device},
                    { nameof(AuxInput.Standard).ToLower(), auxInfo.Standard},
                    { nameof(AuxInput.Baud).ToLower(), auxInfo.Baud.ToString()},
                });

            // convert the device in to the corresponding int to send to teensy
            await SetDeviceSync(input, DeviceToInt(auxInfo.Device));
            await SetStandardSync(input, StandardToInt(auxInfo.Standard));
            await SetBaudSync(input, auxInfo.Baud);

            var deviceChangedArgs = new AuxDeviceArgs
            {
                OldDevice = AuxDevices.ContainsKey(input) ? AuxDevices[input] : null,
                NewDevice = StringToAuxDevice(device, input, serial, teensyListener, Settings),
                Input = input
            };

            deviceChangedArgs.NewDevice?.LoadSettings();

            // update the dictionary of objects so we know what the previous device was when it changes
            AuxDevices.Update(input, deviceChangedArgs.NewDevice);

            OnNotify($"Device{input}:{auxInfo.Device}");

            DeviceChanged?.Invoke(this, deviceChangedArgs);
        }

        /// <summary>
        /// Set the serial standard, e.g. 232 or 485
        /// </summary>
        /// <param name="input"> Input to configure </param>
        /// <param name="standard"> Mode of protocol to use </param>
        [RemoteCommand]
        [Alias("DriverMode", "Protocol", "Standard")]
        public void SetStandard(int input, string standard)
        {
            if (standard != RS232 && standard != RS485)
            {
                OnNotify($"{standard} is invalid. Please select a valid serial standard.", MessageTypes.Error);
                return;
            }

            var auxInfo = AuxInputs.ContainsKey(input) ? AuxInputs[input] : new AuxInput();//AuxInputs.First(a => a.Input == input);

            auxInfo.Standard = standard;

            Settings.Update("Aux" + input, attributes: new Dictionary<string, string>()
            {
                { nameof(AuxInput.Input).ToLower(), input.ToString()},
                { nameof(AuxInput.Device).ToLower(), auxInfo.Device},
                { nameof(AuxInput.Standard).ToLower(), standard},
                { nameof(AuxInput.Baud).ToLower(), auxInfo.Baud.ToString()}
            });

            OnNotify($"Standard{input}:{standard}");

            SetStandard(input, StandardToInt(standard));
        }

        /// <summary>
        /// Set the given aux port to default settings
        /// </summary>
        /// <param name="input"> Aux input to set to default </param>
        /// <returns> Task </returns>
        public async Task ToDefault(int input)
        {
            SetStandard(input, RS485);
            SetBaud(input, 115200);
            await SetDevice(input, NoDevice);
        }

        /// <summary>
        /// Get the int representation of the device
        /// </summary>
        /// <param name="device"> Device to get int </param>
        /// <returns> Int representing device </returns>
        private static int DeviceToInt(string device)
        {
            switch (device)
            {
                case Controller:
                    return 8;

                case NoDevice:
                    return 0;

                case "ROS Pan and Tilt":
                    return 6;

                case Skate:
                case SkateMk2:
                case MantaRay:
                    return 3;

                default:
                    return 4;
            }
        }

        /// <summary>
        /// Get the int that represents the given protocol
        /// </summary>
        /// <param name="standard"> Protocol to get int for </param>
        /// <returns> 0 if the protocol contains 232, 1 if not </returns>
        private static int StandardToInt(string standard) => (standard ?? string.Empty).Contains("232") ? 0 : 1;

        /// <summary>
        /// Make a new aux device object for the given device name
        /// </summary>
        /// <param name="device"> Device to make an object for </param>
        /// <param name="input"> Input the device is connected </param>
        /// <param name="serial"> Serial device used for communication </param>
        /// <param name="teensyListener"> Teensy listener to get packaged data from </param>
        /// <returns> New object for given device type </returns>
        private static AuxDevice StringToAuxDevice(
            string device,
            int input,
            AndroidSerial serial,
            TeensyListener teensyListener,
            ISettingsService settings)
        {
            switch (device)
            {
                case AquoreaMk3:
                    return new AquoreaMk3(serial, input, teensyListener, settings);

                case Controller:
                    return new Controller(serial, input, teensyListener);

                case "ROS Pan and Tilt":
                    return new ROSPT(serial, input, teensyListener);

                case KongsbergPanTilt:
                    return new KongsbergPanTiltAux(serial, input, teensyListener);

                case PanTiltEmulator:
                    return new ForumPanTilt(serial, input, teensyListener);

                case Skate:
                case SkateMk2:
                case MantaRay:
                    return new Skate(serial, input, teensyListener, settings);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Send the device information to the teensy
        /// </summary>
        /// <param name="input"> Input information to send to the teensy </param>
        private async Task ConfigureInput(AuxInput input)
        {
            await SetDevice(input.Input, input.Device);
            await SetStandardSync(input.Input, StandardToInt(input.Standard));
            await SetBaudSync(input.Input, input.Baud);
        }

        /// <summary>
        /// Set the value of the given property on the given aux input
        /// </summary>
        /// <param name="input"> Aux input to set property </param>
        /// <param name="property"> Property to set </param>
        /// <param name="value"> Value of property </param>
        private async void Set(int input, string property, string value)
        {
            await serial.SendAsync($"{SubCTeensy.TeensyStartCharacter}aux{input} set {property}:{value}");
        }

        private Task SetBaudSync(int input, int baud) => SetSync(input, "driverbaud", baud.ToString());

        /// <summary>
        /// Set the device on the aux port
        /// </summary>
        /// <param name="auxInput"> Aux input to change device </param>
        /// <param name="device"> Device to set </param>
        private void SetDevice(int auxInput, int device) => Set(auxInput, "device", device.ToString());

        private Task SetDeviceSync(int auxInput, int device) => SetSync(auxInput, "device", device.ToString());

        /// <summary>
        /// Set the drive mode of the aux input
        /// </summary>
        /// <param name="input"> Aux input to set mode </param>
        /// <param name="standard"> Mode to set device </param>
        private void SetStandard(int input, int standard)
        {
            Set(input, "drivermode", standard.ToString());
        }

        private Task SetStandardSync(int auxInput, int standard) => SetSync(auxInput, "drivermode", standard.ToString());

        private Task SetSync(int input, string property, string value)
            => serial.SendSync(new CommunicationData($"{SubCTeensy.TeensyStartCharacter}aux{input} set {property}:{value}", @"(.+)\n"));
    }
}
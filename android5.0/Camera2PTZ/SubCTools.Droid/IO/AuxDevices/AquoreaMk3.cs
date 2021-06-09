// <copyright file="AquoreaMk3.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace SubCTools.Droid.IO.AuxDevices
{
    using SubCTools.Attributes;
    using SubCTools.Communicators.DataTypes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Enums;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Settings.Interfaces;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Class for Aquorea Mk3's.
    /// </summary>
    public class AquoreaMk3 : Aquorea
    {
        /// <summary>
        /// The value to set the lamp if strobe is enabled.
        /// </summary>
        private const int StrobeDimBrightness = 1;

        /// <summary>
        /// Settings for holding the addresses
        /// </summary>
        private readonly ISettingsService settings;

        private int lampBrightness;

        private string previousState;
        private int productSerialNumber;
        private string productState = "Unknown";

        /// <summary>
        /// Serial addresses to send to specific aquoreas
        /// </summary>
        private string serialAddress = string.Empty;

        private int strobeBrightness;
        private StrobeStates strobeState = StrobeStates.Ready;
        private bool wasStrobeEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="AquoreaMk3" /> class.
        /// </summary>
        /// <param name="serial"> Base serial object. </param>
        /// <param name="input"> Aux input the light is connected. </param>
        /// <param name="listener"> Teensy listener for passing through parsed data. </param>
        /// <param name="settings"> Settings for holding the led state. </param>
        public AquoreaMk3(
            AndroidSerial serial,
            int input,
            TeensyListener listener,
            ISettingsService settings)
            : base(serial, input, listener) => this.settings = settings;

        /// <summary>
        /// Gets or sets the brightness of the lamp
        /// </summary>
        [RemoteState(true)]
        public int LampBrightness
        {
            get => lampBrightness;
            set
            {
                lampBrightness = value;
                OnNotify($"{nameof(LampBrightness)}:{LampBrightness}");
                settings.Update("AquoreaMk3_" + input, value);
            }
        }

        /// <summary>
        /// Gets the product state.
        /// </summary>
        [RemoteState]
        public string ProductState
        {
            get => productState;
            private set
            {
                productState = value;
                OnNotify($"{nameof(ProductState)}:{value}");

                if (ProductState == "OVERTEMPERATURE")
                {
                    previousState = "O";
                    wasStrobeEnabled = IsStrobeEnabled;
                    DisableStrobe();
                }
                else if (previousState == "O" && wasStrobeEnabled)
                {
                    previousState = string.Empty;
                    EnableStrobe();
                }
            }
        }

        /// <summary>
        /// Gets or sets serial addresses to send data
        /// </summary>
        [RemoteState]
        public string SerialAddress
        {
            get => serialAddress;
            set
            {
                serialAddress = value;
                settings.Update(nameof(SerialAddress), value);
            }
        }

        /// <summary>
        /// Gets or sets the strobe brightness.
        /// </summary>
        [RemoteState(true)]
        public int StrobeBrightness
        {
            get => strobeBrightness;
            set
            {
                strobeBrightness = value;
                OnNotify($"{nameof(StrobeBrightness)}:{StrobeBrightness}");
                settings.Update($"AquoreaMk3_{input}StrobeBrightness", value);
            }
        }

        /// <summary>
        /// Gets the current strobe state.
        /// </summary>
        [RemoteState]
        public StrobeStates StrobeState
        {
            get => strobeState;
            private set
            {
                strobeState = value;
                OnNotify($"{nameof(StrobeState)}:{value}");
            }
        }

        /// <summary>
        /// Clear the serial address
        /// </summary>
        [RemoteCommand]
        [Alias("ResetSerialAddress")]
        public void ClearSerialAddress()
        {
            SerialAddress = string.Empty;
            OnNotify($"{nameof(SerialAddress)}:{SerialAddress}");
        }

        /// <summary>
        /// If the lamp brightness is at 1% to allow strobe to work just turn it off when the user
        /// disables strobe.
        /// </summary>
        public override void DisableStrobe()
        {
            base.DisableStrobe();

            if (LampBrightness == StrobeDimBrightness)
            {
                LampOff();
            }
        }

        /// <summary>
        /// Implemented to set lamp output to minimum in order to handle the issue with charge in
        /// Aquaorea MK3.
        /// </summary>
        public override void EnableStrobe()
        {
            if (LampBrightness < StrobeDimBrightness)
            {
                SetLampBrightness(StrobeDimBrightness);
            }

            base.EnableStrobe();
        }

        /// <summary>
        /// Gets the product serial number.
        /// </summary>
        /// <returns> Product serial number prepended with ProductSerialNumber:. </returns>
        [Alias("ProductSerialNumber")]
        [RemoteCommand]
        public string GetProductSerialNumber() => $"ProductSerialNumber{input}:SUBC{productSerialNumber}";

        /// <summary>
        /// Gets whether the connect device is an Aquorea Mk3.
        /// </summary>
        /// <returns> True if the device is an Aquorea Mk3. </returns>
        public override bool IsDevice()
        {
            var productCode = @"@producthardwarecode:300";

            var result = serial.SendSync(new CommunicationData(
                PrependTo(Wrap("~product print hardware code", newLine), input),
                1000,
                $"({productCode.ToHex().Replace(" ", string.Empty)})")).Result;

            OnNotify("IsDeviceResult: " + result.HexToAscii());
            return result == productCode.ToHex().Replace(" ", string.Empty);
        }

        /// <summary>
        /// Sets the power level of the lamp
        /// </summary>
        /// <param name="value"> The value to set </param>
        [RemoteCommand]
        public void LampPower(int value)
        {
            if (string.IsNullOrEmpty(serialAddress))
            {
                Send(PackageSet("lamp", value));
            }
            else
            {
                foreach (var item in serialAddress.Split(' '))
                {
                    Send(PackageSet("lamp", value) + "|" + item);
                }
            }
        }

        /// <summary>
        /// Load all the settings
        /// </summary>
        public override void LoadSettings()
        {
            if (settings.TryLoad("AquoreaMk3_" + input, out int brightness))
            {
                SetLampBrightness(brightness);
            }

            // set the default strobe brightness
            if (settings.TryLoad($"AquoreaMk3_{input}StrobeBrightness", out int strobe))
            {
                SetStrobeBrightness(strobe);
            }
            else
            {
                SetStrobeBrightness(65);
            }

            if (settings.TryLoad(nameof(SerialAddress), out string address))
            {
                SerialAddress = address;
            }
        }

        /// <summary>
        /// Set the current limit of the LED.
        /// </summary>
        /// <param name="limit"> Limit to set. </param>
        [RemoteCommand]
        public void SetCurrentLimit(int limit)
        {
            Send($@"~device set current limit:{limit}");
        }

        /// <summary>
        /// Set the brightness of the lamp
        /// </summary>
        /// <param name="value"> Percentage from 0-100 </param>
        [Alias("LampBrightness")]
        public override void SetLampBrightness(int value)
        {
            // If strobe is enabled change clamp to not allow turning the lamp completely off.
            var minValue = IsStrobeEnabled ? StrobeDimBrightness : 0;

            value = value.Clamp(minValue);
            LampBrightness = value;
            LampPower(value);
        }

        /// <summary>
        /// Set the strobe brightness for the LED
        /// </summary>
        /// <param name="value"> Percentage from 0-100 </param>
        [Alias("StrobeBrightness")]
        [RemoteCommand]
        public void SetStrobeBrightness(int value)
        {
            value = value.Clamp(max: 65);

            StrobeBrightness = value;

            if (string.IsNullOrEmpty(serialAddress))
            {
                Send(PackageSet("strobe", value));
            }
            else
            {
                foreach (var item in serialAddress.Split(' '))
                {
                    Send(PackageSet("strobe", value) + "|" + item);
                }
            }
        }

        /// <summary>
        /// Callback when aux data is received
        /// </summary>
        /// <param name="e"> Aux data received </param>
        protected override void AuxDataReceived(AuxData e)
        {
            var match = Regex.Match(e.Data, @"State = (\d)");
            if (match.Success)
            {
                StrobeState = (StrobeStates)int.Parse(match.Groups[1].Value);
            }

            match = Regex.Match(e.Data, "productstate:(.+)_");
            if (match.Success)
            {
                ProductState = match.Groups[1].Value;
            }

            match = Regex.Match(e.Data, @"SUBC(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var result))
            {
                productSerialNumber = result;
                OnNotify($"ProductSerialNumber{input}:SUBC{result}");
            }
        }

        /// <summary>
        /// Configuration to send when the serial is connected
        /// </summary>
        protected async override void Connected()
        {
            // configure the teensy to all the strobe signal to be passed through
            await serial.SendAsync($"~aux{input} set strobe output:100");
            await serial.SendAsync($"~aux{input} set lamp output:0");

            // set the current limit of the led SetCurrentLimit(2);
            SendHex("~test set debug:1");
            SendHex("~product print state");
        }

        /// <summary>
        /// Prepend the device command to get the proerty value
        /// </summary>
        /// <param name="property"> Property get value of </param>
        /// <returns> The property with the device print command prepended </returns>
        private static string PackageGet(string property)
            => $"~device print {property}";

        /// <summary>
        /// Prepend the device command to set the property
        /// </summary>
        /// <param name="property"> Name of property to set </param>
        /// <param name="value"> Value of the property </param>
        /// <returns> Property set in proper format </returns>
        private static string PackageSet(string property, int value)
            => $"~device set {property}:{value}";
    }
}
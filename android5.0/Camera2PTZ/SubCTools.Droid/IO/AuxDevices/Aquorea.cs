// <copyright file="Aquorea.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SubCTools.Droid.IO.AuxDevices
{
    using SubCTools.Attributes;
    using SubCTools.Droid.Communicators;
    using System;

    /// <summary>
    /// Base class for aquorea leds.
    /// </summary>
    public abstract class Aquorea : HexController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Aquorea" /> class.
        /// </summary>
        /// <param name="serial"> Base serial for comms. </param>
        /// <param name="input"> Aux input. </param>
        /// <param name="listener"> Listener for parsing data. </param>
        public Aquorea(
            AndroidSerial serial,
            int input,
            TeensyListener listener)
            : base(serial, input, listener)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the property from the system props on whether the strobe
        /// is enabled
        /// </summary>
        /// <returns> True if the strobe is enabled, false if it is not </returns>
        [RemoteState]
        public bool IsStrobeEnabled
        {
            get
            {
                var isEnabledValue = DroidSystem.ShellSync("getprop persist.camera.led.mode").Trim();
                return Convert.ToBoolean(Convert.ToInt32(!string.IsNullOrEmpty(isEnabledValue) ? isEnabledValue : "0"));
            }
        }

        /// <summary>
        /// Turn off the Rayfin's strobing function.
        /// </summary>
        [RemoteCommand]
        public virtual void DisableStrobe()
        {
            DroidSystem.ShellSync("setprop persist.camera.led.mode 0");
            OnNotify($"IsStrobeEnabled:{false}");
        }

        /// <summary>
        /// Turn on the Rayfin's strobing function.
        /// </summary>
        [RemoteCommand]
        public virtual void EnableStrobe()
        {
            DroidSystem.ShellSync("setprop persist.camera.led.mode 1");
            OnNotify($"IsStrobeEnabled:{true}");
        }

        /// <summary>
        /// Turn on the lamp.
        /// </summary>
        [RemoteCommand]
        public void LampOff()
        {
            if (IsStrobeEnabled)
            {
                SetLampBrightness(1);
            }
            else
            {
                SetLampBrightness(0);
            }
        }

        /// <summary>
        /// Turn off the lamp.
        /// </summary>
        [RemoteCommand]
        public void LampOn()
        {
            SetLampBrightness(100);
        }

        /// <summary>
        /// Set lamp brightness for the aquorea
        /// </summary>
        /// <param name="value"> Value to set between 0-100 </param>
        [RemoteCommand]
        [Alias("LampBrightness")]
        public abstract void SetLampBrightness(int value);

        /// <summary>
        /// Callback when aux data is received
        /// </summary>
        /// <param name="e"> Data received. </param>
        protected override abstract void AuxDataReceived(AuxData e);

        /// <summary>
        /// Callback when teensy is connected
        /// </summary>
        protected override abstract void Connected();
    }
}
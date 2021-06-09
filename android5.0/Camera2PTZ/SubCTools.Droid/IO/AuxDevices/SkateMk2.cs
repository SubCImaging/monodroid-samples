namespace SubCTools.Droid.IO.AuxDevices
{
    using SubCTools.Attributes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Extensions;
    using SubCTools.Settings.Interfaces;

    public class SkateMk2 : Skate
    {
        public SkateMk2(
            AndroidSerial serial,
            int input,
            TeensyListener listener,
            ISettingsService settings)
        : base(serial, input, listener, settings)
        {
        }

        /// <summary>
        /// Sets the brightness of the laser
        /// </summary>
        /// <param name="percentage"> brightness value between 0 and 100 </param>
        [RemoteCommand]
        [Alias("LaserBrightness")]
        public override void Brightness(int percentage)
        {
            // Instructions said to use this command: ~device set lamp:[0 - 100]
            serial.Send($@"{SubCTeensy.TeensyStartCharacter}aux{input} set lamp:" + percentage.Clamp());
            IsOn = percentage > 0;
        }

        /// <summary>
        /// Turns the laser off
        /// </summary>
        [RemoteCommand]
        [Alias("LaserOff")]
        public override void Off()
        {
            Brightness(0);
        }

        /// <summary>
        /// Turns the laser on
        /// </summary>
        [RemoteCommand]
        [Alias("LaserOn")]
        public override void On()
        {
            Brightness(100);
        }

        /// <summary>
        /// Configuration to send when the serial is connected
        /// </summary>
        protected async override void Connected()
        {
            // configure the teensy to all the strobe signal to be passed through Instructions said
            // to use this command: ~device set ttl:0
            await serial.SendAsync($"~aux{input} set ttl:0");
        }
    }
}
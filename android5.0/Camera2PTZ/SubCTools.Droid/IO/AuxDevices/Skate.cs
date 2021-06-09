namespace SubCTools.Droid.IO.AuxDevices
{
    using SubCTools.Attributes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Extensions;
    using SubCTools.Settings.Interfaces;
    using System;

    public class Skate : AuxDevice
    {
        private readonly ISettingsService settings;
        private bool isOn;

        public Skate(
            AndroidSerial serial,
            int input,
            TeensyListener listener,
            ISettingsService settings)
            : base(serial, input, listener)
        {
            this.settings = settings;

            LoadSkateSettings();
        }

        public bool IsOn
        {
            get => isOn;
            protected set
            {
                isOn = value;
                OnNotify($"{nameof(IsOn)}:{IsOn}");
                settings.Update("SkateIsOn", value);
            }
        }

        /// <summary>
        /// Sets the brightness of the laser
        /// </summary>
        /// <param name="percentage"> brightness value between 0 and 100 </param>
        [RemoteCommand]
        [Alias("LaserBrightness")]
        public virtual void Brightness(int percentage)
        {
            serial.Send($@"{SubCTeensy.TeensyStartCharacter}aux{input} set lamp output:" + percentage.Clamp());
            IsOn = percentage > 0;
        }

        [RemoteCommand]
        public void LoadSkateSettings()
        {
            settings.TryLoad("SkateIsOn", out bool isOn);

            IsOn = isOn;

            if (IsOn)
            {
                On();
            }
        }

        /// <summary>
        /// Turns the laser off
        /// </summary>
        [RemoteCommand]
        [Alias("LaserOff")]
        public virtual void Off()
        {
            Brightness(0);
        }

        /// <summary>
        /// Turns the laser on
        /// </summary>
        [RemoteCommand]
        [Alias("LaserOn")]
        public virtual void On()
        {
            Brightness(100);
        }

        protected override void AuxDataReceived(AuxData e)
        {
            Console.WriteLine(e.Data);
        }

        protected override void Connected()
        {
            //throw new NotImplementedException();
        }
    }
}
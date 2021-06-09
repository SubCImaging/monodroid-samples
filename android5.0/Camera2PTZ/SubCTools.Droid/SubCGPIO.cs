namespace SubCTools.Droid
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class SubCGPIO
    {
        public SubCGPIO(int pin)
        {
            Pin = pin;
            GetAccess();
        }

        public int Pin { get; }

        public void Input()
        {
            DroidSystem.ShellSync($"echo in > /sys/class/gpio/gpio{Pin}/direction");
        }

        public void Off()
        {
            DroidSystem.ShellSync($"echo 0 > /sys/class/gpio/gpio{Pin}/value");
        }

        public void On()
        {
            DroidSystem.ShellSync($"echo 1 > /sys/class/gpio/gpio{Pin}/value");
        }

        public void Output()
        {
            DroidSystem.ShellSync($"echo out > /sys/class/gpio/gpio{Pin}/direction");
        }

        private void GetAccess()
        {
            DroidSystem.ShellSync($"echo {Pin} > /sys/class/gpio/export");
        }
    }
}
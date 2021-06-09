namespace SubCTools.Droid.Managers
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class ThermalManager : INotifiable
    {
        private readonly SubCGPIO gpio;

        public ThermalManager(SubCGPIO gpio)
        {
            gpio.Output();
            this.gpio = gpio;
        }

        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            // turn the GPIO off when the ambient temperate is greater than 60 to prevent the camera from overheating
            if (e.Message.Contains("AmbientTemp"))
            {
                var temp = Regex.Match(e.Message, @"\d+");

                if (!temp.Success)
                {
                    return;
                }

                if (int.TryParse(temp.Value, out int t))
                {
                    if (t > 60)
                    {
                        // off
                        gpio.Off();
                    }
                    else
                    {
                        // on
                        gpio.On();
                    }
                }
            }
        }
    }
}
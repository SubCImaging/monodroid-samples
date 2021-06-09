namespace SubCTools.Droid.IO
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using SubCTools.Droid.Communicators;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class ExpansionOutput : DroidBase
    {
        private readonly AndroidSerial serial;
        private readonly TeensyListener listener;

        public ExpansionOutput(
            ISettingsService settings,
            AndroidSerial serial,
            TeensyListener listener,
            object savable = null)
            : base(settings, savable)
        {
            this.serial = serial;
            this.listener = listener;

            listener.TeensyDataReceived += Listener_TeensyDataReceived;
        }

        /// <summary>
        /// Forces a refresh of the state of the expansion board
        /// </summary>
        [RemoteCommand]
        public void ExpansionState()
        {
            serial.Send($"{SubCTeensy.TeensyStartCharacter}exp print status");
        }

        /// <summary>
        /// Requests the current fault state of the breaker
        /// </summary>
        [RemoteCommand]
        public void IsFaulted()
        {
            serial.Send($"{SubCTeensy.TeensyStartCharacter}exp print breaker fault");
        }

        /// <summary>
        /// Turns all breakers off
        /// </summary>
        [RemoteCommand]
        [Alias("BreakerOff")]
        public void Off()
        {
            serial.Send($"{SubCTeensy.TeensyStartCharacter}exp set output all:0");
            //for (int i = 0; i < 3; i++)
            //{
            //    Settings.Update("ExpansionOutputs/Output", true, new Dictionary<string, string>() { { "id", i.ToString() } });
            //}
        }

        /// <summary>
        /// Turns on the specified breaker
        /// </summary>
        /// <param name="input">The index of the breaker to turn on</param>
        [RemoteCommand]
        [Alias("BreakerOn")]
        public void On(int input)
        {
            serial.Send($"{SubCTeensy.TeensyStartCharacter}exp set output:{input}:1");
            //OnNotify($"IsOn{input}:{true}");
            //Settings.Update("ExpansionOutputs/Output", true, new Dictionary<string, string>() { { "id", input.ToString() } });
        }

        /// <summary>
        /// Turns off the specified breaker
        /// </summary>
        /// <param name="input">The index of the breaker to turn off</param>
        [RemoteCommand]
        [Alias("BreakerOff")]
        public void Off(int input)
        {
            serial.Send($"{SubCTeensy.TeensyStartCharacter}exp set output:{input}:0");
            //OnNotify($"IsOn{input}:{false}");
            //Settings.Update("ExpansionOutputs/Output", false, new Dictionary<string, string>() { { "id", input.ToString() } });
        }

        //public override void LoadSettings()
        //{
        //    foreach (var item in Settings.LoadAll("ExpansionOutputs"))
        //    {
        //        var isOn = Convert.ToBoolean(item.Value);
        //        var input = Convert.ToInt32(item.Attributes["id"]);
        //        if (isOn)
        //        {
        //            On(input);
        //        }
        //        else
        //        {
        //            Off(input);
        //        }
        //    }
        //}

        private void Listener_TeensyDataReceived(object sender, TeensyInfo e)
        {
            var faultPattern = @"expbreakerfault:(\d):(\d)";
            var match = Regex.Match(e.Raw, faultPattern);

            if (match.Success)
            {
                OnNotify($@"IsFaulted{match.Groups[1].Value}:{(match.Groups[2].Value == "0" ? false : true)}");
                return;
            }

            var onPattern = @"expoutput:(\d):(\d)";
            match = Regex.Match(e.Raw, onPattern);

            if (match.Success)
            {
                OnNotify($"IsOn{match.Groups[1].Value}:{(match.Groups[2].Value == "0" ? false : true)}");
            }
        }
    }
}
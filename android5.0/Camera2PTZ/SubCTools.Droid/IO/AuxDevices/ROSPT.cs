using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SubCTools.Droid.Communicators;
using SubCTools.Messaging.Interfaces;
using SubCTools.Messaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubCTools.Droid.IO.AuxDevices
{
    public class ROSPT : AuxDevice, INotifiable
    {
        public ROSPT(
            AndroidSerial serial,
            int input,
            TeensyListener listener) : base(serial, input, listener)
        {

        }

        /// <summary>
        /// Notify the message router and send the data through
        /// </summary>
        /// <param name="e">Aux data received</param>
        protected override void AuxDataReceived(AuxData e)
        {
            OnNotify("<" + (e.From == 0 ? 2 : e.From) + e.Data);
        }

        /// <summary>
        /// We don't care when we're connected
        /// </summary>
        protected override void Connected()
        {
            // ignore
        }

        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            // TODO: Move this to base class so all devices can be spoken to directly

            if (e.MessageType == MessageTypes.Aux)
            {
                var pattern = @">(\d+)(.+)";

                var match = Regex.Match(e.Message, pattern);

                if (int.TryParse(match.Groups[1].Value, out var to))
                {
                    to = to == 2 ? 0 : to;

                    if (to == input)
                    {
                        Send(match.Groups[2].Value);
                    }
                }
            }
        }
    }
}
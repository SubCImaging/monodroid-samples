namespace SubCTools.Droid.Communicators
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Attributes;
    using SubCTools.Communicators;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class CommunicationManager : DroidBase
    {
        private readonly Dictionary<ICommunicator, Dictionary<string, string>> communicators = new Dictionary<ICommunicator, Dictionary<string, string>>();

        public CommunicationManager(ISettingsService settings)
            : base(settings)
        {
        }

        public event EventHandler<string> OverlayDataRecieved;
        //public event EventHandler<string> LogDataReceived;

        public void OverlayPort(int port)
        {
            var udp = communicators.Keys.FirstOrDefault(c => c.Address["Port"] == port.ToString());
            if (udp != null)
            {
                SubCTools.Extensions.Dictionary.Update(communicators[udp], "Overlay", "true");
            }
        }

        public void LogPort(int port, string filename)
        {
            var udp = communicators.Keys.FirstOrDefault(c => c.Address["Port"] == port.ToString());
            if (udp != null)
            {
                SubCTools.Extensions.Dictionary.Update(communicators[udp], "Log", filename);
            }
        }

        /// <summary>
        /// Adds a UDP listener on the specified <see cref="port"/>
        /// </summary>
        /// <param name="port">The port number</param>
        [RemoteCommand]
        public void OpenPort(int port)
        {
            if (communicators.Keys.Any(c => c.Address["Port"] == port.ToString())) return;
            var udp = new SubCUDP(port);
            communicators.Add(udp, new Dictionary<string, string>());
            udp.DataReceived += Udp_DataReceived;
            Settings.Update("Communicators/UDP" + port, port);
        }

        /// <summary>
        /// Removes any UDP listener from the specified <see cref="port"/>
        /// </summary>
        /// <param name="port">The port number</param>
        [RemoteCommand]
        public void ClosePort(int port)
        {
            if (communicators.Keys.Any(c => c.Address["Port"] == port.ToString()))
            {
                var udp = communicators.Keys.First(c => c.Address["Port"] == port.ToString());
                udp.DataReceived -= Udp_DataReceived;
                communicators.Remove(udp);
                Settings.Remove("Communicators/UDP" + port);
            }
        }

        /// <summary>
        /// Remove all UDP port listeners.
        /// </summary>
        [RemoteCommand]
        public void ClearPorts()
        {
            foreach (var item in communicators.Keys)
            {
                ClosePort(Convert.ToInt32(item.Address["Port"]));
            }
        }

        public override void LoadSettings()
        {
            foreach (var item in Settings.LoadAll("Communicators"))
            {
                OpenPort(Convert.ToInt32(item.Value));
            }
        }

        private void Udp_DataReceived(object sender, string e)
        {
            var udp = communicators.Keys.FirstOrDefault(c => c == sender);
            if (communicators[udp].ContainsValue("Overlay"))
            {
                if (communicators[udp]["Overlay"] == "true")
                {
                    OverlayDataRecieved?.Invoke(this, e);
                }
            }

        }
    }
}
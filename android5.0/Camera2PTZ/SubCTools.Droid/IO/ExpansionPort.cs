//-----------------------------------------------------------------------
// <copyright file="ExpansionPort.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.IO
{
    using SubCTools.Attributes;
    using SubCTools.Droid.Communicators;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Type of expansion port configured in the Android OS.
    /// </summary>
    public enum PortType
    {
        /// <summary>
        /// No current port configuration.
        /// </summary>
        None = 0,

        /// <summary>
        /// Input port configuration.
        /// </summary>
        Input = 2,

        /// <summary>
        /// Breaker port configuration.
        /// </summary>
        Output = 8,

        /// <summary>
        /// Long line driver port configuration.
        /// </summary>
        LongLineDriver = 7
    }

    /// <summary>
    /// A static class which will return the <see cref="ExpansionPort"/> configuration.
    /// </summary>
    public class ExpansionPort
    {
        private readonly AndroidSerial serial;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionPort"/> class.
        /// </summary>
        public ExpansionPort(AndroidSerial serial)
        {
            //if (!Enum.TryParse(DroidSystem.ShellSync("getprop persist.rayfin.expansion.port").TrimEnd(), out PortType portType))
            //{
            //    ExpansionPortType = PortType.Output;//PortType.None;
            //}
            //else
            //{
            //    ExpansionPortType = portType;
            //}

            this.serial = serial;


            if (serial.IsConnected)
            {
                UpdateMode();
            }
            else
            {
                serial.IsConnectedChanged += (s, e) =>
                {
                    if (e)
                    {
                        UpdateMode();
                    }
                };
            }
        }

        /// <summary>
        /// Gets the current <see cref="PortType"/> configured on the Rayfin
        /// </summary>
        [RemoteState]
        public PortType ExpansionPortType { get; private set; }

        private async void UpdateMode()
        {
            var mode = await serial.SendSync(new SubCTools.Communicators.DataTypes.CommunicationData("~exp print mode", 2000, @"expmode:(\d)"));

            if (int.TryParse(mode, out var iMode))
            {
                ExpansionPortType = (PortType)iMode;
            }
            else
            {
                await Task.Delay(2000);
                UpdateMode();
            }

        }
    }
}
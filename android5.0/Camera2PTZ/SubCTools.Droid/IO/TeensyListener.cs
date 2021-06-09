//-----------------------------------------------------------------------
// <copyright file="TeensyListener.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.IO
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using SubCTools.Droid.Communicators;
    using SubCTools.Droid.IO.AuxDevices;
    using SubCTools.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class TeensyListener
    {
        /// <summary>
        /// Pattern to match aux information
        /// </summary>
        private const string AuxPattern = @"<(\d)(.+:?.*)";

        /// <summary>
        /// Pattern to match packaged hex data
        /// </summary>
        private const string HexPattern = @"Hex:([A-Fa-f0-9]+)";

        /// <summary>
        /// Pattern to match teensy information
        /// </summary>
        private const string TeensyPattern = @"([@&])(\w+)(:?(.*))?";

        /// <summary>
        /// Matcher for getting generic data
        /// </summary>
        private readonly Regex auxMatcher = new Regex(AuxPattern);

        /// <summary>
        /// Matcher for getting hex data
        /// </summary>
        private readonly Regex hexMatcher = new Regex(HexPattern);

        /// <summary>
        /// Matcher for getting teensy data
        /// </summary>
        private readonly Regex teensyMatcher = new Regex(TeensyPattern);

        /// <summary>
        /// Serial device to listen to information
        /// </summary>
        private readonly AndroidSerial serial;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeensyListener"/> class.
        /// </summary>
        /// <param name="serial">Low level serial for communication</param>
        public TeensyListener(AndroidSerial serial)
        {
            this.serial = serial;
            serial.DataReceived += (s, e) => Serial_DataReceived(e);
        }

        /// <summary>
        /// Event to fire when teensy data is received
        /// </summary>
        public event EventHandler<TeensyInfo> TeensyDataReceived;

        /// <summary>
        /// Event to fire when Aux data is received
        /// </summary>
        public event EventHandler<AuxData> AuxDataReceived;

        /// <summary>
        /// Call when serial receives data
        /// </summary>
        /// <param name="e">Data received from serial</param>
        private void Serial_DataReceived(string e)
        {
            // loop though all the data, the appender might have packaged a bunch
            foreach (var item in e.Split('\n'))
            {
                // ignore empty strings
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }

                var teensyMatch = teensyMatcher.Match(item);
                if (teensyMatch.Success)
                {
                    TeensyDataReceived?.Invoke(
                        this,
                        new TeensyInfo
                        {
                            Type = teensyMatch.Groups[1].Value,
                            Property = teensyMatch.Groups[2].Value,
                            Value = teensyMatch.Groups[4].Value,
                            Raw = teensyMatch.Value
                        });
                    continue;
                }

                // match the data
                var match = auxMatcher.Match(item);
                if (match.Success)
                {
                    // who are you from?
                    var from = match.Groups[1].Value;

                    // flip 2 to 0
                    from = from == "2" ? "0" : from;

                    // match any hex data
                    var hexMatch = hexMatcher.Match(e);

                    // set the hex data if there is any
                    var hexData = hexMatch.Success ? hexMatch.Groups[1].ToString() : string.Empty;

                    // convert the hex to ascii if there is any hex data, just take the data otherwise
                    var ascii = string.IsNullOrEmpty(hexData) ? match.Groups[2].Value.ToString() : hexData.HexToAscii();

                    // peel the end off in case there's some white space
                    ascii = ascii.TrimEnd();

                    // bubble up the data received
                    var auxInfo = new AuxData
                    {
                        From = int.Parse(from),
                        Data = ascii,
                        Hex = Strings.HexToByteArray(hexData)
                    };

                    AuxDataReceived?.Invoke(this, auxInfo);
                    continue;
                }
            }
        }
    }
}
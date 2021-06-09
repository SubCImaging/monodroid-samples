// <copyright file="KongsbergPanTilt.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Models
{
    using SubCTools.Communicators;
    using SubCTools.Communicators.Interfaces;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Kongsberg pan and tilt model for UWash.
    /// </summary>
    public class KongsbergPanTilt
    {
        private static readonly byte ChkInd = 0x47;
        private static readonly byte Colon = 0x3A;
        private static readonly byte GreaterThan = 0x3E;
        private static readonly byte LessThan = 0x3C;

        private readonly DataAppender appender;
        private readonly StringBuilder builder = new StringBuilder();
        private readonly string commandPattern;
        private readonly ISenderReceiver<byte[]> communicator;
        private readonly byte from = 0x01;
        private readonly byte to = 0x02;
        private int pan;
        private int tilt;

        /// <summary>
        /// Initializes a new instance of the <see cref="KongsbergPanTilt"/> class.
        /// </summary>
        /// <param name="communicator">Communicator used to transmit data to the pan tilt.</param>
        /// <param name="from">Who the message is coming from.</param>
        /// <param name="to">Who the message is going to.</param>
        public KongsbergPanTilt(ISenderReceiver<byte[]> communicator, byte from = 0x01, byte to = 0x02)
        {
            this.from = from;
            this.to = to;
            this.communicator = communicator;

            commandPattern = "(" + LessThan.ToHexString() +
                from.ToHexString() +
                Colon.ToHexString() +
                to.ToHexString() + "(.+)" +
                Colon.ToHexString() +
                "[0-9a-fA-F]{2}" +
                Colon.ToHexString() +
                ChkInd.ToHexString() +
                GreaterThan.ToHexString() + ")";

            appender = new DataAppender
            {
                Pattern = commandPattern,
            };

            appender.Notify += Appender_Notify;
            communicator.DataReceived += Communicator_DataReceived;
        }

        /// <summary>
        /// Event to fire when the pan angle changes.
        /// </summary>
        public event EventHandler<int> PanChanged;

        /// <summary>
        /// Event to fire when the tilt angle changes.
        /// </summary>
        public event EventHandler<int> TiltChanged;

        /// <summary>
        /// Gets the value of the pan angle.
        /// </summary>
        public int Pan
        {
            get => pan;
            private set
            {
                if (pan != value)
                {
                    pan = value;
                    PanChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Gets the value of the tilt angle.
        /// </summary>
        public int Tilt
        {
            get => tilt;
            private set
            {
                if (tilt != value)
                {
                    tilt = value;
                    TiltChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// Pan to the given angle.
        /// </summary>
        /// <param name="angle">Angle to pan in degrees.</param>
        public void PanGoto(int angle)
        {
            var d = angle.ToString("000");
            d = d.ToHex();
            var data = Strings.HexToByteArray(d);

            Send(BuildCommand(to, from, 0x06, Convert.ToByte('P'), Convert.ToByte('P'), data[0], data[1], data[2]));
        }

        /// <summary>
        /// Pan left.
        /// </summary>
        public void PanLeft()
        {
            Send(BuildCommand(to, from, 0x07, Convert.ToByte('P'), Convert.ToByte('F'), 0x02, 0x01, 0x00, 0x00));
        }

        /// <summary>
        /// Pan right.
        /// </summary>
        public void PanRight()
        {
            Send(BuildCommand(to, from, 0x07, Convert.ToByte('P'), Convert.ToByte('C'), 0x01, 0x01, 0x00, 0x00));
        }

        /// <summary>
        /// Stop panning and tilting.
        /// </summary>
        public void StopAll()
        {
            Send(BuildCommand(to, from, 0x07, Convert.ToByte('P'), Convert.ToByte('F'), 0x00, 0x00, 0x00, 0x00));
        }

        /// <summary>
        /// Tilt down.
        /// </summary>
        public void TiltDown()
        {
            Send(BuildCommand(to, from, 0x07, Convert.ToByte('P'), Convert.ToByte('F'), 0x04, 0x00, 0x64, 0x00));
        }

        /// <summary>
        /// Tilt to the given angle.
        /// </summary>
        /// <param name="angle">Angle to tilt in degrees.</param>
        public void TiltGoto(int angle)
        {
            var d = angle.ToString("000");
            d = d.ToHex();
            var data = Strings.HexToByteArray(d);

            Send(BuildCommand(to, from, 0x06, Convert.ToByte('T'), Convert.ToByte('P'), data[0], data[1], data[2]));
        }

        /// <summary>
        /// Tilt up.
        /// </summary>
        public void TiltUp()
        {
            Send(BuildCommand(to, from, 0x07, Convert.ToByte('P'), Convert.ToByte('F'), 0x08, 0x01, 0x01, 0x00));
        }

        private static byte[] BuildCommand(byte to, byte from, byte length, byte cmd1, byte cmd2, params byte[] data)
        {
            var toSend = new List<byte> { to, Colon, from, Colon, length, Colon, cmd1, cmd2, Colon };

            toSend.AddRange(data);

            var checkSum = CalCheckSum(toSend.ToArray(), toSend.Count);

            var c = new List<byte>()
            {
                LessThan,
            };

            c.AddRange(toSend);
            c.AddRange(new[] { Colon, checkSum, Colon, ChkInd, GreaterThan });

            return c.ToArray();
        }

        private static byte CalCheckSum(byte[] packetData, int packetLength)
        {
            byte checkSumByte = 0x00;

            for (var i = 0; i < packetLength; i++)
            {
                checkSumByte ^= packetData[i];
            }

            return checkSumByte;
        }

        private void Appender_Notify(object sender, NotifyEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
            }
        }

        private void Communicator_DataReceived(object sender, string e)
        {
            builder.Append(e.ToHex().Replace(" ", string.Empty));

            var match = Regex.Matches(builder.ToString(), commandPattern);

            var last = match.Count - 1;

            if (match.Count > 0 && match[last].Success)
            {
                builder.Clear();

                for (var i = 0; i < match[last].Groups.Count; i++)
                {
                    Console.WriteLine(i + " " + match[last].Groups[i].Value);
                }

                // builder.Remove(match.Index, match.Length);
                ProcessCommand(match[last].Groups[1].Value);
            }
        }

        private void ProcessCommand(string command)
        {
            var d = command.HexToAscii();

            var chkStr = command.Substring(2, command.Length - 12);

            var tChkStr = chkStr.HexToAscii();

            // var chk = Strings.HexToByteArray(chkStr);
            // var sum = CalCheckSum(chk, chk.Length);
            d = Regex.Replace(d, @"[\u001F]+", string.Empty);

            Console.WriteLine("Warning: " + d);

            var pfpattern = @"PF(\d{3})(\d{3})";
            var ppPattern = @"PP(\d{3})";
            var tpPattern = @"TP(\d{3})";

            var reg = Regex.Match(d, pfpattern);
            if (reg.Success && tChkStr.EndsWith("11"))
            {
                Pan = int.Parse(reg.Groups[1].Value);

                var t = int.Parse(reg.Groups[2].Value);
                if (Tilt != t)
                {
                    Tilt = t;
                }
            }

            reg = Regex.Match(d, ppPattern);
            if (reg.Success)
            {
                Pan = int.Parse(reg.Groups[1].Value);
            }

            reg = Regex.Match(d, tpPattern);
            if (reg.Success)
            {
                Tilt = int.Parse(reg.Groups[1].Value);
            }
        }

        private void Send(byte[] data)
        {
            communicator.SendAsync(data);
        }
    }
}
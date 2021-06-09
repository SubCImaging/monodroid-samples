//-----------------------------------------------------------------------
// <copyright file="SerialAppender.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Communicators
{
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Appender for serial data from the Rayfin.
    /// </summary>
    public class SerialAppender : DataAppender
    {
        /// <summary>
        /// End of Transmission.
        /// </summary>
        private const string EOT = "\u0004";

        /// <summary>
        /// End of Text.
        /// </summary>
        private const string ETX = "\u0003";

        /// <summary>
        /// Start of Header.
        /// </summary>
        private const string SOH = "\u0001";

        /// <summary>
        /// Start of Text.
        /// </summary>
        private const string STX = "\u0002";

        /// <summary>
        /// builder used for building a string when a SOH and EOT are used.
        /// </summary>
        private readonly StringBuilder builder = new StringBuilder();

        /// <summary>
        /// flag to set when SOH and EOT are used.
        /// </summary>
        private bool isBuildingString = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialAppender"/> class.
        /// </summary>
        public SerialAppender()
        {
            MasterTimer.Elapsed += (s, e) => MasterTimer_Elapsed();
        }

        /// <summary>
        /// Append the input data to the string being built.
        /// </summary>
        /// <param name="data">Data received from serial.</param>
        public override void Append(string data)
        {
            // reset the master timer
            MasterTimer.Stop();
            MasterTimer.Start();

            // start building the string
            PartialString.Append(data);

            // split all the data on a new line, and remove it from the partial string
            foreach (var item in SplitOnNewline(PartialString))
            {
                // peel the teensy start characters off
                var d = ClearBeginning(item);

                // if you're building a string from a bunch of chunks, you want to piece it all back together
                if (isBuildingString)
                {
                    var match = Regex.Match(d, STX + "(.+)" + ETX);
                    if (!match.Success)
                    {
                        OnNotify(d);
                        continue;
                    }
                    else
                    {
                        d = Regex.Replace(d.Replace(STX, string.Empty).Replace(ETX, string.Empty), @"^<\d", string.Empty);
                    }

                    // clean up the string and send it off if it contains the end of transmission character
                    if (d.Contains(EOT))
                    {
                        isBuildingString = false;
                        builder.Append(d.TrimEnd().Replace(EOT, string.Empty));
                        OnNotify(builder.ToString());
                        builder.Clear();
                        continue;
                    }

                    builder.Append(d.TrimEnd());
                    continue;
                }

                // start string builder mode if you have SOH
                // just notify the string otherwise
                if (d.Contains(SOH))
                {
                    isBuildingString = true;
                    builder.Append(d.TrimEnd().Replace(SOH, string.Empty).Replace(STX, string.Empty).Replace(ETX, string.Empty));
                }
                else
                {
                    OnNotify(d);
                }
            }
        }

        /// <summary>
        /// Split the data up on a newline, remove it from the string builder.
        /// </summary>
        /// <param name="partialString">Data to split apart.</param>
        /// <returns>Collection of strings that have been split on new line.</returns>
        private static IEnumerable<string> SplitOnNewline(StringBuilder partialString)
        {
            foreach (Match item in Regex.Matches(partialString.ToString(), @".+\n"))
            {
                partialString.Replace(item.Value, string.Empty);
                yield return item.Value;
            }
        }

        /// <summary>
        /// String the teensy character from the beginning.
        /// </summary>
        /// <param name="data">Data to strip characters off.</param>
        /// <returns>String with characters stripped off.</returns>
        private string ClearBeginning(string data)
        {
            return Regex.Replace(data, @"<\d@", string.Empty);
        }

        /// <summary>
        /// Clear up all the partial strings if you've lost connection.
        /// </summary>
        private void MasterTimer_Elapsed()
        {
            NotifyBuilder(builder);
        }

        /// <summary>
        /// Notify the data that in the given builder and clear it.
        /// </summary>
        /// <param name="builder">Builder to notify if there's data.</param>
        private void NotifyBuilder(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(builder.ToString()))
            {
                OnNotify(builder.ToString());
                builder.Clear();
            }
        }
    }
}
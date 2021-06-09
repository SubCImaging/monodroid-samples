//-----------------------------------------------------------------------
// <copyright file="CommunicationData.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Communicators.DataTypes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// <see cref="CommunicationData"/> class which holds the data, pattern and time to wait.
    /// </summary>
    public class CommunicationData : IEnumerable<Tuple<string, TimeSpan>>
    {
        /// <summary>
        /// The data to send.
        /// </summary>
        private readonly IEnumerable<Tuple<string, TimeSpan>> data;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationData"/> class.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="pattern">The expected return pattern.</param>
        public CommunicationData(string data, string pattern = "")
            : this(data, TimeSpan.FromMilliseconds(250), pattern)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationData"/> class.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="waitAfterSendMs">The amount of time to wait in ms.</param>
        /// <param name="pattern">The expected return pattern.</param>
        public CommunicationData(string data, int waitAfterSendMs, string pattern = "")
            : this(data, TimeSpan.FromMilliseconds(waitAfterSendMs), pattern)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationData"/> class.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="waitAfterSend">The amount of time to wait.</param>
        /// <param name="pattern">The expected return pattern.</param>
        public CommunicationData(string data, TimeSpan waitAfterSend, string pattern = "")
            : this(new[] { new Tuple<string, TimeSpan>(data, waitAfterSend) }, pattern)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationData"/> class.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="pattern">The expected return pattern.</param>
        public CommunicationData(IEnumerable<Tuple<string, int>> data)
            : this(from d in data
                   select new Tuple<string, TimeSpan>(d.Item1, TimeSpan.FromMilliseconds(d.Item2)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommunicationData"/> class.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="pattern">The expected return pattern.</param>
        public CommunicationData(IEnumerable<Tuple<string, TimeSpan>> data, string pattern = "")
        {
            this.data = data;
            Pattern = pattern;
            TimeToWait = data.First().Item2;
        }

        /// <summary>
        /// Gets the expected return pattern.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Gets the amount of time to wait.
        /// </summary>
        public TimeSpan TimeToWait { get; }

        /// <summary>
        /// <see cref="implicit"/> <see cref="operator"/> to create <see cref="CommunicationData"/> from a <see cref="string"/>.
        /// </summary>
        /// <param name="s">The <see cref="string"/> to create the <see cref="CommunicationData"/> from.</param>
        public static implicit operator CommunicationData(string s)
        {
            return new CommunicationData(s);
        }

        /// <summary>
        /// <see cref="implicit"/> <see cref="operator"/> to create <see cref="CommunicationData"/> from a <see cref="Tuple{string, TimeSpan}[]"/>.
        /// </summary>
        /// <param name="s">The <see cref="Tuple{string, TimeSpan}[]"/> to create the <see cref="CommunicationData"/> from.</param>
        public static implicit operator CommunicationData(Tuple<string, TimeSpan>[] s)
        {
            return new CommunicationData(data: s);
        }

        /// <summary>
        /// <see cref="implicit"/> <see cref="operator"/> to create <see cref="CommunicationData"/> from a <see cref="Tuple{string, int}[]"/>.
        /// </summary>
        /// <param name="s">The <see cref="Tuple{string, int}[]"/> to create the <see cref="CommunicationData"/> from.</param>
        public static implicit operator CommunicationData(Tuple<string, int>[] s)
        {
            return new CommunicationData(data: s);
        }

        /// <summary>
        /// Returns the <see cref="IEnumerator"/> from the <see cref="data"/>.
        /// </summary>
        /// <returns>A <see cref="IEnumerator"/>.</returns>
        public IEnumerator<Tuple<string, TimeSpan>> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        /// <summary>
        /// Requirement to impliment <see cref="IEnumerable"/>,         // ------------------------------------NOT tested in unit tests.
        /// </summary>
        /// <returns>A <see cref="IEnumerator"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        /// <summary>
        /// Override for <see cref="ToString"/>.
        /// </summary>
        /// <returns>A <see cref="string"/>.</returns>
        public override string ToString()
        {
            return data.FirstOrDefault()?.Item1 ?? string.Empty;
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="Matcher.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools
{
    using Newtonsoft.Json;
    using SubCTools.Helpers;
    using SubCTools.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for matching strings and displaying them in the desired format.
    /// </summary>
    public class Matcher : IMatcher
    {
        private string expressionToMatch = @"[\s\S]+";

        private string format = "{0}";

        private string headerToMatch = string.Empty;

        /// <summary>
        /// Field for holding the latest match.
        /// </summary>
        private string latestMatch = string.Empty;

        private MatchTypes matchType = MatchTypes.Expression;

        /// <summary>
        /// Event for alerting when the match has been updated
        /// </summary>
        public event EventHandler<string> MatchUpdated;

        /// <summary>
        /// Event for alerting when a property changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a regular expression to use for matching.
        /// </summary>
        public string ExpressionToMatch
        {
            get => expressionToMatch;
            set
            {
                if (expressionToMatch == value)
                {
                    return;
                }

                expressionToMatch = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExpressionToMatch)));
            }
        }

        /// <summary>
        /// Gets or sets a value which contains the format for displaying the matched values.
        /// </summary>
        public string Format
        {
            get => format;
            set
            {
                if (format == value)
                {
                    return;
                }

                format = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Format)));
            }
        }

        /// <summary>
        /// Gets or sets a value to filter incoming strings only starting with this header.
        /// </summary>
        public string HeaderToMatch
        {
            get => headerToMatch;
            set
            {
                if (headerToMatch == value)
                {
                    return;
                }

                headerToMatch = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HeaderToMatch)));
            }
        }

        /// <summary>
        /// Gets a value that contains the latest matched string.
        /// </summary>
        [JsonIgnore]
        public string LatestMatch
        {
            get => latestMatch;

            private set
            {
                latestMatch = value;
                MatchUpdated?.Invoke(this, value);
            }
        }

        /// <summary>
        /// Gets or sets the match type of this matcher.
        /// </summary>
        public MatchTypes MatchType
        {
            get => matchType;
            set
            {
                if (matchType == value)
                {
                    return;
                }

                matchType = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchType)));
            }
        }

        /// <summary>
        /// Gets or sets a value that determines what to split the incoming strings on.
        /// </summary>
        public string SplitSequence { get; set; } = ",";

        /// <summary>
        /// Parse input data based on the set properties.
        /// </summary>
        /// <param name="data">Input string.</param>
        /// <returns>Result of parse function.</returns>
        public string Parse(string data)
        {
            // return if you receive nothing
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }

            // Return the incoming data if you're not matching anything
            // if (string.IsNullOrEmpty(Format)
            //    && string.IsNullOrEmpty(HeaderToMatch))
            // {
            //    LatestMatch = data;
            //    return;
            // }

            // if (string.IsNullOrEmpty(Format))
            // {
            //    LatestMatch = data;
            //    return;
            // }
            string[] splitData;

            if (MatchType == MatchTypes.Header)
            {
                // split all the data on the SplitSequence
                splitData = HeaderParse(data).ToArray();
            }
            else
            {
                splitData = ExpressionParse(data);
            }

            // There was a header to match, and it wasn't matched
            if (!splitData.Any())
            {
                return string.Empty;
            }

            var output = Format.ToLiteral();

            for (var i = 0; i < splitData.Length; i++)
            {
                output = output.Replace("{" + i + "}", splitData[i]);
            }

            // execute a math expression
            var operationMatches = Regex.Match(output.ToString(), @"\[.+\]");

            if (operationMatches.Success)
            {
                foreach (Match m in operationMatches.Groups)
                {
                    var compute = m.Value.Replace("[", string.Empty).Replace("]", string.Empty);

                    var table = new DataTable();
                    try
                    {
                        var result = table.Compute(compute, string.Empty);
                        output = output.Replace(m.Value, result.ToString());
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            LatestMatch = output.ToString();
            return LatestMatch;
        }

        /// <summary>
        /// Asynchronously parse input data based on the set properties.
        /// </summary>
        /// <param name="data">Input string.</param>
        /// <returns>Empty task.</returns>
        public async Task ParseAsync(string data)
        {
            await Task.Run(() => Parse(data));
        }

        /// <summary>
        /// ToString override.
        /// </summary>
        /// <returns>the matcher as a string.</returns>
        public override string ToString()
        {
            return "Matcher (" + (MatchType == MatchTypes.Header ? $"H, /{HeaderToMatch}/, {Format})" : $"E, /{ExpressionToMatch}/, {Format})");
        }

        /// <summary>
        /// Parse a string with regex.
        /// </summary>
        /// <param name="data">the string to parse.</param>
        /// <returns>an array of result strings.</returns>
        private string[] ExpressionParse(string data)
        {
            Match match = null;

            try
            {
                match = Regex.Match(data, ExpressionToMatch);
            }
            catch
            {
            }

            if (!(match?.Success ?? false))
            {
                return new[] { data }; // Shouldn't this return an empty list???
            }

            if (match.Groups.Count < 2)
            {
                return new string[] { data };
            }

            var results = new List<string>();

            foreach (var item in match.Groups)
            {
                results.Add(item.ToString());
            }

            results.RemoveAt(0);

            return results.ToArray();
        }

        /// <summary>
        /// Create a list of headers to parse from the original string of headers to match.
        /// </summary>
        /// <param name="data">the headers to match.</param>
        /// <returns>list of headers to parse.</returns>
        private IEnumerable<string> HeaderParse(string data)
        {
            // break all the lines if you have multiple
            var splitLines = data.Split('\n');
            data = !string.IsNullOrEmpty(HeaderToMatch) ? splitLines.FirstOrDefault(l => l.StartsWith(HeaderToMatch)) : data;

            // Return if there's a header to match and you don't get it
            return data == null ? new string[] { } : Regex.Split(data, SplitSequence);
        }
    }
}
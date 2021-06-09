// <copyright file="Entry.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;

    public static class DiveHelpers
    {
        /// <summary>
        /// Convert the entry in to a dictionary.
        /// </summary>
        /// <returns>All the properties as a dictionary.</returns>
        public static Dictionary<string, string> ToDictionary(this object o, params string[] ignore)
        {
            return o.GetType()
                       .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                       .Where(p => !ignore.Any(i => i.ToLower() == p.Name.ToLower()))
                       .ToDictionary(prop => prop.Name, prop => prop.GetValue(o, null)?.ToString() ?? string.Empty);
        }

        /// <summary>
        /// Gets the dictionary elements as Key:Value string with the given suffix.
        /// </summary>
        /// <param name="d">The dictionary to stringify.</param>
        /// <param name="suffix">A suffix to add to each keyvaluepair.</param>
        /// <returns>A Key:ValueSuffix string.</returns>
        public static string ToFriendlyString(this Dictionary<string, string> d, string suffix)
        {
            var appender = new StringBuilder();
            foreach (var item in d)
            {
                appender.Append(item.Key).Append(": ").Append(item.Value).AppendLine(suffix);
            }

            return appender.ToString();
        }

        /// <summary>
        /// Gets the dictionary elements as Key:Value string.
        /// </summary>
        /// <param name="d">The dictionary to stringify.</param>
        /// <returns>A Key:Value string.</returns>
        public static string ToFriendlyString(this Dictionary<string, string> d)
        {
            return d.ToFriendlyString(string.Empty);
        }
    }

    /// <summary>
    /// Base class for an entry.
    /// </summary>
    public abstract class Entry : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Entry"/> class.
        /// </summary>
        /// <param name="creationDate">Date the entry was created.</param>
        public Entry(DateTime creationDate)
        {
            CreationDate = creationDate;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the date the entry was created.
        /// </summary>
        public DateTime CreationDate { get; }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
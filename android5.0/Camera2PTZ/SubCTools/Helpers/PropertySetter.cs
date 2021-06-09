// <copyright file="PropertySetter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using SubCTools.Attributes;
    using SubCTools.Interfaces;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class PropertySetter
    {
        private readonly IDispatcher dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertySetter"/> class.
        /// </summary>
        public PropertySetter()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertySetter"/> class.
        /// </summary>
        /// <param name="notifier"></param>
        public PropertySetter(INotifier notifier)
            : this(notifier, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertySetter"/> class.
        /// </summary>
        /// <param name="notifier"></param>
        /// <param name="dispatcher"></param>
        public PropertySetter(INotifier notifier, IDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;

            notifier.Notify += Notifier_Notify;
        }

        public IList<object> ReactiveObjects
        {
            get;
        }
= new List<object>();

        public Dictionary<object, string> AppendPropertyName
        {
            get;
        }
= new Dictionary<object, string>();

        public List<string> ToRemove { get; } = new List<string>();

        public bool UseBoundary { get; set; }

        public bool MatchBeginning { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="command"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public Task ProcessCommandAsync(string command)
        {
            return Task.Run(() => ProcessCommand(command));
        }

        public void ProcessCommand(string command)
        {
            command = ToRemove.Aggregate(command, (current, str) => current.Replace(str, string.Empty));

            var matches = from obj in ReactiveObjects
                          from prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                          let propName = AppendPropertyName.ContainsKey(obj) ? prop.Name + AppendPropertyName[obj] : prop.Name
                          let match = Regex.Match(command, (MatchBeginning ? "^" : string.Empty) + (UseBoundary ? @"\b" : string.Empty) + propName + @":(.*)")
                          where match.Success
                          select new { obj, prop, match.Groups[1].Value };

            foreach (var match in matches)
            {
                TrySetProperty(match.obj, match.prop, match.Value);
            }

            var attributeMatches = from obj in ReactiveObjects
                                   from prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                   from reactive in prop.GetCustomAttributes(true).OfType<ReactiveAttribute>()
                                   let match = Regex.Match(command, (MatchBeginning ? "^" : string.Empty) + (UseBoundary ? @"\b" : string.Empty) + (!string.IsNullOrEmpty(reactive.Name) ? reactive.Name : prop.Name) + @":(.*)")
                                   where match.Success
                                   select new { obj, prop, match.Groups[1].Value };

            foreach (var match in attributeMatches)
            {
                TrySetProperty(match.obj, match.prop, match.Value);
            }
        }

        private async void Notifier_Notify(object sender, NotifyEventArgs e)
        {
            // return if this isn't a receive type message, or it's empty
            if (e.MessageType != MessageTypes.Receive || string.IsNullOrEmpty(e.Message))
            {
                return;
            }

            await ProcessCommandAsync(e.Message);
        }

        private bool TrySetProperty(object obj, PropertyInfo prop, string value)
        {
            value = value.Trim();// .TrimEnd(' ', '-');
            object convertedValue;

            // convert the value if you have one
            if (prop.GetCustomAttributes(true).OfType<ConverterAttribute>().Any())
            {
                value = prop.GetCustomAttributes(true)
                    .OfType<ConverterAttribute>()
                    .First()
                    .Converter
                    .TryConvert(value, out convertedValue).ToString();
            }
            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
            {
                // get the associated converter
                var converter = TypeDescriptor.GetConverter(prop.PropertyType);

                try
                {
                    // convert the value
                    convertedValue = value == "null" ? null : converter.ConvertFrom(value);
                }
                catch
                {
                    // default to converting numbers to bool if a converter hasn't been specified
                    convertedValue = value == "1";
                }
            }
            else
            {
                convertedValue = value;
            }

            try
            {
                // check to see if it's a nullable type before you try to convert
                var nullableBaseType = Nullable.GetUnderlyingType(prop.PropertyType);

                // var setValue = nullableBaseType != null
                //    ? Convert.ChangeType(convertedValue, nullableBaseType)
                //    : Convert.ChangeType(convertedValue, prop.PropertyType);
                if (convertedValue != null
                    && nullableBaseType == null
                    && convertedValue.GetType() != prop.PropertyType)
                {
                    convertedValue = Convert.ChangeType(convertedValue, prop.PropertyType, CultureInfo.InvariantCulture);
                }

                if (dispatcher != null)
                {
                    dispatcher.Invoke(() =>
                    {
                        prop?.SetValue(obj, convertedValue, new object[] { });
                    });
                }
                else
                {
                    prop?.SetValue(obj, convertedValue, new object[] { });
                }

                return true;
            }
            catch
            {
                Console.WriteLine("Could not convert value: {0} to type: {1} on {2}", value, prop.PropertyType.Name, obj.GetType().Name);

                // could not convert type
                return false;
            }
        }
    }
}
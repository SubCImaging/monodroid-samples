//-----------------------------------------------------------------------
// <copyright file="NmeaField.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Models
{
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel;

    public class NmeaField : INotifyPropertyChanged
    {
        /// <summary>
        /// Default name to use.
        /// </summary>
        public const string DefaultName = "-Ignore-";

        private string name = DefaultName;
        private object val;

        /// <summary>
        /// Initializes a new instance of the <see cref="NmeaField"/> class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public NmeaField(string value, string name = DefaultName)
        {
            Name = name;
            Type = GetFieldType(value);
            UpdateValue(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NmeaField"/> class.
        /// </summary>
        /// <param name="name"></param>
        [JsonConstructor]
        public NmeaField(string name = DefaultName)
        {
            Name = name;
            Type = typeof(int);
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                {
                    return;
                }

                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        [JsonIgnore]
        public Type Type { get; set; }

        [JsonIgnore]
        public object Value
        {
            get => val;
            set
            {
                if (val == value)
                {
                    return;
                }

                val = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public void UpdateValue(string value)
        {
            if (!TryParseField(value, Type, out var v))
            {
                Type = GetFieldType(value);
                if (TryParseField(value, Type, out var vv))
                {
                    Value = vv;
                }
            }

            Value = v;
        }

        private Type GetFieldType(string s)
        {
            //if(int.TryParse(s, out var i))
            //{
            //    return typeof(int);
            //}

            try
            {
                int.Parse(s);
                return typeof(int);
            }
            catch
            {
            }

            try
            {
                double.Parse(s);
                return typeof(double);
            }
            catch
            {
            }

            try
            {
                DateTime.Parse(s);
                return typeof(DateTime);
            }
            catch
            {
            }

            return typeof(string);
        }

        private bool TryParseField(string s, Type type, out object value)
        {
            if (type == typeof(string))
            {
                value = s;
                return true;
            }

            try
            {
                var converter = TypeDescriptor.GetConverter(type);
                if (converter != null)
                {
                    value = converter.ConvertFromString(s);
                    return true;
                }

                value = s;
                return false;
            }
            catch
            {
                value = s;
                return false;
            }
        }
    }
}
// <copyright file="JSONRecord.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings
{
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// A POCO for serializing objects to be stored in an SQLite database.
    /// </summary>
    public class JSONRecord
    {
        /// <summary>
        /// Serialization settings.
        /// </summary>
        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JSONRecord"/> class.
        /// Required parameterless constructor.
        /// </summary>
        public JSONRecord()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSONRecord"/> class.
        /// </summary>
        /// <param name="type">The name of the type of the object being stored.</param>
        /// <param name="pk">The serialized primary key of the object being stored.</param>
        /// <param name="obj">The serialized object being stored.</param>
        public JSONRecord(string type, string pk, string obj)
        {
            Type = type;
            PrimaryKey = pk;
            SerializedObject = obj;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSONRecord"/> class.
        /// </summary>
        /// <param name="type">The type of the object being stored.</param>
        /// <param name="pk">The primary key of the object being stored.</param>
        /// <param name="obj">The object being stored.</param>
        public JSONRecord(Type type, object pk, object obj)
        {
            Type = type.Name;
            PrimaryKey = Serialize(pk);
            SerializedObject = Serialize(obj);
        }

        /// <summary>
        /// Gets or sets a property for holding the serialized primary key.  Properties translate directly to database columns.
        /// </summary>
        public string PrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets a property for holding the serialized object.  Properties translate directly to database columns.
        /// </summary>
        public string SerializedObject { get; set; }

        /// <summary>
        /// Gets or sets a property for holding the type of the object.  Properties translate directly to database columns.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Converts the JSON string back into an object.
        /// </summary>
        /// <typeparam name="T">The type of the object to create.</typeparam>
        /// <param name="s">The JSON string.</param>
        /// <returns>A value of type <see cref="T"/>.</returns>
        public static T Deserialize<T>(string s)
        {
            return JsonConvert.DeserializeObject<T>(s.Replace("`", "\"").Replace("&backtick;", "`"), jsonSettings);
        }

        /// <summary>
        /// Converts an object into a JSON string.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>A string representing the object.</returns>
        public static string Serialize(object o)
        {
            return JsonConvert.SerializeObject(o, jsonSettings).Replace("`", "&backtick;").Replace("\"", "`");
        }

        /// <summary>
        /// Accessor.
        /// </summary>
        /// <typeparam name="T">The type of the object to create.</typeparam>
        /// <returns>A value of type <see cref="T"/>.</returns>
        public T GetObject<T>()
        {
            return Deserialize<T>(SerializedObject);
        }

        /// <summary>
        /// Accessor.
        /// </summary>
        /// <typeparam name="T">The type of the object to create.</typeparam>
        /// <returns>The primary key.</returns>
        public object GetPrimaryKey<T>()
        {
            return Deserialize<T>(PrimaryKey);
        }
    }
}
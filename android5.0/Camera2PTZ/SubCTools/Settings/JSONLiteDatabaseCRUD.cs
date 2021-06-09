// <copyright file="JSONLiteDatabaseCRUD.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings
{
    using LiteDB;
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Extensions;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Class for loading and saving JSON data from a file.
    /// </summary>
    public class JSONLiteDatabaseCRUD : IFileCRUD
    {
        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
        };

        private readonly LiteDatabase db;

        /// <summary>
        /// Initializes a new instance of the <see cref="JSONLiteDatabaseCRUD"/> class.
        /// </summary>
        /// <param name="file">File to save and load data to/from.</param>
        public JSONLiteDatabaseCRUD(FileInfo file)
        {
            File = file;
            db = new LiteDatabase(file.FullName);
        }

        /// <summary>
        /// Gets the file of the settings.
        /// </summary>
        public FileInfo File { get; }

        /// <summary>
        /// Create a new entry in the settings. Deletes previous entry of same type with same ID.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="entry">Entry to enter in to settings.</param>
        public void Create<T>(T entry)
        {
            if (Exists(entry))
            {
                Delete(entry);
            }

            var t = typeof(T).ToString();
            var key = GetID(entry);
            var contents = JsonConvert.SerializeObject(entry, jsonSettings);

            var c = db.GetCollection<string>(t);
            c.Insert(new BsonValue(key), contents);
        }

        /// <summary>
        /// Delete the entry from the settings.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="entry">Entry to delete from settings.</param>
        public void Delete<T>(T entry)
        {
            var o = Read(entry);

            if (o == null)
            {
                throw new NullReferenceException($"Entry with id {GetID(entry)} not found");
            }

            var t = typeof(T).ToString();
            var key = GetID(entry);
            var c = db.GetCollection<string>(t);
            c.Delete(new BsonValue(key));
        }

        /// <summary>
        /// Determines if an entry with the matching type <see cref="T"/> and ID exists.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <param name="entry">Entry with the ID of the object to load.</param>
        /// <returns>True if the item exists.</returns>
        public bool Exists<T>(T entry)
        {
            var t = typeof(T).ToString();
            var key = GetID(entry);
            var c = db.GetCollection<string>(t);
            return c.FindOne(s => GetID((T)JsonConvert.DeserializeObject(s, jsonSettings)) == key) != null;
        }

        /// <summary>
        /// Get the object from the settings.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <param name="entry">Entry with the ID of the object to load.</param>
        /// <returns>Object with the same ID from the settings.</returns>
        public T Read<T>(T entry)
        {
            var t = typeof(T).ToString();
            var key = GetID(entry);
            var c = db.GetCollection<string>(t);
            var contents = c.FindOne(s => GetID((T)JsonConvert.DeserializeObject(s, jsonSettings)) == key);
            return string.IsNullOrEmpty(contents) ? default : (T)JsonConvert.DeserializeObject(contents, jsonSettings);
        }

        /// <summary>
        /// Read all the objects of the given type.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <returns>Array of all the objects of the given type.</returns>
        public T[] ReadAll<T>()
        {
            var t = typeof(T).ToString();
            var c = db.GetCollection<string>(t);
            return c.FindAll().Select(s => (T)JsonConvert.DeserializeObject(s, jsonSettings)).ToArray();
        }

        /// <summary>
        /// Update the entry with the given ID.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <param name="entry">Entry to update.</param>
        public void Update<T>(T entry)
        {
            if (Exists(entry))
            {
                Delete(entry);
                Create(entry);
            }
        }

        private object GetID<T>(T entry)
        {
            var key = entry.GetType().GetProperties().FirstOrDefault(p => p.HasAttribute<PrimaryKey>())?.GetValue(entry);

            if (key == null)
            {
                throw new Exception($"Type {typeof(T)} doesn't have a key");
            }

            return key;
        }
    }
}
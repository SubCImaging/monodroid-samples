// <copyright file="JSONFileCRUD.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings
{
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Class for loading and saving JSON data from a file.
    /// </summary>
    public class JSONFileCRUD : IFileCRUD
    {
        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
        };

        private List<object> settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="JSONFileCRUD"/> class.
        /// </summary>
        /// <param name="file">File to save and load data to/from.</param>
        public JSONFileCRUD(FileInfo file)
        {
            File = file;
            Load();
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
            if (Read(entry) != null)
            {
                Delete(entry);
            }

            settings.Add(entry);
            Save();
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

            settings.Remove(o);
            Save();
        }

        /// <summary>
        /// Get the object from the settings.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <param name="entry">Entry with the ID of the object to load.</param>
        /// <returns>Object with the same ID from the settings.</returns>
        public T Read<T>(T entry) //=> (T)settings.Find(s => GetID(s).ToString() == GetID(entry).ToString() && s.GetType() == typeof(T));
        {
            // get the id from the given entry
            var id = GetID(entry);

            // get all the objects in the settings that are of the same type you want to read
            var objectsOfType = settings.Where(s => s.GetType() == typeof(T)).ToList();

            return (T)objectsOfType.FirstOrDefault(o => GetID(o).ToString() == id.ToString());
        }

        /// <summary>
        /// Read all the objects of the given type.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <returns>Array of all the objects of the given type.</returns>
        public T[] ReadAll<T>()
        {
            return (from s in settings
                    where s is T
                    select (T)s).ToArray();
        }

        /// <summary>
        /// Update the entry with the given ID.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <param name="entry">Entry to update.</param>
        public void Update<T>(T entry)
        {
            if (Read(entry) != null)
            {
                Delete(entry);
                Create(entry);
            }
        }

        private object GetID<T>(T entry)
        {
            if (entry == null)
            {
                throw new Exception($"Object of type {typeof(T)} is null");
            }

            var key = Array.Find(entry.GetType().GetProperties(), p => p.HasAttribute<PrimaryKey>());

            if (key == null)
            {
                throw new Exception($"Type {typeof(T)} doesn't have a key");
            }

            var keyValue = key.GetValue(entry);

            if (keyValue == null)
            {
                throw new NullReferenceException($"The {nameof(PrimaryKey)} object was found, but it's value is null");
            }

            return keyValue;
        }

        private void Load()
        {
            if (File == null)
            {
                settings = new List<object>();
                return;
            }

            // Use static System.IO methods because the object ones can fail
            if (!System.IO.Directory.Exists(File.Directory.FullName))
            {
                File.Directory.Create();
            }

            // Use static System.IO methods because the object ones can fail
            if (!System.IO.File.Exists(File.FullName))
            {
                using var f = File.Create();
            }

            settings = JsonConvert.DeserializeObject<List<object>>(System.IO.File.ReadAllText(File.FullName), jsonSettings) ?? new List<object>();
        }

        private void Save()
        {
            if (File != null)
            {
                var contents = JsonConvert.SerializeObject(settings.ToList(), jsonSettings);
                try
                {
                    Timers.RetryWithTimeout(() => System.IO.File.WriteAllText(File.FullName, contents), TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="SqlSettings.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe, Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Settings
{
    using Android.Content;
    using Newtonsoft.Json;
    using SubCTools.Droid.SQLite;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Save/Load all application settings with a sqlite database.
    /// </summary>
    public class SqlSettings : ISettingsService
    {
        /// <summary>
        /// Connection to the database file.
        /// </summary>
        private readonly GoogleSQLiteConnection db;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSettings"/> class.
        /// </summary>
        /// <param name="databasePath">Path to the database will, will create if doesn't exist.</param>
        public SqlSettings(Context context, string databasePath)
            : this(new GoogleSQLiteConnection(context))
        {
            var file = new FileInfo(databasePath);

            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            db.Open(file.FullName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSettings"/> class.
        /// </summary>
        /// <param name="db">The <see cref="ISQLiteConnection"/> to use as the db.</param>
        public SqlSettings(GoogleSQLiteConnection db)
        {
            this.db = db;
        }

        /// <summary>
        /// Add a new entry to the data base, will update if already exists.
        /// </summary>
        /// <param name="entry">ID of the entry.</param>
        /// <param name="nodeValue">Value of data.</param>
        /// <param name="attributes">Any additional attributes you wish to save.</param>
        public void Add(string entry, string nodeValue = null, Dictionary<string, string> attributes = null)
        {
            var encodedAttributes = string.Empty;
            if (attributes != null)
            {
                // encode the attributes to save as a string in to the database
                // encodedAttributes = string.Join(",", attributes?.Select(k => $"{k.Key}: {k.Value}"));
                encodedAttributes = JsonConvert.SerializeObject(attributes);
            }

            var sqlInfo = new SqlInfo() { Name = entry, Value = nodeValue, Attributes = encodedAttributes };

            // update the database entry if the key already exists, insert otherwise
            if (db.Read(sqlInfo.Name) != null)
            {
                db.Update(sqlInfo);
            }
            else
            {
                db.Create(sqlInfo);
            }
        }

        /// <summary>
        /// Load the name, value, and attributes of the associated entry key.
        /// </summary>
        /// <param name="entry">Entry key for data to load.</param>
        /// <param name="createNewIfDoesntExist">Deprecated.</param>
        /// <returns>XInfo with name, value, and attributes if the entry key exists, null if not.</returns>
        public XInfo Load(string entry, bool createNewIfDoesntExist = true)
        {
            if (entry.Equals("r", StringComparison.InvariantCultureIgnoreCase))
            {
            }

            var sqlInfo = db.Read(entry);
            if (sqlInfo != null)
            {
                return new XInfo(sqlInfo.Name, sqlInfo.Value, GetAttributes(sqlInfo.Attributes));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Load the converted value of the given entry, e.g. Entry = Age, Value = 30, convert "30" in to 30.
        /// </summary>
        /// <typeparam name="T">Type of object to try to convert to.</typeparam>
        /// <param name="entry">Entry key of value you'd like to retrieve.</param>
        /// <param name="createNewIfDoesntExist">Deprecated.</param>
        /// <returns>Converted value if success, default value of type if not.</returns>
        public T Load<T>(string entry, bool createNewIfDoesntExist = true)
        {
            T newValue = default;

            if (entry.Equals("r", StringComparison.InvariantCultureIgnoreCase))
            {
            }

            var loadValue = Load(entry, createNewIfDoesntExist).Value;

            try
            {
                newValue = (T)Convert.ChangeType(loadValue, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            return newValue;
        }

        /// <summary>
        /// Load all values with the given entry point.
        /// </summary>
        /// <param name="entry">Entry of values to load.</param>
        /// <returns>Entry = Files will return File1, File2, File3 if the exist.</returns>
        public IEnumerable<XInfo> LoadAll(string entry = "SubC")
        {
            // this is a remnant from SubCXDoc
            if (entry == "SubC")
            {
                entry = string.Empty;
            }

            var results = new List<XInfo>();

            // get all the entries that start with the given entry
            var query = from q in db.ReadAll()
                        where q.Name.StartsWith(entry)
                        select q;

            // go through all the results and add them to the list to be returned
            foreach (var item in query)
            {
                var name = item.Name;

                if (!string.IsNullOrEmpty(entry))
                {
                    name = item.Name.Replace(entry, string.Empty).TrimStart('\\').TrimStart('/');
                }

                results.Add(new XInfo(name, item.Value, GetAttributes(item.Attributes)));
            }

            return results;
        }

        /// <summary>
        /// Opens the database from a given filename.
        /// </summary>
        /// <param name="fileName">The sqlite file.</param>
        /// <param name="overwrite">Not used.</param>
        public void Open(string fileName, bool overwrite = false)
        {
            db.Open(fileName);
        }

        /// <summary>
        /// Remove the data at the given entry key.
        /// </summary>
        /// <param name="entry">Entry key that holds the data.</param>
        public void Remove(string entry)
        {
            var sqlInfo = db.Read(entry);
            if (sqlInfo != null)
            {
                db.Delete(sqlInfo.Name);
            }
        }

        /// <summary>
        /// Try to load a value of the given type.
        /// </summary>
        /// <typeparam name="T">Type to convert value in to.</typeparam>
        /// <param name="entry">Entry key of data.</param>
        /// <param name="value">Converted value.</param>
        /// <returns>True if the value was loaded and converted, false if not.</returns>
        public bool TryLoad<T>(string entry, out T value)
        {
            if (Load(entry) != null)
            {
                value = Load<T>(entry);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Update an entry to the data base, will add if doesn't exist.
        /// </summary>
        /// <param name="entry">ID of the entry.</param>
        /// <param name="nodeValue">Value of data.</param>
        /// <param name="attributes">Any additional attributes you wish to save.</param>
        /// <param name="overwrite">Deprecated.</param>
        public void Update(string entry, string nodeValue = null, Dictionary<string, string> attributes = null, bool overwrite = true)
        {
            Add(entry, nodeValue, attributes);
        }

        /// <summary>
        /// Update an entry to the data base, will add if doesn't exist.
        /// </summary>
        /// <param name="entry">ID of the entry.</param>
        /// <param name="nodeValue">Value of data.</param>
        /// <param name="attributes">Any additional attributes you wish to save.</param>
        /// <param name="overwrite">Deprecated.</param>
        public void Update(string entry, object nodeValue, Dictionary<string, string> attributes = null, bool overwrite = true)
        {
            Add(entry, nodeValue.ToString(), attributes);
        }

        /// <summary>
        /// Get the dictionary of attributes from the encoded attributes string.
        /// </summary>
        /// <param name="encodedAttributes">Encoded string that was entered in to the database.</param>
        /// <returns>Dictionary of attributes if there are any in the encoded string, empty dictionary if not.</returns>
        private static Dictionary<string, string> GetAttributes(string encodedAttributes)
        {
            try
            {
                // try the new methods of getting the attributes
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(encodedAttributes);
            }
            catch
            {
                // use the old method if it fails
                var attributes = new Dictionary<string, string>();
                var match = Regex.Matches(encodedAttributes, "(\\w+):\\s*([^,|\"]*)");
                if (match.Count > 0)
                {
                    foreach (Match item in match)
                    {
                        attributes.Add(item.Groups[1].Value, item.Groups[2].Value);
                    }
                }

                return attributes;
            }
        }
    }
}
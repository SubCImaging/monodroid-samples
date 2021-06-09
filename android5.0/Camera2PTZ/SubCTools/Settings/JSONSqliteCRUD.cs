//// <copyright file="JSONSqliteCRUD.cs" company="SubC Imaging">
//// Copyright (c) SubC Imaging. All rights reserved.
//// </copyright>

//namespace SubCTools.Settings
//{
//    using SQLite;
//    using SubCTools.Attributes;
//    using SubCTools.Extensions;
//    using SubCTools.Settings.Interfaces;
//    using System;
//    using System.IO;
//    using System.Linq;

//    /// <summary>
//    /// A CRUD that stores records into an SQLite database.
//    /// </summary>
//    public class JSONSqliteCRUD : ICRUD
//    {
//        private readonly SQLiteConnection db;

//        /// <summary>
//        /// Initializes a new instance of the <see cref="JSONSqliteCRUD"/> class.
//        /// </summary>
//        /// <param name="file">The database file.</param>
//        public JSONSqliteCRUD(FileInfo file)
//        {
//            db = new SQLiteConnection(file.FullName);
//            db.CreateTable<JSONRecord>();
//        }

//        /// <inheritdoc/>
//        public void Create<T>(T entry)
//        {
//            try
//            {
//                Read(entry);
//            }
//            catch
//            {
//                db.Insert(GetJSONRecord<T>(entry));
//            }
//        }

//        /// <inheritdoc/>
//        public void Delete<T>(T entry)
//        {
//            db.Execute($"DELETE FROM \"JSONRecord\" WHERE Type = \"{typeof(T).Name}\" AND PrimaryKey = \"{GetJSONRecord<T>(entry).PrimaryKey}\";");
//        }

//        /// <inheritdoc/>
//        public T Read<T>(T entry)
//        {
//            var pk = JSONRecord.Serialize(GetID(entry));
//            return db.Table<JSONRecord>().Where(r => r.Type == typeof(T).Name && r.PrimaryKey == pk).FirstOrDefault().GetObject<T>();
//        }

//        /// <inheritdoc/>
//        public T[] ReadAll<T>()
//        {
//            var t = db.Table<JSONRecord>();
//            return t.Where(r => r.Type == typeof(T).Name).Select(r => r.GetObject<T>()).ToArray();
//        }

//        /// <inheritdoc/>
//        public void Update<T>(T entry)
//        {
//            var o = GetJSONRecord<T>(entry);
//            db.Execute($"UPDATE \"JSONRecord\" SET SerializedObject = \"{o.SerializedObject}\" WHERE Type = \"{typeof(T).Name}\" AND PrimaryKey = \"{o.PrimaryKey}\";");
//        }

//        /// <summary>
//        /// Retrieves the <see cref="PrimaryKey"/> value from an object.
//        /// </summary>
//        /// <typeparam name="T">The type of the object.</typeparam>
//        /// <param name="entry">The object.</param>
//        /// <returns>The primary key value.</returns>
//        private object GetID<T>(T entry)
//        {
//            if (entry == null)
//            {
//                throw new NullReferenceException($"Object of type {typeof(T)} is null");
//            }

//            var key = Array.Find(entry.GetType().GetProperties(), p => p.HasAttribute<PrimaryKey>());

//            if (key == null)
//            {
//                throw new InvalidOperationException($"Type {typeof(T)} doesn't have a key");
//            }

//            var keyValue = key.GetValue(entry);

//            if (keyValue == null)
//            {
//                throw new NullReferenceException($"The {nameof(PrimaryKey)} object was found, but it's value is null");
//            }

//            return keyValue;
//        }

//        /// <summary>
//        /// Creates a <see cref="JSONRecord"/> from an object.
//        /// </summary>
//        /// <typeparam name="T">The type of the object.</typeparam>
//        /// <param name="obj">The object.</param>
//        /// <returns>The JSONRecord.</returns>
//        private JSONRecord GetJSONRecord<T>(object obj)
//        {
//            try
//            {
//                return new JSONRecord(typeof(T), GetID(obj), obj);
//            }
//            catch
//            {
//                throw new Exception("Error creating JSONRecord.");
//            }
//        }
//    }
//}
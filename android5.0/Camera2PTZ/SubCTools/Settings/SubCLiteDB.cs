// <copyright file="SubCLiteDB.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings
{
    using LiteDB;
    using SubCTools.Attributes;
    using SubCTools.Extensions;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.IO;
    using System.Linq;

    public class SubCLiteDB : ICRUD
    {
        private readonly LiteDatabase db;

        public SubCLiteDB(FileInfo path)
        {
            db = new LiteDatabase(path.FullName);
        }

        public static object GetID<T>(T entry)
        {
            var key = entry.GetType().GetProperties().FirstOrDefault(p => p.HasAttribute<PrimaryKey>())?.GetValue(entry);

            if (key == null)
            {
                throw new Exception($"Type {typeof(T)} doesn't have a key");
            }

            return key;
        }

        public void Create<T>(T entry)
        {
            var c = db.GetCollection<T>();
            c.Insert(entry);
        }

        public void Delete<T>(T entry)
        {
            var c = db.GetCollection<T>();
            c.Delete(e => GetID(e) == GetID(entry));
        }

        public T Read<T>(object id)
        {
            var c = db.GetCollection<T>();
            var result = c.FindOne(s => GetID(s) == id);
            return result;
        }

        public T Read<T>(T entry)
        {
            var c = db.GetCollection<T>();
            var result = c.FindOne(s => GetID(s) == (object)entry);
            return result;
        }

        public T[] ReadAll<T>()
        {
            var c = db.GetCollection<T>();
            return c.FindAll().ToArray();
        }

        public void Update<T>(T entry)
        {
            Delete(entry);
            Create(entry);
        }
    }
}
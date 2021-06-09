// <copyright file="RayfinDive.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid
{
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.DiveLog;
    using SubCTools.DiveLog.Interfaces;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;

    public class JSONCrud : ICRUD
    {
        private FileInfo file = new FileInfo(@"Settings\Settings.json");

        public void Create<T>(T entry)
        {
            var s = File.ReadAllText(file.FullName).Split('\n').ToList();
            s.Add(JsonConvert.SerializeObject(entry));
            File.WriteAllText(file.FullName, string.Join("\n", s));
        }

        public void Delete<T>(T entry)
        {
        }

        public T Read<T>(string id)
        {
            throw new NotImplementedException();
        }

        public void Update<T>(T entry)
        {
            throw new NotImplementedException();
        }
    }
}
// <copyright file="ISettingsService.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings.Interfaces
{
    using System.Collections.Generic;

    public interface ISettingsService
    {
        void Add(string entry, string nodeValue = null, Dictionary<string, string> attributes = null);

        XInfo Load(string entry, bool createNewIfDoesntExist = true);

        T Load<T>(string entry, bool createNewIfDoesntExist = true);

        IEnumerable<XInfo> LoadAll(string entry = "SubC");

        void Open(string fileName, bool overwrite = false);

        void Remove(string entry);

        bool TryLoad<T>(string entry, out T value);

        void Update(string entry, string nodeValue = null, Dictionary<string, string> attributes = null, bool overwrite = true);

        void Update(string entry, object nodeValue, Dictionary<string, string> attributes = null, bool overwrite = true);
    }
}
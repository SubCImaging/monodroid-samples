// <copyright file="ILoadable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings.Interfaces
{
    public interface ILoadable
    {
        void LoadSettings(string path);

        void SaveSettings();
    }
}

// <copyright file="IOHelper.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using System.Collections.Generic;
    using System.IO;

    public class IOHelper
    {
        /// <summary>
        /// Command for getting the drives attached to the system.
        /// </summary>
        /// <returns>Collection of drive infos.</returns>
        [RemoteCommand]
        public IEnumerable<DriveInfo> Dir()
        {
            return DriveInfo.GetDrives();
        }

        [RemoteCommand]
        public string Dir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                return string.Empty;
            }

            var d = Directory.GetDirectories(dir);
            var f = Directory.GetFiles(dir);

            return JsonConvert.SerializeObject(new Dictionary<string, IEnumerable<string>> { { "Directories", d }, { "Files", f } });
        }
    }
}
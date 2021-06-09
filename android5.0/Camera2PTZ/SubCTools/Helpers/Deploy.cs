// <copyright file="Deploy.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using SubCTools.Enums;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public static class Deploy
    {
        public static DeploymentEnvironments GetDeploymentEnvironment()
        {
            var ver = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion.ToLower();
            var match = Regex.Match(ver, @"(?:.*\s)?v?([0-9]+)\.([0-9]+)(?:\.([0-9]+))?(([\s-]?)([a-zA-Z]+)(?:\.(\d+))?)?");
            if (match.Success)
            {
                if (match.Groups[3].Value == string.Empty)
                {
                    return DeploymentEnvironments.Dev;
                }

                switch (match.Groups[6].Value)
                {
                    case "a":
                        return DeploymentEnvironments.Alpha;

                    case "b":
                        return DeploymentEnvironments.Beta;

                    case "rc":
                        return DeploymentEnvironments.ReleaseCandidate;

                    case "":
                        return DeploymentEnvironments.Production;
                }

                return DeploymentEnvironments.Dev;
            }
            else
            {
                return DeploymentEnvironments.Dev;
            }
        }
    }
}

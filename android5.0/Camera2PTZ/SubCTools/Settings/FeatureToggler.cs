//-----------------------------------------------------------------------
// <copyright file="FeatureToggler.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Settings
{
    using SubCTools.Enums;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// A class for checking which features are enabled.
    /// </summary>
    public class FeatureToggler
    {
        private readonly SubCXDoc reader;
        private readonly List<(string Name, bool Debug, bool Dev, bool Production)> toggles = new List<(string, bool, bool, bool)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureToggler"/> class.
        /// </summary>
        /// <param name="togglesFile">a source XML file containing the feature toggles.</param>
        /// <param name="env">the current deployment environment.</param>
        public FeatureToggler(FileInfo togglesFile, DeploymentEnvironments env)
        {
            environment = env;

            if (togglesFile.Exists)
            {
                reader = new SubCXDoc(new Uri(togglesFile.FullName));

                var loaded = reader.LoadAll();

                toggles = loaded.Select(
                    x => (x.Name,
                    Convert.ToBoolean(x.Attributes["Debug"]),
                    Convert.ToBoolean(x.Attributes["Dev"]),
                    Convert.ToBoolean(x.Attributes["Production"]))).ToList();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureToggler"/> class.
        /// </summary>
        /// <param name="togglesFileStream">An XML file stream containing the feature toggles.</param>
        /// <param name="env">the current deployment environment.</param>
        public FeatureToggler(Stream togglesFileStream, DeploymentEnvironments env)
        {
            environment = env;

            var doc = XDocument.Load(togglesFileStream);

            var loaded = doc.Elements().First().Elements();

            toggles = loaded.Select(
                x => (x.Name.ToString(),
                Convert.ToBoolean(x.Attributes().Where(a => a.Name.ToString() == "Debug").FirstOrDefault()?.Value ?? "false"),
                Convert.ToBoolean(x.Attributes().Where(a => a.Name.ToString() == "Dev").FirstOrDefault()?.Value ?? "false"),
                Convert.ToBoolean(x.Attributes().Where(a => a.Name.ToString() == "Production").FirstOrDefault()?.Value ?? "false"))).ToList();
        }

        /// <summary>
        /// Gets the current deployment environment.
        /// </summary>
        public DeploymentEnvironments environment { get; }

        /// <summary>
        /// Finds a specific toggle.  Returns all falses if it doesn't exist.
        /// </summary>
        /// <param name="featureName">the name of the feature.</param>
        /// <returns>a value tuple indicating which environments the feature is enabled in.</returns>
        public (string Name, bool Debug, bool Dev, bool Production) Find(string featureName)
        {
            (string Name, bool Debug, bool Dev, bool Production) result;

            try
            {
                result = toggles.Find(x => x.Name == featureName);
            }
            catch (ArgumentNullException)
            {
                result = (featureName, false, false, false);
            }

            return result;
        }

        /// <summary>
        /// Returns a boolean defining if a feature is enabled in the current deployment environment.
        /// </summary>
        /// <param name="featureName">The name of the feature.</param>
        /// <param name="env">The environment (found in app.xaml on windows apps).</param>
        /// <returns>a bool.</returns>
        public bool IsFeatureOn(string featureName)
        {
            return IsFeatureOn(featureName, environment);
        }

        /// <summary>
        /// Returns a boolean defining if a feature is enabled in the specified deployment environment.
        /// </summary>
        /// <param name="featureName">The name of the feature.</param>
        /// <param name="env">The environment.</param>
        /// <returns>a bool.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Redundancy", "RCS1163:Unused parameter.", Justification = "<Pending>")]
        public bool IsFeatureOn(string featureName, DeploymentEnvironments env)
        {
#if DEBUG
            return Find(featureName).Debug;
#else
            if (env == DeploymentEnvironments.ReleaseCandidate || env == DeploymentEnvironments.Production)
            {
                return Find(featureName).Production;
            }
            else
            {
                return Find(featureName).Dev;
            }
#endif
        }
    }
}
// <copyright file="SubCPropertySettings.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings
{
    using SubCTools.Attributes;
    using SubCTools.Interfaces;
    using SubCTools.Settings.Interfaces;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Old method of setting properties.
    /// </summary>
    public class SubCPropertySettings
    {
        private readonly object saveableClass;
        private readonly ISettingsService settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCPropertySettings"/> class.
        /// </summary>
        /// <param name="id">ID of the property to append on.</param>
        /// <param name="saveableClass">Class you want to save.</param>
        /// <param name="settings">Settings service to use.</param>
        public SubCPropertySettings(
            int id,
            object saveableClass,
            ISettingsService settings)
            : this(id.ToString(), saveableClass, settings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCPropertySettings"/> class.
        /// </summary>
        /// <param name="id">ID of the property to append on.</param>
        /// <param name="saveableClass">Class you want to save.</param>
        /// <param name="settings">Settings service to use.</param>
        public SubCPropertySettings(
            string id,
            object saveableClass,
            ISettingsService settings)
            : this(saveableClass, settings)
        {
            ID = id.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCPropertySettings"/> class.
        /// </summary>
        /// <param name="saveableClass">Class you want to save.</param>
        /// <param name="settings">Settings service to use.</param>
        public SubCPropertySettings(object saveableClass, ISettingsService settings)
        {
            this.saveableClass = saveableClass;
            this.settings = settings;
        }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Load the settings on the given object.
        /// </summary>
        /// <param name="obj">Object to load settings on.</param>
        /// <param name="settings">Settings service to load from.</param>
        /// <param name="dispatcher">Dispatcher used to make any UI related calls.</param>
        /// <param name="prefix">Prefix to put on the front.</param>
        /// <param name="suffix">Suffix to add to the back.</param>
        public static void LoadSettings(
            object obj,
            ISettingsService settings,
            IDispatcher dispatcher = null,
            string prefix = "",
            string suffix = "")
        {
            if (settings == null)
            {
                return;
            }

            // get all the savable properties
            var savableProperties = from p in obj.GetType().GetProperties()
                                    let attributes = p.GetCustomAttributes()
                                    where attributes.Any(a => a.GetType() == typeof(Savable))
                                    select p;

            // bail if you don't have any savable properties
            if (!savableProperties.Any())
            {
                return;
            }

            // order the Savable attributes by the Order property
            savableProperties = savableProperties
                .OrderBy(p => p.GetCustomAttributes(true).OfType<Savable>().First().Order);

            foreach (var property in savableProperties)
            {
                // get the savable attribute object
                var attribute = property.GetCustomAttribute<Savable>();

                // get the save name of one is set
                var saveName = !string.IsNullOrEmpty(attribute.SaveAsName) ? attribute.SaveAsName : property.Name;

                // create the settings key
                var key = prefix + saveName + suffix;

                // try to load the value, just keep going if you can't
                if (!settings.TryLoad(key, out string v))
                {
                    continue;
                }

                try
                {
                    object value;

                    // use the converter attribute if it exists, otherwise try to convert with the system converter
                    var propertyConverter = property.GetCustomAttribute<PropertyConverterAttribute>()?.Converter;
                    if (propertyConverter != null)
                    {
                        propertyConverter.TryConvert(v, out value);
                    }
                    else
                    {
                        // get the casting type from the attribute if one has been set
                        var type = attribute.CastingType ?? property.PropertyType;

                        // get the associated converter
                        var converter = TypeDescriptor.GetConverter(type);

                        // convert the value
                        value = converter.ConvertFrom(v);
                    }

                    // set the value if it's not null
                    if (value != null)
                    {
                        if (dispatcher != null)
                        {
                            dispatcher.Invoke(() => property.SetValue(obj, value, null));
                        }
                        else
                        {
                            property.SetValue(obj, value, null);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Load settings async.
        /// </summary>
        /// <param name="obj">Object to load settings on.</param>
        /// <param name="settings">Settings service to load from.</param>
        /// <param name="dispatcher">Dispatcher used to make any UI related calls.</param>
        /// <param name="prefix">Prefix to put on the front.</param>
        /// <param name="suffix">Suffix to add to the back.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task LoadSettingsAsync(object obj, ISettingsService settings, IDispatcher dispatcher = null, string prefix = "", string suffix = "")
        {
            await Task.Run(() => LoadSettings(obj, settings, dispatcher, prefix, suffix));
        }

        /// <summary>
        /// Load all the properties with the Saveable attribute.
        /// </summary>
        public void LoadSettings()
        {
            LoadSettings(saveableClass, settings, null, ID); // as SubCXDoc
        }

        /// <summary>
        /// Save all the properties with the attribute.
        /// </summary>
        public void SaveSettings()
        {
            foreach (var p in saveableClass.GetType().GetProperties()
                .Where(p => p.GetCustomAttributes(true).OfType<Savable>().Any()))
            {
                var val = p.GetValue(saveableClass, null);

                if (val != null)
                {
                    settings.Update(p.Name + ID != null ? ID : string.Empty, val.ToString(), new Dictionary<string, string>() { { "type", val.GetType().ToString() } });
                }
            }
        }
    }
}
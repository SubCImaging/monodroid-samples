//-----------------------------------------------------------------------
// <copyright file="DroidBase.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using SubCTools.Attributes;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Base class for droid objects that want a notify shortcut.
    /// </summary>
    public class DroidBase : INotifier
    {
        /// <summary>
        /// Object to save all settings on.
        /// </summary>
        private readonly object savableObject;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DroidBase()
            : this(null, null)
        {
        }

        /// <summary>
        /// Constructor that takes settings and the object to save.
        /// </summary>
        /// <param name="settings">Settings service to save data.</param>
        /// <param name="savable">Object to save data of.</param>
        public DroidBase(ISettingsService settings, object savable = null)
        {
            Settings = settings;
            this.savableObject = savable ?? this;
        }

        /// <summary>
        /// Notification event to alert other interested objects
        /// </summary>
        public event EventHandler<NotifyEventArgs> Notify;

        /// <summary>
        /// Gets the settings service.
        /// </summary>
        public ISettingsService Settings { get; }

        /// <summary>
        /// See if the new value is different, set if is, update the property in the settings service.
        /// </summary>
        /// <typeparam name="T">Type of property to set.</typeparam>
        /// <param name="propertyName">Name of property.</param>
        /// <param name="field">Backing field with data.</param>
        /// <param name="newValue">The new value to compare.</param>
        /// <returns>True if the new value is different from the old.</returns>
        public bool Set<T>(string propertyName, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;

            var property = savableObject.GetType().GetProperty(propertyName);

            if (property == null)
            {
                throw new Exception($"Property {propertyName} does exist on type {savableObject.GetType().Name}");
            }

            // get the savable property if there is one
            var savable = property.GetCustomAttribute<Savable>();

            if (Settings == null || savable == null)
            {
                return true;
            }

            // if you've set a SaveAsName, use that over the property name
            var name = !string.IsNullOrEmpty(savable.SaveAsName) ? savable.SaveAsName : propertyName;

            object value = field;

            var converter = property.GetCustomAttribute<PropertyConverterAttribute>()?.Converter;
            converter?.TryConvertBack(field, out value);

            Settings.Update(name, value);

            return true;
        }

        /// <summary>
        /// Load the settings on to the savable object from the settings service.
        /// </summary>
        public virtual void LoadSettings()
        {
            SubCPropertySettings.LoadSettings(savableObject, Settings);
        }

        /// <summary>
        /// Notify event invocation.
        /// </summary>
        /// <param name="message">Message to notify.</param>
        /// <param name="messageType">The type of message.</param>
        protected void OnNotify(string message,
            MessageTypes messageType = MessageTypes.Information)
        {
            message = message.EndsWith("\n") ? message : message + "\n";
            message = (messageType == MessageTypes.Error || messageType == MessageTypes.Alert) ? messageType.ToString() + ": " + message : message;
            OnNotify(savableObject, new NotifyEventArgs(message, messageType));
        }

        /// <summary>
        /// Notify event invocation.
        /// </summary>
        /// <param name="sender">Who's sending the event.</param>
        /// <param name="e">Event to notify.</param>
        protected void OnNotify(object sender, NotifyEventArgs e)
        {
            Notify?.Invoke(sender, e);
        }
    }
}
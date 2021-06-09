// <copyright file="EventEditor.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Droid
{
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Extensions;
    using SubCTools.Settings;
    using System.Collections.Generic;

    public class EventEditor
    {
        private readonly SubCXDoc eventsDoc = new SubCXDoc(@"Events\E.xml");

        /// <summary>
        /// Initializes a new instance of the <see cref="EventEditor"/> class.
        /// </summary>
        public EventEditor()
        {
            // load all the saved events
            LoadEvents();
        }

        /// <summary>
        /// Gets list of available events.
        /// </summary>
        public Dictionary<string, string> Events { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Clear events.
        /// </summary>
        [RemoteCommand]
        public void ClearEvents()
        {
            Events.Clear();

            var events = eventsDoc.LoadAll();

            foreach (var item in events)
            {
                eventsDoc.Remove(item.Name);
            }
        }

        /// <summary>
        /// Create a new event option.
        /// </summary>
        /// <param name="title">Title of event.</param>
        /// <param name="description">Default event description.</param>
        [RemoteCommand]
        public void CreateEvent(string title, string description)
        {
            Events.Update(title, description);
            eventsDoc.Update(title, description);
        }

        /// <summary>
        /// Get all available evets.
        /// </summary>
        /// <returns>Serialized events string.</returns>
        [RemoteCommand]
        public string GetEvents()
        {
            return JsonConvert.SerializeObject(Events);
        }

        /// <summary>
        /// Load all events from document to dictionary.
        /// </summary>
        private void LoadEvents()
        {
            var events = eventsDoc.LoadAll();

            foreach (var item in events)
            {
                Events.Update(item.Name, item.Value);
            }
        }
    }
}
// <copyright file="Inspection.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.DiveLog
{
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.Droid;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Class for intertacting with a db to create a relationship between media and data.
    /// </summary>
    public class Inspection : DroidBase
    {
        // private readonly IPictureTaker pictureTaker;
        private readonly IDiveRecorder recorder;

        private VideoEntry activeVideo;
        private IFileCRUD db;
        private DataEntry previousEntry;

        /// <summary>
        /// Initializes a new instance of the <see cref="Inspection"/> class.
        /// </summary>
        /// <param name="recorder">Object used to record video.</param>
        /// <param name="notifier">Object notifier when data should be added to file.</param>
        public Inspection(
            IDiveRecorder recorder,
            INotifier notifier)
        {
            this.recorder = recorder;
            recorder.Started += (_, e) => AddVideo(e);
            recorder.Stopped += (_, e) => UpdateVideoLength(e);

            if (notifier != null)
            {
                notifier.Notify += (_, e) => LogData(e);
            }
        }

        /// <summary>
        /// Event to fire when a dive is opened.
        /// </summary>
        public event EventHandler DiveOpened;

        /// <summary>
        /// Gets the file of the db on the file system.
        /// </summary>
        public FileInfo File => db.File;

        /// <summary>
        /// Gets or sets a value indicating whether the dive is open.
        /// </summary>
        public bool? IsOpen { get; set; } = false;

        /// <summary>
        /// Gets a value indicating whether the dive is started.
        /// </summary>
        public bool IsStarted => db != null;

        /// <summary>
        /// Get the definition from the db.
        /// </summary>
        /// <returns>Definition of input data.</returns>
        public IEnumerable<NmeaDefinition> GetDefinitions()
        {
            return db.ReadAll<NmeaDefinition>();
        }

        /// <summary>
        /// Get the JSON serialized stills log.
        /// </summary>
        /// <returns>JSON serialized stills log.</returns>
        [RemoteCommand]
        public string GetStillsLog()
        {
            return JsonConvert.SerializeObject(db.ReadAll<StillEntry>());
        }

        /// <summary>
        /// Get the JSON serialized stills log.
        /// </summary>
        /// <returns>JSON serialized stills log.</returns>
        [RemoteCommand]
        public IEnumerable<VideoEntry> GetVideoEntries()
        {
            return db.ReadAll<VideoEntry>();
        }

        /// <summary>
        /// Log an event with the given title.
        /// </summary>
        /// <param name="title">Title of the event to log.</param>
        [RemoteCommand]
        public void LogEvent(string title)
        {
            LogEvent(title, string.Empty, DateTime.Now);
        }

        /// <summary>
        /// Log an event with given title, description and event time.
        /// </summary>
        /// <param name="title">Title of the event to log.</param>
        /// <param name="description">Description to include with the event.</param>
        /// <param name="loggedTime">Time the event was logged.</param>
        [RemoteCommand]
        [PropertyConverter(typeof(StringToDateTime), "loggedTime")]
        public void LogEvent(string title, string description, DateTime loggedTime)
        {
            LogEvent(title, description, loggedTime, recorder.VideoTime);
        }

        /// <summary>
        /// Log an event with given title, description and event time.
        /// </summary>
        /// <param name="title">Title of the event to log.</param>
        /// <param name="description">Description to include with the event.</param>
        /// <param name="loggedTime">Time the event was logged.</param>
        /// <param name="eventTime">Time of the event after start of video.</param>
        [RemoteCommand]
        [PropertyConverter(typeof(StringToDateTime), "loggedTime")]
        [PropertyConverter(typeof(StringToTimeSpan), "eventTime")]
        public void LogEvent(string title, string description, DateTime loggedTime, TimeSpan eventTime)
        {
            if (!IsStarted)
            {
                OnNotify("Please start an inspection before performing this action");
                return;
            }

            // get the last data entry logged
            var lastData = activeVideo.DataEntries.LastOrDefault();

            DataEntry data = null;

            // if you've received a data entry within 5 seconds of taking this event, attach it to the event
            if (DateTime.Now - lastData?.CreationDate > TimeSpan.FromSeconds(5))
            {
                data = lastData;
            }

            LogEvent(title, description, loggedTime, eventTime, data);
        }

        /// <summary>
        /// Log an event with given title, description and event time.
        /// </summary>
        /// <param name="title">Title of the event to log.</param>
        /// <param name="description">Description to include with the event.</param>
        /// <param name="loggedTime">Time the event was logged.</param>
        /// <param name="data">data to associate with the event.</param>
        public void LogEvent(string title, string description, DateTime loggedTime, DataEntry data)
        {
            LogEvent(title, description, loggedTime, recorder.VideoTime, data);
        }

        /// <summary>
        /// Log an event with given title, description and event time.
        /// </summary>
        /// <param name="title">Title of the event to log.</param>
        /// <param name="description">Description to include with the event.</param>
        /// <param name="loggedTime">Time the event was logged.</param>
        /// <param name="eventTime">Time of the event after start of video.</param>
        /// <param name="data">data to associate with the event.</param>
        public void LogEvent(string title, string description, DateTime loggedTime, TimeSpan eventTime, DataEntry data)
        {
            // I'm not sure if we actually need to calc the difference, it's messing up Presenter. AR
            // get the difference between what time the event was supposed to be logged, with the current time right now
            // var diff = DateTime.Now - eventTime;
            // var entry = new DiveEntry(eventTime, recorder.CurrentTime - diff, title, description);
            var entry = new DiveEntry(loggedTime, eventTime, title, description);

            if (data != null)
            {
                entry.Data = data;
            }

            activeVideo.Entries.Add(entry);

            db.Update(activeVideo);

            OnNotify(JsonConvert.SerializeObject(entry));
        }

        /// <summary>
        /// Update an event with given title, description and event time.
        /// </summary>
        /// <param name="title">Title of the event to log.</param>
        /// <param name="description">Description to include with the event.</param>
        /// <param name="loggedTime">Time the event was logged.</param>
        /// <param name="eventTime">Time of the event after start of video.</param>
        [RemoteCommand]
        [Alias("UpdateEntry")]
        [PropertyConverter(typeof(StringToDateTime), "loggedTime")]
        [PropertyConverter(typeof(StringToTimeSpan), "eventTime")]
        public void UpdateEvent(string title, string description, DateTime loggedTime, TimeSpan eventTime)
        {
            UpdateEvent(title, description, loggedTime, eventTime, null);
        }

        /// <summary>
        /// Update an event with given title, description and event time.
        /// </summary>
        /// <param name="title">Title of the event to log.</param>
        /// <param name="description">Description to include with the event.</param>
        /// <param name="loggedTime">Time the event was logged.</param>
        /// <param name="eventTime">Time of the event after start of video.</param>
        /// <param name="data">data to associate with the event.</param>
        public void UpdateEvent(string title, string description, DateTime loggedTime, TimeSpan eventTime, DataEntry data)
        {
            var entry = new DiveEntry(loggedTime, eventTime, title, description);

            if (activeVideo == null)
            {
                throw new InvalidOperationException("Select an active video before updating");
            }

            var activeEntry = activeVideo.Entries.FirstOrDefault(e => e.CreationDate == entry.CreationDate);

            if (activeEntry == null)
            {
                throw new InvalidOperationException("Event cannot be found on the current video entry.");
            }

            entry.Data = data ?? activeEntry.Data;

            activeVideo.Entries.Remove(activeEntry);
            activeVideo.Entries.Add(entry);
            db.Update(activeVideo);
        }

        /// <summary>
        /// Deletes the event on the active video with the given CreationDate.
        /// </summary>
        /// <param name="loggedTime">The time of the event to delete.</param>
        [RemoteCommand]
        [Alias("DeleteEntry")]
        [PropertyConverter(typeof(StringToDateTime), "loggedTime")]
        public void DeleteEvent(DateTime loggedTime)
        {
            if (activeVideo == null)
            {
                throw new InvalidOperationException("Select an active video before deleting.");
            }

            if (!activeVideo.Entries.Any())
            {
                throw new IndexOutOfRangeException("The selected set doesn't have any entries");
            }

            var activeEntry = activeVideo.Entries.FirstOrDefault(e => e.CreationDate == loggedTime);

            if (activeEntry == null)
            {
                throw new InvalidOperationException("An event with that CreationDate cannot be found on the current video entry.");
            }

            activeVideo.Entries.Remove(activeEntry);
            db.Update(activeVideo);
        }

        /// <summary>
        /// Start the inspection.
        /// </summary>
        /// <param name="db">DB Used to store information.</param>
        [RemoteCommand]
        [Alias("CreateDive", "OpenDive")]
        [PropertyConverter(typeof(StringToFileInfo))]
        public void StartInspection(IFileCRUD db)
        {
            this.db = db;
            IsOpen = null;

            //db.Update(new NmeaDefinition("$-SUBC", new List<string>()
            //{
            //    "DateTime",
            //    "Altitude",
            //    "Depth",
            //    "Northing",
            //    "Easting",
            //    "Speed",
            //    "DCC",
            //    "KP",
            //    "Heading",
            //}));

            try
            {
                DiveOpened?.Invoke(this, null);
                IsOpen = true;
            }
            catch
            {
                IsOpen = false;
            }
        }

        public void UpdateDefinition(NmeaDefinition d)
        {
            if (!IsStarted || d == null)
            {
                return;
            }

            if (db.Read(d) == null)
            {
                db.Create(d);
                return;
            }

            db.Update(d);
        }

        /// <summary>
        /// Add a still path to the dive.
        /// </summary>
        /// <param name="stillPath">Path of the still.</param>
        private void AddStillToInspection(string stillPath)
        {
            if (!IsStarted)
            {
                return;
            }

            var path = FilesFolders.MakeRelativePath(db.File.FullName, stillPath);

            var entry = new StillEntry(path, DateTime.Now);

            // if there's a previous data entry, and it's within the 5 seconds of the still, attach it
            if (previousEntry != default && previousEntry.CreationDate - entry.CreationDate > TimeSpan.FromSeconds(5))
            {
                entry.Data = previousEntry;
            }

            db.Create(entry);
            OnNotify(JsonConvert.SerializeObject(entry));
        }

        /// <summary>
        /// Add a new video to the dive.
        /// </summary>
        /// <param name="file">Path to the video file.</param>
        private void AddVideo(FileInfo file)
        {
            if (!IsStarted)
            {
                return;
            }

            var video = FilesFolders.MakeRelativePath(db.File.FullName, file.FullName);

            var videoEntry = new VideoEntry(video, DateTime.Now, default);

            var v = db.Read(videoEntry);

            // only create the video if it doesn't exist
            if (v == null)
            {
                db.Create(videoEntry);

                // set the active video to the new entry
                activeVideo = videoEntry;
            }
            else
            {
                // set the active video to the one you pulled from the db
                activeVideo = v;
            }
        }

        private void LogData(NotifyEventArgs e)
        {
            if (e.MessageType != MessageTypes.SensorData)
            {
                return;
            }

            if (string.IsNullOrEmpty(e.Message))
            {
                return;
            }

            var data = new DataEntry(DateTime.Now, e.Message) { RecordingTime = recorder.VideoTime };

            // save to the previous entry to use with stills
            previousEntry = data;

            if (activeVideo != null)
            {
                activeVideo.DataEntries.Add(data);
            }
        }

        /// <summary>
        /// Update the video length of previously recorded file.
        /// </summary>
        /// <param name="file">Video just finished recording.</param>
        private void UpdateVideoLength(Tuple<FileInfo, TimeSpan> file)
        {
            if (!IsStarted)
            {
                return;
            }

            var relativePath = FilesFolders.MakeRelativePath(db.File.FullName, file.Item1.FullName);

            var video = Array.Find(db.ReadAll<VideoEntry>(), v => v.Path == relativePath);

            if (video?.VideoLength == TimeSpan.Zero)
            {
                // update the video length of the capture file
                video.VideoLength = file.Item2;
                db.Update(video);
            }
        }
    }
}
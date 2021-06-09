// <copyright file="Dive.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
//namespace SubCTools.DiveLog
//{
//    using SubCTools.Enums;
//    using SubCTools.Extensions;
//    using SubCTools.Settings;
//    using SubCTools.Settings.Interfaces;
//    using System;
//    using System.Collections.Generic;
//    using System.Globalization;
//    using System.IO;
//    using System.Linq;
//    using System.Text.RegularExpressions;

//    /// <summary>
//    /// Class representing a dive from DVRO.
//    /// </summary>
//    public class Dive
//    {
//        private SubCXDoc dive;

//        /// <summary>
//        /// Event to fire when a dive closes.
//        /// </summary>
//        public event EventHandler DiveClosed;

//        /// <summary>
//        /// Event to fire when a dive is opened.
//        /// </summary>
//        public event EventHandler DiveOpened;

//        /// <summary>
//        /// Gets contains the file this dive represents.
//        /// </summary>
//        public FileInfo File { get; private set; }

//        /// <summary>
//        /// Gets a value indicating whether checks to see if a .sdl file is open.
//        /// </summary>
//        public bool IsOpen => dive != null;

//        /// <summary>
//        /// Gets or sets the number of videos.
//        /// </summary>
//        public int? NumVideos
//        {
//            get; set;
//        }

//        /// <summary>
//        /// Gets or sets the Set ID.
//        /// </summary>
//        public int SetID { get; set; } = -1;

//        /// <summary>
//        /// Get the sets from the dive.
//        /// </summary>
//        /// <param name="xdoc">XML document with all the dive info.</param>
//        /// <returns>Collection of XInfo representing the sets.</returns>
//        public static IEnumerable<XInfo> GetSets(SubCXDoc xdoc)
//        {
//            if (xdoc == null)
//            {
//                throw new ArgumentNullException(nameof(xdoc), "Cannot get sets from null");
//            }

//            return from entry in xdoc.LoadAll()
//                   where entry.Name.StartsWith("Set")
//                   select new XInfo(entry.Name, entry.Value, entry.Attributes);
//        }

//        /// <summary>
//        /// Add the data file to the set attributes.
//        /// </summary>
//        /// <param name="path">Path of the definitions json file.</param>
//        public void AddDefinitions(string path)
//        {
//            dive?.Update("Definitions", attributes: new Dictionary<string, string>()
//            {
//                { "path", path },
//            });
//        }

//        /// <summary>
//        /// Add the data file to the set attributes.
//        /// </summary>
//        /// <param name="defFile">FileInfo of the definitions json file.</param>
//        public void AddDefinitions(FileInfo defFile)
//        {
//            if (defFile.Exists)
//            {
//                AddDefinitions(defFile.FullName.Replace(File.Directory.FullName, string.Empty));
//            }
//        }

//        /// <summary>
//        /// Add the data file to the set attributes.
//        /// </summary>
//        /// <param name="path">Path of the data file.</param>
//        public void AddDataToSet(string path)
//        {
//            dive?.Update("Set" + SetID, attributes: new Dictionary<string, string>()
//            {
//                { "data", path },
//            });
//        }

//        /// <summary>
//        /// Add the still entry to the dive.
//        /// </summary>
//        /// <param name="entry">Still entry information.</param>
//        public void AddStill(_StillEntry entry)
//        {
//            dive?.Add(@"StillsLog\Still", attributes: new Dictionary<string, string>(entry));
//        }

//        /// <summary>
//        /// Add the collection of video paths to the set.
//        /// </summary>
//        /// <param name="videoPaths">Collection of video paths to add.</param>
//        public void AddVideosToSet(IEnumerable<string> videoPaths)
//        {
//            var i = 0;
//            foreach (var videoPath in videoPaths)
//            {
//                if (string.IsNullOrEmpty(videoPath))
//                {
//                    continue;
//                }

//                // update the video set
//                AddVideoToSet(videoPath, i);
//                i++;
//            }
//        }

//        /// <summary>
//        /// Add a video to the set.
//        /// </summary>
//        /// <param name="path">Path to the video file.</param>
//        /// <param name="index">Index the video is taken through DVRO. E.g. Video1, Video2, etc.</param>
//        public void AddVideoToSet(string path, int index)
//        {
//            // update the video set
//            // only add numbers to the video attributes if they're greater than 0 so it's reverse compatible
//            dive?.Update("Set" + SetID, attributes: new Dictionary<string, string>()
//                {
//                    { "video" + (index > 0 ? index.ToString() : string.Empty), path },
//                    { "DateTime", DateTime.Now.ToString(CultureInfo.InvariantCulture) },
//                });
//        }

//        /// <summary>
//        /// Close the dive.
//        /// </summary>
//        public void Close()
//        {
//            if (dive == null && File == null)
//            {
//                return;
//            }

//            Save();
//            dive = null;
//            File = null;

//            DiveClosed?.Invoke(this, EventArgs.Empty);
//        }

//        /// <summary>
//        /// Saves the dive.
//        /// </summary>
//        public void Save()
//        {
//            dive?.Save();
//        }

//        /// <summary>
//        /// Increase the set id by one.
//        /// </summary>
//        public void CreateNewSet()
//        {
//            SetID++;
//        }

//        /// <summary>
//        /// Delete the given entry from the set.
//        /// </summary>
//        /// <param name="entry">Entry to delete,.</param>
//        public void Delete(IDictionary<string, string> entry)
//        {
//            try
//            {
//                dive?.Remove($@"Set{entry["SetID"]}\Events\Event{entry["EventID"]}");
//            }
//            catch
//            {
//                throw new Exception("Invalid entry, doesn't contain a SetID and/or EventID");
//            }
//        }

//        /// <summary>
//        /// Get the date file from the set.
//        /// </summary>
//        /// <param name="setName">Set to grab data file from.</param>
//        /// <returns>Path to data file.</returns>
//        public string GetDataFile(string setName)
//        {
//            return GetDataFile(setName, dive);
//        }

//        public IEnumerable<string> GetDataFiles()
//        {
//            return GetDataFiles(dive);
//        }

//        public IEnumerable<string> GetDataFiles(SubCXDoc xdoc)
//        {
//            if (xdoc == null)
//            {
//                throw new ArgumentNullException(nameof(xdoc), "Cannot get data from null");
//            }

//            return GetSets(xdoc).Select(s => s.Attributes["data"]);
//        }

//        /// <summary>
//        /// Load all the dive info from the dive log file.
//        /// </summary>
//        /// <returns></returns>
//        public IEnumerable<Dictionary<string, string>> GetDiveInfo()
//        {
//            return dive?.LoadAll("DiveInfo").Select(d => d.Attributes) ?? null;
//        }

//        public IEnumerable<_DiveEntry> GetEntries()
//        {
//            return GetEntries(dive, GetSetNames(dive));
//        }

//        /// <summary>
//        /// Get all the entries for the specified set.
//        /// </summary>
//        /// <param name="set">Set name to retrieve entries.</param>
//        /// <returns>All entries under set.</returns>
//        public IEnumerable<_DiveEntry> GetEntries(string set)
//        {
//            return GetEntries(new string[] { set });
//        }

//        public IEnumerable<_DiveEntry> GetEntries(IEnumerable<string> sets)
//        {
//            return GetEntries(dive, sets);
//        }

//        public int GetNextEventID(int setNumber)
//        {
//            var entries = GetEntries($"Set{setNumber}").OrderBy(d => d.EventID).ToList();

//            for (var i = 0; i < entries.Count(); i++)
//            {
//                if (entries[i].EventID != i)
//                {
//                    return i;
//                }
//            }

//            return entries.Count();
//        }

//        public XInfo GetSet(int setNumber)
//        {
//            if (dive.TryLoad("Set" + setNumber, out XInfo set))
//            {
//                return set;
//            }
//            else
//            {
//                return null;
//            }
//        }

//        /// <summary>
//        /// Gets the NmeaDefinitions from dive.
//        /// </summary>
//        /// <returns>A <see cref="FileInfo"/> to the definitions file.</returns>
//        public FileInfo GetDefinitions()
//        {
//            if (dive == null)
//            {
//                throw new ArgumentNullException(nameof(dive), "Cannot get definitions from null");
//            }

//            var d = from entry in dive.LoadAll()
//                    where entry.Name.StartsWith("Definition")
//                    select entry.Attributes.FirstOrDefault();

//            return new FileInfo(Path.Combine(File.Directory.FullName, d?.FirstOrDefault().Value ?? string.Empty));
//        }

//        public IEnumerable<XInfo> GetSets()
//        {
//            return GetSets(dive);
//        }

//        public IEnumerable<_StillEntry> GetStillsLog()
//        {
//            return GetStillsLog(dive);
//        }

//        public IEnumerable<_StillEntry> GetStillsLog(SubCXDoc xdoc)
//        {
//            if (xdoc == null)
//            {
//                throw new ArgumentNullException(nameof(xdoc), "Cannot get stills log from null");
//            }

//            return from s in dive.LoadAll("StillsLog")
//                   select new _StillEntry(s.Attributes);
//        }

//        public int GetTotalNumberVideos()
//        {
//            var num = 0;
//            for (var i = 0; i <= GetSets().Count(); i++)
//            {
//                var videosInSet = GetVideos($"Set{i}");
//                if (videosInSet != null)
//                {
//                    num += videosInSet.Where(s => s.Key.StartsWith("video")).Count();
//                }
//            }
//            return num;
//        }

//        public FileInfo GetVideo(int set)
//        {
//            var s = GetSet(set);
//            return new FileInfo(Path.Combine(Path.GetDirectoryName(dive.FileName), s.Attributes["video"]));
//        }

//        /// <summary>
//        /// Get all the entries in the video log.
//        /// </summary>
//        /// <returns>All entires in the video log.</returns>
//        public IEnumerable<_VideoEntry> GetVideoLog()
//        {
//            return GetVideoLog(dive);
//        }

//        public IEnumerable<_VideoEntry> GetVideoLog(SubCXDoc xdoc)
//        {
//            if (xdoc == null)
//            {
//                throw new ArgumentNullException(nameof(xdoc), "Cannot get video log from null");
//            }

//            return from v in xdoc.LoadAll("VideoLog")
//                   select new _VideoEntry(v.Attributes);
//        }

//        public IDictionary<string, string> GetVideos(string setName)
//        {
//            return GetVideos(setName, dive);
//        }

//        public IEnumerable<string> GetVideos()
//        {
//            return GetVideos(dive);
//        }

//        public IEnumerable<string> GetVideos(SubCXDoc xdoc)
//        {
//            if (xdoc == null)
//            {
//                throw new ArgumentNullException(nameof(xdoc), "Cannot get videos from null");
//            }

//            return GetSets(xdoc).Select(s => s.Attributes["video"]);
//        }

//        public int GetVideosBeforeSet(int set)
//        {
//            var videos = 0;
//            for (var i = 0; i < set; i++)
//            {
//                videos += GetVideosInSet(i);
//            }

//            return videos;
//        }

//        public int GetVideosInSet(int i)
//        {
//            var num = 0;
//            var videosInSet = GetVideos($"Set{i}");
//            if (videosInSet != null)
//            {
//                num += videosInSet.Count;
//            }

//            return num;
//        }

//        public void Log(_DiveEntry entry)
//        {
//            if (entry == null)
//            {
//                return;
//            }

//            Update(entry);
//        }

//        public void Log(_VideoEntry entry)
//        {
//            dive?.Add(@"VideoLog\Entry", attributes: entry);
//        }

//        /// <summary>
//        /// Open the dive at the given path.
//        /// </summary>
//        /// <param name="path">Path to create dive.</param>
//        /// <exception cref="Exception">Throws if directory fails to create.</exception>
//        public void Open(string path)
//        {
//            Close();

//            var directory = new DirectoryInfo(Path.GetDirectoryName(path));
//            if (!directory.Exists)
//            {
//                try
//                {
//                    directory.Create();
//                }
//                catch
//                {
//                    throw new Exception("Directory failed to create");
//                }
//            }

//            File = new FileInfo(path);

//            // if we open a non sdl we lose a lot of functionality. We need to restrict this
//            if (path.EndsWith(".sdl"))
//            {
//                dive = new SubCXDoc(path);

//                // the current SetID is equal to the existing number of sets - 1
//                SetID = GetSets().Count() - 1;
//            }

//            // else
//            // {
//            //    dive = null;
//            //    SetID = 0;
//            // }
//            DiveOpened?.Invoke(this, EventArgs.Empty);
//        }

//        public void RemoveDiveInfo(IDictionary<string, string> diveInfo)
//        {
//            dive?.Remove($@"DiveInfo\DI{diveInfo["ID"]}");
//        }

//        /// <inheritdoc/>
//        public override string ToString()
//        {
//            return dive?.ToString() ?? string.Empty;
//        }

//        public void Update(_DiveEntry entry)
//        {
//            var sid = (entry.ContainsKey(nameof(SetID)) && !string.IsNullOrEmpty(entry[nameof(SetID)])) ? entry[nameof(SetID)] : SetID.ToString();
//            var eid = (entry.ContainsKey("EventID") && !string.IsNullOrEmpty(entry["EventID"])) ? entry["EventID"] : GetNextEventID(Convert.ToInt32(sid)).ToString();

//            entry.Update("SetID", sid);
//            entry.Update("EventID", eid);

//            dive?.Update($@"Set{entry.SetID}\Events\Event{entry.EventID}", attributes: new Dictionary<string, string>(entry));
//        }

//        public void UpdateDiveInfo(IDictionary<string, string> diveInfo)
//        {
//            dive?.Update($@"DiveInfo\DI{diveInfo["ID"]}", attributes: new Dictionary<string, string>(diveInfo));
//        }

//        /// <summary>
//        /// Get all the dive log entries from the doc and fill the DiveLogEntries property.
//        /// </summary>
//        /// <param name="xdoc">Document to load the entries from.</param>
//        private static IEnumerable<_DiveEntry> GetEntries(SubCXDoc xdoc, IEnumerable<string> setNames)
//        {
//            if (xdoc == null)
//            {
//                throw new ArgumentNullException("The given xdoc cannot be null");
//            }

//            // go through all the entries and create all the dive logs
//            foreach (var set in setNames)
//            {
//                foreach (var item in xdoc.LoadAll(set + @"\Events"))
//                {
//                    item.Attributes.Update("SetID", Regex.Match(set, @"\d+").Value);
//                    item.Attributes.Update("EventID", Regex.Match(item.Name, @"\d+").Value);

//                    yield return new _DiveEntry(
//                        item.Attributes[nameof(_DiveEntry.Title)],
//                        item.Attributes[nameof(_DiveEntry.Description)],
//                        TimeSpan.FromSeconds(double.Parse(item.Attributes[nameof(_DiveEntry.RecordingTime)])),
//                        DateTime.Parse(item.Attributes[nameof(_DiveEntry.DateTime)]),
//                        EventTypes.General,
//                        bool.Parse(item.Attributes[nameof(_DiveEntry.IsAnomaly)]),
//                        int.Parse(item.Attributes[nameof(_DiveEntry.SetID)]),
//                        int.Parse(item.Attributes[nameof(_DiveEntry.EventID)]));
//                }
//            }
//        }

//        private static IEnumerable<string> GetSetNames(ISettingsService xdoc)
//        {
//            return from entry in xdoc.LoadAll()
//                   where entry.Name.StartsWith("Set")
//                   select entry.Name;
//        }

//        /// <summary>
//        /// Handler for when the file changes.
//        /// </summary>
//        /// <param name="file">New file to open.</param>
//        private void Dive_FileChanged(FileInfo file)
//        {
//            Open(file.FullName);
//        }

//        private string GetDataFile(string setName, SubCXDoc doc)
//        {
//            if (string.IsNullOrWhiteSpace(setName))
//            {
//                throw new ArgumentNullException(nameof(setName));
//            }

//            if (doc == null)
//            {
//                throw new ArgumentNullException(nameof(doc));
//            }

//            return GetSets(doc)?.FirstOrDefault(s => s.Name == setName)?.Attributes["data"] ?? string.Empty;
//        }

//        private IDictionary<string, string> GetVideos(string setName, SubCXDoc doc)
//        {
//            if (string.IsNullOrWhiteSpace(setName))
//            {
//                throw new ArgumentNullException(nameof(setName));
//            }

//            if (doc == null)
//            {
//                throw new ArgumentNullException(nameof(doc));
//            }

//            return GetSets(doc)?.FirstOrDefault(s => s.Name == setName)?.Attributes;
//        }
//    }
//}
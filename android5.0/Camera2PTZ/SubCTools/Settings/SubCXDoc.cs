//-----------------------------------------------------------------------
// <copyright file="SubCXDoc.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Settings
{
    using SubCTools.Interfaces;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// A representation of an XML document.
    /// </summary>
    public class SubCXDoc : ISettingsService, ILoader<XInfo, string>
    {
        /// <summary>
        /// A readonly lock to be used for multithreaded updating..
        /// </summary>
        private readonly object sync = new object();

        /// <summary>
        /// The XML document object/.
        /// </summary>
        private XDocument doc;

        /// <summary>
        /// The root node of the XML document.
        /// </summary>
        private XElement root;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCXDoc"/> class.
        /// </summary>
        /// <param name="uri">A uri pointing to the XML document this class represents.</param>
        public SubCXDoc(Uri uri)
        {
            Open(uri);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubCXDoc"/> class.
        /// </summary>
        /// <param name="path">The path to the XML document this class represents.</param>
        /// <param name="overwrite">Whether or not to overwrite if the document already exists.</param>
        public SubCXDoc(string path, bool overwrite = false)
        {
            Console.WriteLine($"Opening path: {path}");
            Open(path, overwrite);
        }

        /// <summary>
        /// Event raised if there is an error accessing the XML document.
        /// </summary>
        public event EventHandler<string> Error;

        /// <summary>
        /// Gets the full path for the XML file this represents.
        /// </summary>
        public string FileName { get; private set; } = string.Empty;

        /// <summary>
        /// Add a new node, even if one already exists.
        /// </summary>
        /// <param name="entry">Name of the node. Multiple nodes can be given by using a /. Ie. parent/child/... and all missing nodes will be created along the way.</param>
        /// <param name="value">Value the node contains.</param>
        /// <param name="attributes">A dictionary of attributes.</param>
        public void Add(string entry, string value = "", Dictionary<string, string> attributes = null)
        {
            Update(entry, value, attributes, false);
        }

        /// <summary>
        /// Add a new node, even if one already exists. Then Move it to the top.
        /// </summary>
        /// <param name="entry">The name of the XML node to add.</param>
        /// <param name="value">The value of the XML node to add.</param>
        /// <param name="attributes">The attributes of the XML node to add.</param>
        public void AddToTop(string entry, string value = "", Dictionary<string, string> attributes = null)
        {
            var node = Get(entry, overwrite: false);
            Update(node, value, attributes);
            MoveToTop(node);
        }

        /// <summary>
        /// Load the attributes and value in to an XInfo class.
        /// </summary>
        /// <param name="entry">Node you're trying to get information for.</param>
        /// <param name="createNewIfDoesntExist">Determines whether to create a new empty node if one is found or not.</param>
        /// <returns>XInfo class with all information.</returns>
        public XInfo Load(string entry, bool createNewIfDoesntExist = true)
        {
            lock (sync)
            {
                var node = Get(entry, createNewIfDoesntExist, load: true);

                if (node == null)
                {
                    return new XInfo(string.Empty, new Dictionary<string, string>());
                }

                var attributes = LoadAttributes(entry);
                var value = node.Value;

                return new XInfo(node.Name.LocalName, value, attributes);
            }
        }

        /// <summary>
        /// Load a specified entry.
        /// </summary>
        /// <param name="key">The name of the XML node to load.</param>
        /// <returns>The XML node if found, an empty XML node if not.</returns>
        public XInfo Load(string key)
        {
            return Load(key, true);
        }

        /// <summary>
        /// Load the specified value as type T.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="entry">Node you wish to get.</param>
        /// <param name="createNewIfDoesntExist">Create a new node if one doesn't exist or not. True by default.</param>
        /// <returns>Value cast as input type.</returns>
        public T Load<T>(string entry, bool createNewIfDoesntExist = true)
        {
            var newValue = default(T);

            var loadValue = Load(entry, createNewIfDoesntExist).Value;

            try
            {
                newValue = (T)Convert.ChangeType(loadValue, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
            }

            return newValue;
        }

        /// <summary>
        /// Load all node held within an entry. Can be called recusively to build a tree.
        /// </summary>
        /// <param name="entry">Node entry name. Defaults to the parent node.</param>
        /// <returns>List of all nodes under that entry.</returns>
        public IEnumerable<XInfo> LoadAll(string entry = "SubC")
        {
            // Console.WriteLine("LoadAll: {0}", entry);
            var xCollection = new List<XInfo>();

            // get the root node
            var node = Get(entry, false, load: true);

            if (node == null || node.Elements().Count() == 0)
            {
                return xCollection;
            }

            // get all the children
            foreach (var item in node.Elements())
            {
                // create the dictionary to hold all the attributes
                var dAttributes = new Dictionary<string, string>();

                foreach (var attribute in item.Attributes())
                {
                    // add each attribute
                    dAttributes.Add(attribute.Name.LocalName, attribute.Value);
                }

                // add the new xinfo to the collection
                xCollection.Add(new XInfo(item.Name.LocalName, item.Value.ToString(), dAttributes));
            }

            return xCollection;
        }

        /// <summary>
        /// Load all the attributes from a given entry.
        /// </summary>
        /// <param name="entry">The name of the XML node to load.</param>
        /// <returns>All attributes on the XML node given.</returns>
        public Dictionary<string, string> LoadAttributes(string entry)
        {
            var attributes = new Dictionary<string, string>();

            var node = Get(entry, load: true);

            if (node == null)
            {
                return attributes;
            }

            foreach (var attribute in node.Attributes())
            {
                attributes.Add(attribute.Name.ToString(), attribute.Value);
            }

            return attributes;
        }

        /// <summary>
        /// Loads a specific node in the XML document.
        /// </summary>
        /// <param name="entry">The name of the node to load.</param>
        /// <returns>The XML node represented by <paramref name="entry"/>.</returns>
        public XElement LoadElement(string entry)
        {
            return Get(entry, false, load: true);
        }

        /// <summary>
        /// Load a value from the settings for PropertyInfo.
        /// </summary>
        /// <param name="name">Name of property you want to load.</param>
        /// <param name="variable">The variable you want to load the value in to.</param>
        public void LoadProperty(string name, ref object variable)
        {
            object newValue = null;
            var type = variable.GetType();
            try
            {
                newValue = Convert.ChangeType(Get(name, false).Value, type);
            }
            catch
            {
            }

            if (newValue != null)
            {
                variable = newValue;
            }
        }

        /// <summary>
        /// Move the node to the bottom of the document.
        /// </summary>
        /// <param name="entry">Name of entry to move.</param>
        public void MoveToBottom(string entry)
        {
            // This method appears to create a copy of the item at the bottom of the file
            ReOrder(entry, short.MaxValue);
        }

        /// <summary>
        /// Move the node to the top of the document.
        /// </summary>
        /// <param name="entry">Name of entry to move.</param>
        public void MoveToTop(string entry)
        {
            MoveToTop(Get(entry, false, false, true));
        }

        /// <summary>
        /// Opens an XML document.
        /// </summary>
        /// <param name="uri">A uri pointing to the XML document to open.</param>
        public void Open(Uri uri)
        {
            try
            {
                doc = XDocument.Load(uri.OriginalString);
                root = doc.Root;
                this.FileName = uri.LocalPath;
            }
            catch
            {
                // could not load file
            }
        }

        /// <summary>
        /// Opens an XML document.
        /// </summary>
        /// <param name="fileName">The path to the XML document to open.</param>
        /// <param name="overwrite">Whether or not to overwrite if the document already exists.</param>
        public void Open(string fileName, bool overwrite = false)
        {
            if (new FileInfo(fileName).Extension.ToLower() != ".xml")
            {
                return;
            }

            this.FileName = fileName;

            string dir;
            if (!Directory.Exists(dir = Path.GetDirectoryName(FileName)))
            {
                if (dir != string.Empty)
                {
                    Directory.CreateDirectory(dir);
                }
            }

            if (!File.Exists(fileName) || overwrite)
            {
                doc = new XDocument(
                    new XElement("SubC"));

                Save();
            }
            else
            {
                try
                {
                    lock (sync)
                    {
                        doc = XDocument.Load(fileName);
                    }
                }
                catch (XmlException exception)
                {
                    Console.WriteLine("There was an exception {0}", exception.ToString());

                    OnError("File: " + FileName + " is corrupt. This could be from a power loss, or from manipulation of the file. If the problem continues, please contact support@subccontrol.com");

                    // if you can't load the file, rename it as being corrupt, delete it if it already exists
                    Helpers.FilesFolders.CreateCorrupt(fileName);

                    // create a new one to replace it
                    doc = new XDocument(
                    new XElement("SubC"));

                    Save();
                }
            }

            root = doc.Root;
        }

        /// <summary>
        /// Remove the node with the specified entry from the document.
        /// </summary>
        /// <param name="entry">The name of the XML node to remove.</param>
        public void Remove(string entry)
        {
            Remove(Get(entry, false));
        }

        /// <summary>
        /// Remove the last node in the XML document.
        /// </summary>
        public void RemoveLast()
        {
            Remove(root.LastNode);
        }

        /// <summary>
        /// Reorder the node in the document.
        /// </summary>
        /// <param name="entry">Name of the node to move.</param>
        /// <param name="relativePlacement">How many steps to move the node. Postitive to move down the document, negative to move up.</param>
        public void ReOrder(string entry, int relativePlacement)
        {
            ReOrder(Get(entry, false, false, true), relativePlacement);
        }

        /// <summary>
        /// Save the document.
        /// </summary>
        public void Save()
        {
            try
            {
                doc?.Save(FileName, SaveOptions.None);
            }
            catch (Exception e)
            {
                // file may have been deleted
                Console.WriteLine($"Error saving XDoc: {e}");
                //// throw e;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return doc?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Tried to load the XML node represented by entry string.
        /// </summary>
        /// <typeparam name="T">The type to load.</typeparam>
        /// <param name="entry">The name of the XML node to try and load.</param>
        /// <param name="value">The found value or the default value.</param>
        /// <returns>True if the load was successful, false otherwise.</returns>
        public bool TryLoad<T>(string entry, out T value)
        {
            if (Get(entry, false, load: true) != null)
            {
                value = Load<T>(entry);
                return true;
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Updates a node in the XML document, or adds it if it does not exist.
        /// </summary>
        /// <param name="entry">The name of the XML node to update.</param>
        /// <param name="nodeValue">The new value of the XML node to update.</param>
        /// <param name="attributes">The new attributes of the XML node to update.</param>
        /// <param name="overwrite">Not used.</param>
        public void Update(string entry, object nodeValue, Dictionary<string, string> attributes = null, bool overwrite = true)
        {
            Update(entry, (nodeValue ?? string.Empty).ToString(), attributes, overwrite);
        }

        /// <summary>
        /// Update a node value. Multiple nodes can be given by using a /. Ie. parent/child/... and all missing nodes will be created along the way.
        /// </summary>
        /// <param name="entry">The name of the XML node you want to update.</param>
        /// <param name="value">The new value held by the XML node.</param>
        /// <param name="attributes">The new attributes of the XML node.</param>
        /// <param name="overwrite">Not used.</param>
        public void Update(string entry, string value = null, Dictionary<string, string> attributes = null, bool overwrite = true)
        {
            Update(Get(entry, overwrite: overwrite), value, attributes);
        }

        /// <summary>
        /// Get the requested entry as an element. Will parse string to find children with parent/child/... format.
        /// </summary>
        /// <param name="entry">The name of the XML entry to Get.</param>
        /// <param name="createNewIfDoesntExist">Determines if it should add an empty element if one is not found. True by default.</param>
        /// <param name="overwrite">Not used.</param>
        /// <param name="load">If true returns just the child else it returns the whole tree. False by default.</param>
        /// <returns>The element corresponding to the name given.</returns>
        private XElement Get(string entry, bool createNewIfDoesntExist = true, bool overwrite = true, bool load = false)
        {
            // return the root element if you're looking for it
            if (entry == "SubC")
            {
                // Console.WriteLine("returning SubC root");
                return root;
            }

            // replace any backslashes with a forwardslash
            entry = entry.Replace('\\', '/');

            var split = entry.Split('/');

            var parent = root;
            if (root == null)
            {
                return root;
            }

            // loop through the array of node
            for (var i = 0; i < split.Length; i++)
            {
                var newNode = Validate(split[i]);

                // skip if your at a blank node
                if (newNode == string.Empty)
                {
                    continue;
                }

                // does node already exist?
                try
                {
                    var child = parent.Element(newNode);

                    if (child == null)
                    {
                        // are we creating a new one?
                        if (createNewIfDoesntExist)
                        {
                            child = new XElement(newNode);
                            parent.Add(child);
                            parent = parent.Element(newNode);
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if (i == split.Length - 1)
                        {
                            if (load || overwrite)
                            {
                                return child;
                            }

                            child = new XElement(newNode);
                            parent.Add(child);

                            // set the parent to the last element
                            parent = parent.Elements().Last();
                        }
                        else
                        {
                            parent = child;
                        }
                    }
                }
                catch
                {
                }
            }

            return parent;
        }

        /// <summary>
        /// Moves the given element one "step" down in the XML document.
        /// </summary>
        /// <param name="element">The element of the XML document to move.</param>
        /// <returns>The element in its new position.</returns>
        private XElement MoveDown(XElement element)
        {
            element.NextNode?.AddAfterSelf(element);
            var n = element.NextNode?.NextNode;
            element.Remove();
            return n as XElement;
        }

        /// <summary>
        /// Moves the element to the top of the XML document.
        /// </summary>
        /// <param name="element">The element of the XML document to move.</param>
        private void MoveToTop(XElement element)
        {
            ReOrder(element, short.MinValue);
        }

        /// <summary>
        /// Moves the given element one "step" up in the XML document.
        /// </summary>
        /// <param name="element">The element of the XML document to move.</param>
        /// <returns>The element in its new position.</returns>
        private XElement MoveUp(XElement element)
        {
            element.PreviousNode.AddBeforeSelf(element);
            var n = element.PreviousNode.PreviousNode;
            element.Remove();
            return n as XElement;
        }

        /// <summary>
        /// Raises the <see cref="Error"/> event with the given error message.
        /// </summary>
        /// <param name="error">The error message to include in the raised error.</param>
        private void OnError(string error)
        {
            Error?.Invoke(this, error);
        }

        /// <summary>
        /// Removes the element from the XML document.
        /// </summary>
        /// <param name="element">The element of the XML document to move.</param>
        private void Remove(XElement element)
        {
            element?.Remove();
            Save();
        }

        /// <summary>
        /// Removes the node from the XML document.
        /// </summary>
        /// <param name="node">The node of the XML document to move.</param>
        private void Remove(XNode node)
        {
            node.Remove();
            Save();
        }

        /// <summary>
        /// Reorders nodes in the XML document.
        /// </summary>
        /// <param name="element">The name of the element to move.</param>
        /// <param name="relativePlacement">The number of "steps" to move. Positive numbers move down, negative move up.</param>
        private void ReOrder(XElement element, int relativePlacement)
        {
            // just return if you don't want to go anywhere
            if (relativePlacement == 0)
            {
                return;
            }

            // return if the node you want to move doesn't exist
            if (element == null)
            {
                return;
            }

            // the number of steps you've already moved
            var steps = 0;

            // stop when you've reached the number of steps, or there's no more nodes
            while ((relativePlacement > 0 ? element.NextNode : element.PreviousNode) != null
                || steps >= Math.Abs(relativePlacement))
            {
                if (relativePlacement > 0)
                {
                    element = MoveDown(element);
                }
                else
                {
                    element = MoveUp(element);
                }

                steps++;
            }

            Save();
        }

        /// <summary>
        /// Updates the given element in the XML document.
        /// </summary>
        /// <param name="element">The element to update.</param>
        /// <param name="value">The value to update to.</param>
        /// <param name="attributes">The attributes of the updated element.</param>
        private void Update(XElement element, string value = null, Dictionary<string, string> attributes = null)
        {
            lock (sync)
            {
                var parent = element;

                if (parent == null)
                {
                    return;
                }

                if (value != null)
                {
                    // set the value of the node if it exists
                    if (parent.Value == string.Empty)
                    {
                        parent.Add(new XText(value));
                    }
                    else
                    {
                        if (value != null)
                        {
                            parent.Value = value;
                        }
                        else
                        {
                            parent.Value = string.Empty;
                        }
                    }
                }

                // append all the attributes if it's not null
                if (attributes != null)
                {
                    // append all the attributes
                    foreach (var kvp in attributes)
                    {
                        XAttribute attribute = null;
                        if ((attribute = parent.Attribute(kvp.Key)) == null)
                        {
                            var key = Validate(kvp.Key);

                            // Console.WriteLine("Adding new attribute to parent: {0}, {1}", key, kvp.Value);
                            parent.Add(new XAttribute(key, kvp.Value));
                        }
                        else
                        {
                            // Console.WriteLine("Updating Attribute: {0}, {1}, New {2}", kvp.Key, attribute.Value, kvp.Value);
                            attribute.Value = kvp.Value;
                        }
                    }
                }

                // Console.WriteLine("Parent {0}", parent.ToString());
                Save();
            }
        }

        /// <summary>
        /// Parse the string to make sure it doesn't contain any breaking characters.
        /// </summary>
        /// <param name="value">The string to be validated.</param>
        /// <returns>The validated string.</returns>
        private string Validate(string value)
        {
            var match = Regex.Match(value, @"^\d+");
            if (match.Success)
            {
                value = value.Replace(match.Value.ToString(), string.Empty);
            }

            value = Helpers.FilesFolders.ParseFilename(value);
            value = value.Replace(@"/", string.Empty).Replace(@"\", string.Empty).Replace(@"&", string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty);

            return value.Replace(@"/", string.Empty).Replace(@"\", string.Empty).Replace(@"&", string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty);
        }
    }
}
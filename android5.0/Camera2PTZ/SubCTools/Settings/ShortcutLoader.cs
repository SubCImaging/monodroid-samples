// <copyright file="ShortcutLoader.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// namespace SubCTools.Settings
// {
//    public class ShortcutLoader : ILoader<string, string>
//    {
//        readonly ISettingsService shortcuts;
//        readonly IDynamicLoader<string, string> shortcutsLoader;

// Dictionary<string, string> shortcutsDictionary = new Dictionary<string, string>();

// public ShortcutLoader(ISettingsService shortcuts)
//        {
//            this.shortcuts = shortcuts;
//            PopulateDictionary();
//        }

// public ShortcutLoader(IDynamicLoader<string, string> dynamicLoader)
//        {
//            shortcutsLoader = dynamicLoader;
//        }

// /// <summary>
//        /// This does
//        /// </summary>
//        void PopulateDictionary()
//        {
//            // get all the entries with shortcut attributes
//            foreach (var entry in shortcuts.LoadAll())
//            {
//                if (entry.Attributes.ContainsKey("shortcut"))
//                {
//                    List<string> shortcutKeys = new List<string>();

// //if the key to actuate it isn't the same as what to show the user (e.g. "Minus" & OemMinus)
//                    if (entry.Attributes.ContainsKey("shortcutKey"))
//                    {
//                        shortcutKeys = entry.Attributes["shortcutKey"].Split(',').ToList();
//                    }
//                    else
//                    {
//                        shortcutKeys.Add(entry.Attributes["shortcut"]);
//                    }

// Console.WriteLine("Contains Keys:");
//                    foreach (string key in shortcutKeys)
//                    {
//                        Console.WriteLine(key);
//                        //store in the dictionary
//                        if (!shortcutsDictionary.ContainsKey(key))
//                        {
//                            shortcutsDictionary.Add(key, entry.Name);
//                        }
//                    }
//                }
//            }
//        }

// public string Load(string key)
//        {
//            if (shortcutsDictionary.ContainsKey(key))
//            {
//                return shortcutsDictionary[key];
//            }

// return string.Empty;
//        }
//    }
// }

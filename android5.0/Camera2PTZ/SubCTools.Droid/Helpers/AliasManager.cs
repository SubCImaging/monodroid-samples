//-----------------------------------------------------------------------
// <copyright file="AliasManager.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer & Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Helpers
{
    using SubCTools.Attributes;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// <see cref="AliasManager"/> class that manages all the <see cref="CommandAlias"/>s.
    /// </summary>
    public class AliasManager : DroidBase, INotifiable
    {
        /// <summary>
        /// <see cref="List{CommandAlias}"/> that holds all the commands.
        /// </summary>
        private List<CommandAlias> commands = new List<CommandAlias>();

        /// <summary>
        /// <see cref="List{Type}"/> that holds all classes that have remote commands
        /// </summary>
        private List<Type> classes = new List<Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AliasManager"/> class
        /// </summary>
        /// <param name="settings">the settings service</param>
        public AliasManager(ISettingsService settings)
            : base(settings)
        {
            LoadSettings();
        }

        /// <summary>
        /// Clears all the commands from the <see cref="List{CommandAlias}"/> <see cref="commands"/>.
        /// </summary>
        [RemoteCommand]
        public void ClearAll()
        {
            commands.Clear();
            Settings.Remove("CustomCommands");
        }

        /// <summary>
        /// Clears all the aliases and replies from a specific command from the <see cref="List{CommandAlias}"/> <see cref="commands"/>.
        /// </summary>
        /// <param name="internalCommand">the actual command name</param>
        [RemoteCommand]
        public void Clear(string internalCommand)
        {
            foreach (var c in commands)
            {
                if (c.InternalCommand == internalCommand)
                {
                    Settings.Remove($@"CustomCommands/{internalCommand}");
                    commands.Remove(c);
                    return;
                }
            }
        }

        /// <summary>
        /// Adds a command to a <see cref="CommandAlias"/>.
        /// </summary>
        /// <param name="internalCommand">The <see cref="RemoteCommand"/> you want to execute.</param>
        /// <param name="inputCommand">The command you want to use to execute the <see cref="RemoteCommand"/>.</param>
        [RemoteCommand]
        public void AddCommandAlias(string internalCommand, string inputCommand)
        {
            internalCommand = internalCommand.Trim();
            inputCommand = inputCommand.Trim();

            if (ValidateAndCreate(internalCommand))
            {
                foreach (CommandAlias c in commands)
                {
                    if (c.InternalCommand == internalCommand)
                    {
                        if (!(inputCommand == string.Empty) && !c.InputCommands.Contains(inputCommand))
                        {
                            c.AddCommand(inputCommand, CommandAlias.CommandType.Input);
                        }

                        SaveToSettings(internalCommand, inputCommand, "Alias");

                        OnNotify($"Added Alias '{inputCommand}' to {internalCommand}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a command to a <see cref="CommandAlias"/>.
        /// </summary>
        /// <param name="internalCommand">The <see cref="RemoteCommand"/> you want to execute.</param>
        /// <param name="replyCommand">The output you want to receive when the device is done executing the <see cref="RemoteCommand"/></param>
        [RemoteCommand]
        public void AddCommandReply(string internalCommand, string replyCommand)
        {
            internalCommand = internalCommand.Trim();
            replyCommand = replyCommand.Trim();

            if (ValidateAndCreate(internalCommand))
            {
                foreach (CommandAlias c in commands)
                {
                    if (c.InternalCommand == internalCommand)
                    {
                        if (!(replyCommand == string.Empty) && !c.ReplyCommands.Contains(replyCommand))
                        {
                            c.AddCommand(replyCommand, CommandAlias.CommandType.Reply);
                        }

                        SaveToSettings(internalCommand, replyCommand, "Reply");

                        OnNotify($"Added return '{replyCommand}' to {internalCommand}");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a command from the <see cref="CommandAlias"/>.
        /// </summary>
        /// <param name="internalCommand">The <see cref="RemoteCommand"/> associated with the command.</param>
        /// <param name="inputCommand"><see cref="CommandAlias.CommandType.Input"/> command to remove.</param>
        [RemoteCommand]
        public void RemoveCommandAlias(string internalCommand, string inputCommand)
        {
            inputCommand = inputCommand.Trim();
            internalCommand = internalCommand.Trim();

            if (internalCommand == string.Empty | !MethodExists(internalCommand))
            {
                OnNotify($"Command not found: {internalCommand}");
                return;
            }

            foreach (CommandAlias c in commands)
            {
                if (c.InternalCommand == internalCommand)
                {
                    if (!(inputCommand == string.Empty))
                    {
                        c.ClearCommand(inputCommand, CommandAlias.CommandType.Input);
                        OnNotify($"Removed alias {inputCommand} from {internalCommand}");
                    }

                    RemoveFromSettings(internalCommand, inputCommand, "Alias");

                    if (c.InputCommands.Count == 0 && c.ReplyCommands.Count == 0)
                    {
                        Clear(c.InternalCommand);
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Removes a command from the <see cref="CommandAlias"/>.
        /// </summary>
        /// <param name="internalCommand">The <see cref="RemoteCommand"/> associated with the command.</param>
        /// <param name="replyCommand"><see cref="CommandAlias.CommandType.Reply"/> command to remove.</param>
        [RemoteCommand]
        public void RemoveCommandReply(string internalCommand, string replyCommand)
        {
            internalCommand = internalCommand.Trim();
            replyCommand = replyCommand.Trim();

            if (internalCommand == string.Empty | !MethodExists(internalCommand))
            {
                OnNotify($"Command not found: {internalCommand}");
                return;
            }

            foreach (CommandAlias c in commands)
            {
                if (c.InternalCommand == internalCommand)
                {
                    if (!(replyCommand == string.Empty))
                    {
                        c.ClearCommand(replyCommand, CommandAlias.CommandType.Reply);
                        OnNotify($"Removed return command {replyCommand} from {internalCommand}");
                    }

                    RemoveFromSettings(internalCommand, replyCommand, "Reply");

                    if (c.InputCommands.Count == 0 && c.ReplyCommands.Count == 0)
                    {
                        Clear(c.InternalCommand);
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// This method will be called when a command is received
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">event args</param>
        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            Task.Run(() =>
            {
                foreach (CommandAlias command in commands)
                {
                    foreach (string inputCommand in command.InputCommands)
                    {
                        if (Interpret(e.Message, out string parameters) == inputCommand)
                        {
                            OnNotify(command.InternalCommand + ((parameters == string.Empty) ? string.Empty : (":" + parameters)), MessageTypes.CameraCommand);
                        }
                    }

                    // This section probably has to be moved to a place that can be reached only if we are sure the command actually executed.
                    foreach (string replyCommand in command.ReplyCommands)
                    {
                        if (e.Message.Equals(replyCommand, StringComparison.InvariantCultureIgnoreCase))
                        {
                            OnNotify(replyCommand, MessageTypes.CameraCommand);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Saves an alias command into the settings file
        /// </summary>
        /// <param name="internalCommand">the actual command name</param>
        /// <param name="cmd">the alias command name</param>
        /// <param name="commandType">'Reply' specifies that this is a reply command</param>
        public void SaveToSettings(string internalCommand, string cmd, string commandType)
        {
            if (commandType == "Reply")
            {
                Settings.Update($@"CustomCommands/{internalCommand}/Replies/{cmd}");
            }
            else
            {
                Settings.Update($@"CustomCommands/{internalCommand}/Aliases/{cmd}");
            }
        }

        /// <summary>
        /// Deletes an alias command from the settings file
        /// </summary>
        /// <param name="internalCommand">the actual command name</param>
        /// <param name="cmd">the alias command name</param>
        /// <param name="commandType">'Reply' specifies that this is a reply command</param>
        public void RemoveFromSettings(string internalCommand, string cmd, string commandType)
        {
            if (commandType == "Reply")
            {
                Settings.Remove($@"CustomCommands/{internalCommand}/Replies/{cmd}");
            }
            else
            {
                Settings.Remove($@"CustomCommands/{internalCommand}/Aliases/{cmd}");
            }
        }

        /// <summary>
        /// Loads all alias commands from the settings file
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();

            var cmds = from entry in Settings.LoadAll("CustomCommands")
                       select new XInfo(entry.Name, entry.Value, entry.Attributes);
            foreach (var cmd in cmds)
            {
                var aliases = from entry in Settings.LoadAll($"CustomCommands/{cmd.Name}/Aliases")
                              select new XInfo(entry.Name, entry.Value, entry.Attributes);
                foreach (var alias in aliases)
                {
                    AddCommandAlias(cmd.Name, alias.Name);
                }

                var replies = from entry in Settings.LoadAll($"CustomCommands/{cmd.Name}/Replies")
                              select new XInfo(entry.Name, entry.Value, entry.Attributes);
                foreach (var rep in replies)
                {
                    AddCommandReply(cmd.Name, rep.Name);
                }
            }
        }

        /// <summary>
        /// Checks that the internal command exists in the system and finds if there is a CommandAlias for it.  If not it creates one.
        /// </summary>
        /// <param name="internalCommand">the actual command name</param>
        /// <returns><see cref="bool"/> representing whether or not the <see cref="CommandAlias"/> was created.</returns>
        private bool ValidateAndCreate(string internalCommand)
        {
            if (internalCommand == string.Empty | !MethodExists(internalCommand))
            {
                OnNotify($"Command not found: {internalCommand}");
                return false;
            }

            if (!commands.Contains(new CommandAlias(internalCommand)))
            {
                CommandAlias command = new CommandAlias(internalCommand);
                commands.Add(command);
                MessageRouter.Instance.Add(command);
            }

            return commands.Contains(new CommandAlias(internalCommand));
        }

        /// <summary>
        /// A method that replys whether or not reflection can see the <see cref="RemoteCommand"/> in the system.
        /// </summary>
        /// <param name="remoteCommand">The <see cref="RemoteCommand"/> to try and find.</param>
        /// <returns><see cref="bool"/> representing whether or not the <see cref="RemoteCommand"/> could be found.</returns>
        private bool MethodExists(string remoteCommand)
        {
            foreach (INotifier notifier in MessageRouter.Instance.Notifiers)
            {
                classes.Add(notifier.GetType());
            }

            foreach (Type t in classes)
            {
                MethodInfo[] methods = t.GetMethods(BindingFlags.Public);
                foreach (MethodInfo m in methods)
                {
                    if (m.Module.Name.Equals(remoteCommand, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// This method parses a message and separates the command from the parameters
        /// </summary>
        /// <param name="message">the incoming message</param>
        /// <param name="parameters">out: the parameters of the command</param>
        /// <returns>the command</returns>
        private string Interpret(string message, out string parameters)
        {
            parameters = string.Empty;

            if (string.IsNullOrEmpty(message))
            {
                return null;
            }

            // take the whitespace off the end of the string
            message = message.TrimEnd();

            var match = Regex.Match(message, @"(\w+):?(.+)?");

            // bail if you couldn't match anything
            if (!match.Success)
            {
                return null;
            }

            // this will either be a property, method, or alias command
            var command = match.Groups[1].Value;

            // this will be everything after the :
            parameters = match.Groups[2].Value;

            return command;
        }
    }
}

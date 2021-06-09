//-----------------------------------------------------------------------
// <copyright file="CommandAlias.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark & Aaron</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.Helpers
{
    using SubCTools.Attributes;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="CommandAlias"/> class to set aliases for <see cref="RemoteCommand"/>s.
    /// </summary>
    public class CommandAlias : DroidBase, IEquatable<CommandAlias>
    {
        /// <summary>
        /// The name of the <see cref="RemoteCommand"/> linked to this <see cref="CommandAlias"/>.
        /// </summary>
        private string internalCommand;

        /// <summary>
        /// <see cref="List{string}"/> of commands that will execute the <see cref="InternalCommand"/>.
        /// </summary>
        private List<string> inputCommands = new List<string>();

        /// <summary>
        /// <see cref="List{string}"/> of commands that will execute after completion of the <see cref="InternalCommand"/>.
        /// </summary>
        private List<string> replyCommands = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAlias"/> class.
        /// </summary>
        /// <param name="internalCommand">The name of the <see cref="RemoteCommand"/> linked to this <see cref="CommandAlias"/>.</param>
        public CommandAlias(string internalCommand)
        {
            this.internalCommand = internalCommand;
        }

        /// <summary>
        /// Type of command to set.
        /// </summary>
        public enum CommandType
        {
            /// <summary>
            /// Command used to send the <see cref="internalCommand"/>.
            /// </summary>
            Input,

            /// <summary>
            /// Command used to alert the user that the action has completed.
            /// </summary>
            Reply
        }

        public List<string> InputCommands => inputCommands;

        public List<string> ReplyCommands => replyCommands;

        /// <summary>
        /// The name of the <see cref="RemoteCommand"/> linked to this <see cref="CommandAlias"/>.
        /// </summary>
        public string InternalCommand => internalCommand;

        /// <summary>
        /// Adds a command alias to the system.
        /// </summary>
        /// <param name="command">The command to add.</param>
        /// <param name="commandType">The <see cref="CommandType"/> of the command.</param>
        public void AddCommand(string command, CommandType commandType)
        {
            if (command == string.Empty)
            {
                return;
            }

            switch (commandType)
            {
                case CommandType.Input:
                    if (inputCommands.Contains(command))
                    {
                        return;
                    }

                    inputCommands.Add(command);
                    break;

                case CommandType.Reply:
                    if (replyCommands.Contains(command))
                    {
                        return;
                    }

                    replyCommands.Add(command);
                    break;
            }
        }

        /// <summary>
        /// Clears a command alias.
        /// </summary>
        /// <param name="command">The command to remove.</param>
        /// <param name="commandType">The <see cref="CommandType"/> of the command.</param>
        public void ClearCommand(string command, CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.Input:
                    if (inputCommands.Contains(command))
                    {
                        inputCommands.Remove(command);
                    }

                    break;

                case CommandType.Reply:
                    if (replyCommands.Contains(command))
                    {
                        replyCommands.Remove(command);
                    }

                    break;
            }
        }

        /// <summary>
        /// Clears all the input and reply commands.
        /// </summary>
        public void ClearAll()
        {
            inputCommands.Clear();
            replyCommands.Clear();
        }

        public bool Equals(CommandAlias other)
        {
            return this.InternalCommand == other.InternalCommand;
        }
    }
}
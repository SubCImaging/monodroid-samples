//-----------------------------------------------------------------------
// <copyright file="ExpansionBoard.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.IO
{
    using SubCTools.Attributes;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// An object that handles expansion commands from the teensy
    /// </summary>
    public class ExpansionInput : DroidBase
    {
        /// <summary>
        /// List of all the available inputs and states
        /// </summary>
        private readonly List<ExpansionLookup> inputs = new List<ExpansionLookup>()
        {
            new ExpansionLookup() { Input = 0, State = 0 },
            new ExpansionLookup() { Input = 0, State = 1 },
            new ExpansionLookup() { Input = 1, State = 0 },
            new ExpansionLookup() { Input = 1, State = 1 },
            new ExpansionLookup() { Input = 2, State = 0 },
            new ExpansionLookup() { Input = 2, State = 1 },
            new ExpansionLookup() { Input = 3, State = 0 },
            new ExpansionLookup() { Input = 3, State = 1 }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionInput" /> class
        /// </summary>
        /// <param name="settings"> Settings service to save </param>
        public ExpansionInput(ISettingsService settings) : base(settings)
        {
        }

        /// <summary>
        /// Execute the expansion command with the given input and state
        /// </summary>
        /// <param name="input"> Input the signal was generated </param>
        /// <param name="state"> State of the signal </param>
        [RemoteCommand]
        public void ExpInput(int input, int state)
        {
            if (!IsValid(input, state))
            {
                return;
            }

            // get the command from the associated input and state
            var command = inputs.First(i => i.State == state && i.Input == input).Command;

            if (!string.IsNullOrEmpty(command))
            {
                // send it off to the interpreter to be executed
                OnNotify(command, Messaging.Models.MessageTypes.CameraCommand);
            }
            else
            {
                OnNotify($"Input: {input} State: {state} does not have an associated command. Enter one with UpdateExpansionCommand:input,state,command");
            }
        }

        /// <summary>
        /// Retrieve the expansion command with the given input and state
        /// </summary>
        /// <param name="input"> Input the signal was generated </param>
        /// <param name="state"> State of the signal </param>
        [RemoteCommand("ExpansionCommand")]
        public string GetExpansionCommand(int input, int state)
        {
            if (!IsValid(input, state))
            {
                return string.Empty;
            }

            // get the command from the associated input and state
            var command = inputs.First(i => i.State == state && i.Input == input).Command;
            return command;
        }

        /// <summary>
        /// Gets the high command for a given input
        /// </summary>
        /// <param name="input"> The input from which to retrieve the command </param>
        /// <returns> The high command for a given input </returns>
        [RemoteCommand]
        public string HighCommand(int input) => $"HighCommand{input}:{GetExpansionCommand(input, 1)}";

        /// <summary>
        /// Load the settings for this class
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();

            // go through all the expansion inputs and update the related one in the list
            foreach (var item in Settings.LoadAll("ExpansionInputs"))
            {
                var expansionInput = inputs.First(i => i.State == Convert.ToInt16(item.Attributes["state"]) && i.Input == Convert.ToInt16(item.Attributes["input"]));

                expansionInput.Command = item.Attributes["command"];
            }
        }

        /// <summary>
        /// Gets the low command for a given input
        /// </summary>
        /// <param name="input"> The input from which to retrieve the command </param>
        /// <returns> The low command for a given input </returns>
        [RemoteCommand]
        public string LowCommand(int input) => $"LowCommand{input}:{GetExpansionCommand(input, 0)}";

        /// <summary>
        /// Update the command for the given input and state, save it to the settings
        /// </summary>
        /// <param name="input"> Input to call command </param>
        /// <param name="state"> State of input when command should be called </param>
        /// <param name="command"> Command to execute </param>
        [RemoteCommand]
        public void UpdateExpansionCommand(int input, int state, string command)
        {
            if (!IsValid(input, state))
            {
                return;
            }

            var expansionInput = inputs.First(i => i.State == state && i.Input == input);

            expansionInput.Command = command;

            Settings.Update(
                @"ExpansionInputs\Input" + input + state,
                attributes: new Dictionary<string, string>()
                {
                    { "input", input.ToString() },
                    { "state", state.ToString() },
                    { "command", command }
                });
        }

        /// <summary>
        /// Check to see if a valid input and state are given
        /// </summary>
        /// <param name="input"> Input signal was generated </param>
        /// <param name="state"> State of the input signal, 0 - Low, 1 - High </param>
        /// <returns> True if the input and state are 0-3 and 0-1 respectively </returns>
        private bool IsValid(int input, int state)
        {
            if (input > 3 || input < 0)
            {
                OnNotify($"Input {input} is outside the valid range. Please enter an integer 0-3");
                return false;
            }

            if (state > 1 || state < 0)
            {
                OnNotify($"State {state} is outside the valid range. Please enter an integer 0-1");
                return false;
            }

            return true;
        }
    }
}
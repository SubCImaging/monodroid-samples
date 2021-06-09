//-----------------------------------------------------------------------
// <copyright file="ScriptBuilder.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid
{
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Converters;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Helpers;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Settings;
    using SubCTools.Settings.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class executes scripts on the Rayfin.
    /// </summary>
    public class ScriptBuilder : DroidBase, INotifiable
    {
        /// <summary>
        /// the command interpreter.
        /// </summary>
        private readonly IInterpreter interpreter;

        /// <summary>
        /// the directory to store scripts in.
        /// </summary>
        private readonly string scriptDirectory;

        /// <summary>
        /// List of commands to execute whenever a particular condition is met.
        /// </summary>
        private readonly List<string> whenCommands = new List<string>();

        /// <summary>
        /// true if the camera is to start executing the loaded script upon startup.
        /// </summary>
        private bool executeOnStart;

        /// <summary>
        /// The script that will execute on startup if <see cref="ExecuteOnStart" /> is true.
        /// </summary>
        private string executeOnStartScript;

        /// <summary>
        /// The line of a script that is currently being executed.
        /// </summary>
        private int executionIndex = 0;

        /// <summary>
        /// true if a script is currently executing.
        /// </summary>
        private bool isScriptRunning;

        /// <summary>
        /// a <see cref="FileInfo" /> of a script.
        /// </summary>
        private FileInfo script;

        /// <summary>
        /// A cancellation token to stop execution of script.
        /// </summary>
        private CancellationToken token;

        /// <summary>
        /// A cancellation token source for <see cref="token" />.
        /// </summary>
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptBuilder" /> class.
        /// </summary>
        /// <param name="interpreter"> the command interpreter. </param>
        /// <param name="settings"> the settings service. </param>
        /// <param name="scriptDirectory"> the script directory. </param>
        /// <param name="startTrigger"> an action to assist with <see cref="ExecuteOnStart" />. </param>
        public ScriptBuilder(
            IInterpreter interpreter,
            ISettingsService settings,
            string scriptDirectory,
            Action<EventHandler> startTrigger = null)
            : base(settings)
        {
            this.interpreter = interpreter;
            this.scriptDirectory = scriptDirectory;

            if (startTrigger != null)
            {
                startTrigger?.Invoke((s, e) => OnStartTrigger());
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the camera is to start executing the loaded
        /// script upon startup.
        /// </summary>
        [Savable]
        [RemoteState]
        public bool ExecuteOnStart
        {
            get => executeOnStart;
            set
            {
                if (executeOnStart == value)
                {
                    return;
                }

                Set(nameof(ExecuteOnStart), ref executeOnStart, value);
                OnNotify($"{nameof(ExecuteOnStart)}:{ExecuteOnStart}");
            }
        }

        /// <summary>
        /// Gets or sets the script that will execute on startup if <see cref="ExecuteOnStart" /> is true.
        /// </summary>
        [Savable]
        [RemoteState]
        public string ExecuteOnStartScript
        {
            get => executeOnStartScript;
            set
            {
                if (executeOnStartScript == value)
                {
                    return;
                }

                Set(nameof(ExecuteOnStartScript), ref executeOnStartScript, value);
                OnNotify($"{nameof(ExecuteOnStartScript)}:{ExecuteOnStartScript}");
            }
        }

        private bool isConcurrent;

        /// <summary>
        /// Gets a value indicating whether the camera is to execute commands concurrently.
        /// </summary>
        public bool IsConcurrent
        {
            get => isConcurrent;
            private set
            {
                if (Set(nameof(IsConcurrent), ref isConcurrent, value))
                {
                    OnNotify($"{nameof(IsConcurrent)}:{IsConcurrent}");
                }
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether the camera is to start executing the loaded
        /// script upon startup.
        /// </summary>
        [RemoteState]
        public bool IsScriptRunning
        {
            get => isScriptRunning;
            set
            {
                if (isScriptRunning == value)
                {
                    return;
                }

                isScriptRunning = value;
                OnNotify($"{nameof(IsScriptRunning)}:{IsScriptRunning}");
            }
        }

        /// <summary>
        /// Gets the filename of the loaded script.
        /// </summary>
        [RemoteState]
        public string Script => script?.FullName ?? string.Empty;

        private int whenConditionCount;

        /// <summary>
        /// Gets the number of when conditions currently set.
        /// </summary>
        public int WhenConditionCount
        {
            get => whenConditionCount;
            private set
            {
                if (Set(nameof(WhenConditionCount), ref whenConditionCount, value))
                {
                    OnNotify($"{nameof(WhenConditionCount)}:{WhenConditionCount}");
                }
            }
        }

        /// <summary>
        /// Clear all the when commands.
        /// </summary>
        [RemoteCommand]
        [Alias("ClearSchedule")]
        public void ClearWhen()
        {
            whenCommands.Clear();
            WhenConditionCount = whenCommands.Count();
        }

        /// <summary>
        /// starts concurrent execution.
        /// </summary>
        [RemoteCommand]
        [Alias("StartConcurrent")]
        public void Concurrent()
        {
            IsConcurrent = true;
        }

        /// <summary>
        /// Creates a new script file.
        /// </summary>
        /// <param name="filename"> the filename to create. </param>
        [RemoteCommand]
        public void CreateScript(string filename)
        {
            SubCLogger.Instance.Write(string.Empty, filename, scriptDirectory);
        }

        /// <summary>
        /// Deletes a script file.
        /// </summary>
        /// <param name="filename"> the filename to delete. </param>
        [RemoteCommand]
        public void DeleteScript(string filename)
        {
            var file = new FileInfo($"{scriptDirectory}/{filename}");
            file.Delete();
        }

        /// <summary>
        /// Execute the loaded script file.
        /// </summary>
        [RemoteCommand]
        [Alias("StartScript")]
        public async void ExecuteScript()
        {
            if (script == null)
            {
                OnNotify("Please set script before executing", Messaging.Models.MessageTypes.Error);
                return;
            }

            ExecuteScript(await ParseScript(script));
        }

        /// <summary>
        /// Execute the given script file.
        /// </summary>
        /// <param name="file"> Script to execute. </param>
        [RemoteCommand]
        public async void ExecuteScript(string file)
        {
            if (IsValidScript(file, out var fileInfo))
            {
                LoadScript(file);
                ExecuteScript();
            }
        }

        [RemoteCommand(hidden: true)]
        public string GetScriptText()
        {
            return GetScriptText(Script);
        }

        /// <summary>
        /// Returns the full contents of the specified script file.
        /// </summary>
        /// <param name="file"> the filename to open. </param>
        /// <returns> The contents of the script. </returns>
        [RemoteCommand]
        public string GetScriptText(string file)
        {
            var scriptText = "\n";
            if (IsValidScript(Path.Combine(scriptDirectory, file), out var fileInfo))
            {
                scriptText = Strings.EncodeNewlines(fileInfo.OpenText().ReadToEndAsync().Result.Trim());
            }

            if (IsScriptRunning)
            {
                OnNotify($"ScriptExecutionIndex:{executionIndex}");
            }

            return "ScriptText:{" + scriptText + "}";
        }

        /// <summary>
        /// Get all the stored when commands.
        /// </summary>
        [RemoteCommand]
        [Alias("WhenCommands", "Schedule")]
        public string GetWhenCommands()
        {
            return $"WhenCommands:{JsonConvert.SerializeObject(whenCommands)}";
        }

        /// <summary>
        /// evaluates an if conditional statement in a script.
        /// </summary>
        /// <param name="input"> the condition to evaluate. </param>
        /// <returns> a task. </returns>
        [RemoteCommand]
        public async Task If(string input)
        {
            if (!input.Contains("|"))
            {
                return;
            }

            var colonIndex = input.IndexOf("|");

            // everything before the colon is the command
            var expression = input.Substring(0, colonIndex);

            // everything after are the arguments
            var command = input.Substring(colonIndex + 1, input.Length - 1 - colonIndex);

            // go through all the statements and try to get the values for the inputs e.g.
            // SystemDateTime.Minute > 5 It will send SystemDateTime.Minute off to the interpreter
            // to try and get the value
            foreach (var item in expression.Split(' '))
            {
                var results = await interpreter.InterpretSync(item);

                // skip to the next input if you couldn't find any results
                if (results == null || !results.Any())
                {
                    continue;
                }

                // go through all the results until you find a successful one
                foreach (var result in results)
                {
                    if (result.MessageType == MessageTypes.Error)
                    {
                        continue;
                    }

                    var r = result.Message.Split(':');
                    expression = expression.Replace(item, r[1]);
                }
            }

            var table = new DataTable();

            try
            {
                // this will evaluate the expression in to a true/false result
                var o = table.Compute(expression, string.Empty);

                if ((bool)o)
                {
                    await interpreter.InterpretSync(command);
                }
            }
            catch (Exception e)
            {
                OnNotify("Expression: " + expression + " is invalid\n" + e.Message);
            }

            return;
        }

        /// <summary>
        /// returns a list of all script files in the <see cref="ScriptDirectory" />.
        /// </summary>
        /// <returns> a list of all script files in the <see cref="ScriptDirectory" />. </returns>
        [RemoteCommand]
        public string ListScripts()
        {
            var scriptsDir = new DirectoryInfo(scriptDirectory);
            return "Scripts:{" + string.Join(",", scriptsDir.EnumerateFiles("*.rsl").Select(s => s.ToString().Replace(scriptDirectory + "/", string.Empty)).ToList()) + "}";
        }

        /// <summary>
        /// Sets the script <see cref="file" /> to execute on start.
        /// </summary>
        /// <param name="file"> the script file to execute on start. </param>
        [RemoteCommand]
        public void LoadExecuteOnStartScript(string file)
        {
            if (IsValidScript(file, out var fileInfo))
            {
                ExecuteOnStartScript = fileInfo.FullName;
            }
        }

        /// <summary>
        /// Loads a script by name.
        /// </summary>
        /// <param name="file"> the file to load. </param>
        [RemoteCommand]
        [Alias("Script")]
        public void LoadScript(string file)
        {
            if (IsValidScript(file, out var fileInfo))
            {
                script = fileInfo;
                OnNotify($"{nameof(Script)}:{Script}");
            }
        }

        /// <summary>
        /// an override for the settings service to load the script name.
        /// </summary>
        public override void LoadSettings()
        {
            base.LoadSettings();

            OnStartTrigger();
        }

        /// <summary>
        /// Run a match and When check when you receive a command.
        /// </summary>
        /// <param name="sender"> Who sent the message. </param>
        /// <param name="e"> Notification arguments. </param>
        public async void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            var match = Regex.Match(e.Message, @"(\w+):");
            if (match.Success)
            {
                var property = match.Groups[1].Value;

                var commands = whenCommands.Where(w => w.StartsWith(property));
                foreach (var item in commands)
                {
                    await If(item);
                }
            }
        }

        /// <summary>
        /// Remove the specified when command.
        /// </summary>
        /// <param name="command"> Command to remove. </param>
        [RemoteCommand]
        [Alias("RemoveFromSchedule")]
        public void RemoveWhen(string command)
        {
            whenCommands.Remove(command);
            WhenConditionCount = whenCommands.Count();
        }

        /// <summary>
        /// starts a loop that repeats the commands in its contents a given number of times.
        /// </summary>
        /// <param name="input">
        /// the commands to repeat delimited by a | ending with an integer after the last delimiter
        /// to specify the number of times to repeat (-1 indicates to repeat indefinitely).
        /// </param>
        /// <returns> the task. </returns>
        [RemoteCommand]
        public async Task Repeat(string input)
        {
            var pattern = @"(.+)\|(-?\d+)";

            var match = Regex.Match(input, pattern);

            if (!match.Success)
            {
                return;
            }

            var commands = GetCommands(match.Groups[1].Value);

            var numberOfTimes = Convert.ToInt32(match.Groups[2].Value);

            await Task.Run(async () =>
            {
                var i = 0;
                while (numberOfTimes == -1 ? !token.IsCancellationRequested : i < numberOfTimes)
                {
                    foreach (var command in commands)
                    {
                        await interpreter.InterpretSync(command);

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    if (numberOfTimes != -1)
                    {
                        i++;
                    }
                }
            });
        }

        /// <summary>
        /// Saves a script file.
        /// </summary>
        /// <param name="input">
        /// Using the following format {ScriptName.rsl}{Script_Contents} where Script_Contents has
        /// it's newline characters encoded as `%.
        /// </param>
        [RemoteCommand]
        public void SaveScript(string input)
        {
            var regex = new Regex(@"^{(.*\.rsl)}{(.*)}$");
            var match = regex.Match(input);
            if (match.Success)
            {
                DeleteScript(match.Groups[1].Value);
                SubCLogger.Instance.Write(Strings.DecodeNewlines(match.Groups[2].Value), match.Groups[1].Value, scriptDirectory);
            }
        }

        /// <summary>
        /// Ends concurrent execution.
        /// </summary>
        [RemoteCommand]
        [Alias("EndConcurrent")]
        public void StopConcurrent()
        {
            IsConcurrent = false;
        }

        /// <summary>
        /// Stops execution of the script.
        /// </summary>
        [RemoteCommand]
        [Alias("CancelScript")]
        public void StopScript()
        {
            ClearWhen();
            tokenSource.Cancel();
            ExecuteOnStart = false;
            IsScriptRunning = false;
        }

        /// <summary>
        /// Causes the script to pause for the specified duration.
        /// </summary>
        /// <param name="timeSpan"> the duration to pause for. </param>
        /// <returns> The task. </returns>
        [RemoteCommand]
        [Alias("Delay")]
        [PropertyConverter(typeof(StringToTimeSpan))]
        public async Task WaitFor(TimeSpan timeSpan)
        {
            try
            {
                await Task.Delay(timeSpan, token);
            }
            catch
            {
                // we don't care if it throws an exception when the token is cancelled
            }
        }

        /// <summary>
        /// Causes the script to pause for the specified duration.
        /// </summary>
        /// <param name="ms"> the duration to pause for (in milliseconds). </param>
        /// <returns> The task. </returns>
        [RemoteCommand]
        public async Task WaitFor(int ms)
        {
            await WaitFor(TimeSpan.FromMilliseconds(ms));
        }

        /// <summary>
        /// Add a command to listen to internal events and preform an action.
        /// </summary>
        /// <param name="command"> Command to execute. </param>
        [RemoteCommand]
        [Alias("Schedule")]
        public void When(string command)
        {
            whenCommands.Add(command);
            WhenConditionCount = whenCommands.Count();
        }

        /// <summary>
        /// searches a string for commands.
        /// </summary>
        /// <param name="input"> the string to search. </param>
        /// <returns> an enumerable of commands. </returns>
        private static IEnumerable<string> GetCommands(string input)
        {
            // split everything not inside brackets on |
            var commands = new List<string>();

            var cmd = string.Empty;
            var insideBrackets = false;
            foreach (var c in input)
            {
                if (c == ')')
                {
                    insideBrackets = false;
                    commands.Add(cmd);
                    cmd = string.Empty;
                    continue;
                }

                if (insideBrackets)
                {
                    cmd += c;
                    continue;
                }

                if (c == '(')
                {
                    insideBrackets = true;
                    continue;
                }

                if (c == '|')
                {
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        commands.Add(cmd);
                        cmd = string.Empty;
                    }

                    continue;
                }

                cmd += c;
            }

            if (!string.IsNullOrEmpty(cmd))
            {
                commands.Add(cmd);
            }

            return commands;
        }

        /// <summary>
        /// Begins execution of a script.
        /// </summary>
        /// <param name="script"> the list of instructions in the script. </param>
        private async void ExecuteScript(IEnumerable<string> script)
        {
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;

            IsScriptRunning = true;

            executionIndex = 0;
            foreach (var item in script.ToList())
            {
                OnNotify($"ScriptExecutionIndex:{executionIndex}");
                if (!string.IsNullOrWhiteSpace(item) && !item.StartsWith("//"))
                {
                    if (IsConcurrent && !item.Contains("Concurrent"))
                    {
                        ProcessItemConcurrent(item);
                    }
                    else
                    {
                        await ProcessItemAsync(item);
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }

                executionIndex++;

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            IsScriptRunning = false;
        }

        /// <summary>
        /// Verifies that the specified <see cref="file" /> exists in the <see
        /// cref="scriptDirectory" />.
        /// </summary>
        /// <param name="file"> the filename to search for. </param>
        /// <param name="fileInfo"> out parameter with the <see cref="FileInfo" /> object. </param>
        /// <returns> true if the file is a valid script. </returns>
        private bool IsValidScript(string file, out FileInfo fileInfo)
        {
            fileInfo = null;

            if (string.IsNullOrEmpty(file))
            {
                return false;
            }

            fileInfo = new FileInfo(Path.Combine(scriptDirectory, file));
            if (!fileInfo.Exists)
            {
                OnNotify($"File: {fileInfo.FullName} doesn't exist");
            }

            return fileInfo.Exists;
        }

        /// <summary>
        /// Executed when the application launches if <see cref="ExecuteOnStart" /> is true.
        /// </summary>
        private async void OnStartTrigger()
        {
            if (ExecuteOnStart)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                ExecuteScript(ExecuteOnStartScript);
            }
        }

        /// <summary>
        /// Parses a script file and returns an enumerable of the lines in it.
        /// </summary>
        /// <param name="fileInfo"> a <see cref="FileInfo" /> to operate on. </param>
        /// <returns> IEnumerable of lines from the script file. </returns>
        private async Task<IEnumerable<string>> ParseScript(FileInfo fileInfo)
        {
            return (await fileInfo.OpenText().ReadToEndAsync()).Split('\n');
        }

        /// <summary>
        /// Executes one line from the script file.
        /// </summary>
        /// <param name="item"> a script line. </param>
        /// <returns> a task. </returns>
        private async Task ProcessItemAsync(string item)
        {
            ////SubCLogger.Instance.Write("Starting: " + item, "scriptlog.txt", DroidSystem.LogDirectory); //Debug
            var result = (await interpreter.InterpretSync(item))?.FirstOrDefault();
            ////SubCLogger.Instance.Write($"Completing: {item} - result: {result}", "scriptlog.txt", DroidSystem.LogDirectory); //Debug

            if (result != null)
            {
                OnNotify(this, result);
            }
        }

        /// <summary>
        /// an asynchronous call to <see cref="ParseItem" />.
        /// </summary>
        /// <param name="item"> a script line. </param>
        /// <returns> the task. </returns>
        private Task ProcessItemConcurrent(string item)
        {
            return Task.Run(() => ProcessItemAsync(item));
        }
    }
}
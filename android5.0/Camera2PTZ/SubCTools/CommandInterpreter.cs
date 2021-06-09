//-----------------------------------------------------------------------
// <copyright file="CommandInterpreter.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Adam Rowe</author>
//-----------------------------------------------------------------------
namespace SubCTools
{
    using F23.StringSimilarity;
    using Newtonsoft.Json;
    using SubCTools.Attributes;
    using SubCTools.Droid;
    using SubCTools.Droid.Interfaces;
    using SubCTools.Extensions;
    using SubCTools.Helpers;
    using SubCTools.Interfaces;
    using SubCTools.Messaging.Interfaces;
    using SubCTools.Messaging.Models;
    using SubCTools.Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for interpreting incoming commands and excuting them.
    /// </summary>
    public class CommandInterpreter : DroidBase, INotifiable, IInterpretationEngine, IInterpreter
    {
        private readonly IDispatcher dispatcher;

        /// <summary>
        /// All the available remote methods for registered objects.
        /// </summary>
        private readonly Dictionary<object, IEnumerable<MethodInfo>> remoteMethods = new Dictionary<object, IEnumerable<MethodInfo>>();

        /// <summary>
        /// All the properties of the registered products.
        /// </summary>
        private readonly Dictionary<object, IEnumerable<PropertyInfo>> remoteProperties = new Dictionary<object, IEnumerable<PropertyInfo>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandInterpreter"/> class.
        /// </summary>
        /// <param name="controllingObjects">Objects to execute methods and properties.</param>
        public CommandInterpreter(params object[] controllingObjects)
            : this(null, controllingObjects)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandInterpreter"/> class.
        /// </summary>
        /// <param name="dispatcher">Dispatcher used to set and get property infos.</param>
        /// <param name="controllingObjects">Objects for the interperter to control.</param>
        public CommandInterpreter(IDispatcher dispatcher, params object[] controllingObjects)
        {
            this.dispatcher = dispatcher;
            Register(controllingObjects);
            Register(this);
        }

        /// <summary>
        /// Gets Property names to append to property types for parsing IDs.
        /// </summary>
        public Dictionary<object, string> AppendPropertyName { get; } = new Dictionary<object, string>();

        /// <summary>
        /// Get all the commands to be used by the camera.
        /// </summary>
        /// <returns>Newline separated string of commands.</returns>
        [RemoteCommand]
        [Alias("Help", "?")]
        public string GetCommands()
        {
            return string.Join("\n", (from commands in remoteMethods.Values
                                      from command in commands
                                      where !((command.GetCustomAttributes(typeof(RemoteCommand)).FirstOrDefault() as RemoteCommand)?.Hidden ?? true)
                                      select $"{command.Name}" + (command.GetParameters().Length > 0 ? $": {string.Join(",", command.GetParameters().Select(c => c.ParameterType.Name + " " + c.Name))}" : string.Empty))
                                      .OrderBy(s => s));
        }

        /// <summary>
        /// Get the state JSON'd
        /// </summary>
        /// <returns>A dictionary of string,object with the property name, and value, JSON'd.</returns>
        [RemoteCommand]
        public string GetJSONState()
        {
            // create copies of the properties before touching them
            var rp = new Dictionary<object, IEnumerable<PropertyInfo>>(remoteProperties);

            // get all the remote properties tagged with RemoteState
            var properties = from o in rp.Keys
                             from prop in rp[o]
                             where prop.HasAttribute<RemoteState>()
                             select (o, prop);

            var results = new Dictionary<string, object>();

            foreach (var (o, prop) in properties)
            {
                if (TryProcessProperty(o, prop, string.Empty, out var r, false).MessageType != MessageTypes.Error)
                {
                    results.Update(r.Item1, r.Item2);
                }
            }

            return JsonConvert.SerializeObject(results);
        }

        /// <summary>
        /// Get the state of all the classes registered with the command interpreter.
        /// </summary>
        /// <returns>Newline separated string of property name and value.</returns>
        [RemoteCommand]
        [Alias("State")]
        public string GetState()
        {
            // create copies of the properties before touching them
            var rp = new Dictionary<object, IEnumerable<PropertyInfo>>(remoteProperties);

            // get all the remote properties tagged with RemoteState
            var properties = from o in rp.Keys
                             from prop in rp[o]
                             where prop.HasAttribute<RemoteState>()
                             select $"{TryProcessProperty(o, prop, string.Empty, out _).Message}".TrimEnd();

            var rm = new Dictionary<object, IEnumerable<MethodInfo>>(remoteMethods);

            // get all the methods that also have remote state
            var stateMethods = from o in rm.Keys
                               from m in rm[o]
                               where m.HasAttribute<RemoteState>()
                               select $"{m.Name}:{ProcessMethod(o, m, new string[] { }).Result.Message}";

            // join them together and order them alphbetically
            var result = properties.Concat(stateMethods).OrderBy(a => a);

            return string.Join("\n", result);
        }

        /// <summary>
        /// Interpret the given input, execute method or property if possible. Will notify each result.
        /// </summary>
        /// <param name="input">Input sent by user to interpret.</param>
        /// <returns>Interpretation result with all the results generated from each action.</returns>
        public async Task<InterpretationResult> Interpret(string input)
        {
            var results = await InterpretSync(input).ConfigureAwait(false);
            foreach (var result in results.Where(r => r != null))
            {
                OnNotify(this, result);
            }

            var couldInterpret = (!(results?.Any() ?? false)) ? false : !results?.Any(r => r != null && r.MessageType == MessageTypes.Error) ?? false;

            // if there are no results, it failed to interpret
            // if there are any Error messages, it failed
            return new InterpretationResult(string.Join("\n", results?.Select(r => r?.Message ?? string.Empty)), couldInterpret);
        }

        /// <summary>
        /// Interpret the given input, execute method or property if possible.
        /// </summary>
        /// <param name="input">Input sent by user to interpret.</param>
        /// <returns>IAll the individual interpretation results.</returns>
        async Task<InterpretationResult> IInterpretationEngine.InterpretSync(string input)
        {
            return await Interpret(input).ConfigureAwait(false);
        }

        /// <summary>
        /// Interpret the given input, execute method or property if possible.
        /// </summary>
        /// <param name="input">Input sent by user to interpret.</param>
        /// <returns>IAll the individual interpretation results.</returns>
        public async Task<IEnumerable<NotifyEventArgs>> InterpretSync(string input)
        {
            string command;
            List<string> arguments;
            try
            {
                var parsedInput = ParseInput(input);
                command = parsedInput.command;
                arguments = parsedInput.args.ToList();
            }
            catch (ArgumentException e)
            {
                return new[] { new NotifyEventArgs(e.Message, MessageTypes.Error) };
            }

            var isNest = command.Contains(".");

            var objectName = string.Empty;

            // Remove this later when we add functionality for setting properties like this Object.Property:ValueToSet
            if (isNest)
            {
                if (arguments.Count > 0)
                {
                    var parsed = ParseCommand(command).ToList();

                    objectName = parsed[0];
                    command = parsed[1];
                }
                else
                {
                    // Sets the arguments to the list of subproperties of the object "separated by dots"
                    (arguments = ParseCommand(command).ToList()).RemoveAt(0);
                    command = isNest ? ParseCommand(command).First() : command;
                }
            }

            // find all the matching commands with the given name and argument count
            var matchingCommands = GetRemoteCommands(command, arguments.Count(), objectName).ToList();

            // it's only going to be a property if the argumentsCount is 0 or 1
            IEnumerable<(object, PropertyInfo)> matchingProperties = new (object, PropertyInfo)[] { };
            if (arguments.Count() <= 1)
            {
                // you're trying to set a property if there's an agument
                matchingProperties = GetRemoteProperties(command, !isNest && arguments.Count() == 1);
            }

            // check to see if it's a property or command, list potential alternatives if it's not
            // if (!matchingProperties.Any() && !matchingCommands.Any())
            // {
            //     var alternatives = GetAlternatives(input);
            //     return new[] { new NotifyEventArgs("Could not find reference to " + input + (alternatives.Any() ? " Did you mean:\n" + string.Join("\n", alternatives) : string.Empty), MessageTypes.Error) };
            // }
            var verb = matchingProperties.Any() ? "set " : "execute ";

            var results = new List<NotifyEventArgs>();

            results.AddRange(await ProcessCommands(matchingCommands, arguments));

            // go through all the properties and set them
            foreach ((var owner, var property) in matchingProperties)
            {
                if (IsCancel(property.GetCustomAttributes<CancelWhenAttribute>(), verb, command, out var message))
                {
                    return new[] { new NotifyEventArgs(message, MessageTypes.Error) };
                }
                else
                {
                    var o = owner;
                    var p = property;

                    foreach (var item in arguments.ToList())
                    {
                        var t = p.GetValue(owner, null);
                        if (t?.IsProperty(item) ?? false)
                        {
                            arguments.Remove(item);
                            o = t;
                            p = t.GetProperty(item);
                        }
                    }

                    var result = TryProcessProperty(o, p, arguments.FirstOrDefault(), out _);
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Receive a notification to interpret.
        /// </summary>
        /// <param name="sender">Who sent the request.</param>
        /// <param name="e">Notification to interpret.</param>
        public void ReceiveNotification(object sender, NotifyEventArgs e)
        {
            _ = Interpret(e.Message);
        }

        /// <summary>
        /// Register objects to make them available to execute members.
        /// </summary>
        /// <param name="objects">Objects to register.</param>
        public void Register(params object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj == null)
                {
                    continue;
                }

                remoteMethods.Update(obj, from m in obj.GetType().GetMethods() where m.HasAttribute<RemoteCommand>() || m.HasAttribute<RemoteState>() select m);

                remoteProperties.Update(obj, from p in obj.GetType().GetProperties() select p);
            }
        }

        /// <summary>
        /// Unregister an object so it's no longer touched by the command interpreter.
        /// </summary>
        /// <param name="obj">Object to unregister from the interpreter.</param>
        public void Unregister(object obj)
        {
            if (remoteMethods.ContainsKey(obj))
            {
                remoteMethods.Remove(obj);
            }

            if (remoteProperties.ContainsKey(obj))
            {
                remoteProperties.Remove(obj);
            }
        }

        /// <summary>
        /// Get a list of alternative members that are similar to the input.
        /// </summary>
        /// <param name="input">Input to get a list of available alternatives for.</param>
        /// <returns>An enumerable of string alternatives.</returns>
        private IEnumerable<string> GetAlternatives(string input)
        {
            var l = new NormalizedLevenshtein();

            var matches = (from m in GetCommands().Split('\n').Concat(GetState().Split('\n'))
                           let d = l.Distance(input, m)
                           where d < 0.9
                           select new { Name = m, Distance = d }).OrderBy(t => t.Distance);

            var relevant = matches.Where(m => Math.Abs(matches.First().Distance - m.Distance) < 0.1);
            return relevant.Select(r => r.Name);
        }

        /// <summary>
        /// Get the results from overloaded methods. If one fails, but one succeeds, only return the result of the succeeding method. Return all results if they all have failed.
        /// </summary>
        /// <param name="results">The results acquired from overloaded methods.</param>
        /// <returns>Collection of appropriate results.</returns>
        private IEnumerable<NotifyEventArgs> GetOverloadedResults(Dictionary<object, List<NotifyEventArgs>> results)
        {
            var r = new List<NotifyEventArgs>();

            foreach (var item in results)
            {
                // if there's no successes, return all the failures
                // otherwise send back the successful one
                if (item.Value.Any(v => v?.MessageType == MessageTypes.Information) || item.Value.Any(v => v == null))
                {
                    r.Add(item.Value.FirstOrDefault(v => v?.MessageType == MessageTypes.Information));
                }
                else
                {
                    r.AddRange(item.Value);
                }
            }

            return r;
        }

        /// <summary>
        /// Get the owner of the given property.
        /// </summary>
        /// <param name="property">Property name to get owner.</param>
        /// <returns>Owner of property.</returns>
        private object GetOwner(string property)
        {
            return GetRemoteProperties(property, false).FirstOrDefault().owner;
        }

        /// <summary>
        /// Get a collection of remote commands that have the name, or alias, with the given number of parameters.
        /// </summary>
        /// <param name="name">Name of command to reteieve.</param>
        /// <param name="numberOfParams">Number of parameters the command must have.</param>
        /// <returns>A tuple collection with the owner and method info.</returns>
        private IEnumerable<Tuple<object, MethodInfo>> GetRemoteCommands(string name, int numberOfParams, string objectName = "")
        {
            var remoteMethods = this.remoteMethods;

            if (!string.IsNullOrEmpty(objectName))
            {
                // see if an object with the owner exists if supplied
                if (remoteMethods.Any(k => k.Key.GetType().Name.ToLower() == objectName.ToLower()))
                {
                    var kvp = remoteMethods.First(k => k.Key.GetType().Name.ToLower() == objectName.ToLower());
                    remoteMethods = new Dictionary<object, IEnumerable<MethodInfo>>() { { kvp.Key, kvp.Value } };
                }
            }

            // get all the methods that have this name
            var query1 = from k in remoteMethods
                         from m in k.Value
                         let methodName = m.Name.ToLower()
                         where methodName + (AppendPropertyName.ContainsKey(k.Key) ? AppendPropertyName[k.Key] : string.Empty) == name.ToLower()
                         where m.GetParameters().Length == numberOfParams
                         select new Tuple<object, MethodInfo>(k.Key, m);

            // remove all the other result if any of them are overriding
            if (query1.Any(v => v.Item2.GetCustomAttribute<RemoteCommand>().Override))
            {
                query1 = query1.Where(v => v.Item2.GetCustomAttribute<RemoteCommand>().Override);
            }

            // get all the methods that have an alias that has this name
            var query2 = from k in remoteMethods
                         from m in k.Value
                         where m.GetParameters().Length == numberOfParams
                         where m.HasAttribute<AliasAttribute>()
                         let a = m.GetCustomAttribute<AliasAttribute>()
                         from alias in a.Aliases
                         where alias.ToLower() == name.ToLower()
                         select new Tuple<object, MethodInfo>(k.Key, m);

            // remove all the other result if any of them are overriding
            if (query2.Any(v => v.Item2.GetCustomAttribute<RemoteCommand>()?.Override ?? false))
            {
                query2 = query2.Where(v => v.Item2.GetCustomAttribute<RemoteCommand>().Override);
            }

            // join both lists together
            return query1.Concat(query2);
        }

        /// <summary>
        /// Get a collection of remote properties that have the name, or alias.
        /// </summary>
        /// <param name="name">Name or alias of property to get.</param>
        /// <param name="isSet">True if a you are requesting a property that can be set.  (if true, properties with '<see cref="RemoteState.IsPrivateSet"/> == true' will not be returned). </param>
        /// <returns>A tuple collection with the owner and property.</returns>
        private IEnumerable<(object owner, PropertyInfo property)> GetRemoteProperties(string name, bool isSet)
        {
            // get all the methods that have this name
            var query1 = from k in remoteProperties
                         from p in k.Value
                         let propertyName = p.Name.ToLower()
                         where propertyName + (AppendPropertyName.ContainsKey(k.Key) ? AppendPropertyName[k.Key] : string.Empty) == name.ToLower()
                         select (k.Key, p);

            // get all the methods that have an alias that has this name
            var query2 = from k in remoteProperties
                         from p in k.Value
                         where p.HasAttribute<AliasAttribute>()
                         let a = p.GetCustomAttribute<AliasAttribute>()
                         from alias in a.Aliases
                         where alias.ToLower() == name.ToLower()
                         select (k.Key, p);

            // combine both the lists
            var result = query1.Concat(query2).ToList();

            // if you're a set command, remove all the entries that you can't set
            if (isSet)
            {
                foreach ((var key, var p) in result.ToList())
                {
                    // if it's a list, and we can to add values to it, don't remove
                    if (typeof(IList).IsAssignableFrom(p.PropertyType))
                    {
                        continue;
                    }

                    if (!p.CanWrite || !p.SetMethod.IsPublic || (p.HasAttribute<RemoteState>() ? p.GetCustomAttribute<RemoteState>().IsPrivateSet : false))
                    {
                        result.Remove((key, p));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check to see if a particular cancel case prevents this interpretation from being executed.
        /// </summary>
        /// <param name="cancelAttributes">A list of <see cref="CancelWhenAttribute"/>s to check. </param>
        /// <param name="verb">Verb to use in error reporting.  The word 'set' if this is a property, otherwise the word 'execute' or empty string.</param>
        /// <param name="command">Command to check.</param>
        /// <param name="message">Message to return.</param>
        /// <returns>True if is a cancel case, false if not.</returns>
        private bool IsCancel(IEnumerable<CancelWhenAttribute> cancelAttributes, string verb, string command, out string message)
        {
            message = string.Empty;

            foreach (var ccase in cancelAttributes)
            {
                var conditionsMet = IsCancel(ccase, verb, command, out message);

                if (conditionsMet)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check to see if a particular cancel case prevents this interpretation from being executed.
        /// </summary>
        /// <param name="cancelAttribute">A <see cref="CancelWhenAttribute"/> to check. </param>
        /// <param name="verb">Verb to use in error reporting.  The word 'set' if this is a property, otherwise the word 'execute' or empty string.</param>
        /// <param name="command">Command to check.</param>
        /// <param name="message">Message to return.</param>
        /// <returns>True if is a cancel case, false if not.</returns>
        private bool IsCancel(CancelWhenAttribute cancelAttribute, string verb, string command, out string message)
        {
            message = string.Empty;

            var conditionsMet = true;

            foreach (var cond in cancelAttribute.CancelConditions)
            {
                conditionsMet &= cond.Evaluate(GetOwner(cond.CancelWhenProp));

                if (conditionsMet)
                {
                    message += ((message == string.Empty) ? string.Empty : " and ") + cond.ToString();
                }
                else
                {
                    break;
                }
            }

            if (conditionsMet)
            {
                if (cancelAttribute.CustomMessage != null)
                {
                    message = cancelAttribute.CustomMessage;
                }
                else
                {
                    message = $"Cannot {verb}{command} when {message}";
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse the input arguments, and break them in to individual strings.
        /// </summary>
        /// <param name="input">Arguments given to command.</param>
        /// <returns>Each argument separated that is not an empty string.</returns>
        private IEnumerable<string> ParseArgument(string input)
        {
            if (input.IsValidJson())
            {
                return new[] { input };
            }

            // it's a comma separated argument list
            return input.Split(',').Where(s => !string.IsNullOrEmpty(s));
        }

        /// <summary>
        /// Parse the input arguments, and break them in to individual strings.
        /// </summary>
        /// <param name="input">Arguments given to command.</param>
        /// <returns>Each argument separated that is not an empty string.</returns>
        private IEnumerable<string> ParseCommand(string input)
        {
            // it's a dot separated object path
            return input.Split('.').Where(s => !string.IsNullOrEmpty(s));
        }

        /// <summary>
        /// Parse the interpreter input in to command and arguments.
        /// </summary>
        /// <param name="input">Given input string to interpreter.</param>
        /// <returns>Command and collection of arguments.</returns>
        private (string command, IEnumerable<string> args) ParseInput(string input)
        {
            // take the whitespace off the ends of the string
            input = input.Trim();

            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input is empty");
            }

            // command with parameter(s)
            if (input.Contains(":"))
            {
                var colonIndex = input.IndexOf(":");

                // everything before the colon is the command
                var command = input.Substring(0, colonIndex);

                // everything after are the arguments
                var parameters = input.Substring(colonIndex + 1, input.Length - 1 - colonIndex);

                // returns tuple<command, arguments>
                return (command, ParseArgument(parameters));
            }
            else
            {
                return (input, new List<string>());
            }
        }

        /// <summary>
        /// Prosess call the given methods.
        /// </summary>
        /// <param name="matchingCommands">All the methods that have matched the given input.</param>
        /// <param name="arguments">Arguments to use when executing method.</param>
        /// <returns>Collection of results from the method executions.</returns>
        private async Task<IEnumerable<NotifyEventArgs>> ProcessCommands(
            List<Tuple<object, MethodInfo>> matchingCommands,
            IEnumerable<string> arguments)
        {
            var results = new List<NotifyEventArgs>();
            var overloadedResults = new Dictionary<object, List<NotifyEventArgs>>();

            // go through all the commands and execute them in turn
            foreach ((var owner, var method) in matchingCommands)
            {
                var command = method.Name;

                if (IsCancel(method.GetCustomAttributes<CancelWhenAttribute>(), "execute", command, out var message))
                {
                    return new[] { new NotifyEventArgs(message, MessageTypes.Error) };
                }
                else
                {
                    NotifyEventArgs result;

                    try
                    {
                        result = await ProcessMethod(owner, method, arguments.ToArray());
                    }
                    catch (Exception e)
                    {
                        result = new NotifyEventArgs($"Error: Could not execute {command}! {e.Message}", MessageTypes.Error);
                    }

                    // check to see if it's an overloaded commands
                    if (matchingCommands.Count(m => m.Item1 == owner) > 1)
                    {
                        if (overloadedResults.ContainsKey(owner))
                        {
                            overloadedResults[owner].Add(result);
                        }
                        else
                        {
                            overloadedResults.Add(owner, new List<NotifyEventArgs>() { result });
                        }
                    }
                    else
                    {
                        results.Add(result);
                    }
                }
            }

            results.AddRange(GetOverloadedResults(overloadedResults));

            return results;
        }

        /// <summary>
        /// Process the given method.
        /// </summary>
        /// <param name="controllingObject">Owner of method.</param>
        /// <param name="method">Method to execute.</param>
        /// <param name="arguments">Array of parameters.</param>
        /// <returns>Notification of execution result.</returns>
        private async Task<NotifyEventArgs> ProcessMethod(object controllingObject, MethodInfo method, string[] arguments)
        {
            var convertedArguments = new List<object>();

            if (arguments != null)
            {
                convertedArguments.AddRange(arguments);
            }

            // check to see if there's a specific property converter
            var converterAttribute = method.GetCustomAttribute<PropertyConverterAttribute>();

            // see if there's an output converter for the return type
            var outputConverterAttribute = method.GetCustomAttribute<OutputConverter>();

            // try to convert the value, send the proper format back if you can't
            var converter = converterAttribute?.Converter;

            var outputConverter = outputConverterAttribute?.Converter;

            var a = method.GetParameters();

            try
            {
                // make an array to hold all the converted parameters in
                var methodParams = method.GetParameters();

                for (var i = 0; i < (arguments?.Length ?? 0); i++)
                {
                    object convertedValue;
                    if (arguments[i].IsValidJson())
                    {
                        try
                        {
                            // , jsonConverters.ToArray());
                            convertedValue = JsonConvert.DeserializeObject(arguments[i], methodParams[i].ParameterType);
                        }
                        catch
                        {
                            // failed to convert in to JSON object
                            return new NotifyEventArgs($"Could not convert value: {arguments[i]} is to {methodParams[i].ParameterType}", MessageTypes.Error);
                        }
                    }
                    else if (converter != null && (string.IsNullOrEmpty(converterAttribute.ArgumentName) ? true : converterAttribute.ArgumentName == a[i].Name))
                    {
                        if (!converter.TryConvert(arguments[i], out convertedValue))
                        {
                            return new NotifyEventArgs($"Could not convert value: {arguments[i]}\nPlease follow this format:{converter.Format}", MessageTypes.Error);
                        }
                    }
                    else if (methodParams[i].ParameterType == typeof(string))
                    {
                        continue;
                    }
                    else
                    {
                        // no specific converter was specified, just try the generic one
                        convertedValue = Convert.ChangeType(arguments[i], methodParams[i].ParameterType);
                    }

                    convertedArguments[i] = convertedValue;
                }

                object result = null;
                try
                {
                    result = method?.Invoke(controllingObject, convertedArguments.Count() > 0 ? convertedArguments.ToArray() : null);
                }
                catch (Exception e)
                {
                    return new NotifyEventArgs($"{method.Name} failed to execute: {e.Message}", MessageTypes.Error);
                }

                // check to see if the return type is a task
                if (method.ReturnType.IsSubclassOf(typeof(Task)) || method.ReturnType == typeof(Task))
                {
                    var task = (Task)result;
                    await task;

                    // reset the result so it doesn't print System.Task.whatever
                    result = null;

                    // it has underlying generic arguments, like a bool, or int, then wait for the Task to complete to get the result
                    if (method.ReturnType.GenericTypeArguments.Length > 0)
                    {
                        result = task.GetType().GetProperty("Result").GetValue(task);
                    }
                }

                // send back the result if the return was not void
                if (result != null)
                {
                    object r = null;
                    if (outputConverter?.TryConvert(result, out r) ?? false)
                    {
                        result = r;
                    }

                    // serialize as json if it's an ienumerable object
                    // a string is an IEnumerable<char> but we don't want that to be json'd
                    if (!(result is string) && (typeof(IEnumerable).IsAssignableFrom(result.GetType()) || result.GetType().IsArray))
                    {
                        // string.Join(",", v.ToArray());
                        result = JsonConvert.SerializeObject(result);
                    }

                    // prepend the prefix if supplied so we can match it topside, e.g. BaudRate:9600
                    var prefix = method?.GetCustomAttribute<RemoteCommand>()?.Prefix;
                    if (prefix != null && prefix != string.Empty)
                    {
                        prefix += ":";
                    }

                    return new NotifyEventArgs($"{prefix}{result}", MessageTypes.Information);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Process the given property info.
        /// </summary>
        /// <param name="controllingObject">Owner of the property.</param>
        /// <param name="property">Property to execute.</param>
        /// <param name="parameter">Paramter to set if there is any.</param>
        /// <returns>Notification of execution result.</returns>
        private NotifyEventArgs TryProcessProperty(object controllingObject, PropertyInfo property, string parameter, out Tuple<string, object> result, bool jsonArrays = true)
        {
            result = null;

            // get the properties value
            var propertyValue = property.GetValue(controllingObject);

            var propertyType = property.PropertyType;

            object convertedValue = null;

            // check to see if there's a specific property converter
            var converterAttribute = property.GetCustomAttribute<PropertyConverterAttribute>();

            // try to convert the value, send the proper format back if you can't
            var converter = converterAttribute?.Converter;

            // you want to set the property with the given parameter
            if (!string.IsNullOrEmpty(parameter))
            {
                if (parameter.IsValidJson())
                {
                    try
                    {
                        // try to convert the incoming value into an object if it's Json
                        convertedValue = JsonConvert.DeserializeObject(parameter, property.PropertyType);
                    }
                    catch
                    {
                        // failed to convert in to JSON object
                        return new NotifyEventArgs($"Could not convert value: {parameter} is to {property.PropertyType}", MessageTypes.Error);
                    }
                }

                // set command, e.g. focus:100
                else if (converter != null)
                {
                    if (!converter.TryConvert(parameter, out convertedValue))
                    {
                        return new NotifyEventArgs($"Could not convert value: {propertyValue}\nPlease follow this format:{converter.Format}", MessageTypes.Error);
                    }
                }
                else
                {
                    try
                    {
                        // no specific converter was specified, just try the generic one
                        convertedValue = Convert.ChangeType(parameter, propertyType); // propertyValue.GetType());
                    }
                    catch
                    {
                        return new NotifyEventArgs($"Could not convert value: {propertyValue}", MessageTypes.Error);
                    }
                }

                void MakeEqual()
                {
                    var a = property.GetValue(controllingObject) as IList;
                    a.MakeIListEqualTo(convertedValue as IList);
                }

                // add all the elements to the list, otherwise try to set the property
                if (typeof(IList).IsAssignableFrom(property.PropertyType))
                {
                    if (dispatcher != null)
                    {
                        dispatcher.Invoke(() => MakeEqual());
                    }
                    else
                    {
                        MakeEqual();
                    }
                }
                else
                {
                    // set the value
                    property.SetValue(controllingObject, convertedValue);
                }

                propertyValue = property.GetValue(controllingObject);
            }

            // now try convert the value back to send
            if (converter != null && !converter.TryConvertBack(propertyValue, out propertyValue))
            {
                return new NotifyEventArgs($"Could not convert value: {propertyValue}\nPlease follow this format:{converter.Format}", MessageTypes.Error);
            }

            // serialize as json if it's an ienumerable object
            // a string is an IEnumerable<char> but we don't want that to be json'd
            if (jsonArrays && propertyValue != null && !(propertyValue is string) && (typeof(IEnumerable).IsAssignableFrom(propertyValue.GetType()) || propertyValue.GetType().IsArray))
            {
                // string.Join(",", v.ToArray());
                propertyValue = JsonConvert.SerializeObject(propertyValue);
            }

            result = new Tuple<string, object>(property.Name, propertyValue);

            // always send the result get command, e.g. focus
            return new NotifyEventArgs($"{result.Item1}:{result.Item2 ?? "null"}", MessageTypes.Information);
        }
    }
}
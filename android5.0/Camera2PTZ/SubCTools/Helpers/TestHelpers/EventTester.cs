namespace SubCTools.Helpers.TestHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// A class to assist with testing events.
    /// </summary>
    public class EventTester
    {
        /// <summary>
        /// Returns the delegate attached to the event. <see langword="null"/>if there are none.
        /// </summary>
        /// <param name="objectWithEvent">The object with the event to test.</param>
        /// <param name="eventName">The name of the event to test.</param>
        /// <returns>The degate attached to the event.</returns>
        public static Delegate GetDelegateAttachedTo(object objectWithEvent, string eventName)
        {
            if (objectWithEvent == null)
            {
                return null;
            }

            var allBindings = BindingFlags.IgnoreCase | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            var type = objectWithEvent.GetType();
            var fieldInfo = type.GetField(eventName, allBindings);

            while (fieldInfo == null)
            {
                if (type == typeof(object))
                {
                    throw new ArgumentException($"Event {eventName} does not exist on type {objectWithEvent.GetType().FullName} or any of its base classes.");
                }

                type = type.BaseType;
                fieldInfo = type.GetField(eventName, allBindings);
            }

            var value = fieldInfo.GetValue(objectWithEvent);

            var handler = value as Delegate;
            return handler;
        }

        /// <summary>
        /// Returns all delegates attached to all events on the given object.
        /// </summary>
        /// <param name="objectWithEvent">The object to get events of.</param>
        /// <returns>An enumerable of all attached delegates.</returns>
        public static IEnumerable<Delegate> GetAllDelegatesAttachedTo(object objectWithEvent)
        {
            if (objectWithEvent == null)
            {
                return null;
            }

            var handlers = new List<Delegate>();
            var eventNames = objectWithEvent.GetType().GetEvents().Select(e => e.Name);

            foreach (var eventName in eventNames)
            {
                var handler = GetDelegateAttachedTo(objectWithEvent, eventName);
                if (handler != null)
                {
                    handlers.Add(handler);
                }
            }

            return handlers;
        }

        /// <summary>
        /// Gets a dictionary of eventNames to delegates indicating event subscriptions. 
        /// </summary>
        /// <param name="objectWithEvent">The object to test the events of.</param>
        /// <returns>A dictionary that relates event names to their attached handlers.</returns>
        public static IDictionary<string, Delegate> GetAllDelegates(object objectWithEvent)
        {
            if (objectWithEvent == null)
            {
                return null;
            }

            var handlers = new Dictionary<string, Delegate>();
            var eventNames = objectWithEvent.GetType().GetEvents().Select(e => e.Name);

            foreach (var eventName in eventNames)
            {
                var handler = GetDelegateAttachedTo(objectWithEvent, eventName);
                handlers[eventName] = handler;
            }

            return handlers;
        }

        /// <summary>
        /// Fires all events with Null Parameters.
        /// </summary>
        /// <param name="objectWithEvent">The object to fire the event on.</param>
        public static void FireAllEvents(object objectWithEvent)
        {
            var eventNames = objectWithEvent.GetType().GetEvents().Select(e => e.Name);

            foreach (var eventName in eventNames)
            {
                Raise(objectWithEvent, eventName);
            }
        }

        /// <summary>
        /// Fires all events matching the generic type on the given object.
        /// </summary>
        /// <typeparam name="TEventArgs">The type expected for the event handerl.</typeparam>
        /// <param name="objectWithEvent">The object to raise the event on.</param>
        /// <paramm name="args">The event args to use with the specified event.</paramm>
        public static void FireAllEvents<TEventArgs>(object objectWithEvent, TEventArgs args)
             where TEventArgs : EventArgs
        {
            var eventNames = objectWithEvent.GetType().GetEvents().Select(e => e.Name);

            foreach (var eventName in eventNames)
            {
                var evt = objectWithEvent.GetType().GetEvent(eventName);
                var invk = evt.EventHandlerType.GetMethod("Invoke");
                var paramType = invk.GetParameters()[1].ParameterType;

                if (args.GetType() == paramType)
                {
                    Raise<TEventArgs>(objectWithEvent, eventName, args);
                }
            }
        }

        /// <summary>
        /// Raises the given event on the given object with null eventargs.
        /// </summary>
        /// <param name="objectWithEvent">The object to raise the event on.</param>
        /// <param name="eventName">The name of the event to raise.</param>
        public static void Raise(object objectWithEvent, string eventName)
        {
            var eventDelegate = (Delegate)objectWithEvent.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(objectWithEvent);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { objectWithEvent, null });
                }
            }
        }

        /// <summary>
        /// Raised the given event on the given object with the given args.
        /// </summary>
        /// <typeparam name="TEventArgs">The eventArgs type of the eventhandler.</typeparam>
        /// <param name="objectWithEvent">The object to raise the event on.</param>
        /// <param name="eventName">The name of the event to raise.</param>
        /// <param name="eventArgs">The event args to use with the event.</param>
        public static void Raise<TEventArgs>(object objectWithEvent, string eventName, TEventArgs eventArgs)
            where TEventArgs : EventArgs
        {
            var eventDelegate = (Delegate)objectWithEvent.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(objectWithEvent);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { objectWithEvent, eventArgs });
                }
            }
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="ObjectExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Various</author>
//-----------------------------------------------------------------------
namespace SubCTools.Extensions
{
    using SubCTools.Attributes;
    using SubCTools.Helpers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    /// <summary>
    /// Extensions for all objects.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Get the property info from the supplied object.
        /// </summary>
        /// <param name="sender">Object to get property from.</param>
        /// <param name="propertyName">Name of the property to retrieve.</param>
        /// <returns>Property info associated with object.</returns>
        public static PropertyInfo GetProperty(this object sender, string propertyName)
        {
            return sender.GetType().GetProperty(propertyName);
        }

        /// <summary>
        /// Update all the properties of an object with a new one.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="o">Object to update.</param>
        /// <param name="obj">Object to get information from.</param>
        public static void UpdateAll<T>(this T o, T obj)
            where T : class
        {
            // get all the properties of the object
            var properties = o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            // loop through them all
            foreach (var property in properties)
            {
                o.UpdateProperty(obj, property);
            }
        }

        /// <summary>
        /// Update property value from identical type.
        /// </summary>
        /// <typeparam name="T">Type of object to update.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="o">Object to update property on.</param>
        /// <param name="updateFrom">Object to update property from.</param>
        /// <param name="propertyName">Property name to update.</param>
        public static void UpdateProperty<T, K>(this T o, K updateFrom, string propertyName)
            where T : class
            where K : class
        {
            o.UpdateProperty(updateFrom, o.GetProperty(propertyName));
        }

        /// <summary>
        /// Update property value from identical type.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="o">Object to update property on.</param>
        /// <param name="updateFrom">Object to update property from.</param>
        /// <param name="getPropertyName">Property to get.</param>
        /// <param name="setPropertyName">Property to set.</param>
        public static void UpdateProperty<T, K>(this T o, K updateFrom, string getPropertyName, string setPropertyName)
            where T : class
            where K : class
        {
            o.UpdateProperty(updateFrom, updateFrom.GetProperty(getPropertyName), o.GetProperty(setPropertyName));
        }

        /// <summary>
        /// Update property value from identical type.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="o">Object to update property on.</param>
        /// <param name="updateFrom">Object to update property from.</param>
        /// <param name="property">Property to update.</param>
        public static void UpdateProperty<T, K>(this T o, K updateFrom, PropertyInfo property)
            where T : class
            where K : class
        {
            o.UpdateProperty(updateFrom, property, property);
        }

        /// <summary>
        /// Update property value from identical type.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="o">Object to update property on.</param>
        /// <param name="updateFrom">Object to update property from.</param>
        /// <param name="getProperty">Property to get.</param>
        /// <param name="setProperty">Property to set.</param>
        public static void UpdateProperty<T, K>(this T o, K updateFrom, PropertyInfo getProperty, PropertyInfo setProperty)
            where T : class
            where K : class
        {
            // get the property of the object to update from
            var updaterProperty = updateFrom.GetType().GetProperty(getProperty.Name);

            if (updaterProperty == null)
            {
                throw new Exception("Property: " + getProperty.Name + " does exist in class: " + updateFrom.GetType().Name);
            }

            // get the current value
            var currentValue = setProperty.GetValue(o, new object[] { });

            // get the new value
            var newValue = updaterProperty.GetValue(updateFrom, new object[] { });

            try
            {
                // set the value of the property to the new value if it's different
                setProperty.SetValue(o, newValue, new object[] { });
            }
            catch
            {
                // is readonly property
            }
        }

        /// <summary>
        /// Sync the property values of a list of objects.
        /// </summary>
        /// <typeparam name="T">Type of object o sync.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="sync">Object to sync with.</param>
        /// <param name="syncableObjects">Objects you wish to sync with the parent.</param>
        public static void Sync<T, K>(this T sync, IEnumerable<K> syncableObjects)
            where T : INotifyPropertyChanged
            where K : class
        {
            foreach (var syncable in syncableObjects)
            {
                sync.Sync(syncable);
            }
        }

        /// <summary>
        /// Sync the property values of two objects.
        /// </summary>
        /// <typeparam name="T">Type of object o sync.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="syncer">Object to sync with.</param>
        /// <param name="syncObject">Object you wish to sync with the parent.</param>
        public static void Sync<T, K>(this T syncer, K syncObject)
            where T : INotifyPropertyChanged
            where K : class
        {
            syncer.Sync(syncObject, (s) => true);
        }

        /// <summary>
        /// Sync the specified property values of two objects.
        /// </summary>
        /// <typeparam name="T">Type of object o sync.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="syncer">Object to sync with.</param>
        /// <param name="syncObject">Object you wish to sync with the parent.</param>
        /// <param name="syncOnlyThese">An array containing the property names to sync.</param>
        public static void Sync<T, K>(this T syncer, K syncObject, params string[] syncOnlyThese)
            where T : INotifyPropertyChanged
            where K : class
        {
            syncer.Sync(syncObject, (s) => syncOnlyThese.Contains(s));
        }

        /// <summary>
        /// Sync the specified property values of two objects.
        /// </summary>
        /// <typeparam name="T">Type of object o sync.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="syncer">Object to sync with.</param>
        /// <param name="syncObject">Object you wish to sync with the parent.</param>
        /// <param name="itemsToSync">dictionary of properties to sync.</param>
        public static void Sync<T, K>(this T syncer, K syncObject, Dictionary<string, string> itemsToSync)
            where T : INotifyPropertyChanged
            where K : class
        {
            syncer.Sync(syncObject, (s) => itemsToSync.ContainsKey(s), itemsToSync);
        }

        ////public static void Sync<T, K, U>(this T syncer, K syncObject, string getProperty, string setProperty, Func<U> converter)
        ////    where T : INotifyPropertyChanged
        ////    where K : class
        ////{

        ////syncer.Sync(syncObject, (s) => itemsToSync.ContainsKey(s), itemsToSync);
        ////}

        ////public static void Sync<T, K, U>(this T syncer, K syncObject, PropertyInfo setProperty, Func<U> converter)
        ////    where T : INotifyPropertyChanged
        ////    where K : class
        ////{
        ////syncer.Sync(syncObject, (s) => itemsToSync.ContainsKey(s), itemsToSync);
        ////}

        /// <summary>
        /// Sync the specified property values of two objects of different types <see cref="T"/> and <see cref="K"/>. 
        /// </summary>
        /// <typeparam name="T">Type of object o sync.</typeparam>
        /// <typeparam name="K">Type of source object.</typeparam>
        /// <param name="syncer">Object to sync with.</param>
        /// <param name="syncObject">Object you wish to sync with the parent.</param>
        /// <param name="condition">determine if property is to be synced.</param>
        /// <param name="itemsToSync">dictionary of property names to translate from type T to type K.</param>
        public static void Sync<T, K>(this T syncer, K syncObject, Func<string, bool> condition, Dictionary<string, string> itemsToSync = null)
            where T : INotifyPropertyChanged
            where K : class
        {
            syncer.PropertyChanged += (s, e) =>
            {
                if (condition(e.PropertyName))
                {
                    var setPropertyName = e.PropertyName;
                    if (itemsToSync != null)
                    {
                        setPropertyName = itemsToSync[e.PropertyName];
                    }

                    syncObject.UpdateProperty(syncer as object, e.PropertyName, setPropertyName);
                }
            };
        }

        /// <summary>
        /// Returns true if the object is locked.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <returns>true if locked.</returns>
        public static bool IsLocked(this object o)
        {
            if (!Monitor.TryEnter(o))
            {
                return true;
            }

            Monitor.Exit(o);
            return false;
        }

        /// <summary>
        /// Returns true if the object has a property with the specified name.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <param name="name">the property name.</param>
        /// <returns>true if property exists.</returns>
        public static bool IsProperty(this object o, string name)
        {
            return o.GetType()
.GetProperty(name) != null;
        }

        /// <summary>
        /// Try to get a property from the supplied object.
        /// </summary>
        /// <param name="o">Object to get property from.</param>
        /// <param name="propertyName">Name of property to get.</param>
        /// <param name="property">Property that was found.</param>
        /// <returns>True if the property was found, false if not.</returns>
        public static bool TryGetProperty(this object o, string propertyName, out PropertyInfo property)
        {
            property = o.GetType().GetProperty(propertyName);
            return property != null;
        }

        /// <summary>
        /// Returns true if the object has a property of the given name and it is publicly set-able.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <param name="name">the property name.</param>
        /// <returns>true if peroperty is public set-able.</returns>
        public static bool IsSetProperty(this object o, string name)
        {
            return o.GetType()
.GetProperty(name)?.CanWrite != null;
        }

        /// <summary>
        /// Returns true if the object has a property of the given name and it is publicly readable.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <param name="name">the property name.</param>
        /// <returns>true if peroperty is public readable.</returns>
        public static bool IsGetProperty(this object o, string name)
        {
            return o.GetType()
.GetProperty(name)?.CanRead != null;
        }

        /// <summary>
        /// Returns true if the object has a method with the specified number of parameters.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <param name="name">the property name.</param>
        /// <param name="paramsCount">the number of parameters.</param>
        /// <returns>true if the method exists.</returns>
        public static bool IsMethod(this object o, string name, int paramsCount)
        {
            return GetMethod(o, name, paramsCount) != null;
        }

        /// <summary>
        /// Returns a methodInfo of a method on the object if the object has a method of the specified name and number of parameters.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <param name="name">the property name.</param>
        /// <param name="paramsCount">the number of parameters.</param>
        /// <returns>the MethodInfo if the method exists.</returns>
        public static MethodInfo GetMethod(this object o, string name, int paramsCount)
        {
            return (from m in o.GetType().GetMethods()
                    let a = m.GetCustomAttribute<AliasAttribute>()?.Aliases ?? new string[0]
                    where (m.Name.ToLower() == name.ToLower() || a.Select(s => s.ToLower()).Contains(name.ToLower())) && m.GetParameters().Count() == paramsCount
                    select m).FirstOrDefault();
        }

        /// <summary>
        /// Returns a methodInfo of a method on the object if the object has a method of the specified name and number of parameters with the RemoteCommand attribute.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <param name="name">the property name.</param>
        /// <param name="paramsCount">the number of parameters.</param>
        /// <returns>the MethodInfo if the method exists.</returns>
        public static MethodInfo GetRemoteCommand(this object o, string name, int paramsCount)
        {
            return (from m in o.GetType().GetMethods()
                    let a = m.GetCustomAttribute<AliasAttribute>()?.Aliases ?? new string[0]
                    where m.HasAttribute<RemoteCommand>() && (m.Name.ToLower() == name.ToLower() || a.Select(s => s.ToLower()).Contains(name.ToLower())) && m.GetParameters().Count() == paramsCount
                    select m).FirstOrDefault();
        }

        /// <summary>
        /// Checks to see if the member has given attribute.
        /// </summary>
        /// <typeparam name="T">Type of attribute to check.</typeparam>
        /// <param name="m">Member to check.</param>
        /// <returns>True if the member has the given attribute, false if not.</returns>
        public static bool HasAttribute<T>(this MemberInfo m) where T : Attribute
        {
            return m.GetCustomAttributes<T>().Any();
        }

        /// <summary>
        /// Try to get the method from the supplied object with the same number of parameters.
        /// </summary>
        /// <param name="o">Object to get method from.</param>
        /// <param name="methodName">Name of method to get.</param>
        /// <param name="numberOfParameters">Number of parameters the method must have.</param>
        /// <param name="method">Method to retrieve.</param>
        /// <returns>True if a method with the same number of parameters was found, false if not.</returns>
        public static bool TryGetMethod(this object o, string methodName, int numberOfParameters, out MethodInfo method)
        {
            method = o.GetMethod(methodName, numberOfParameters);
            return method != null;
        }

        /// <summary>
        /// Try to get the method from the supplied object with the same number of parameters that has the RemoteCommand attribute.
        /// </summary>
        /// <param name="o">Object to get method from.</param>
        /// <param name="methodName">Name of method to get.</param>
        /// <param name="numberOfParameters">Number of parameters the method must have.</param>
        /// <param name="method">Method to retrieve.</param>
        /// <returns>True if a method with the same number of parameters was found, false if not.</returns>
        public static bool TryGetRemoteCommand(this object o, string methodName, int numberOfParameters, out MethodInfo method)
        {
            method = o.GetRemoteCommand(methodName, numberOfParameters);
            return method != null;
        }
    }
}

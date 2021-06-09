// <copyright file="ICRUD.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Settings.Interfaces
{
    /// <summary>
    /// Class for creating an ICRUD interface.
    /// </summary>
    public interface ICRUD
    {
        /// <summary>
        /// Create a new entry in the settings.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="entry">Entry to enter in to settings.</param>
        void Create<T>(T entry);

        /// <summary>
        /// Delete the entry from the settings.
        /// </summary>
        /// <typeparam name="T">Type of object.</typeparam>
        /// <param name="entry">Entry to delete from settings.</param>
        void Delete<T>(T entry);

        /// <summary>
        /// Get the object from the settings.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <param name="entry">Entry containing the ID of the object to load.</param>
        /// <returns>Object with the same ID from the settings.</returns>
        T Read<T>(T entry);

        /// <summary>
        /// Read all the objects of the given type.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <returns>Array of all the objects of the given type.</returns>
        T[] ReadAll<T>();

        /// <summary>
        /// Update the entry with the given ID.
        /// </summary>
        /// <typeparam name="T">Type to get out.</typeparam>
        /// <param name="entry">Entry to update.</param>
        void Update<T>(T entry);
    }
}
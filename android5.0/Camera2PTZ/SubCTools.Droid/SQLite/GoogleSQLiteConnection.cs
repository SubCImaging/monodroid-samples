//-----------------------------------------------------------------------
// <copyright file="DatabaseDataWorker.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.SQLite
{
    using Android.Content;
    using Android.Database;
    using SubCTools.Droid.Properties;
    using SubCTools.Settings.Interfaces;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// The SQLite data worker that handles basic CRUD operations.
    /// </summary>
    public class GoogleSQLiteConnection// : ISQLiteConnection Reason: SQLite was moved to extras because it doesn't work on Android
    {
        private readonly Context context;

        private readonly string[] propertyColumns =
        {
            RayfinDatabaseContract.NameColumn,
            RayfinDatabaseContract.ValueColumn,
            RayfinDatabaseContract.AttributesColumn
        };

        /// <summary>
        /// A reference to the <see cref="RayfinDatabaseOpenHelper"/>
        /// </summary>
        private RayfinDatabaseOpenHelper databaseHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleSQLiteConnection"/> class.
        /// </summary>
        /// <param name="context">The <see cref="Context"/></param>
        public GoogleSQLiteConnection(Context context)
        {
            this.context = context;
        }

        /// <summary>
        /// Performs the INSERT statement to create a new record in the database
        /// </summary>
        /// <param name="property">The property to add to the database</param>
        /// <returns>Return code</returns>
        public int Create(SqlInfo property)
            => Create(property.Name, property.Value, property.Attributes);

        public int Create(string name, string value, string attributes)
            => Update(name, value, attributes, true);

        /// <summary>
        /// Deletes a specified row in the WHERE clause
        /// </summary>
        /// <param name="name">The primary key to delete</param>
        /// <returns>Return code</returns>
        public int Delete(string name)
        {
            var selection = $"{RayfinDatabaseContract.NameColumn} = ?";
            string[] selectionArgs =
            {
                name
            };

            var database = databaseHelper.WritableDatabase;
            return database.Delete(RayfinDatabaseContract.TableName, selection, selectionArgs);
        }

        /// <summary>
        /// Opens the database from the given file
        /// </summary>
        /// <param name="filename">The fully qualified path to the database file</param>
        public void Open(string filename)
        {
            databaseHelper = new RayfinDatabaseOpenHelper(context, filename);
        }

        /// <summary>
        /// Opens the database using the default path
        /// </summary>
        public void Open()
        {
            databaseHelper = new RayfinDatabaseOpenHelper(context);
        }

        /// <summary>
        /// Reads the table records based on the primary keynoted within the input parameter
        /// </summary>
        /// <param name="name">The primary key to read</param>
        /// <returns>A <see cref="RayfinProperty"/></returns>
        public SqlInfo Read(string name)
        {
            var selection = $"{RayfinDatabaseContract.NameColumn} = ?";
            string[] selectionArgs = { name };
            var db = databaseHelper.ReadableDatabase;

            // Get the database cursor
            var cursor = db.Query(
                RayfinDatabaseContract.TableName,
                propertyColumns,
                selection,
                selectionArgs,
                null,
                null,
                null);

            cursor.MoveToFirst();

            // Get all the column indicies
            var valuePos = cursor.GetColumnIndex(RayfinDatabaseContract.ValueColumn);
            var attributesPos = cursor.GetColumnIndex(RayfinDatabaseContract.AttributesColumn);

            // Get the data
            try
            {
                var value = cursor.GetString(valuePos);
                var attributes = cursor.GetString(attributesPos);

                // Close the cursor to prevent memory leaks
                cursor.Close();
                return new SqlInfo { Name = name, Value = value, Attributes = attributes };
            }
            catch (CursorIndexOutOfBoundsException)
            {
                // Database doesn't contain entry
                cursor.Close();
                return null;
            }
        }

        /// <summary>
        /// Loads all <see cref="RayfinProperty"/>s from the database
        /// </summary>
        /// <param name="dbHelper">The <see cref="RayfinDatabaseOpenHelper"/></param>
        /// <returns>A collection of <see cref="RayfinProperty"/>s</returns>
        public IEnumerable<SqlInfo> ReadAll()
        {
            var db = databaseHelper.ReadableDatabase;

            var cursor = db.Query(
                RayfinDatabaseContract.TableName,
                propertyColumns,
                null,
                null,
                null,
                null,
                RayfinDatabaseContract.NameColumn,
                null);

            // Get all column indices
            var propertyNamePos = cursor.GetColumnIndex(RayfinDatabaseContract.NameColumn);
            var propertyValuePos = cursor.GetColumnIndex(RayfinDatabaseContract.ValueColumn);
            var propertyAttributesPos = cursor.GetColumnIndex(RayfinDatabaseContract.AttributesColumn);

            // Get a reference to the database manager singleton
            var properties = new List<SqlInfo>();

            // iterate through the rows
            while (cursor.MoveToNext())
            {
                // Get all data from the row
                var propertyName = cursor.GetString(propertyNamePos);
                var propertyValue = cursor.GetString(propertyValuePos);
                var propertyAttributes = cursor.GetString(propertyAttributesPos);

                var info = new SqlInfo()
                {
                    Name = propertyName,
                    Value = propertyValue,
                    Attributes = propertyAttributes
                };

                properties.Add(info);
            }

            // Close cursor to prevent memory leaks
            cursor.Close();

            return properties;
        }

        /// <summary>
        /// Executes an UPDATE statement on the table based on the specified primary key
        /// for a record within the WHERE clause of the statement
        /// </summary>
        /// <param name="info">The new values to update</param>
        /// <returns>Return code</returns>
        public int Update(SqlInfo info) => Update(info.Name, info.Value, info.Attributes);

        /// <summary>
        /// Executes an UPDATE statement on the table based on the specified primary key
        /// for a record within the WHERE clause of the statement.
        /// </summary>
        /// <param name="name">The primary key to update</param>
        /// <param name="value">The new value</param>
        /// <param name="attributes">The new attributes</param>
        /// <param name="createNew">Whether or not to create a new row</param>
        /// <returns>Return code</returns>
        public int Update(string name, string value, string attributes, bool createNew = false)
        {
            var selection = $"{RayfinDatabaseContract.NameColumn} = ?";
            string[] selectionArgs = { name };

            // Put all the values together for insertion into the table
            ContentValues values = new ContentValues();
            values.Put(RayfinDatabaseContract.NameColumn, name);
            values.Put(RayfinDatabaseContract.ValueColumn, value);
            values.Put(RayfinDatabaseContract.AttributesColumn, attributes);

            // Get a writeable reference to the database and update the values
            var database = databaseHelper.WritableDatabase;

            return createNew ? (int)database.Insert(RayfinDatabaseContract.TableName, null, values)
            : database.Update(RayfinDatabaseContract.TableName, values, selection, selectionArgs);
        }
    }
}
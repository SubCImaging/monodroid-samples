//-----------------------------------------------------------------------
// <copyright file="RayfinDatabaseOpenHelper.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Aaron Watson</author>
//-----------------------------------------------------------------------

namespace SubCTools.Droid.SQLite
{
    using Android.Content;
    using Android.Database.Sqlite;
    using SubCTools.Droid.Properties;

    /// <summary>
    /// A helper class to manage database creation and version management
    /// </summary>
    public class RayfinDatabaseOpenHelper : SQLiteOpenHelper
    {
        /// <summary>
        /// The filename to use to save the database
        /// </summary>
        public const string DatabaseFilename = "Settings.db";

        /// <summary>
        /// The location to save the database file
        /// </summary>
        public const string DatabaseLocation = "/sdcard/Settings/" + DatabaseFilename;

        /// <summary>
        /// The version of the database, this is used to handle updates
        /// to new database versions
        /// </summary>
        public const int DatabaseVersion = 1;

        /// <summary>
        /// The SQL query to create the SqlInfo table if it doesn't already exist.
        /// </summary>
        private readonly string createTable = $"CREATE TABLE IF NOT EXISTS {RayfinDatabaseContract.TableName} (" +
            $"{RayfinDatabaseContract.NameColumn} varchar primary key not null, {RayfinDatabaseContract.ValueColumn} " +
            $"varchar, {RayfinDatabaseContract.AttributesColumn} varchar)";

        /// <summary>
        /// Initializes a new instance of the <see cref="RayfinDatabaseOpenHelper"/> class.
        /// </summary>
        /// <param name="context">The <see cref="Context"/></param>
        public RayfinDatabaseOpenHelper(Context context)
            : base(context, DatabaseLocation, null, DatabaseVersion)
        {
            // Create the table if it doesn't already exist
            var db = WritableDatabase;
            db.ExecSQL(createTable);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RayfinDatabaseOpenHelper"/> class.
        /// </summary>
        /// <param name="context">The <see cref="Context"/></param>
        /// <param name="absolutePath">The absolute path to the database file</param>
        public RayfinDatabaseOpenHelper(Context context, string absolutePath)
            : base(context, absolutePath, null, DatabaseVersion)
        {
            // Create the table if it doesn't already exist
            var db = WritableDatabase;
            db.ExecSQL(createTable);
        }

        /// <summary>
        /// OnCreate override(not used)
        /// </summary>
        /// <param name="db">The <see cref="SQLiteDatabase"/></param>
        public override void OnCreate(SQLiteDatabase db)
        {
        }

        /// <summary>
        /// OnUpgrade will be used in the future to handle migration between database
        /// versions
        /// </summary>
        /// <param name="db">A reference to the database in question</param>
        /// <param name="oldVersion">The version of the database to upgrade</param>
        /// <param name="newVersion">The version to update the database to</param>
        public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            throw new System.NotImplementedException();
        }
    }
}
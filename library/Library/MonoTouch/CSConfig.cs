using System;
using System.IO;
using Mono.Data.Sqlite;

namespace Vici.CoolStorage
{
	[Flags]
	public enum SqliteOption
	{
		None = 0,
		CreateIfNotExists = 1,
		UseConnectionPooling = 2
	}
	
	public static partial class CSConfig
	{
        public static void SetDB(string dbName)
        {
			SetDB(dbName,SqliteOption.UseConnectionPooling);
        }

		public static void SetDB(string dbName,  Action creationDelegate)
        {
			SetDB(dbName,SqliteOption.UseConnectionPooling|SqliteOption.CreateIfNotExists, creationDelegate);
        }

        public static void SetDB(string dbName, SqliteOption sqliteOption)
        {
			SetDB(dbName,sqliteOption,null);
        }

        public static void SetDB(string dbName, SqliteOption sqliteOption, Action creationDelegate)
        {
			bool exists = File.Exists(dbName);

			bool createIfNotExists = (sqliteOption & SqliteOption.CreateIfNotExists) != 0;
			bool usePooling = (sqliteOption & SqliteOption.UseConnectionPooling) != 0;
			
			if (!exists && createIfNotExists)
				SqliteConnection.CreateFile(dbName);
			
			
            SetDB(new CSDataProviderSQLite("Data Source=" + dbName + ";Pooling=" + usePooling), DEFAULT_CONTEXTNAME);
			
			if (!exists && createIfNotExists && creationDelegate != null)
				creationDelegate();
        }
	}
}

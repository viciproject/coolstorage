using System;
using System.IO;
using Windows.Storage;

namespace Vici.CoolStorage.WP8.Sqlite
{
    public class CS : CSConfig
    {
        public static void SetDB(string dbName)
        {
            SetDB(dbName,SqliteOption.CreateIfNotExists, null);
        }

        public static void SetDB(string dbName,  Action creationDelegate)
        {
            SetDB(dbName,SqliteOption.CreateIfNotExists, creationDelegate);
        }

        public static void SetDB(string dbName, SqliteOption sqliteOption)
        {
            SetDB(dbName,sqliteOption,null);
        }

        private static bool FileExists(StorageFolder folder, string fileName)
        {
            try
            {
                var task = folder.GetFileAsync(fileName).AsTask();

                task.Wait();
                
                return true;
            }
            catch { return false; }
        }


        public static void SetDB(string dbName, SqliteOption sqliteOption, Action creationDelegate)
        {
            SetDB(ApplicationData.Current.LocalFolder,dbName,sqliteOption,creationDelegate);
        }

        public static void SetDB(StorageFolder folder, string dbName, SqliteOption sqliteOption, Action creationDelegate)
        {            
            bool createIfNotExists = (sqliteOption & SqliteOption.CreateIfNotExists) != 0;
            bool createAlways = (sqliteOption & SqliteOption.CreateAlways) != 0;

            bool exists = FileExists(folder,dbName);

            if (createAlways && exists)
            {
                exists = false;

                var task = folder.GetFileAsync(dbName).AsTask();

                task.Wait();

                task.Result.DeleteAsync().AsTask().Wait();
            }

            SetDB(new CSDataProviderSqliteWP8("uri=file://" + Path.Combine(folder.Path,dbName)), DEFAULT_CONTEXTNAME);
			
            if (!exists && (createIfNotExists || createAlways) && creationDelegate != null)
                creationDelegate();
        }
    }

    [Flags]
    public enum SqliteOption
    {
        None = 0,
        CreateIfNotExists = 1,
        CreateAlways = 2
    }

}
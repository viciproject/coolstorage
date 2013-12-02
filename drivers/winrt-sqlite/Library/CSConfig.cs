#region License
//=============================================================================
// Vici CoolStorage - .NET Object Relational Mapping Library 
//
// Copyright (c) 2004-2011 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.IO;
using Windows.Storage;

namespace Vici.CoolStorage.WinRT.Sqlite
{
	[Flags]
	public enum SqliteOption
	{
		None = 0,
		CreateIfNotExists = 1,
        CreateAlways = 2
	}
	
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

            SetDB(new CSDataProviderSqliteWinRT("uri=file://" + Path.Combine(folder.Path,dbName)), DEFAULT_CONTEXTNAME);
			
			if (!exists && (createIfNotExists || createAlways) && creationDelegate != null)
				creationDelegate();
        }
	}
}

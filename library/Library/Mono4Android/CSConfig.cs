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

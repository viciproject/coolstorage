#region License
//=============================================================================
// Vici CoolStorage - .NET Object Relational Mapping Library 
//
// Copyright (c) 2004-2009 Philippe Leybaert
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
using System.Collections.Generic;
using System.Data;

namespace Vici.CoolStorage
{
	public class CSTransaction : IDisposable
	{
		private readonly CSDataProvider _database;
		private bool _completed;
			
		public CSTransaction(CSIsolationLevel isolationLevel)
		{
			_database = CSConfig.GetDB(CSConfig.DEFAULT_CONTEXTNAME);

			_database.BeginTransaction(isolationLevel);
		}
		
        public CSTransaction(CSIsolationLevel isolationLevel, string contextName)
        {
            _database = CSConfig.GetDB(contextName);

            _database.BeginTransaction(isolationLevel);
        }

	    internal CSTransaction()
	    {
            _database = CSConfig.GetDB(CSConfig.DEFAULT_CONTEXTNAME);

            _database.BeginTransaction();
        }

        internal CSTransaction(CSDataProvider dataProvider)
        {
            _database = dataProvider;

            _database.BeginTransaction();
        }

        internal CSTransaction(CSDataProvider dataProvider, CSIsolationLevel isolationLevel)
        {
            _database = dataProvider;

            _database.BeginTransaction(isolationLevel);
        }

		internal CSTransaction(CSSchema schema)
		{
			_database = schema.DB;

			_database.BeginTransaction();
		}
		
		internal CSTransaction(CSSchema schema, CSIsolationLevel isolationLevel)
		{
			_database = schema.DB;

			_database.BeginTransaction(isolationLevel);
		}

		public void Commit()
		{
			_completed = true;

			_database.Commit();
		}
		
		public void Rollback()
		{
			_completed = true;

			_database.Rollback();
		}
		
		public void Dispose()
		{
			if (!_completed)
				Rollback();
		}
	}
}

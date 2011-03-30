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
using System.Collections;
using System.Collections.Generic;

namespace Vici.CoolStorage
{
	internal class CSSchemaColumnCollection : IEnumerable<CSSchemaColumn>
	{
        private readonly List<CSSchemaColumn> _columnList = new List<CSSchemaColumn>();
        private readonly Dictionary<string, CSSchemaColumn> _columnMap = new Dictionary<string, CSSchemaColumn>(StringComparer.CurrentCultureIgnoreCase);

		internal CSSchemaColumn this[string columnName]
		{
			get
			{
                if (_columnMap.ContainsKey(columnName))
                    return _columnMap[columnName];
                else
                    return null;
			}
		}

		internal CSSchemaColumn this[int index]
		{
			get
			{
                return _columnList[index];
			}
		}

		internal void Add(CSSchemaColumn column)
		{
			_columnMap[column.Name] = column;
            _columnList.Add(column);
		}

		internal void Clear()
		{
			_columnMap.Clear();
            _columnList.Clear();
		}

		internal int Count
		{
			get
			{
				return _columnList.Count;
			}
		}


        #region IEnumerable<CSSchemaColumn> Members

        public IEnumerator<CSSchemaColumn> GetEnumerator()
        {
            return _columnList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _columnList.GetEnumerator();
        }

        #endregion
    }
}

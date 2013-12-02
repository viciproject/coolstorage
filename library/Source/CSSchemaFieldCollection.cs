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
	internal class CSSchemaFieldCollection : IEnumerable<CSSchemaField>
	{
        private readonly List<CSSchemaField> _fieldList = new List<CSSchemaField>();
        private readonly Dictionary<string, CSSchemaField> _fieldMap = new Dictionary<string, CSSchemaField>();

		internal CSSchemaField this[string fieldName]
		{
			get
			{
                CSSchemaField value;

                if (_fieldMap.TryGetValue(fieldName, out value))
                    return value;
                else
                    return null;
			}
		}

		internal void Add(CSSchemaField field)
		{
			_fieldList.Add(field);
			_fieldMap[field.Name] = field;
		}

		public IEnumerator<CSSchemaField> GetEnumerator()
		{
			return _fieldList.GetEnumerator();
		}

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fieldList.GetEnumerator();
        }
	}
}

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
	internal class CSFieldValueCollection : IEnumerable<CSFieldValue>
	{
        private readonly Dictionary<string, CSFieldValue> _map = new Dictionary<string, CSFieldValue>();
        private readonly List<CSFieldValue> _list = new List<CSFieldValue>();

		internal CSFieldValueCollection(CSObject csObject)
		{
			foreach (CSSchemaField schemaField in csObject.Schema.Fields)
			{
				CSFieldValue fieldValue = new CSFieldValue(csObject,schemaField);

				_list.Add(fieldValue);

				if (fieldValue.SchemaField.HasProperty)
					_map[fieldValue.SchemaField.Name] = fieldValue;

				if (fieldValue.SchemaField.MappedColumn != null)
					_map["#" + fieldValue.SchemaField.MappedColumn.Name] = fieldValue;
			}
		}

		internal CSFieldValue this[string memberName]
		{
			get
			{
                CSFieldValue value;

                if (_map.TryGetValue(memberName, out value))
                    return value;
                else
                    return null;
			}
		}

		internal bool IsDirty
		{
			get
			{
				foreach (CSFieldValue fieldValue in _list)
					if (fieldValue.IsDirty)
						return true;

				return false;
			}
		}

		public IEnumerator<CSFieldValue> GetEnumerator()
		{
			return _list.GetEnumerator();
		}

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
	}
}

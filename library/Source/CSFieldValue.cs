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
using Vici.Core;

namespace Vici.CoolStorage
{
	internal enum CSFieldValueState
	{
		Unread,
		Read,
		Dirty
	}

	internal class CSFieldValue
	{
		private CSFieldValueState _valueState = CSFieldValueState.Unread;
		private object _value;

		private readonly CSSchemaField  _schemaField;

		private readonly CSObject       _csObject;

		internal CSFieldValue(CSObject objectData , CSSchemaField schemaField)
		{
			_csObject = objectData;
			_schemaField = schemaField;
		}

		internal CSSchemaField SchemaField
		{
			get
			{
				return _schemaField;
			}
		}

		internal Type BaseType
		{
			get
			{
				return SchemaField.FieldType;
			}
		}

		internal void Reset()
		{
			_value = null;
			_valueState = CSFieldValueState.Unread;
		}

		internal object Value
		{
			get
			{
				if (_valueState == CSFieldValueState.Unread)
					_csObject.ReadField(SchemaField);

				return _value;
			}
			set
			{
				if (_valueState != CSFieldValueState.Dirty)
					_valueState = CSFieldValueState.Read;

				if (!Equals(value,_value))
				{
					ValueDirect = value;

					_valueState = CSFieldValueState.Dirty;
				}
			}
		}

		internal object ValueDirect
		{
			get
			{
				return _value;
			}
			set
			{
				if (value != null)
				{
					if (value is string && _schemaField.RealType == typeof(DateTime))
						_value = ((string)value).To(_schemaField.FieldType,"yyyy-MM-dd HH:mm:ss.FFFFFFF");
					else
                        _value = value.Convert(_schemaField.FieldType);
				}
			    else
				    _value = null;
			}
		}

		internal CSFieldValueState ValueState
		{
			get
			{
				return _valueState;
			}
			set
			{
				_valueState = value;
			}
		}

		internal bool IsDirty
		{
			get
			{
				return _valueState == CSFieldValueState.Dirty;
			}
		}

		public override string ToString()
		{
			if (_value == null)
				return null;
			else
				return _value.ToString();
		}
	}
}

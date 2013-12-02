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
using System.Reflection;
using Vici.Core;

namespace Vici.CoolStorage
{
	internal class CSSchemaField
	{
		private readonly PropertyInfo   _propertyInfo;
		private readonly CSSchema       _schema;
		private readonly CSSchemaColumn _mappedColumn;
		private readonly CSRelation     _relation;
        private readonly Type           _fieldType;
        private readonly Type           _realType;
		private readonly bool           _lazy;
		private readonly object         _nullValue;
		private readonly bool           _noCreate;
        private readonly bool           _trim;
        private readonly bool           _optimisticLock;
		private readonly bool           _prefetch;
	    private readonly bool           _serverGenerated;
	    private readonly bool           _clientGenerated;
	    private readonly bool           _notMapped;
        private readonly string         _sequenceName;

		internal CSSchemaField(CSSchemaColumn schemaColumn , CSSchema schema)
		{
			_schema = schema;
			_mappedColumn = schemaColumn;

			//if (schemaColumn.DataType == typeof(byte[]))
			//    _lazy = true;

			schemaColumn.MappedField = this;

            _fieldType = MappedColumn.DataType;

		    _realType = _fieldType.Inspector().RealType;
		}

		internal CSSchemaField(PropertyInfo propInfo , CSSchema schema)
		{
			_propertyInfo = propInfo;
			_schema = schema;
            _fieldType = _propertyInfo.PropertyType;
		    _realType = _fieldType.Inspector().RealType;

		    RelationAttribute attRelation = propInfo.GetCustomAttribute<RelationAttribute>(true);
			
            _prefetch = propInfo.IsDefined(typeof(PrefetchAttribute), true);

			if (attRelation != null)
			{
				_relation = new CSRelation(schema,attRelation);

 				return;
			}

			_lazy = propInfo.IsDefined(typeof(LazyAttribute) , true);
			_noCreate = propInfo.IsDefined(typeof(NoCreateAttribute) , true);
            _trim = propInfo.IsDefined(typeof(TrimAttribute), true);
            _optimisticLock = propInfo.IsDefined(typeof(OptimisticLockAttribute), true);
		    _clientGenerated = propInfo.IsDefined(typeof(ClientGeneratedAttribute), true);
		    _serverGenerated = propInfo.IsDefined(typeof(ServerGeneratedAttribute), true);
		    _notMapped = propInfo.IsDefined(typeof (NotMappedAttribute), true);

			var mapToAttribute = propInfo.GetCustomAttribute<MapToAttribute>(true);
			var nullValueAttribute  = propInfo.GetCustomAttribute<NullValueAttribute>(true);
            var identityAttribute = propInfo.GetCustomAttribute<IdentityAttribute>(true);

            if (!_notMapped)
            {
                if (CSConfig.ColumnMappingOverrideMap.ContainsValue(propInfo.DeclaringType.Name + ":" + propInfo.Name))
                    _mappedColumn =
                        schema.Columns[
                            CSConfig.ColumnMappingOverrideMap[propInfo.DeclaringType.Name + ":" + propInfo.Name]];
                else if (mapToAttribute != null)
                    _mappedColumn = schema.Columns[mapToAttribute.Name];
                else
                    _mappedColumn = schema.Columns[propInfo.Name];

                if (_mappedColumn != null)
                    _mappedColumn.MappedField = this;
            }


            var sequenceAttribute = propInfo.GetCustomAttribute<SequenceAttribute>(true);

            if (sequenceAttribute != null && _schema.DB.SupportsSequences)
            {
                _sequenceName = sequenceAttribute.SequenceName;

                if (_mappedColumn != null && sequenceAttribute.Identity)
                {
                    _mappedColumn.Identity = true;
                    _schema.IdentityColumn = _mappedColumn;
                }
            }

            if (identityAttribute != null)
            {
                if (_mappedColumn != null)
                {
                    _mappedColumn.Identity = true;
                    _schema.IdentityColumn = _mappedColumn;
                }
            }

		    if (nullValueAttribute != null)
			{
				_nullValue = nullValueAttribute.NullValue;
			}
			else
			{
				Type fieldType = FieldType;

				if (fieldType == typeof(string))         
                    _nullValue = String.Empty;
                else if (fieldType.GetTypeInfo().IsValueType)
                    _nullValue = Activator.CreateInstance(fieldType);
			}

			if (_mappedColumn != null && _mappedColumn.ReadOnly)
			{
				if (_propertyInfo.CanWrite)
					throw new CSException("Property [" + Name + "] for class [" + _schema.ClassType.Name + "] should be read-only");
			}
		}

		internal bool ReadOnly
		{
			get
			{
				if (_propertyInfo != null)
					return !(_propertyInfo.CanWrite);
				else
					return MappedColumn.ReadOnly;
			}
		}

        internal bool Trim
        {
            get
            {
                return _trim;
            }
        }

		internal bool Lazy
		{
			get
			{
				return _lazy;
			}
		}

		internal bool NoCreate
		{
			get
			{
				return _noCreate;
			}
		}

        internal bool OptimisticLock
        {
            get
            {
                return _optimisticLock;
            }
        }

		internal bool Prefetch
		{
			get
			{
				return _prefetch;
			}
		}

        internal bool ServerGenerated
        {
            get
            {
                return _serverGenerated;
            }
        }

        internal bool ClientGenerated
        {
            get
            {
                return _clientGenerated;
            }
        }

		internal bool HasProperty
		{
			get
			{
				return _propertyInfo != null;
			}
		}

		internal object NullValue
		{
			get
			{
				return _nullValue;
			}
		}

	    internal  string SequenceName
	    {
	        get
	        {
	            return _sequenceName;
	        }
	    }

		internal CSSchemaColumn MappedColumn
		{
			get
			{
				return _mappedColumn;
			}
		}

		internal CSRelation Relation
		{
			get
			{
				return _relation;
			}
		}

		internal Type FieldType
		{
			get
			{
			    return _fieldType;
			}
		}

        internal Type RealType
        {
            get
            {
                return _realType;
            }
        }

		internal string Name
		{
			get
			{
				if (_propertyInfo != null)
					return _propertyInfo.Name;
				else
					return "#"+MappedColumn.Name;
			}
		}
	}
}

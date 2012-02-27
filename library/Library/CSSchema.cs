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
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using Vici.Core;

namespace Vici.CoolStorage
{
    internal class CSSchema
    {
        private static readonly object _staticLock = new object();

        private static readonly Dictionary<Type, CSSchema> _schemaMap = new Dictionary<Type, CSSchema>();
        private static readonly Dictionary<Type, string> _typeToTableMap = new Dictionary<Type, string>();
        private static readonly Dictionary<Type, string> _typeToContextMap = new Dictionary<Type, string>();

        private static bool _firstSchemaCreated;

        private readonly CSSchemaColumnCollection _columnList = new CSSchemaColumnCollection();
        private readonly CSSchemaFieldCollection _fieldList = new CSSchemaFieldCollection();
        private readonly CSSchemaColumnCollection _keyColumnList = new CSSchemaColumnCollection();
        private readonly CSStringCollection _columnsToRead = new CSStringCollection();
        private CSSchemaColumn _identityColumn;
        private PropertyInfo _toStringProperty;
        private readonly Type _classType;
        private readonly bool _nonAbstract;
        private string _defaultSortExpression;
        private string _tableName;
        private string _context;
        private CSSchemaField _softDeleteField;

        internal Type ClassType
        {
            get { return _classType; }
        }

        internal CSSchemaColumn IdentityColumn
        {
            get { return _identityColumn; }
            set { _identityColumn = value; }
        }

        internal PropertyInfo ToStringProperty
        {
            get { return _toStringProperty; }
        }

        internal CSSchemaColumnCollection KeyColumns
        {
            get { return _keyColumnList; }
        }

        internal CSSchemaColumnCollection Columns
        {
            get { return _columnList; }
        }

        internal CSSchemaFieldCollection Fields
        {
            get { return _fieldList; }
        }

        internal CSStringCollection ColumnsToRead
        {
            get { return _columnsToRead; }
        }

        internal string TableName
        {
            get { return _tableName; }
        }

        internal CSDataProvider DB
        {
            get { return CSConfig.GetDB(_context); }
        }

        internal string DefaultSortExpression
        {
            get { return _defaultSortExpression; }
        }

        internal CSSchemaField SoftDeleteField
        {
            get { return _softDeleteField; }
        }

        internal static void ChangeMapTo(Type type, string tableName, string context)
        {
            lock (_staticLock)
            {
                if (_firstSchemaCreated)
                    throw new CSException("MapTo() override not allowed after application start");

                _typeToTableMap[type] = tableName;
                _typeToContextMap[type] = context ?? CSConfig.DEFAULT_CONTEXTNAME;
            }
        }

        private CSSchema(Type objType)
        {
            lock (_staticLock)
                _firstSchemaCreated = true;

            if (objType.BaseType.IsGenericType && objType.BaseType.BaseType.IsGenericType && objType.BaseType.BaseType.GetGenericTypeDefinition() == typeof(CSObject<>))
            {
                _classType = objType;
                _nonAbstract = true;
            }
            else if (objType.IsAbstract)
                _classType = objType;
            else
                _classType = objType.BaseType;

            CreateContext();

            CreateColumns();

            CreateFields();

            CreateColumnsToRead();
        }

        internal static CSSchema Get(Type objectType)
        {
            if (!objectType.IsSubclassOf(typeof(CSObject)))
                throw new CSException("CSSchema.Get() called with type not derived from CSObject");

            lock (_staticLock)
            {
                CSSchema schema;

                if (!_schemaMap.TryGetValue(objectType, out schema))
                {
                    schema = new CSSchema(objectType);

                    _schemaMap[objectType] = schema;

                    schema.CreateRelations();
                }

                return schema;
            }
        }

        private void CreateContext()
        {
            lock (_staticLock)
            {
                if (_typeToTableMap.ContainsKey(_classType))
                {
                    _tableName = _typeToTableMap[_classType];
                    _context = _typeToContextMap[_classType];
                }
                else
                {
                    MapToAttribute mapToAttribute = ClassType.GetAttribute<MapToAttribute>(true);
                    DefaultSortExpressionAttribute sortAttribute = ClassType.GetAttribute<DefaultSortExpressionAttribute>(true);

                    if (mapToAttribute == null)
                        throw new CSException(string.Format("No MapTo() attribute defined for class {0}", ClassType.Name));

                    _tableName = mapToAttribute.Name;
                    _context = mapToAttribute.Context;

                    if (sortAttribute != null)
                        _defaultSortExpression = sortAttribute.Expression;
                }
            }

            if (_context == null)
                _context = CSConfig.DEFAULT_CONTEXTNAME;
        }

        private void CreateColumns()
        {
            CSSchemaColumn[] columns = DB.GetSchemaColumns(TableName);

            _identityColumn = null;

            _columnList.Clear();
            _keyColumnList.Clear();

            foreach (var column in columns)
            {
                if (column.IsKey)
                    _keyColumnList.Add(column);

                if (column.Identity)
                    _identityColumn = column;

                _columnList.Add(column);
            }
        }

        private void CreateFields()
        {
            PropertyInfo[] propInfoList = ClassType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            bool foundDefaultSort = false;

            foreach (PropertyInfo propInfo in propInfoList)
            {
                if (propInfo.DeclaringType == typeof(CSObject))
                    continue;

                MethodInfo getMethod = propInfo.GetGetMethod();
                MethodInfo setMethod = propInfo.GetSetMethod();

                if ((getMethod == null && setMethod == null) || (getMethod != null && !getMethod.IsAbstract && !_nonAbstract))
                    continue;

                CSSchemaField schemaField = new CSSchemaField(propInfo, this);

                if (schemaField.MappedColumn != null || schemaField.Relation != null)
                    _fieldList.Add(schemaField);

                if (propInfo.IsDefined(typeof(ToStringAttribute), true))
                    _toStringProperty = propInfo;

                if (propInfo.IsDefined(typeof(SoftDeleteAttribute), true))
                    _softDeleteField = schemaField;

                DefaultSortAttribute sortAttribute = (DefaultSortAttribute)Attribute.GetCustomAttribute(propInfo,typeof(DefaultSortAttribute), true);

                if (sortAttribute != null)
                {
                    if (foundDefaultSort)
                        throw new CSException(String.Format("Field [{0}.{1}] : only one field can have the DefaultSort attribute", ClassType.Name, propInfo.Name));

                    if (!string.IsNullOrEmpty(_defaultSortExpression))
                        throw new CSException(String.Format("Field [{0}.{1}] has DefaultSort attribute, but class already has a DefaultSortExpression attribute", ClassType.Name, propInfo.Name));

                    _defaultSortExpression = schemaField.Name;

                    if (sortAttribute.SortDirection == CSSort.Descending)
                        _defaultSortExpression += "-";

                    foundDefaultSort = true;
                }

            }

            foreach (CSSchemaColumn schemaColumn in _columnList)
            {
                if (schemaColumn.MappedField == null)
                    _fieldList.Add(new CSSchemaField(schemaColumn, this));
            }
        }

        private void CreateRelations()
        {
            foreach (CSSchemaField schemaField in _fieldList)
            {
                if (schemaField.Relation == null)
                    continue;

                if (schemaField.Relation.Attribute.LocalKey != null)
                    schemaField.Relation.LocalKey = schemaField.Relation.Attribute.LocalKey;

                if (schemaField.Relation.Attribute.ForeignKey != null)
                    schemaField.Relation.ForeignKey = schemaField.Relation.Attribute.ForeignKey;

                if (schemaField.Relation.Attribute is OneToManyAttribute)
                {
                    schemaField.Relation.RelationType = CSSchemaRelationType.OneToMany;

                    Type collectionType = schemaField.FieldType;

                    while (!collectionType.IsGenericType || collectionType.GetGenericTypeDefinition() != typeof(CSList<>))
                        collectionType = collectionType.BaseType;

                    schemaField.Relation.ForeignType = collectionType.GetGenericArguments()[0];

                    if (schemaField.Relation.LocalKey == null && KeyColumns.Count == 1)
                        schemaField.Relation.LocalKey = KeyColumns[0].Name;

                    if (schemaField.Relation.ForeignKey == null)
                        schemaField.Relation.ForeignKey = schemaField.Relation.LocalKey;

                    if (schemaField.Relation.LocalKey == null)
                        throw new CSException(string.Format("OneToMany relation [{0}] for class [{1}] cannot be created. Local key is not supplied and no single primary key exists", schemaField.Name, ClassType.Name));

                    if (_columnList[schemaField.Relation.LocalKey] == null)
                        throw new CSException(string.Format("OneToMany relation [{0}] for class [{1}] cannot be created. Local key [{2}] not defined in DB", schemaField.Name, ClassType.Name, schemaField.Relation.LocalKey));
                }

                if (schemaField.Relation.Attribute is ManyToOneAttribute)
                {
                    schemaField.Relation.RelationType = CSSchemaRelationType.ManyToOne;

                    schemaField.Relation.ForeignType = schemaField.FieldType;

                    if (schemaField.Relation.ForeignKey == null && Get(schemaField.FieldType).KeyColumns.Count == 1)
                        schemaField.Relation.ForeignKey = Get(schemaField.FieldType).KeyColumns[0].Name;

                    if (schemaField.Relation.LocalKey == null)
                        schemaField.Relation.LocalKey = schemaField.Relation.ForeignKey;

                    if (schemaField.Relation.ForeignKey == null)
                        throw new CSException(string.Format("ManyToOne relation [{0}] for class [{1}] cannot be created. Foreign key is not supplied and related table has no single primary", schemaField.Name, ClassType.Name));

                    if (_columnList[schemaField.Relation.LocalKey] == null)
                        throw new CSException(string.Format("ManyToOne relation [{0}] for class [{1}] cannot be created. Local key [{2}] not defined in DB", schemaField.Name, ClassType.Name, schemaField.Relation.LocalKey));
                }

                if (schemaField.Relation.Attribute is OneToOneAttribute)
                {
                    schemaField.Relation.RelationType = CSSchemaRelationType.ManyToOne;

                    schemaField.Relation.ForeignType = schemaField.FieldType;

                    if (schemaField.Relation.LocalKey == null && schemaField.Relation.ForeignKey == null)
                        throw new CSException(string.Format("LocalKey or ForeignKey is required for OneToOne relation <{0}> in class <{1}>", schemaField.Name, ClassType.Name));

                    if (schemaField.Relation.LocalKey == null)
                        schemaField.Relation.LocalKey = schemaField.Relation.ForeignKey;

                    if (schemaField.Relation.ForeignKey == null)
                        schemaField.Relation.ForeignKey = schemaField.Relation.LocalKey;

                    if (_columnList[schemaField.Relation.LocalKey] == null)
                        throw new CSException(string.Format("OneToOne relation <{0}> for class <{1}> cannot be created. Local key <{2}> not defined in DB", schemaField.Name, ClassType.Name, schemaField.Relation.LocalKey));
                }

                if (schemaField.Relation.Attribute is ManyToManyAttribute)
                {
                    schemaField.Relation.RelationType = CSSchemaRelationType.ManyToMany;

                    Type collectionType = schemaField.FieldType;

                    while (!collectionType.IsGenericType || collectionType.GetGenericTypeDefinition() != typeof(CSList<>))
                        collectionType = collectionType.BaseType;

                    schemaField.Relation.ForeignType = collectionType.GetGenericArguments()[0];

                    schemaField.Relation.LinkTable = ((ManyToManyAttribute)schemaField.Relation.Attribute).LinkTable;
                    schemaField.Relation.ForeignLinkKey = ((ManyToManyAttribute)schemaField.Relation.Attribute).ForeignLinkKey;
                    schemaField.Relation.LocalLinkKey = ((ManyToManyAttribute)schemaField.Relation.Attribute).LocalLinkKey;
                    schemaField.Relation.PureManyToMany = ((ManyToManyAttribute)schemaField.Relation.Attribute).Pure;

                    if (schemaField.Relation.LocalKey == null)
                        schemaField.Relation.LocalKey = KeyColumns[0].Name;

                    if (schemaField.Relation.ForeignKey == null)
                        schemaField.Relation.ForeignKey = schemaField.Relation.ForeignSchema.KeyColumns[0].Name;

                    if (schemaField.Relation.LocalLinkKey == null)
                        schemaField.Relation.LocalLinkKey = schemaField.Relation.LocalKey;

                    if (schemaField.Relation.ForeignLinkKey == null)
                        schemaField.Relation.ForeignLinkKey = schemaField.Relation.ForeignKey;


                    if (_columnList[schemaField.Relation.LocalKey] == null)
                        throw new CSException(string.Format("ManyToMany relation [{0}] for class [{1}] cannot be created. Local key [{2}] not defined in DB", schemaField.Name, ClassType.Name, schemaField.Relation.LocalKey));
                }
            }
        }

        private void CreateColumnsToRead()
        {
            _columnsToRead.Clear();

            foreach (CSSchemaColumn schemaColumn in Columns)
            {
                if (schemaColumn.MappedField != null && schemaColumn.MappedField.Lazy)
                    continue;

                _columnsToRead.Add(schemaColumn.Name);
            }

            if (_columnsToRead.Count < 1)
                throw new CSException(string.Format("No data fields mapped or primary key is lazy for object type <{0}>", ClassType.Name));
        }
    }
}

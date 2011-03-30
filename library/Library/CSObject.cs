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
using System.ComponentModel;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Reflection;

namespace Vici.CoolStorage
{
    internal enum CSObjectDataState
    {
        New, KeysLoaded, Loaded, Modified, Deleted, MarkedForDelete
    }

    internal class PrefetchField
    {
        public CSSchemaField SchemaField;
        public Dictionary<string, string> AliasMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
    }

    [Serializable]
    public abstract class CSObject : IEquatable<CSObject>, ISerializable, INotifyPropertyChanged
    {
        private CSFieldValueCollection _fieldData;

        private CSSchema _schema;

        private CSObjectDataState _dataState;

        public event PropertyChangedEventHandler PropertyChanged;

        internal CSObject()
        {
            Initialize();
        }

        protected void Deserialize(SerializationInfo info, StreamingContext context)
        {
            _dataState = (CSObjectDataState)info.GetValue("DataState", typeof(CSObjectDataState));

            foreach (CSFieldValue fieldValue in _fieldData)
            {
                fieldValue.ValueState = (CSFieldValueState)info.GetValue("FieldState_" + fieldValue.SchemaField.Name, typeof(CSFieldValueState));
                fieldValue.ValueDirect = info.GetValue("FieldValue_" + fieldValue.SchemaField.Name, typeof(object));

                if (fieldValue.SchemaField.Relation != null && (fieldValue.SchemaField.Relation.RelationType == CSSchemaRelationType.OneToMany || fieldValue.SchemaField.Relation.RelationType == CSSchemaRelationType.ManyToMany))
                {
                    if (fieldValue.ValueDirect is CSList)
                    {
                        ((CSList)fieldValue.ValueDirect).Relation = fieldValue.SchemaField.Relation;
                        ((CSList)fieldValue.ValueDirect).RelationObject = this;
                    }
                }
            }
        }

        private void Initialize()
        {
            if (_schema == null)
                _schema = CSSchema.Get(GetType());

            _dataState = CSObjectDataState.New;

            _fieldData = new CSFieldValueCollection(this);
        }

        internal CSSchema Schema
        {
            get
            {
                return _schema;
            }
        }

        internal CSFieldValueCollection Data
        {
            get
            {
                return _fieldData;
            }
        }

        internal CSObjectDataState DataState
        {
            get
            {
                return _dataState;
            }
        }

        internal void FromDataReader(ICSDbReader dataReader, Dictionary<string, string> aliasMap)
        {
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                string aliasName = dataReader.GetName(i);
                string fieldName;

                if (aliasMap == null)
                    fieldName = aliasName;
                else if (!aliasMap.TryGetValue(aliasName, out fieldName))
                    continue;

                CSSchemaColumn schemaColumn = _schema.Columns[fieldName];

                if (schemaColumn == null || schemaColumn.Hidden)
                    continue;

                object columnValue = dataReader[i];

                if (columnValue is DBNull)
                    columnValue = null;

                if (schemaColumn.MappedField.Trim && columnValue is string)
                    columnValue = ((string)columnValue).Trim();

                CSFieldValue fieldValue = _fieldData["#" + schemaColumn.Name];

                fieldValue.ValueDirect = columnValue;
                fieldValue.ValueState = CSFieldValueState.Read;

            }

            _dataState = CSObjectDataState.Loaded;
        }

        internal bool ReadFields(CSStringCollection columnList, CSStringCollection keyList, List<object> valueList)
        {
            List<string> fieldList = new List<string>();
            List<string> aliasList = new List<string>();
            Dictionary<string, string> fieldAliasMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            CSFilter whereClause = new CSFilter();
            CSTable table = new CSTable(_schema);

            CSParameterCollection parameters = new CSParameterCollection();

            foreach (CSSchemaColumn schemaColumn in _schema.Columns)
            {
                if (keyList.Contains(schemaColumn.Name))
                {
                    CSParameter parameter = parameters.Add();

                    parameter.Value = valueList[keyList.IndexOf(schemaColumn.Name)];

                    whereClause = whereClause.And(table.TableAlias + "." + _schema.DB.QuoteField(schemaColumn.Name) + "=" + parameter.Name);

                    _fieldData["#" + schemaColumn.Name].ValueDirect = parameter.Value;
                    _fieldData["#" + schemaColumn.Name].ValueState = CSFieldValueState.Read;
                }
                else if (columnList.Contains(schemaColumn.Name))
                {
                    string alias = CSNameGenerator.NextFieldAlias;

                    fieldList.Add(table.TableAlias + "." + schemaColumn.Name);
                    aliasList.Add(alias);

                    fieldAliasMap[alias] = schemaColumn.Name;
                }
            }


            /** Build query for prefetch of relations **/

            CSJoinList joinList = new CSJoinList();

            List<PrefetchField> prefetchFields = GetPrefetchFieldsOne(table, fieldList, aliasList, joinList, null);

            if (whereClause.Expression.Length == 0)
                return false;

            if (fieldList.Count == 0)
                return true;

            string sqlQuery = _schema.DB.BuildSelectSQL(table.TableName, table.TableAlias, fieldList.ToArray(), aliasList.ToArray(), joinList.BuildJoinExpressions(), whereClause.Expression, null, 1, 1, true, false);

            using (CSTransaction csTransaction = new CSTransaction(_schema))
            {
                using (ICSDbReader dataReader = _schema.DB.CreateReader(sqlQuery, parameters))
                {
                    if (!dataReader.Read())
                        return false;

                    FromDataReader(dataReader, fieldAliasMap);

                    foreach (PrefetchField prefetchField in prefetchFields)
                        ReadRelationToOne(prefetchField.SchemaField, dataReader, prefetchField.AliasMap);
                }

                csTransaction.Commit();
            }

            return true;
        }

        internal static List<PrefetchField> GetPrefetchFieldsOne(CSTable table, List<string> fieldList, List<string> aliasList, CSJoinList joinList, string[] prefetchPaths)
        {
            List<PrefetchField> prefetchFields = new List<PrefetchField>();

            foreach (CSSchemaField schemaField in table.Schema.Fields)
            {
                bool prefetch = schemaField.Prefetch;

                string fieldName = schemaField.Name;

                prefetch |= (prefetchPaths != null && prefetchPaths.Any(s =>
                                                                            {
                                                                                if (s.IndexOf('.') > 0)
                                                                                    s = s.Substring(0,s.IndexOf('.'));

                                                                                return s == fieldName;
                                                                            }));

                if (schemaField.Relation != null && schemaField.Relation.RelationType == CSSchemaRelationType.ManyToOne && prefetch)
                {
                    CSJoin join = new CSJoin(schemaField.Relation, table.TableAlias);

                    joinList.Add(join);

                    PrefetchField prefetchField = new PrefetchField();

                    prefetchField.SchemaField = schemaField;

                    foreach (string columnName in schemaField.Relation.ForeignSchema.ColumnsToRead)
                    {
                        string alias = CSNameGenerator.NextFieldAlias;

                        fieldList.Add(join.RightAlias + "." + columnName);
                        aliasList.Add(alias);

                        prefetchField.AliasMap[alias] = columnName;
                    }

                    prefetchFields.Add(prefetchField);
                }

            }

            return prefetchFields;
        }

        private bool ReadFields(CSStringCollection columnList)
        {
            CSStringCollection keyList = new CSStringCollection();
            List<object> valueList = new List<object>();

            foreach (CSSchemaColumn schemaColumn in _schema.Columns)
            {
                if (schemaColumn.IsKey)
                {
                    keyList.Add(schemaColumn.Name);
                    valueList.Add(_fieldData["#" + schemaColumn.Name].ValueDirect);
                }
            }

            return ReadFields(columnList, keyList, valueList);
        }

        internal bool Read()
        {
            bool ok = ReadFields(_schema.ColumnsToRead);

            if (ok)
                _dataState = CSObjectDataState.Loaded;

            return ok;
        }

        internal bool ReadField(CSSchemaField schemaField)
        {
            if (schemaField.Relation != null)
            {
                if (schemaField.Relation.RelationType == CSSchemaRelationType.OneToMany || schemaField.Relation.RelationType == CSSchemaRelationType.ManyToMany)
                    ReadRelationToMany(schemaField);
                else
                    ReadRelationToOne(schemaField, null, null);

                return true;
            }
            else
            {
                return ReadFields(new CSStringCollection(schemaField.MappedColumn.Name));
            }
        }

        internal void ReadRelationToOne(CSSchemaField schemaField, ICSDbReader dataReader, Dictionary<string, string> aliasMap)
        {
            try
            {
                CSFieldValue fieldLocal = _fieldData["#" + schemaField.Relation.LocalKey];

                if (fieldLocal.Value != null)
                {
                    CSObject relationObject = CSFactory.New(schemaField.FieldType);

                    if (dataReader != null)
                    {
                        relationObject.FromDataReader(dataReader, aliasMap);
                    }
                    else if (!relationObject.ReadUsingUniqueKey(schemaField.Relation.ForeignKey, fieldLocal.Value))
                    {
                        throw new CSException("Relation " + schemaField.Name + " could not be read");
                    }

                    _fieldData[schemaField.Name].ValueDirect = relationObject;
                    _fieldData[schemaField.Name].ValueState = CSFieldValueState.Read;
                }
                else
                {
                    _fieldData[schemaField.Name].ValueDirect = null;
                    _fieldData[schemaField.Name].ValueState = CSFieldValueState.Read;
                }
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        private void ReadRelationToMany(CSSchemaField schemaField)
        {
//            if (_schema.KeyColumns.Count != 1)
//                throw new CSException("...ToMany only supported with single primary key");
//
            try
            {
                CSList relationCollection = (CSList)Activator.CreateInstance(schemaField.FieldType);

                relationCollection.Relation = schemaField.Relation;
                relationCollection.RelationObject = this;

                _fieldData[schemaField.Name].ValueDirect = relationCollection;
                _fieldData[schemaField.Name].ValueState = CSFieldValueState.Read;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Saves the object to the database
        /// </summary>
        /// <returns>true if the object was saved successfully, otherwise false</returns>
        /// <remarks>
        /// The object will be saved together with all child objects which have been changed. If the object was new, it will be created in the database. 
        /// If required, any default data which has been filled in by the database server can be retrieved after the object is saved. This includes the primary key
        /// that has been generated by the database server (for autonumber/identity fields)
        /// </remarks>
        public bool Save()
        {
            bool cancelSave = false;

            Fire_ObjectSaving(ref cancelSave);

            if (cancelSave)
                return false;

            if (_dataState == CSObjectDataState.New)
                Fire_ObjectCreating(ref cancelSave);
            else
                Fire_ObjectUpdating(ref cancelSave);

            CSObjectDataState oldState = _dataState;

            if (cancelSave)
                return false;

            using (CSTransaction csTransaction = new CSTransaction(_schema, IsolationLevel.ReadUncommitted))
            {
                bool saveOk = SaveChildrenBefore();

                if (saveOk)
                {
                    if (_dataState == CSObjectDataState.MarkedForDelete)
                    {
                        saveOk = Delete();
                    }
                    else
                    {
                        if (_dataState == CSObjectDataState.New)
                            saveOk = Create();
                        else
                            saveOk = Write();
                    }
                }

                if (saveOk)
                    saveOk = SaveChildrenAfter();

                if (saveOk)
                    csTransaction.Commit();
                else
                    csTransaction.Rollback();

                if (saveOk)
                {
                    if (oldState == CSObjectDataState.New)
                    {
                        Fire_ObjectCreated();

                        Reload();
                    }
                    else
                    {
                        Fire_ObjectUpdated();
                    }

                    Fire_ObjectSaved();
                }

                return saveOk;
            }
        }

        private bool SaveChildrenBefore()
        {
            foreach (CSFieldValue fieldValue in _fieldData)
            {
                if (fieldValue.SchemaField != null && fieldValue.SchemaField.Relation != null)
                {
                    CSRelation relation = fieldValue.SchemaField.Relation;

                    if (fieldValue.IsDirty)
                    {
                        switch (relation.RelationType)
                        {
                            case CSSchemaRelationType.OneToMany:
                                {
                                    break;
                                }

                            case CSSchemaRelationType.ManyToMany:
                                {
                                    break;
                                }

                            case CSSchemaRelationType.OneToOne:
                                {
                                    goto case CSSchemaRelationType.ManyToOne;
                                }

                            case CSSchemaRelationType.ManyToOne: // Set local keys to correct values
                                {
                                    if (fieldValue.Value == null)
                                    {
                                        if (_fieldData["#" + relation.LocalKey].SchemaField.MappedColumn.AllowNull)
                                            _fieldData["#" + relation.LocalKey].Value = null;
                                        else
                                            throw new CSException("Column [" + fieldValue.SchemaField.MappedColumn.Name + "] cannot be set to null");
                                    }
                                    else
                                    {
                                        CSObject valueObj = (CSObject)fieldValue.Value;

                                        if (valueObj.IsNew || valueObj.IsDirty)
                                            valueObj.Save();

                                        _fieldData["#" + relation.LocalKey].Value = valueObj.Data["#" + relation.ForeignKey].Value;
                                    }

                                    break;
                                }
                        }
                    }
                    else
                    {
                        if (fieldValue.ValueState != CSFieldValueState.Unread)
                        {
                            if (fieldValue.BaseType.IsSubclassOf(typeof(CSObject)))
                            {
                                CSObject obj = (CSObject)fieldValue.ValueDirect;

                                if (obj != null && !obj.Save())
                                    return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool SaveChildrenAfter()
        {
            foreach (CSFieldValue fieldValue in _fieldData)
            {
                if (fieldValue.SchemaField != null && fieldValue.SchemaField.Relation != null)
                {
                    CSRelation relation = fieldValue.SchemaField.Relation;

                    if (relation.RelationType == CSSchemaRelationType.OneToMany || (relation.RelationType == CSSchemaRelationType.ManyToMany && relation.PureManyToMany))
                    {
                        ((CSList)fieldValue.Value).Save();
                    }
                }
            }

            return true;
        }

        private bool Write()
        {
            if (!_fieldData.IsDirty)
                return true;

            if (_dataState == CSObjectDataState.Deleted)
                return false;

            List<string> fieldNames = new List<string>();
            List<string> fieldValues = new List<string>();

            CSFilter whereClause = new CSFilter();

            CSParameterCollection parameters = new CSParameterCollection();

            foreach (CSSchemaColumn schemaColumn in _schema.Columns)
            {
                CSFieldValue fieldValue = _fieldData["#" + schemaColumn.Name];

                if (!schemaColumn.IsKey && (fieldValue == null || !fieldValue.IsDirty || fieldValue.SchemaField.ReadOnly))
                    continue;

                fieldValue.ValueState = CSFieldValueState.Read;

                CSParameter parameter = parameters.Add();

                parameter.Value = fieldValue.ValueDirect;

                if (schemaColumn.IsKey)
                {
                    whereClause = whereClause.And(_schema.DB.QuoteField(schemaColumn.Name) + "=@" + parameter.Name.Substring(1));
                }
                else
                {
                    fieldNames.Add(schemaColumn.Name);
                    fieldValues.Add("@" + parameter.Name.Substring(1));
                }
            }

            if (whereClause.Expression.Length == 0)
                throw new CSException("No key fields");

            if (fieldValues.Count > 0)
            {
                string sqlQuery = _schema.DB.BuildUpdateSQL(_schema.TableName, fieldNames.ToArray(), fieldValues.ToArray(), whereClause.Expression);

                if (_schema.DB.ExecuteNonQuery(sqlQuery, parameters) != 1)
                    return false;
            }

            return true;
        }

        private bool Create()
        {
            CSParameterCollection parameters = new CSParameterCollection();

            List<string> fieldNames = new List<string>();
            List<string> fieldValues = new List<string>();
            List<string> sequenceNames = new List<string>();

            foreach (CSSchemaColumn schemaColumn in _schema.Columns)
            {
                CSFieldValue fieldValue = _fieldData["#" + schemaColumn.Name];

                if (fieldValue == null)
                    continue;

                CSSchemaField schemaField = fieldValue.SchemaField;

                if (schemaField.ClientGenerated && schemaColumn.IsKey)
                {
                    if (schemaField.FieldType == typeof(Guid))
                    {
                        fieldValue.Value = Guid.NewGuid();
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(schemaField.SequenceName) && (!fieldValue.IsDirty || schemaField.ReadOnly || schemaField.NoCreate || schemaColumn.Identity))
                        continue;
                }

                if (schemaField.ServerGenerated && schemaColumn.IsKey)
                    continue;

                if (!string.IsNullOrEmpty(schemaField.SequenceName))
                {
                    sequenceNames.Add(schemaField.SequenceName);
                    fieldValues.Add(null);
                }
                else
                {
                    CSParameter parameter = parameters.Add();

                    parameter.Value = fieldValue.ValueDirect;

                    fieldValues.Add("@" + parameter.Name.Substring(1));
                    sequenceNames.Add(null);
                }

                fieldNames.Add(schemaColumn.Name);
            }

            string[] primaryKeys = new string[_schema.KeyColumns.Count];

            for (int i = 0; i < _schema.KeyColumns.Count; i++)
                primaryKeys[i] = _schema.KeyColumns[i].Name;


            using (ICSDbReader reader = _schema.DB.ExecuteInsert(_schema.TableName, fieldNames.ToArray(), fieldValues.ToArray(), primaryKeys, sequenceNames.ToArray(), (_schema.IdentityColumn != null && _schema.IdentityColumn.MappedField != null) ? _schema.IdentityColumn.Name : null, parameters))
            {
                if (reader != null && !reader.IsClosed && reader.Read())
                {
                    FromDataReader(reader, null);
                }
            }

            _dataState = CSObjectDataState.Loaded;

            return true;
        }


        /// <summary>
        /// Reloads the object from the database, overwriting any changes you have made to the object
        /// </summary>
        public void Reload()
        {
            Fire_ObjectReading();

            foreach (CSFieldValue fieldValue in _fieldData)
                if (fieldValue.SchemaField.MappedColumn == null || !fieldValue.SchemaField.MappedColumn.IsKey)
                    fieldValue.Reset();

            _dataState = CSObjectDataState.KeysLoaded;
        }

        internal object PrimaryKeyValue
        {
            get
            {
                if (_schema.KeyColumns.Count != 1)
                {
                    return null;
                }
                else
                {
                    return _fieldData["#" + _schema.KeyColumns[0].Name].ValueDirect;
                }
            }
        }

        internal bool Read(object[] primaryKeys)
        {
            if (primaryKeys.Length == 0)
                throw new CSException("Read() requires parameters");

            Initialize();

            CSStringCollection columnNames = new CSStringCollection();
            List<object> keyValues = new List<object>();

            if (primaryKeys.Length != _schema.KeyColumns.Count)
                throw new CSException(GetType().Name + ".Read(..keys..) called with " + primaryKeys.Length + " parameters, but there are " + _schema.KeyColumns.Count + "  key fields defined");

            for (int i = 0; i < primaryKeys.Length; i++)
            {
                columnNames.Add(_schema.KeyColumns[i].Name);
                keyValues.Add(primaryKeys[i]);
            }

            if (!ReadFields(_schema.ColumnsToRead, columnNames, keyValues))
                return false;

            _dataState = CSObjectDataState.Loaded;

            Fire_ObjectRead();

            return true;
        }

        protected bool ReadUsingUniqueKey(string uniqueColumnName, object value)
        {
            Initialize();

            Fire_ObjectReading();

            CSStringCollection keyList = new CSStringCollection(uniqueColumnName);
            List<object> valueList = new List<object>();

            valueList.Add(value);

            if (ReadFields(_schema.ColumnsToRead, keyList, valueList))
            {
                _dataState = CSObjectDataState.Loaded;

                Fire_ObjectRead();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Physically deletes the object from the database
        /// </summary>
        /// <returns><c>true</c> if the object was deleted successfully. Otherwise <c>false</c></returns>
        public bool Delete()
        {
            if (_dataState == CSObjectDataState.Deleted || _dataState == CSObjectDataState.New)
                return false;

            bool cancel = false;

            Fire_ObjectDeleting(ref cancel);

            if (cancel)
                return false;

            StringBuilder whereClause = new StringBuilder();

            CSParameterCollection parameters = new CSParameterCollection();

            foreach (CSSchemaColumn schemaColumn in _schema.Columns)
            {
                if (!schemaColumn.IsKey)
                    continue;

                CSParameter parameter = parameters.Add();

                parameter.Value = _fieldData["#" + schemaColumn.Name].ValueDirect;

                if (whereClause.Length > 0)
                    whereClause.Append(" and ");

                whereClause.Append(_schema.DB.QuoteField(schemaColumn.Name) + "=@" + parameter.Name.Substring(1));
            }

            if (whereClause.Length == 0)
                throw new CSException("No key fields");

            using (CSTransaction csTransaction = new CSTransaction(_schema))
            {
                string deleteSql = _schema.DB.BuildDeleteSQL(_schema.TableName, null, whereClause.ToString());

                int numDeleted = _schema.DB.ExecuteNonQuery(deleteSql, parameters);

                if (numDeleted == 1)
                    csTransaction.Commit();
                else
                    return false;
            }

            _dataState = CSObjectDataState.Deleted;

            Fire_ObjectDeleted();

            return true;
        }

        /// <summary>
        /// Mark the object for deletion. The object will be deleted next time it is saved
        /// </summary>
        public void MarkForDelete()
        {
            _dataState = CSObjectDataState.MarkedForDelete;
        }

        public override string ToString()
        {
            if (_schema.ToStringProperty != null)
                return _schema.ToStringProperty.GetValue(this, null).ToString();
            else
                return GetType().Name;
        }

        internal abstract void Fire_ObjectRead();
        internal abstract void Fire_ObjectReading();
        internal abstract void Fire_ObjectUpdating(ref bool cancel);
        internal abstract void Fire_ObjectUpdated();
        internal abstract void Fire_ObjectSaving(ref bool cancel);
        internal abstract void Fire_ObjectSaved();
        internal abstract void Fire_ObjectCreated();
        internal abstract void Fire_ObjectCreating(ref bool cancel);
        internal abstract void Fire_ObjectDeleting(ref bool cancel);
        internal abstract void Fire_ObjectDeleted();

        /// <summary>
        /// Gets a value indicating whether this object is new (not saved yet)
        /// </summary>
        /// <value><c>true</c> if this instance is new (not saved yet); otherwise, <c>false</c>.</value>
        public bool IsNew
        {
            get
            {
                return _dataState == CSObjectDataState.New;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object is deleted by a previous call to Delete()
        /// </summary>
        /// <value><c>true</c> if this instance is deleted; otherwise, <c>false</c>.</value>
        public bool IsDeleted
        {
            get
            {
                return _dataState == CSObjectDataState.Deleted;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object is marked for deletion by a previous call to MarkForDelete()
        /// </summary>
        /// <value><c>true</c> if this instance is marked for deletion; otherwise, <c>false</c>.</value>
        public bool IsMarkedForDelete
        {
            get
            {
                return _dataState == CSObjectDataState.MarkedForDelete;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object has been modified
        /// </summary>
        /// <value><c>true</c> if this object has been modified; otherwise, <c>false</c>.</value>
        public bool IsDirty
        {
            get
            {
                return _dataState == CSObjectDataState.Modified || _dataState == CSObjectDataState.MarkedForDelete || _fieldData.IsDirty;
            }
        }

        public bool IsPropertyDirty(string propertyName)
        {
            var value =_fieldData[propertyName];

            return (value != null) && (value.IsDirty);
        }


        //        /// <summary>
        //        /// Gets or sets the specified field (by field name)
        //        /// </summary>
        //        /// <value>The field value</value>
        //		public object this[string fieldName]
        //		{
        //			get
        //			{
        //				int dotIndex = fieldName.IndexOf('.');
        //				string rootField = fieldName;
        //
        //				if (dotIndex > 0)
        //					rootField = fieldName.Substring(0,dotIndex);
        //
        //				object value = GetField(rootField);
        //
        //				if (dotIndex > 0 && value is CSObject)
        //					return (value as CSObject)[fieldName.Substring(dotIndex+1)];
        //				else
        //					return value;
        //			}
        //			set
        //			{
        //				SetField(fieldName,value);
        //			}
        //		}

        #region Protected Field Reading Methods

        protected object GetField(string fieldName)
        {
            CSFieldValue fieldValue = _fieldData[fieldName];

            if (fieldValue == null)
                throw new CSException("Type [" + GetType().Name + "] does not contain property [" + fieldName + "]");

            object value = fieldValue.Value;

            if (value == null)
                return fieldValue.SchemaField.NullValue;
            else
                return value;
        }

//        protected T GetField<T>(string fieldName)
//        {
//            return (T) GetField(fieldName);
//        }

        protected void SetField(string fieldName, object value)
        {
            CSFieldValue fieldValue = _fieldData[fieldName];

            if (fieldValue == null)
                throw new CSException("Type [" + GetType().Name + "] does not contain property [" + fieldName + "]");

            PropertyChangedEventArgs e = new PropertyChangedEventArgs(fieldName);

            fieldValue.Value = value;

            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        protected void _S(string fieldName, object value)
        {
            SetField(fieldName,value);
        }

        protected T _G<T>(string fieldName)
        {
            return (T) GetField(fieldName);
        }

        #endregion

        #region Equality tests

        public override bool Equals(object obj)
        {
            if (obj is CSObject && obj.GetType() == GetType())
            {
                CSObject objData = (CSObject)obj;

                foreach (CSSchemaColumn schemaColumn in _schema.KeyColumns)
                {
                    object value1 = _fieldData["#" + schemaColumn.Name].Value;
                    object value2 = objData._fieldData["#" + schemaColumn.Name].Value;

                    if (value1 == null || value2 == null || !value1.Equals(value2))
                        return false;
                }

                return true;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int iHash = 0;

            if (IsNew)
                return GetType().GetHashCode();

            foreach (CSSchemaColumn schemaColumn in _schema.KeyColumns)
                iHash += _fieldData["#" + schemaColumn.Name].Value.GetHashCode();

            return iHash;
        }

        public static bool operator ==(CSObject x, CSObject y)
        {
            if ((object)x == null && (object)y == null)
                return true;

            if ((object)x == null)
                return y.Equals(x);
            else
                return x.Equals(y);
        }

        public static bool operator !=(CSObject x, CSObject y)
        {
            return !(x == y);
        }

        bool IEquatable<CSObject>.Equals(CSObject other)
        {
            return Equals(other);
        }

        #endregion

#if !WINDOWS_PHONE && !SILVERLIGHT
        #region ISerializable Members

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("DataState", _dataState);

            foreach (CSFieldValue fieldValue in _fieldData)
            {
                //if (fieldValue.SchemaField.Relation != null && (fieldValue.SchemaField.Relation.RelationType == CSSchemaRelationType.OneToMany || fieldValue.SchemaField.Relation.RelationType == CSSchemaRelationType.ManyToMany))
                //    continue;

                info.AddValue("FieldState_" + fieldValue.SchemaField.Name, fieldValue.ValueState);
                info.AddValue("FieldValue_" + fieldValue.SchemaField.Name, fieldValue.ValueDirect);
            }
        }

        #endregion
#endif
    }
}

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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Vici.Core;

namespace Vici.CoolStorage
{
    [Serializable]
    public class CSList<TObjectType> : CSList, ITypedList, IList<TObjectType>, IList, IBindingList, IListSource where TObjectType : CSObject<TObjectType>
    {
        private List<TObjectType> _objectArray;
        private List<TObjectType> _removedObjects; // Only used for pure many-to-many relations
        private List<TObjectType> _addedObjects; // Only used for pure many-to-many relations

        [NonSerialized]
        private Dictionary<object, TObjectType> _objectMap;

        [NonSerialized]
        private Predicate<TObjectType> _filterPredicate;

        [OnDeserializing]
        private void BeforeDeserializing(StreamingContext context)
        {
            Schema = CSSchema.Get(typeof(TObjectType));

            if (!string.IsNullOrEmpty(Schema.DefaultSortExpression))
                OrderBy = Schema.DefaultSortExpression;
        }

        [OnDeserialized]
        private void AfterDeserializing(StreamingContext context)
        {
            if (Schema.KeyColumns.Count == 1)
            {
                string columnName = Schema.KeyColumns[0].Name;

                _objectMap = new Dictionary<object, TObjectType>();

                foreach (TObjectType csObject in _objectArray)
                    _objectMap.Add(csObject.Data["#" + columnName].Value, csObject);
            }
        }

        public CSList()
            : base(CSSchema.Get(typeof(TObjectType)))
        {
        }

        public CSList(string filterExpression)
            : this(new CSFilter(filterExpression))
        {
        }

        public CSList(string filterExpression, object parameters)
            : this(filterExpression, new CSParameterCollection(parameters))
        {
        }

        public CSList(string filterExpression, string paramName, object paramValue)
            : this(filterExpression, new CSParameterCollection(paramName, paramValue))
        {
        }

        public CSList(string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2)
            : this(filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2))
        {
        }

        public CSList(string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
            : this(filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3))
        {
        }

        public CSList(string filterExpression, CSParameterCollection parameters)
            : this(new CSFilter(filterExpression, parameters))
        {
        }

        public CSList(string filterExpression, params CSParameter[] parameters)
            : this(new CSFilter(filterExpression, parameters))
        {
        }

        public CSList(CSFilter filter)
            : this()
        {
            Filter = filter;
        }

        public CSList(CSList<TObjectType> sourceCollection)
            : this()
        {
            OrderBy = sourceCollection.OrderBy;
            MaxRecords = sourceCollection.MaxRecords;
            StartRecord = sourceCollection.StartRecord;

            Filter = sourceCollection.Filter;
            FilterPredicate = sourceCollection.FilterPredicate;
            Relation = sourceCollection.Relation;
            RelationObject = sourceCollection.RelationObject;
            PrefetchPaths = sourceCollection.PrefetchPaths;
        }

        public TObjectType this[int i]
        {
            get
            {
                Populate();

                return _objectArray[i];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public CSList<TObjectType> Where(CSFilter filter)
        {
            return FilteredBy(filter);
        }

        public CSList<TObjectType> Where(string filter)
        {
            return FilteredBy(new CSFilter(filter));
        }

        public CSList<TObjectType> Where(string filter, CSParameterCollection parameters)
        {
            return FilteredBy(new CSFilter(filter, parameters));
        }

        public CSList<TObjectType> Where(string filter, params CSParameter[] parameters)
        {
            return FilteredBy(new CSFilter(filter, parameters));
        }

        public CSList<TObjectType> Where(string filter, object parameters)
        {
            return FilteredBy(new CSFilter(filter, parameters));
        }

        public CSList<TObjectType> Where(string filter, string paramName, object paramValue)
        {
            return FilteredBy(new CSFilter(filter, paramName, paramValue));
        }

        public CSList<TObjectType> Where(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return FilteredBy(new CSFilter(filter, paramName1, paramValue1, paramName2, paramValue2));
        }

        public CSList<TObjectType> Where(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return FilteredBy(new CSFilter(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public CSList<TObjectType> Where(Predicate<TObjectType> predicate)
        {
            return FilteredBy(predicate);
        }

        public CSList<TObjectType> FilteredBy(CSFilter filter)
        {
            CSList<TObjectType> newCollection = Clone();

            newCollection.Filter = Filter.And(filter);

            return newCollection;
        }

        public CSList<TObjectType> FilteredBy(string filter)
        {
            return FilteredBy(new CSFilter(filter));
        }

        public CSList<TObjectType> FilteredBy(string filter, CSParameterCollection parameters)
        {
            return FilteredBy(new CSFilter(filter, parameters));
        }

        public CSList<TObjectType> FilteredBy(string filter, params CSParameter[] parameters)
        {
            return FilteredBy(new CSFilter(filter, parameters));
        }

        public CSList<TObjectType> FilteredBy(string filter, object parameters)
        {
            return FilteredBy(new CSFilter(filter, parameters));
        }

        public CSList<TObjectType> FilteredBy(string filter, string paramName, object paramValue)
        {
            return FilteredBy(new CSFilter(filter, paramName, paramValue));
        }

        public CSList<TObjectType> FilteredBy(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return FilteredBy(new CSFilter(filter, paramName1, paramValue1, paramName2, paramValue2));
        }

        public CSList<TObjectType> FilteredBy(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return FilteredBy(new CSFilter(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public CSList<TObjectType> FilteredBy(Predicate<TObjectType> predicate)
        {
            CSList<TObjectType> newCollection = Clone();

            newCollection.FilterPredicate += predicate;

            return newCollection;
        }

        public CSList<TObjectType> OrderedBy(string orderBy)
        {
            CSList<TObjectType> newCollection = Clone();

            newCollection.OrderBy = orderBy;

            return newCollection;
        }

        public CSList<TObjectType> ThenBy(string orderBy)
        {
            if (string.IsNullOrEmpty(OrderBy))
                throw new CSException(".ThenBy() called without .OrderedBy()");

            CSList<TObjectType> newCollection = Clone();

            newCollection.OrderBy += "," + orderBy;

            return newCollection;
        }

        public CSList<TObjectType> Range(int from, int numRecords)
        {
            CSList<TObjectType> newCollection = Clone();

            newCollection.MaxRecords = numRecords;
            newCollection.StartRecord = from;

            return newCollection;
        }

        public CSList<TObjectType> LimitTo(int numRecords)
        {
            CSList<TObjectType> newCollection = Clone();

            newCollection.MaxRecords = numRecords;

            return newCollection;
        }

        public CSList<TObjectType> WithPrefetch(params string[] prefetchPaths)
        {
            CSList<TObjectType> newCollection = Clone();

            newCollection.PrefetchPaths = prefetchPaths;

            return newCollection;
        }

        private CSList<TObjectType> Clone()
        {
            CSList<TObjectType> newCollection = (CSList<TObjectType>)Activator.CreateInstance(GetType());

            newCollection.OrderBy = OrderBy;
            newCollection.MaxRecords = MaxRecords;
            newCollection.StartRecord = StartRecord;
            newCollection.FilterPredicate = FilterPredicate;
            newCollection.Filter = Filter;
            newCollection.Relation = Relation;
            newCollection.RelationObject = RelationObject;
            newCollection.PrefetchPaths = newCollection.PrefetchPaths;

            return newCollection;
        }

        protected TObjectType GetByKey(object key)
        {
            Populate();

            if (_objectMap != null && _objectMap.ContainsKey(key))
                return _objectMap[key];

            return null;
        }

        public override int Count
        {
            get
            {
                Populate();

                return _objectArray.Count;
            }
        }

        public override int CountFast
        {
            get
            {
                if (Populated)
                    return _objectArray.Count;
                
                return GetScalar("*", CSAggregate.Count).Convert<int>();
            }
        }

        public override void Refresh()
        {
            Populated = false;
            _objectArray = null;
            _objectMap = null;

#if !WINDOWS_PHONE && !SILVERLIGHT

            if (ListChanged != null)
                ListChanged(this, new ListChangedEventArgs(ListChangedType.Reset, -1));

#endif            
            }

        public override bool Save()
        {
            if (!Populated)
                return true;

            using (CSTransaction csTransaction = new CSTransaction(Schema, IsolationLevel.ReadUncommitted))
            {
                UpdateForeignKeys();

                foreach (TObjectType obj in _objectArray.ToArray())
                {
                    if (obj.IsDirty)
                        if (!obj.Save())
                            return false;
                }

                if (Relation != null && Relation.PureManyToMany)
                {
                    if (_removedObjects != null)
                    {
                        foreach (TObjectType obj in _removedObjects)
                        {
                            CSParameterCollection parameters = new CSParameterCollection();

                            parameters.Add("@LocalKey").Value = RelationObject.Data["#" + Relation.LocalKey].Value;
                            parameters.Add("@ForeignKey").Value = obj.Data["#" + Relation.ForeignKey].Value;

                            string deleteSql = DB.BuildDeleteSQL(Relation.LinkTable, null, DB.QuoteField(Relation.LocalLinkKey) + "=@LocalKey and " + DB.QuoteField(Relation.ForeignLinkKey) + "=@ForeignKey");

                            DB.ExecuteNonQuery(deleteSql, parameters);
                        }

                        _removedObjects = null;
                    }

                    if (_addedObjects != null)
                    {
                        foreach (TObjectType obj in _addedObjects)
                        {
                            CSParameterCollection parameters = new CSParameterCollection();

                            parameters.Add("@LocalKey").Value = RelationObject.Data["#" + Relation.LocalKey].Value;
                            parameters.Add("@ForeignKey").Value = obj.Data["#" + Relation.ForeignKey].Value;

                            DB.ExecuteInsert(Relation.LinkTable,
                                                new[] { Relation.LocalLinkKey, Relation.ForeignLinkKey },
                                                new[] { "@LocalKey", "@ForeignKey" },
                                                null, null, null, parameters);

//                            string insertSql =
//                                DB.BuildInsertSQL(Relation.LinkTable, 
//                                                new[] { Relation.LocalLinkKey, Relation.ForeignLinkKey },
//                                                new[] { "@LocalKey", "@ForeignKey" }, 
//                                                null, null, null);

                            //DB.ExecuteNonQuery(insertSql, parameters);
                        }
                    }
                }

                csTransaction.Commit();

                return true;
            }
        }

        public TObjectType UniqueItem
        {
            get
            {
                Populate();

                if (Count > 1)
                    throw new CSException("UniqueItem expects 0 or 1 items in list");
                
                if (Count == 1)
                    return _objectArray[0];

                return null;
            }
        }

        public TObjectType FirstItem
        {
            get
            {
                Populate();

                return Count == 0 ? null : _objectArray[0];
            }
        }

        public object GetScalar(string fieldName, CSAggregate aggregate)
        {
            return GetScalar(fieldName, aggregate, null, null);
        }

        public object GetScalar(string fieldName, string orderBy)
        {
            return GetScalar(fieldName, orderBy, null, null);
        }

        public object GetScalar(string fieldName, string orderBy, string filterExpression)
        {
            return GetScalar(fieldName, orderBy, filterExpression, null);
        }

        public object GetScalar(string fieldName, string orderBy, string filterExpression, string paramName, object paramValue)
        {
            return GetScalar(fieldName, orderBy, filterExpression, new CSParameterCollection(paramName, paramValue));
        }

        public object GetScalar(string fieldName, string orderBy, string filterExpression, CSParameterCollection filterParameters)
        {
            string tableAlias = CSNameGenerator.NextTableAlias;

            CSFilter queryFilter = Filter.And(BuildRelationFilter(tableAlias));

            if (!string.IsNullOrEmpty(filterExpression))
            {
                queryFilter = queryFilter.And(filterExpression, filterParameters);
            }

            return CSObject<TObjectType>.GetScalar(fieldName, tableAlias, orderBy, queryFilter);
        }

        public object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression, CSParameterCollection filterParameters)
        {
            string tableAlias = CSNameGenerator.NextTableAlias;

            CSFilter queryFilter = Filter.And(BuildRelationFilter(tableAlias));

            if (!string.IsNullOrEmpty(filterExpression))
            {
                queryFilter = queryFilter.And(filterExpression, filterParameters);
            }

            return CSObject<TObjectType>.GetScalar(fieldName, tableAlias, aggregate, queryFilter);
        }

        public object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression)
        {
            return GetScalar(fieldName, aggregate, filterExpression, null);
        }

        public object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression, object parameters)
        {
            return GetScalar(fieldName, aggregate, filterExpression, new CSParameterCollection(parameters));
        }

        public object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression, string paramName, object paramValue)
        {
            return GetScalar(fieldName, aggregate, filterExpression, new CSParameterCollection(paramName, paramValue));
        }

        public bool DeleteAll()
        {
            Populate();

            List<TObjectType> toDeleteList = new List<TObjectType>();

            foreach (TObjectType obj in _objectArray)
                toDeleteList.Add(obj);

            foreach (TObjectType obj in toDeleteList)
                if (!obj.Delete())
                    return false;

            return true;
        }

        public bool DeleteAll(Predicate<TObjectType> predicate)
        {
            Populate();

            List<TObjectType> toDeleteList = new List<TObjectType>();

            foreach (TObjectType obj in _objectArray)
                if (predicate(obj))
                    toDeleteList.Add(obj);

            foreach (TObjectType obj in toDeleteList)
                if (!obj.Delete())
                    return false;

            return true;
        }


        public TObjectType AddNew()
        {
            TObjectType obj = CSObject<TObjectType>.New();

            Add(obj);

            return obj;
        }

        public void Add(TObjectType obj)
        {
            Populate();

            if (Relation != null && (Relation.RelationType == CSSchemaRelationType.ManyToMany && !Relation.PureManyToMany))
                throw new NotSupportedException("CSList.Add() not supported for non-pure Many-To-Many relations");

            if (Relation != null && Relation.PureManyToMany)
            {
                if (_addedObjects == null)
                    _addedObjects = new List<TObjectType>();

                _addedObjects.Add(obj);
            }

            obj.ObjectDeleted += OnObjectDeleted;

            _objectArray.Add(obj);

#if !WINDOWS_PHONE && !SILVERLIGHT

            if (ListChanged != null)
                ListChanged(this, new ListChangedEventArgs(ListChangedType.ItemAdded, _objectArray.Count - 1));

#endif
        }

        public void AddRange(IEnumerable<TObjectType> range)
        {
            foreach (TObjectType obj in range)
                Add(obj);
        }

        public void Remove(TObjectType value)
        {
            Populate();

            int idx = _objectArray.IndexOf(value);

            if (idx >= 0)
                RemoveAt(idx);
        }

        private CSDataProvider DB
        {
            get
            {
                return Schema.DB;
            }
        }

        public void ForEach(Action<TObjectType> action)
        {
            Populate();

            foreach (TObjectType obj in _objectArray)
                action(obj);
        }

        public TObjectType Find(TObjectType obj)
        {
            int idx = IndexOf(obj);

            return idx >= 0 ? this[idx] : null;
        }

        public TObjectType Find(Predicate<TObjectType> predicate)
        {
            Populate();

            return _objectArray.FirstOrDefault(o => predicate(o));
        }

        public bool Contains(Predicate<TObjectType> predicate)
        {
            return Find(predicate) != null;
        }

        private void Populate()
        {
            if (Populated)
                return;

            if (Relation != null && RelationObject != null && Relation.RelationType == CSSchemaRelationType.OneToMany && RelationObject.IsNew)
            {
                _objectArray = new List<TObjectType>();
                Populated = true;

                return;
            }

            CSTable table = new CSTable(Schema);

            //string mainAlias = CSHelper.NextTableAlias;

            List<string> columnList = new List<string>(Schema.ColumnsToRead.Count);
            List<string> aliasList = new List<string>(Schema.ColumnsToRead.Count);
            Dictionary<string, string> aliasMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string columnName in Schema.ColumnsToRead)
            {
                string alias = CSNameGenerator.NextFieldAlias;

                columnList.Add(table.TableAlias + "." + columnName);
                aliasList.Add(alias);
                aliasMap.Add(alias, columnName);
            }

            CSJoinList filterJoins = new CSJoinList();

            List<PrefetchField> prefetchFields = CSObject.GetPrefetchFieldsOne(table, columnList, aliasList, filterJoins, PrefetchPaths);

            CSFilter whereFilter;

            if (PrefetchFilter != null)
            {
                whereFilter = new CSFilter(DB.QuoteField(table.TableAlias + "." + PrefetchFilter.ForeignKey) + " in (" + PrefetchFilter.InStatement + ")", PrefetchFilter.Parameters);
            }
            else
            {
                string parsedFilterExpression = CSExpressionParser.ParseFilter(Filter.Expression, Schema, table.TableAlias, filterJoins);

                whereFilter = new CSFilter(parsedFilterExpression, Filter.Parameters);

                CSFilter relationFilter = BuildRelationFilter(table.TableAlias);

                whereFilter = whereFilter.And(CSExpressionParser.ParseFilter(relationFilter.Expression, Schema, table.TableAlias, filterJoins), relationFilter.Parameters);
            }

            string parsedOrderBy = CSExpressionParser.ParseOrderBy(OrderBy, Schema, table.TableAlias, filterJoins);

            string sqlQuery = DB.BuildSelectSQL(table.TableName, table.TableAlias, columnList.ToArray(), aliasList.ToArray(), filterJoins.BuildJoinExpressions(), whereFilter.Expression, parsedOrderBy, StartRecord, MaxRecords, true, false);

            _objectArray = GetObjects(sqlQuery, whereFilter.Parameters, aliasMap, prefetchFields);

            if (Schema.KeyColumns.Count == 1)
            {
                string columnName = Schema.KeyColumns[0].Name;

                _objectMap = new Dictionary<object, TObjectType>();

                foreach (TObjectType csObject in _objectArray)
                    _objectMap.Add(csObject.Data["#" + columnName].Value, csObject);
            }

            foreach (CSSchemaField prefetchField in GetPrefetchFieldsMany())
            {
                CSRelation relation = prefetchField.Relation;

                Dictionary<object, TObjectType> prefetchMap = new Dictionary<object, TObjectType>();

                // Creates empty lists in each object of this list
                foreach (TObjectType csObject in _objectArray)
                {
                    prefetchMap[csObject.Data["#" + relation.LocalKey].Value] = csObject;

                    CSList relationCollection = (CSList)Activator.CreateInstance(prefetchField.FieldType);

                    relationCollection.Relation = relation;
                    relationCollection.RelationObject = csObject;

                    relationCollection.InitializePrefetch();

                    csObject.Data[prefetchField.Name].ValueDirect = relationCollection;
                    csObject.Data[prefetchField.Name].ValueState = CSFieldValueState.Read;
                }

                Type objectType = relation.ForeignSchema.ClassType;

                CSList csList = (CSList)Activator.CreateInstance(typeof(CSList<>).MakeGenericType(objectType));

                //string prefetchTableAlias = CSNameGenerator.NextTableAlias;

                string prefetchFilter = DB.BuildSelectSQL(table.TableName, table.TableAlias, new[] { table.TableAlias + "." + relation.LocalKey }, new[] { CSNameGenerator.NextFieldAlias }, filterJoins.BuildJoinExpressions(), whereFilter.Expression, parsedOrderBy, StartRecord, MaxRecords, true, true);

                csList.PrefetchFilter = new PrefetchFilter(relation.ForeignKey, prefetchFilter, whereFilter.Parameters);

                if (PrefetchPaths != null && PrefetchPaths.Length > 0)
                {
                    List<string> newPrefetchPaths = new List<string>();

                    foreach (string path in PrefetchPaths)
                    {
                        if (path.StartsWith(prefetchField.Name + "."))
                        {
                            newPrefetchPaths.Add(path.Substring(prefetchField.Name.Length + 1));
                        }
                    }

                    if (newPrefetchPaths.Count > 0)
                        csList.PrefetchPaths = newPrefetchPaths.ToArray();
                }

                foreach (CSObject csObject in csList)
                {
                    object localKey = csObject.Data["#" + relation.ForeignKey].ValueDirect;

                    CSList relationCollection = (CSList)prefetchMap[localKey].Data[prefetchField.Name].ValueDirect;

                    relationCollection.AddFromPrefetch(csObject);
                }
            }


            Populated = true;
        }

        internal override void AddFromPrefetch(CSObject csObject)
        {
            csObject.Fire_ObjectReading();

            ((TObjectType)csObject).ObjectDeleted += OnObjectDeleted;

            _objectArray.Add((TObjectType)csObject);

            csObject.Fire_ObjectRead();
        }

        internal override void InitializePrefetch()
        {
            Populated = true;

            _objectArray = new List<TObjectType>();
        }

        private List<TObjectType> GetObjects(string sqlQuery, CSParameterCollection parameters, Dictionary<string, string> aliasMap, IEnumerable<PrefetchField> prefetchFields)
        {
            using (CSTransaction csTransaction = new CSTransaction(Schema))
            {
                List<TObjectType> objectList = new List<TObjectType>();

                using (ICSDbReader reader = DB.CreateReader(sqlQuery, parameters))
                {
                    while (reader.Read())
                    {
                        TObjectType csObject = CSObject<TObjectType>.New();

                        csObject.Fire_ObjectReading();

                        csObject.FromDataReader(reader, aliasMap);

                        foreach (PrefetchField prefetchField in prefetchFields)
                            csObject.ReadRelationToOne(prefetchField.SchemaField, reader, prefetchField.AliasMap);

                        if (FilterPredicate != null)
                        {
                            bool shouldAdd = true;

                            foreach (Predicate<TObjectType> predicate in FilterPredicate.GetInvocationList())
                                if (!predicate(csObject))
                                {
                                    shouldAdd = false;
                                    break;
                                }

                            if (!shouldAdd)
                                continue;
                        }

                        csObject.ObjectDeleted += OnObjectDeleted;

                        objectList.Add(csObject);

                        csObject.Fire_ObjectRead();
                    }
                }

                csTransaction.Commit();

                return objectList;
            }
        }

        internal override void UpdateForeignKeys()
        {
            if (!Populated || Relation == null)
                return;

            foreach (TObjectType obj in _objectArray)
            {
                CSFieldValue parentValue = RelationObject.Data["#" + Relation.LocalKey];
                CSFieldValue thisValue = obj.Data["#" + Relation.ForeignKey];

                if (obj.IsNew || thisValue.Value == null || !thisValue.Value.Equals(parentValue.Value))
                {
                    if (Relation.RelationType == CSSchemaRelationType.OneToMany)
                        thisValue.Value = parentValue.Value;
                }
            }
        }

        private void OnObjectDeleted(TObjectType sender, EventArgs e)
        {
            int idx = _objectArray.IndexOf(sender);

            if (idx >= 0)
                _objectArray.RemoveAt(idx);
        }

        public TObjectType[] ToArray()
        {
            Populate();

            return _objectArray.ToArray();
        }

        public T[] ToArray<T>(Converter<TObjectType, T> converter)
        {
            Populate();

            T[] array = new T[Count];

            for (int i = 0; i < Count; i++)
            {
                array[i] = converter(_objectArray[i]);
            }

            return array;
        }

        public List<TObjectType> ToList()
        {
            Populate();

            return new List<TObjectType>(_objectArray);
        }

        public List<T> ToList<T>(Converter<TObjectType, T> converter)
        {
            Populate();

            List<T> array = new List<T>(Count);

            for (int i = 0; i < Count; i++)
                array.Add(converter(_objectArray[i]));

            return array;
        }


        #region IList Members

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        object IList.this[int index]
        {
            get
            {
                Populate();

                return _objectArray[index];
            }
            set
            {
                throw new CSException("Items in CSCollections can not be set");
            }
        }

        public void RemoveAt(int index)
        {
            Populate();

            if (Relation != null && Relation.PureManyToMany)
            {
                if (_removedObjects == null)
                    _removedObjects = new List<TObjectType>();

                _removedObjects.Add(_objectArray[index]);
            }

            _objectArray.RemoveAt(index);

#if !WINDOWS_PHONE && !SILVERLIGHT

            if (ListChanged != null)
                ListChanged(this, new ListChangedEventArgs(ListChangedType.ItemDeleted, index));

#endif            
        }

        public void RemoveAll()
        {
            Populate();

            if (Relation != null && Relation.PureManyToMany)
            {
                _removedObjects = new List<TObjectType>(_objectArray);
            }

            _objectArray.Clear();
        }

        public void RemoveAll(Predicate<TObjectType> predicate)
        {
            Populate();

            if (Relation != null && Relation.PureManyToMany)
            {
                _removedObjects = new List<TObjectType>();

                foreach (TObjectType obj in _objectArray)
                    if (predicate(obj))
                        _removedObjects.Add(obj);

                foreach (TObjectType obj in _removedObjects)
                    _objectArray.Remove(obj);
            }
            else
            {
                _objectArray.RemoveAll(predicate);
            }
        }

        public void Insert(int index, CSObject value)
        {
            throw new NotSupportedException("Insert() not supported for CSList");
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException("Insert() not supported for CSList");
        }

        internal override void Remove(CSObject obj)
        {
            Remove((TObjectType)obj);
        }

        void IList.Remove(object value)
        {
            Remove((TObjectType)value);
        }

        public bool Contains(TObjectType value)
        {
            Populate();

            return _objectArray.Contains(value);
        }

        bool IList.Contains(object value)
        {
            Populate();

            return _objectArray.Contains((TObjectType)value);
        }

        public void Clear()
        {
            throw new NotSupportedException("Clear() not supported for CSList");
        }

        int IList.IndexOf(object value)
        {
            Populate();

            return _objectArray.IndexOf((TObjectType)value);
        }

        public int IndexOf(TObjectType value)
        {
            Populate();

            return _objectArray.IndexOf(value);
        }

        int IList.Add(object value)
        {
            Populate();

            if (value is TObjectType)
                Add((TObjectType)value);
            else
                throw new CSException("Add() only supported for objects of type <" + typeof(TObjectType).Name + ">");

            return _objectArray.Count - 1;
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region ICollection Members

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public void CopyTo(TObjectType[] array, int index)
        {
            Populate();

            _objectArray.CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            Populate();

            _objectArray.CopyTo((TObjectType[])array, index);
        }

        public object SyncRoot
        {
            get
            {
                Populate();

                return _objectArray;
            }
        }

        #endregion

        internal Predicate<TObjectType> FilterPredicate
        {
            get { return _filterPredicate; }
            set { _filterPredicate = value; }
        }


#if !WINDOWS_PHONE && !SILVERLIGHT
        #region ITypedList Members

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            List<PropertyDescriptor> descriptors = new List<PropertyDescriptor>();

            PropertyInfo[] properties = typeof(TObjectType).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (PropertyInfo propertyInfo in properties)
                descriptors.Add(new CSFieldDescriptor(typeof(TObjectType), propertyInfo));

            return new PropertyDescriptorCollection(descriptors.ToArray());
        }

        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return "CSList_" + typeof(TObjectType).Name;
        }

        #endregion

        #region IBindingList Members

        public void AddIndex(PropertyDescriptor property)
        {
        }

        public bool AllowNew
        {
            get
            {
                return true;
            }
        }

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            OrderBy = property.Name;

            if (direction == ListSortDirection.Descending)
                OrderBy += "-";

            Refresh();
        }

        public PropertyDescriptor SortProperty
        {
            get
            {
                string[] arr = OrderBy.Split(' ');

                return arr.Length > 0 ? GetItemProperties(null)[arr[0]] : null;
            }
        }

        public int Find(PropertyDescriptor property, object key)
        {
            throw new NotSupportedException();
        }

        public bool SupportsSorting
        {
            get
            {
                return true;
            }
        }

        public bool IsSorted
        {
            get
            {
                return OrderBy.Length > 0;
            }
        }

        public bool AllowRemove
        {
            get
            {
                return true;
            }
        }

        public bool SupportsSearching
        {
            get
            {
                return false;
            }
        }

        public ListSortDirection SortDirection
        {
            get
            {
                return (OrderBy.ToLower().IndexOf(" desc") > 0) ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
        }


        public event ListChangedEventHandler ListChanged;

        public bool SupportsChangeNotification
        {
            get
            {
                return true;
            }
        }

        public void RemoveSort()
        {
            throw new NotSupportedException();
        }

        object IBindingList.AddNew()
        {
            return AddNew();
        }

        public bool AllowEdit
        {
            get
            {
                return true;
            }
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IListSource Members

        public IList GetList()
        {
            return this;
        }

        public bool ContainsListCollection
        {
            get
            {
                return false;
            }
        }

        #endregion



        #region PropertyDescriptor

        private class CSFieldDescriptor : PropertyDescriptor
        {
            readonly Type _type;
            readonly PropertyInfo _propertyInfo;

            public CSFieldDescriptor(Type type, PropertyInfo propertyInfo)
                : base(propertyInfo.Name, new Attribute[] { })
            {
                _type = type;
                _propertyInfo = propertyInfo;
            }

            public override object GetValue(object component)
            {
                if (!component.GetType().IsSubclassOf(_type))
                    return null;

                return _propertyInfo.GetValue(component, null);
            }

            public override void SetValue(object component, object value)
            {
                if (!component.GetType().IsSubclassOf(_type))
                    return;

                _propertyInfo.SetValue(component, value, null);
            }

            public override void ResetValue(object component)
            {
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override Type PropertyType
            {
                get
                {
                    return _propertyInfo.PropertyType;
                }
            }

            public override Type ComponentType
            {
                get
                {
                    return _type;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return !_propertyInfo.CanWrite;
                }
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }

        #endregion

#endif
        public event EventHandler Disposed;

        public void Dispose()
        {
            if (_objectArray != null)
                _objectArray.Clear();

            _objectArray = null;

            if (Disposed != null)
                Disposed(this, EventArgs.Empty);
        }


        void IList<TObjectType>.Insert(int index, TObjectType item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<TObjectType>.Remove(TObjectType item)
        {
            Remove(item);

            return true;
        }

        public IEnumerator<TObjectType> GetEnumerator()
        {
            Populate();

            return _objectArray.GetEnumerator();
        }

//        public IEnumerator GetEnumerator()
//        {
//            Populate();
//
//            return _objectArray.GetEnumerator();
//        }

        protected override IEnumerator GetTypedEnumerator()
        {
            return GetEnumerator();
        }
    }

    [Serializable]
    public class CSList<TObject, TKey> : CSList<TObject> where TObject : CSObject<TObject>
    {
        public CSList()
        {
        }

        public CSList(string filterExpression)
            : base(new CSFilter(filterExpression))
        {
        }

        public CSList(string filterExpression, string paramName, object paramValue)
            : base(filterExpression, new CSParameterCollection(paramName, paramValue))
        {
        }

        public CSList(string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2)
            : base(filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2))
        {
        }

        public CSList(string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
            : base(filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3))
        {
        }

        public CSList(string filterExpression, CSParameterCollection parameters)
            : base(new CSFilter(filterExpression, parameters))
        {
        }

        public CSList(string filterExpression, params CSParameter[] parameters)
            : base(new CSFilter(filterExpression, parameters))
        {
        }

        public CSList(string filterExpression, object parameters)
            : base(new CSFilter(filterExpression, parameters))
        {
        }

        public CSList(CSFilter filter)
            : base(filter)
        {
        }

        public CSList(CSList<TObject> sourceCollection)
            : base(sourceCollection)
        {
        }

        public TObject GetByKey(TKey key)
        {
            return base.GetByKey(key);
        }

        public new CSList<TObject, TKey> FilteredBy(string filter)
        {
            return (CSList<TObject, TKey>)base.FilteredBy(filter);
        }

        public new CSList<TObject, TKey> FilteredBy(string filter, string paramName, object paramValue)
        {
            return (CSList<TObject, TKey>)base.FilteredBy(filter, paramName, paramValue);
        }

        public new CSList<TObject, TKey> FilteredBy(string filter, object parameters)
        {
            return (CSList<TObject, TKey>)base.FilteredBy(filter, parameters);
        }

        public new CSList<TObject, TKey> FilteredBy(Predicate<TObject> predicate)
        {
            return (CSList<TObject, TKey>)base.FilteredBy(predicate);
        }

        public new CSList<TObject, TKey> OrderedBy(string orderBy)
        {
            return (CSList<TObject, TKey>)base.OrderedBy(orderBy);
        }
    }

}
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
using System.Linq;

namespace Vici.CoolStorage
{
    public delegate void CSToArrayIterator<TSource, TTarget>(TSource objFrom, ref TTarget objTo);

    [Serializable]
    internal class PrefetchFilter
    {
        public PrefetchFilter(string foreignKey, string inStatement, CSParameterCollection parameters)
        {
            ForeignKey = foreignKey;
            InStatement = inStatement;
            Parameters = parameters;
        }

        public string ForeignKey;
        public string InStatement;
        public CSParameterCollection Parameters;
    }

    [Serializable]
    public abstract class CSList : IEnumerable
    {
        [NonSerialized]
        private CSSchema _schema;
        [NonSerialized]
        private CSRelation _relation;
        [NonSerialized]
        private CSObject _relationObject;

        private int _maxRecords;
        private string _orderBy = "";
        private PrefetchFilter _prefetchFilter;
        private CSFilter _filter = CSFilter.None;
        private int _startRecord = 1;
        private bool _populated;
        private string[] _prefetchPaths;

        internal CSList(CSSchema schema)
        {
            _schema = schema;

            if (!string.IsNullOrEmpty(_schema.DefaultSortExpression))
                OrderBy = _schema.DefaultSortExpression;
        }

        public abstract bool Save();

        internal abstract void UpdateForeignKeys();
        internal abstract void Remove(CSObject obj);

        //internal abstract CSList GetPrefetchList(Predicate<object> filter);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetTypedEnumerator();
        }

        protected abstract IEnumerator GetTypedEnumerator();

        internal abstract void InitializePrefetch();
        internal abstract void AddFromPrefetch(CSObject obj);


        public abstract int Count { get; }

        public int MaxRecords
        {
            get { return _maxRecords; }
            set { _maxRecords = value; Refresh(); }
        }

        public string OrderBy
        {
            get { return _orderBy; }
            set { _orderBy = value; Refresh(); }
        }

        internal CSFilter Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        internal int StartRecord
        {
            get { return _startRecord; }
            set { _startRecord = value; }
        }

        internal bool Populated
        {
            get { return _populated; }
            set { _populated = value; }
        }

        internal CSRelation Relation
        {
            get { return _relation; }
            set { _relation = value; }
        }

        internal CSObject RelationObject
        {
            get { return _relationObject; }
            set { _relationObject = value; }
        }

        internal PrefetchFilter PrefetchFilter
        {
            get { return _prefetchFilter; }
            set { _prefetchFilter = value; }
        }

        internal CSSchema Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }

        internal string[] PrefetchPaths
        {
            get { return _prefetchPaths; }
            set { _prefetchPaths = value; }
        }

        public abstract int CountFast { get; }

        public bool HasObjects
        {
            get { return CountFast > 0; }
        }

        public abstract void Refresh();

        public void AddFilter(CSFilter filter)
        {
            Filter &= filter;

            Refresh();
        }

        public void AddFilterOr(CSFilter filter)
        {
            Filter |= filter;

            Refresh();
        }

        public void AddFilter(string filterExpression)
        {
            AddFilter(new CSFilter(filterExpression));
        }

        public void AddFilterOr(string filterExpression)
        {
            AddFilterOr(new CSFilter(filterExpression));
        }

        public void AddFilter(string filterExpression, CSParameterCollection parameters)
        {
            AddFilter(new CSFilter(filterExpression, parameters));
        }

        public void AddFilter(string filterExpression, string paramName, object paramValue)
        {
            AddFilter(new CSFilter(filterExpression, paramName, paramValue));
        }

        public void AddFilterOr(string filterExpression, CSParameterCollection parameters)
        {
            AddFilterOr(new CSFilter(filterExpression, parameters));
        }

        public void AddFilterOr(string filterExpression, string paramName, object paramValue)
        {
            AddFilterOr(new CSFilter(filterExpression, paramName, paramValue));
        }


        internal List<CSSchemaField> GetPrefetchFieldsMany()
        {
            List<CSSchemaField> prefetchFields = new List<CSSchemaField>();

            foreach (CSSchemaField schemaField in Schema.Fields)
            {
                bool prefetch = schemaField.Prefetch;

                prefetch |= (PrefetchPaths != null && PrefetchPaths.Any(s =>
                                                                            {
                                                                                if (s.IndexOf('.') > 0)
                                                                                    s = s.Substring(0, s.IndexOf('.'));

                                                                                return s == schemaField.Name;
                                                                            }));

                if (schemaField.Relation != null && schemaField.Relation.RelationType == CSSchemaRelationType.OneToMany && prefetch)
                {
                    prefetchFields.Add(schemaField);
                }
            }

            return prefetchFields;
        }

        protected CSFilter BuildRelationFilter(string tableAlias)
        {
            if (tableAlias == null)
            {
                tableAlias = "";
            }
            else
                tableAlias += ".";

            if (Relation != null)
            {
                CSParameterCollection parameters = new CSParameterCollection();

                switch (Relation.RelationType)
                {
                    case CSSchemaRelationType.OneToMany:
                        {
                            CSParameter csParameter = parameters.Add();

                            csParameter.Value = RelationObject.Data["#" + Relation.LocalKey].Value;

                            return new CSFilter("{" + tableAlias + Relation.ForeignKey + "}=" + csParameter.Name, parameters);
                        }

                    case CSSchemaRelationType.ManyToMany:
                        {
                            if (Relation.ForeignKey == null)
                                Relation.ForeignKey = Schema.KeyColumns[0].Name;

                            if (Relation.ForeignLinkKey == null)
                                Relation.ForeignLinkKey = Relation.ForeignKey;

                            CSParameter csParameter = parameters.Add();

                            csParameter.Value = RelationObject.Data["#" + Relation.LocalKey].Value;

                            return new CSFilter("{" + tableAlias + Relation.ForeignKey + "} $in ($select {" + Relation.ForeignLinkKey + "} $from [" + Relation.LinkTable + "] where {" + Relation.LocalLinkKey + "}=" + csParameter.Name + ")", parameters);
                        }
                }
            }

            return CSFilter.None;
        }

    }



}

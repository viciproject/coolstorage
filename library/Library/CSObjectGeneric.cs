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
	public delegate void ObjectEventHandler<T>(T sender, EventArgs e);
	public delegate void ObjectWriteEventHandler<T>(T sender, ObjectWriteEventArgs e);
	public delegate void ObjectDeleteEventHandler<T>(T sender, ObjectDeleteEventArgs e);
	public delegate void FieldChangedEventHandler<T>(T sender, FieldChangedEventArgs e);

	public abstract class CSObject<T> : CSObject 
		where T : CSObject<T>
	{
		public event ObjectEventHandler<T>       ObjectReading;
		public event ObjectEventHandler<T>       ObjectRead;
        public event ObjectWriteEventHandler<T>  ObjectSaving;
        public event ObjectEventHandler<T>       ObjectSaved;
        public event ObjectWriteEventHandler<T>  ObjectUpdating;
		public event ObjectEventHandler<T>       ObjectUpdated;
		public event ObjectWriteEventHandler<T>  ObjectCreating;
		public event ObjectEventHandler<T>       ObjectCreated;
		public event ObjectDeleteEventHandler<T> ObjectDeleting;
		public event ObjectEventHandler<T>       ObjectDeleted;

		public static event ObjectEventHandler<T>       AnyObjectReading;
		public static event ObjectEventHandler<T>       AnyObjectRead;
		public static event ObjectWriteEventHandler<T>  AnyObjectUpdating;
		public static event ObjectEventHandler<T>       AnyObjectUpdated;
        public static event ObjectWriteEventHandler<T>  AnyObjectSaving;
        public static event ObjectEventHandler<T>       AnyObjectSaved;
        public static event ObjectWriteEventHandler<T>  AnyObjectCreating;
		public static event ObjectEventHandler<T>       AnyObjectCreated;
		public static event ObjectDeleteEventHandler<T> AnyObjectDeleting;
		public static event ObjectEventHandler<T>       AnyObjectDeleted;

        /// <summary>
        /// Creates a new object
        /// </summary>
        /// <returns>The newly created object</returns>
		public static T New()
		{
			return CSFactory.New<T>(); 
		}

        /// <summary>
        /// Reads the using unique field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
		public static T ReadUsingUniqueField(string fieldName, object value)
		{
			T obj = New();

			if (obj.ReadUsingUniqueKey(fieldName, value))
				return obj;
			else
				return null;
		}

        public static T ReadFirst(CSFilter filter)
        {
            CSList<T> objects = new CSList<T>(filter);

            objects.MaxRecords = 1;

            if (objects.Count < 1)
                return null;
            else
                return objects[0];
        }

        public static T ReadFirst(string filter)
        {
            return ReadFirst(new CSFilter(filter));
        }

		public static T ReadFirst(string filter, CSParameterCollection parameters)
		{
			return ReadFirst(new CSFilter(filter, parameters));
		}

		public static T ReadFirst(string filter, params CSParameter[] parameters)
		{
			return ReadFirst(new CSFilter(filter, parameters));
		}

		public static T ReadFirst(string filter, string paramName1, object paramValue1)
		{
            return ReadFirst(new CSFilter(filter, paramName1, paramValue1));
		}

		public static T ReadFirst(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			return ReadFirst(new CSFilter(filter,paramName1, paramValue1, paramName2, paramValue2));
		}

		public static T ReadFirst(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			return ReadFirst(new CSFilter(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
		}

        public static int Count()
        {
            return List().CountFast;
        }

        public static int Count(CSFilter filter)
        {
            return List(filter).CountFast;
        }

        public static int Count(string filter)
        {
            return Count(new CSFilter(filter));
        }

        public static int Count(string filter, CSParameterCollection parameters)
        {
            return Count(new CSFilter(filter, parameters));
        }

        public static int Count(string filter, params CSParameter[] parameters)
        {
            return Count(new CSFilter(filter, parameters));
        }

        public static int Count(string filter, string paramName1, object paramValue1)
        {
            return Count(new CSFilter(filter, paramName1, paramValue1));
        }

        public static int Count(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return Count(new CSFilter(filter, paramName1, paramValue1, paramName2, paramValue2));
        }

        public static int Count(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return Count(new CSFilter(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public static CSList<T> All()
        {
            return List();
        }

		public static CSList<T> List()
		{
			return new CSList<T>();
		}

        public static CSList<T> List(string filter)
        {
            return new CSList<T>(filter);
        }

		public static CSList<T> List(CSFilter filter)
		{
			return new CSList<T>(filter);
		}

		public static CSList<T> List(string filter, CSParameterCollection parameters)
		{
			return new CSList<T>(filter,parameters);
		}

		public static CSList<T> List(string filter, params CSParameter[] parameters)
		{
			return new CSList<T>(filter, parameters);
		}

		public static CSList<T> List(string filter, string paramName, object paramValue)
	    {
	        return new CSList<T>(filter,paramName,paramValue);
	    }

		public static CSList<T> List(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			return new CSList<T>(filter, paramName1, paramValue1, paramName2, paramValue2);
		}

		public static CSList<T> List(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			return new CSList<T>(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

		public static CSList<T> OrderedList(string orderBy)
		{
			CSList<T> list = List();

			list.OrderBy = orderBy;

			return list;
		}

		public static CSList<T> OrderedList(string orderBy, CSFilter filter)
		{
			CSList<T> list = List(filter);
			
			list.OrderBy = orderBy;

			return list;
		}

		public static CSList<T> OrderedList(string orderBy, string filter)
		{
			CSList<T> list = List(filter);

			list.OrderBy = orderBy;

			return list;
		}

		public static CSList<T> OrderedList(string orderBy, string filter, CSParameterCollection parameters)
		{
			CSList<T> list = List(filter,parameters);

			list.OrderBy = orderBy;

			return list;
		}

		public static CSList<T> OrderedList(string orderBy, string filter, params CSParameter[] parameters)
		{
			CSList<T> list = List(filter, parameters);

			list.OrderBy = orderBy;

			return list;
		}

		public static CSList<T> OrderedList(string orderBy, string filter, string paramName1, object paramValue1)
		{
			CSList<T> list = List(filter, paramName1, paramValue1);

			list.OrderBy = orderBy;

			return list;
		}

		public static CSList<T> OrderedList(string orderBy, string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			CSList<T> list = List(filter, paramName1, paramValue1, paramName2, paramValue2);

			list.OrderBy = orderBy;

			return list;
		}

		public static CSList<T> OrderedList(string orderBy, string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			CSList<T> list = List(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);

			list.OrderBy = orderBy;

			return list;
		}

		public static object GetScalar(string fieldName, string orderBy)
		{
			return GetScalar(fieldName, null, CSAggregate.None, CSFilter.None, orderBy);
		}

        public static TScalar GetScalar<TScalar>(string fieldName, string orderBy)
        {
            return GetScalar<TScalar>(fieldName, null, CSAggregate.None, CSFilter.None, orderBy);
        }

        public static object GetScalar(string fieldName, string orderBy, string filterExpression)
        {
            return GetScalar(fieldName, orderBy, filterExpression, null, orderBy);
        }

        public static TScalar GetScalar<TScalar>(string fieldName, string orderBy, string filterExpression)
        {
            return GetScalar<TScalar>(fieldName, orderBy, filterExpression, null, orderBy);
        }

		public static object GetScalar(string fieldName, string orderBy, string filterExpression, string paramName, object paramValue)
		{
			return GetScalar(fieldName, orderBy , filterExpression, new CSParameterCollection(paramName, paramValue));
		}

        public static TScalar GetScalar<TScalar>(string fieldName, string orderBy, string filterExpression, string paramName, object paramValue)
        {
            return GetScalar<TScalar>(fieldName, orderBy, filterExpression, new CSParameterCollection(paramName, paramValue));
        }

		public static object GetScalar(string fieldName, string orderBy, string filterExpression, CSParameterCollection filterParameters)
		{
			return GetScalar(fieldName, null, orderBy, new CSFilter(filterExpression, filterParameters));
		}

        public static TScalar GetScalar<TScalar>(string fieldName, string orderBy, string filterExpression, CSParameterCollection filterParameters)
        {
            return GetScalar<TScalar>(fieldName, null, orderBy, new CSFilter(filterExpression, filterParameters));
        }

		internal static object GetScalar(string fieldName, string tableAlias, string orderBy, CSFilter queryFilter)
		{
			return GetScalar(fieldName, tableAlias, CSAggregate.None, queryFilter, orderBy);
		}

        internal static TScalar GetScalar<TScalar>(string fieldName, string tableAlias, string orderBy, CSFilter queryFilter)
        {
            return GetScalar<TScalar>(fieldName, tableAlias, CSAggregate.None, queryFilter, orderBy);
        }

        public static object GetScalar(string fieldName, CSAggregate aggregate)
        {
            return GetScalar(fieldName, null, aggregate, CSFilter.None);
        }

        public static TScalar GetScalar<TScalar>(string fieldName, CSAggregate aggregate)
        {
            return GetScalar<TScalar>(fieldName, null, aggregate, CSFilter.None);
        }

        public static object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression)
        {
            return GetScalar(fieldName, aggregate, filterExpression, CSParameterCollection.Empty);
        }

        public static TScalar GetScalar<TScalar>(string fieldName, CSAggregate aggregate, string filterExpression)
        {
            return GetScalar<TScalar>(fieldName, aggregate, filterExpression, CSParameterCollection.Empty);
        }

		public static object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression, string paramName, object paramValue)
		{
			return GetScalar(fieldName, aggregate, filterExpression, new CSParameterCollection(paramName, paramValue));
		}

        public static TScalar GetScalar<TScalar>(string fieldName, CSAggregate aggregate, string filterExpression, string paramName, object paramValue)
        {
            return GetScalar<TScalar>(fieldName, aggregate, filterExpression, new CSParameterCollection(paramName, paramValue));
        }

        public static object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			return GetScalar(fieldName, aggregate, filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
		}

        public static TScalar GetScalar<TScalar>(string fieldName, CSAggregate aggregate, string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return GetScalar<TScalar>(fieldName, aggregate, filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public static object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			return GetScalar(fieldName, aggregate, filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
		}

        public static TScalar GetScalar<TScalar>(string fieldName, CSAggregate aggregate, string filterExpression, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return GetScalar<TScalar>(fieldName, aggregate, filterExpression, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public static object GetScalar(string fieldName, CSAggregate aggregate, string filterExpression, CSParameterCollection filterParameters)
		{
		    return GetScalar(fieldName, null, aggregate, new CSFilter(filterExpression, filterParameters));
		}

        public static TScalar GetScalar<TScalar>(string fieldName, CSAggregate aggregate, string filterExpression, CSParameterCollection filterParameters)
        {
            return GetScalar<TScalar>(fieldName, null, aggregate, new CSFilter(filterExpression, filterParameters));
        }

        internal static object GetScalar(string fieldName, string tableAlias, CSAggregate aggregate, CSFilter queryFilter)
		{
			return GetScalar(fieldName, tableAlias, aggregate, queryFilter, null);
		}

        internal static TScalar GetScalar<TScalar>(string fieldName, string tableAlias, CSAggregate aggregate, CSFilter queryFilter)
        {
            return GetScalar<TScalar>(fieldName, tableAlias, aggregate, queryFilter, null);
        }

        private static TReturn GetScalar<TReturn>(string fieldName, string tableAlias, CSAggregate aggregate, CSFilter queryFilter, string orderBy)
        {
            return GetScalar(fieldName, tableAlias, aggregate, queryFilter, orderBy).Convert<TReturn>();
        }

		private static object GetScalar(string fieldName, string tableAlias, CSAggregate aggregate, CSFilter queryFilter , string orderBy)
		{
			CSSchema schema = CSSchema.Get(typeof(T));

            if (tableAlias == null)
                tableAlias = CSNameGenerator.NextTableAlias;

			if (orderBy == null)
				orderBy = "";

			string aggregateExpr = null;

			int maxRows = 0;

			switch (aggregate)
			{
				case CSAggregate.None         : aggregateExpr = "{0}"; maxRows = 1;    break;
				case CSAggregate.Sum          : aggregateExpr = "sum({0})";            break;
				case CSAggregate.SumDistinct  : aggregateExpr = "sum(distinct {0})";   break;
				case CSAggregate.Count        : aggregateExpr = "count(*)";            break;
				case CSAggregate.CountDistinct: aggregateExpr = "count(distinct {0})"; break;
				case CSAggregate.Avg          : aggregateExpr = "avg({0})";            break;
				case CSAggregate.AvgDistinct  : aggregateExpr = "avg(distinct {0})";   break;
				case CSAggregate.Max          : aggregateExpr = "max({0})";            break;
				case CSAggregate.Min          : aggregateExpr = "min({0})";            break;
			}

			CSJoinList joins = new CSJoinList();

			if (fieldName != "*")
				fieldName = CSExpressionParser.ParseFilter(fieldName, schema, tableAlias, joins);

			string whereFilter = CSExpressionParser.ParseFilter(queryFilter.Expression, schema, tableAlias, joins);
			orderBy = CSExpressionParser.ParseOrderBy(orderBy, schema, tableAlias, joins);

			string sqlQuery = schema.DB.BuildSelectSQL(schema.TableName, tableAlias, new[] { String.Format(aggregateExpr, fieldName) }, null, joins.BuildJoinExpressions(), whereFilter, orderBy, 1, maxRows, false, false);

			return schema.DB.GetScalar(sqlQuery, queryFilter.Parameters);
		}

		internal override void Fire_ObjectRead()
		{
			if (ObjectRead != null)
				ObjectRead((T)this, EventArgs.Empty);

			if (AnyObjectRead != null)
				AnyObjectRead((T)this, EventArgs.Empty);
		}

		internal override void Fire_ObjectReading()
		{
			if (ObjectReading != null)
				ObjectReading((T)this, new EventArgs());

			if (AnyObjectReading != null)
				AnyObjectReading((T)this, new EventArgs());
		}

		internal override void Fire_ObjectUpdating(ref bool cancel)
		{
			if (!IsDirty)
				return;

			if (ObjectUpdating != null)
			{
				ObjectWriteEventArgs eventArgs = new ObjectWriteEventArgs();

				ObjectUpdating((T) this, eventArgs);

				if (eventArgs.CancelWrite)
					cancel = true;
			}

			if (AnyObjectUpdating != null)
			{
				ObjectWriteEventArgs eventArgs = new ObjectWriteEventArgs();

				AnyObjectUpdating((T)this, eventArgs);

				if (eventArgs.CancelWrite)
					cancel = true;
			}
		}

		internal override void Fire_ObjectUpdated()
		{
			if (ObjectUpdated != null)
				ObjectUpdated((T) this, new EventArgs());

			if (AnyObjectUpdated != null)
				AnyObjectUpdated((T)this, new EventArgs());
		}

        internal override void Fire_ObjectSaving(ref bool cancel)
        {
            if (ObjectSaving != null)
            {
                ObjectWriteEventArgs eventArgs = new ObjectWriteEventArgs();

                ObjectSaving((T)this, eventArgs);

                if (eventArgs.CancelWrite)
                    cancel = true;
            }

            if (AnyObjectSaving != null)
            {
                ObjectWriteEventArgs eventArgs = new ObjectWriteEventArgs();

                AnyObjectSaving((T)this, eventArgs);

                if (eventArgs.CancelWrite)
                    cancel = true;
            }
        }

        internal override void Fire_ObjectSaved()
        {
            if (ObjectSaved != null)
                ObjectSaved((T)this, new EventArgs());

            if (AnyObjectSaved != null)
                AnyObjectSaved((T)this, new EventArgs());
        }

		internal override void Fire_ObjectCreated()
		{
			if (ObjectCreated != null)
				ObjectCreated((T) this, new EventArgs());

			if (AnyObjectCreated != null)
				AnyObjectCreated((T)this, new EventArgs());
		}

		internal override void Fire_ObjectCreating(ref bool cancel)
		{
			if (ObjectCreating != null)
			{
				ObjectWriteEventArgs e = new ObjectWriteEventArgs();

				ObjectCreating((T) this, e);

				if (e.CancelWrite)
					cancel = true;
			}

			if (AnyObjectCreating != null)
			{
				ObjectWriteEventArgs e = new ObjectWriteEventArgs();

				AnyObjectCreating((T)this, e);

				if (e.CancelWrite)
					cancel = true;
			}
		}

		internal override void Fire_ObjectDeleting(ref bool cancel)
		{
			if (ObjectDeleting != null)
			{
				ObjectDeleteEventArgs e = new ObjectDeleteEventArgs();

				ObjectDeleting((T) this, e);

				if (e.CancelDelete)
					cancel = true;
			}

			if (AnyObjectDeleting != null)
			{
				ObjectDeleteEventArgs e = new ObjectDeleteEventArgs();

				AnyObjectDeleting((T)this, e);

				if (e.CancelDelete)
					cancel = true;
			}
		}

		internal override void Fire_ObjectDeleted()
		{
			if (ObjectDeleted != null)
				ObjectDeleted((T) this, new EventArgs());

			if (AnyObjectDeleted != null)
				AnyObjectDeleted((T)this, new EventArgs());
		}

	}

    public abstract class CSObject<TObject, TKey1, TKey2, TKey3, TKey4, TKey5> : CSObject<TObject>
    where TObject : CSObject<TObject>
    {
        public static TObject Read(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5)
        {
            return CSFactory.Read<TObject>(key1, key2, key3, key4, key5);
        }
    }

    public abstract class CSObject<TObject, TKey1, TKey2, TKey3, TKey4> : CSObject<TObject>
    where TObject : CSObject<TObject>
    {
        public static TObject Read(TKey1 key1, TKey2 key2, TKey3 key3,TKey4 key4)
        {
            return CSFactory.Read<TObject>(key1, key2, key3, key4);
        }
    }

	public abstract class CSObject<TObject, TKey1, TKey2, TKey3> : CSObject<TObject>
	where TObject : CSObject<TObject>
	{
		public static TObject Read(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			return CSFactory.Read<TObject>(key1, key2, key3);
		}
	}

	public abstract class CSObject<TObject, TKey1, TKey2> : CSObject<TObject>
		where TObject : CSObject<TObject>
	{
		public static TObject Read(TKey1 key1, TKey2 key2)
		{
			return CSFactory.Read<TObject>(key1, key2);
		}
	}

	public abstract class CSObject<TObject, TKey> : CSObject<TObject>
		where TObject : CSObject<TObject>
	{
		public static TObject Read(TKey key)
		{
			return CSFactory.Read<TObject>(key);
		}

        public static TObject ReadSafe(TKey key)
        {
            return CSFactory.ReadSafe<TObject>(key);
        }

		public static bool Delete(TKey key)
		{
			throw new NotSupportedException();
		}

		public static new CSList<TObject,TKey> List()
		{
			return new CSList<TObject,TKey>();
		}

		public static new CSList<TObject, TKey> List(string filter)
		{
			return new CSList<TObject, TKey>(filter);
		}

		public static new CSList<TObject, TKey> List(CSFilter filter)
		{
			return new CSList<TObject, TKey>(filter);
		}

		public static new CSList<TObject, TKey> List(string filter, CSParameterCollection parameters)
		{
			return new CSList<TObject, TKey>(filter, parameters);
		}

		public static new CSList<TObject, TKey> List(string filter, params CSParameter[] parameters)
		{
			return new CSList<TObject, TKey>(filter, parameters);
		}

		public static new CSList<TObject, TKey> List(string filter, string paramName, object paramValue)
		{
			return new CSList<TObject, TKey>(filter, paramName, paramValue);
		}

		public new static CSList<TObject, TKey> List(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			return new CSList<TObject, TKey>(filter, paramName1, paramValue1, paramName2, paramValue2);
		}

		public new static CSList<TObject, TKey> List(string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			return new CSList<TObject, TKey>(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy)
		{
			CSList<TObject, TKey> list = List();

			list.OrderBy = orderBy;

			return list;
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy, CSFilter filter)
		{
			CSList<TObject, TKey> list = List(filter);

			list.OrderBy = orderBy;

			return list;
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy, string filter)
		{
			CSList<TObject, TKey> list = List(filter);

			list.OrderBy = orderBy;

			return list;
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy, string filter, CSParameterCollection parameters)
		{
			CSList<TObject, TKey> list = List(filter, parameters);

			list.OrderBy = orderBy;

			return list;
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy, string filter, params CSParameter[] parameters)
		{
			CSList<TObject, TKey> list = List(filter, parameters);

			list.OrderBy = orderBy;

			return list;
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy, string filter, string paramName1, object paramValue1)
		{
			CSList<TObject, TKey> list = List(filter, paramName1, paramValue1);

			list.OrderBy = orderBy;

			return list;
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy, string filter, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			CSList<TObject, TKey> list = List(filter, paramName1, paramValue1, paramName2, paramValue2);

			list.OrderBy = orderBy;

			return list;
		}

		public new static CSList<TObject, TKey> OrderedList(string orderBy, string filter, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			CSList<TObject, TKey> list = List(filter, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);

			list.OrderBy = orderBy;

			return list;
		}

	}


}
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
using System.Data;
using System.Reflection;

using Vici.Core;

namespace Vici.CoolStorage
{
    public class CSGenericRecord : Dictionary<string, object> { }
    public class CSGenericRecordList : List<CSGenericRecord> { }

    public class CSDatabaseInstance
    {
        private readonly string _contextName;

        internal CSDatabaseInstance(string contextName)
        {
            _contextName = contextName;
        }

        private CSDataProvider DB
        {
            get { return CSConfig.GetDB(_contextName); }
        }

        public int ExecuteNonQuery(string sql)
        {
            return ExecuteNonQuery(sql, CSParameterCollection.Empty);
        }

        public int ExecuteNonQuery(string sql, string paramName, object paramValue)
        {
            return ExecuteNonQuery(sql, new CSParameterCollection(paramName, paramValue));
        }

        public int ExecuteNonQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return ExecuteNonQuery(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public int ExecuteNonQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return ExecuteNonQuery(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public int ExecuteNonQuery(string sql, CSParameterCollection parameters)
        {
            using (new CSTransaction(DB))
                return DB.ExecuteNonQuery(sql, parameters);
        }

        public int ExecuteNonQuery(string sql, params CSParameter[] parameters)
        {
            return ExecuteNonQuery(sql, new CSParameterCollection(parameters));
        }

        public object GetScalar(string sql)
        {
            return GetScalar(sql, CSParameterCollection.Empty);
        }

        public object GetScalar(string sql, string paramName, object paramValue)
        {
            return GetScalar(sql, new CSParameterCollection(paramName, paramValue));
        }

        public object GetScalar(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return GetScalar(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public object GetScalar(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return GetScalar(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public object GetScalar(string sql, params CSParameter[] parameters)
        {
            return GetScalar(sql, new CSParameterCollection(parameters));
        }

        public object GetScalar(string sql, CSParameterCollection parameters)
        {
            using (new CSTransaction(DB))
                return DB.GetScalar(sql, parameters);
        }

        public T[] GetScalarList<T>(string sql)
        {
            return GetScalarList<T>(sql, CSParameterCollection.Empty);
        }

        public T[] GetScalarList<T>(string sql, string paramName, object paramValue)
        {
            return GetScalarList<T>(sql, new CSParameterCollection(paramName, paramValue));
        }

        public T[] GetScalarList<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return GetScalarList<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public T[] GetScalarList<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return GetScalarList<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public T[] GetScalarList<T>(string sql, params CSParameter[] parameters)
        {
            return GetScalarList<T>(sql, new CSParameterCollection(parameters));
        }
        
        public T[] GetScalarList<T>(string sql, CSParameterCollection parameters)
        {
            List<T> list = new List<T>();

            using (new CSTransaction(DB))
            {
                using (ICSDbReader reader = DB.CreateReader(sql, parameters))
                {
                    while (reader.Read())
                    {
                        list.Add(reader[0].Convert<T>());
                    }
                }
            }

            return list.ToArray();
        }

        public T GetScalar<T>(string sql)
        {
            return GetScalar<T>(sql, CSParameterCollection.Empty);
        }

        public T GetScalar<T>(string sql, string paramName, object paramValue)
        {
            return GetScalar<T>(sql, new CSParameterCollection(paramName, paramValue));
        }

        public T GetScalar<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return GetScalar<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public T GetScalar<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return GetScalar<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public T GetScalar<T>(string sql, params CSParameter[] parameters)
        {
            return GetScalar<T>(sql, new CSParameterCollection(parameters));
        }

        public T GetScalar<T>(string sql, CSParameterCollection parameters)
        {
            return GetScalar(sql, parameters).Convert<T>();
        }

        public CSGenericRecordList RunQuery(string sql)
        {
            return RunQuery(sql, CSParameterCollection.Empty);
        }

        public CSGenericRecordList RunQuery(string sql, string paramName, object paramValue)
        {
            return RunQuery(sql, new CSParameterCollection(paramName, paramValue));
        }

        public CSGenericRecordList RunQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return RunQuery(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public CSGenericRecordList RunQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return RunQuery(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public CSGenericRecordList RunQuery(string sql, params CSParameter[] parameters)
        {
            return RunQuery(sql, new CSParameterCollection(parameters));
        }

        public CSGenericRecordList RunQuery(string sql, CSParameterCollection parameters)
        {
            CSGenericRecordList list = new CSGenericRecordList();

            using (new CSTransaction(DB))
            {
                using (ICSDbReader reader = DB.CreateReader(sql, parameters))
                {
                    while (reader.Read())
                    {
                        CSGenericRecord record = new CSGenericRecord();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            record[reader.GetName(i)] = (reader[i] is DBNull) ? null : reader[i];
                        }

                        list.Add(record);
                    }
                }
            }

            return list;
        }

        public CSGenericRecord RunSingleQuery(string sql)
        {
            return RunSingleQuery(sql, CSParameterCollection.Empty);
        }

        public CSGenericRecord RunSingleQuery(string sql, string paramName, object paramValue)
        {
            return RunSingleQuery(sql, new CSParameterCollection(paramName, paramValue));
        }

        public CSGenericRecord RunSingleQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return RunSingleQuery(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public CSGenericRecord RunSingleQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return RunSingleQuery(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public CSGenericRecord RunSingleQuery(string sql, params CSParameter[] parameters)
        {
            return RunSingleQuery(sql, new CSParameterCollection(parameters));
        }

        public CSGenericRecord RunSingleQuery(string sql, CSParameterCollection parameters)
        {
            CSGenericRecord rec = new CSGenericRecord();

            using (new CSTransaction(DB))
            {
                using (ICSDbReader reader = DB.CreateReader(sql, parameters))
                {
                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            rec[reader.GetName(i)] = (reader[i] is DBNull) ? null : reader[i];
                        }

                        return rec;
                    }
                }
            }

            return null;
        }


        public T[] RunQuery<T>(string sql) where T : new()
        {
            return RunQuery<T>(sql, null, 0);
        }

        public T[] RunQuery<T>(string sql, CSParameterCollection parameters) where T : new()
        {
            return RunQuery<T>(sql, parameters, 0);
        }

        public T[] RunQuery<T>(string sql, string paramName, object paramValue) where T : new()
        {
            return RunQuery<T>(sql, new CSParameterCollection(paramName, paramValue), 0);
        }

        public T[] RunQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2) where T : new()
        {
            return RunQuery<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2), 0);
        }

        public T[] RunQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : new()
        {
            return RunQuery<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3), 0);
        }

        public T[] RunQuery<T>() where T : new()
        {
            return RunQuery<T>(CSHelper.GetQueryExpression<T>(), null, 0);
        }

        public T[] RunQuery<T>(CSParameterCollection parameters) where T : new()
        {
            return RunQuery<T>(CSHelper.GetQueryExpression<T>(), parameters, 0);
        }

        public T[] RunQuery<T>(string paramName, object paramValue) where T : new()
        {
            return RunQuery<T>(CSHelper.GetQueryExpression<T>(), new CSParameterCollection(paramName, paramValue), 0);
        }

        public T[] RunQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2) where T : new()
        {
            return RunQuery<T>(CSHelper.GetQueryExpression<T>(), new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2), 0);
        }

        public T[] RunQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : new()
        {
            return RunQuery<T>(CSHelper.GetQueryExpression<T>(), new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3), 0);
        }

        public T RunSingleQuery<T>() where T : class, new()
        {
            return RunSingleQuery<T>(CSParameterCollection.Empty);
        }

        public T RunSingleQuery<T>(CSParameterCollection parameters) where T : class, new()
        {
            T[] objects = RunQuery<T>(CSHelper.GetQueryExpression<T>(), parameters, 1);

            return (objects.Length > 0) ? objects[0] : null;
        }

        public T RunSingleQuery<T>(string paramName, object paramValue) where T : class, new()
        {
            return RunSingleQuery<T>(new CSParameterCollection(paramName, paramValue));
        }

        public T RunSingleQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2) where T : class, new()
        {
            return RunSingleQuery<T>(new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public T RunSingleQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : class, new()
        {
            return RunSingleQuery<T>(new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        public T RunSingleQuery<T>(string sql) where T : class, new()
        {
            return RunSingleQuery<T>(sql, null);
        }

        public T RunSingleQuery<T>(string sql, CSParameterCollection parameters) where T : class, new()
        {
            T[] objects = RunQuery<T>(sql, parameters, 1);

            return (objects.Length > 0) ? objects[0] : null;
        }

        public T RunSingleQuery<T>(string sql, string paramName, object paramValue) where T : class, new()
        {
            return RunSingleQuery<T>(sql, new CSParameterCollection(paramName, paramValue));
        }

        public T RunSingleQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2) where T : class, new()
        {
            return RunSingleQuery<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
        }

        public T RunSingleQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : class, new()
        {
            return RunSingleQuery<T>(sql, new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
        }

        private T[] RunQuery<T>(string sql, CSParameterCollection parameters, int maxRows) where T : new()
        {
            Type objectType = typeof(T);

            List<T> list = new List<T>();

            if (maxRows == 0)
                maxRows = int.MaxValue;

            using (new CSTransaction(DB))
            {
                using (ICSDbReader reader = DB.CreateReader(sql, parameters))
                {
                    int rowNum = 0;

                    while (rowNum < maxRows && reader.Read())
                    {
                        rowNum++;

                        T obj = new T();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);

                            PropertyInfo propertyInfo = objectType.GetProperty(columnName);

                            object columnValue = reader[i];

                            if (columnValue is DBNull)
                                columnValue = null;

                            if (propertyInfo != null)
                            {
                                propertyInfo.SetValue(obj, columnValue.Convert(propertyInfo.PropertyType), null);
                            }
                            else
                            {
                                FieldInfo fieldInfo = objectType.GetField(columnName);

                                if (fieldInfo != null)
                                {
                                    fieldInfo.SetValue(obj, columnValue.Convert(fieldInfo.FieldType));
                                }
                            }
                        }

                        list.Add(obj);
                    }
                }
            }

            return list.ToArray();
        }


    }

    public static class CSDatabase
	{
        private static readonly CSDatabaseContext _dbContext = new CSDatabaseContext();
        
        public class CSDatabaseContext
        {
            public CSDatabaseInstance Default
            {
                get { return this[CSConfig.DEFAULT_CONTEXTNAME]; }
            }

            public CSDatabaseInstance this[string contextName]
            {
                get { return new CSDatabaseInstance(contextName); }
            }
        }

        public static CSDatabaseContext Context
        {
            get { return _dbContext; }
        }

        public static int ExecuteNonQuery(string sql)
        {
			return Context.Default.ExecuteNonQuery(sql);
        }

        public static int ExecuteNonQuery(string sql, string paramName, object paramValue)
        {
            return Context.Default.ExecuteNonQuery(sql, paramName, paramValue);
        }

        public static int ExecuteNonQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return Context.Default.ExecuteNonQuery(sql, paramName1, paramValue1, paramName2, paramValue2);
        }

        public static int ExecuteNonQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return Context.Default.ExecuteNonQuery(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
        }

		public static int ExecuteNonQuery(string sql, CSParameterCollection parameters)
		{
            return Context.Default.ExecuteNonQuery(sql, parameters);
		}

		public static int ExecuteNonQuery(string sql, params CSParameter[] parameters)
		{
			return Context.Default.ExecuteNonQuery(sql, parameters);
		}

        public static object GetScalar(string sql)
        {
            return Context.Default.GetScalar(sql);
        }

		public static object GetScalar(string sql, string paramName, object paramValue)
		{
            return Context.Default.GetScalar(sql, paramName, paramValue);
		}

        public static object GetScalar(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return Context.Default.GetScalar(sql, paramName1, paramValue1, paramName2, paramValue2);
        }

        public static object GetScalar(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return Context.Default.GetScalar(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
        }

		public static object GetScalar(string sql, params CSParameter[] parameters)
		{
            return Context.Default.GetScalar(sql, parameters);
		}

		public static object GetScalar(string sql, CSParameterCollection parameters)
		{
            return Context.Default.GetScalar(sql, parameters);
		}

        public static T GetScalar<T>(string sql)
        {
            return Context.Default.GetScalar<T>(sql);
        }

        public static T GetScalar<T>(string sql, string paramName, object paramValue)
        {
            return Context.Default.GetScalar<T>(sql, paramName, paramValue);
        }

        public static T GetScalar<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return Context.Default.GetScalar<T>(sql, paramName1, paramValue1, paramName2, paramValue2);
        }

        public static T GetScalar<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return Context.Default.GetScalar<T>(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
        }

		public static T GetScalar<T>(string sql, params CSParameter[] parameters)
		{
            return Context.Default.GetScalar<T>(sql, parameters);
		}

		public static T GetScalar<T>(string sql, CSParameterCollection parameters)
		{
            return Context.Default.GetScalar<T>(sql, parameters);
		}

        public static T[] GetScalarList<T>(string sql)
        {
            return Context.Default.GetScalarList<T>(sql);
        }

        public static T[] GetScalarList<T>(string sql, string paramName, object paramValue)
        {
            return Context.Default.GetScalarList<T>(sql, paramName, paramValue);
        }

        public static T[] GetScalarList<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return Context.Default.GetScalarList<T>(sql, paramName1, paramValue1, paramName2, paramValue2);
        }

        public static T[] GetScalarList<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return Context.Default.GetScalarList<T>(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
        }

        public static T[] GetScalarList<T>(string sql, params CSParameter[] parameters)
        {
            return Context.Default.GetScalarList<T>(sql, parameters);
        }

        public static T[] GetScalarList<T>(string sql, CSParameterCollection parameters)
        {
            return Context.Default.GetScalarList<T>(sql, parameters);
        }


        public static CSGenericRecordList RunQuery(string sql)
		{
            return Context.Default.RunQuery(sql);
		}

        public static CSGenericRecordList RunQuery(string sql, string paramName, object paramValue)
		{
            return Context.Default.RunQuery(sql, paramName, paramValue);
		}

        public static CSGenericRecordList RunQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
            return Context.Default.RunQuery(sql, paramName1, paramValue1, paramName2, paramValue2);
		}

        public static CSGenericRecordList RunQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
            return Context.Default.RunQuery(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

		public static CSGenericRecordList RunQuery(string sql, CSParameterCollection parameters)
		{
		    return Context.Default.RunQuery(sql, parameters);
		}

        public static CSGenericRecordList RunQuery(string sql, params CSParameter[] parameters)
        {
            return Context.Default.RunQuery(sql, parameters);
        }

		public static T[] RunQuery<T>(string sql) where T : new()
		{
            return Context.Default.RunQuery<T>(sql);
		}

		public static T[] RunQuery<T>(string sql, CSParameterCollection parameters) where T : new()
		{
            return Context.Default.RunQuery<T>(sql, parameters);
		}

		public static T[] RunQuery<T>(string sql, string paramName, object paramValue) where T : new()
		{
            return Context.Default.RunQuery<T>(sql, paramName, paramValue);
		}

		public static T[] RunQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2) where T : new()
		{
		    return Context.Default.RunQuery<T>(sql, paramName1, paramValue1, paramName2, paramValue2);
		}

		public static T[] RunQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : new()
		{
		    return Context.Default.RunQuery<T>(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

		public static T[] RunQuery<T>() where T:new()
		{
            return Context.Default.RunQuery<T>();
		}

		public static T[] RunQuery<T>(CSParameterCollection parameters) where T : new()
		{
            return Context.Default.RunQuery<T>(parameters);
		}

		public static T[] RunQuery<T>(string paramName, object paramValue) where T:new()
		{
		    return Context.Default.RunQuery<T>(paramName, paramValue);
		}

		public static T[] RunQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2) where T : new()
		{
		    return Context.Default.RunQuery<T>(paramName1, paramValue1, paramName2, paramValue2);
		}

		public static T[] RunQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : new()
		{
		    return Context.Default.RunQuery<T>(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

		public static T RunSingleQuery<T>() where T : class, new()
		{
            return Context.Default.RunSingleQuery<T>();
		}

		public static T RunSingleQuery<T>(CSParameterCollection parameters) where T : class, new()
		{
		    return Context.Default.RunSingleQuery<T>(parameters);
		}

		public static T RunSingleQuery<T>(string paramName, object paramValue) where T:class,new()
		{
            return Context.Default.RunSingleQuery<T>(paramName, paramValue);
		}

		public static T RunSingleQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2) where T : class, new()
		{
            return Context.Default.RunSingleQuery<T>(paramName1, paramValue1, paramName2, paramValue2);
		}

		public static T RunSingleQuery<T>(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : class, new()
		{
            return Context.Default.RunSingleQuery<T>(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

		public static T RunSingleQuery<T>(string sql) where T : class, new()
		{
            return Context.Default.RunSingleQuery<T>(sql);
		}

		public static T RunSingleQuery<T>(string sql,CSParameterCollection parameters) where T : class, new()
		{
		    return Context.Default.RunSingleQuery<T>(sql, parameters);
		}

		public static T RunSingleQuery<T>(string sql, string paramName, object paramValue) where T : class, new()
		{
            return Context.Default.RunSingleQuery<T>(sql,paramName, paramValue);
		}

		public static T RunSingleQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2) where T : class, new()
		{
            return Context.Default.RunSingleQuery<T>(sql, paramName1, paramValue1, paramName2, paramValue2);
		}

		public static T RunSingleQuery<T>(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3) where T : class, new()
		{
            return Context.Default.RunSingleQuery<T>(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

        public static CSGenericRecord RunSingleQuery(string sql)
        {
            return Context.Default.RunSingleQuery(sql);
        }

        public static CSGenericRecord RunSingleQuery(string sql, string paramName, object paramValue)
        {
            return Context.Default.RunSingleQuery(sql, paramName, paramValue);
        }

        public static CSGenericRecord RunSingleQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2)
        {
            return Context.Default.RunSingleQuery(sql, paramName1, paramValue1, paramName2, paramValue2);
        }

        public static CSGenericRecord RunSingleQuery(string sql, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            return Context.Default.RunSingleQuery(sql, paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
        }

        public static CSGenericRecord RunSingleQuery(string sql, CSParameterCollection parameters)
        {
            return Context.Default.RunSingleQuery(sql, parameters);
        }

        public static CSGenericRecord RunSingleQuery(string sql, params CSParameter[] parameters)
        {
            return Context.Default.RunSingleQuery(sql, parameters);
        }

	}
}

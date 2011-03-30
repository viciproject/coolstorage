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
using System.Text.RegularExpressions;
using VistaDB.Provider;

namespace Vici.CoolStorage
{
	public class CSDataProviderVistaDB : CSDataProvider
	{
        public CSDataProviderVistaDB(string connectionString) : base(connectionString)
		{
		}

		protected override IDbConnection CreateConnection()
		{
            VistaDBConnection conn = new VistaDBConnection(ConnectionString);

			conn.Open();

		   // _serverVersion = Convert.ToInt32(conn.ServerVersion.Split('.')[0]);

			return conn;
		}

        protected override void ClearConnectionPool()
        {
            VistaDBConnection.ClearAllPools();
        }

        public CSDataProviderVistaDB(string fileName, string password)
			: this("Data Source=" + fileName + ";Password=" + password)
		{
		}

		protected override CSDataProvider Clone()
		{
            return new CSDataProviderVistaDB(ConnectionString);
		}

		protected override IDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
		{
			VistaDBCommand sqlCommand = (VistaDBCommand) Connection.CreateCommand();

			sqlCommand.Transaction = (VistaDBTransaction) CurrentTransaction;

            if (sqlQuery.StartsWith("!"))
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = sqlQuery.Substring(1);
            }
            else
            {
                sqlCommand.CommandType = CommandType.Text;
                sqlCommand.CommandText = sqlQuery;
            }

			if (parameters != null && !parameters.IsEmpty)
				foreach (CSParameter csParameter in parameters)
				{
					IDbDataParameter dataParameter = sqlCommand.CreateParameter();

					dataParameter.ParameterName = csParameter.Name;
					dataParameter.Direction = ParameterDirection.Input;
                    dataParameter.Value = ConvertParameter(csParameter.Value);

					sqlCommand.Parameters.Add(dataParameter);
				}

			return sqlCommand;
		}

        // Don't throw away yet, might be needed when implementing paging
        private string ReverseOrderBy(string orderBy)
        {
            string orderByReverse = "";

            string[] sortByFields = (orderBy ?? "").Split(',');

            foreach (string sortBy in sortByFields)
            {
                string s = sortBy.Trim();

                if (s.Length < 1)
                    continue;

                if (orderByReverse.Length > 0)
                    orderByReverse += ",";

                if (s.EndsWith(" DESC", StringComparison.InvariantCultureIgnoreCase))
                {
                    s = s.Substring(0, s.Length - 5).Trim();

                    orderByReverse += s + " ASC";
                }
                else
                {
                    if (s.EndsWith(" ASC", StringComparison.InvariantCultureIgnoreCase))
                    {
                        s = s.Substring(0, s.Length - 4).Trim();
                    }

                    orderByReverse += s + " DESC";
                }
            }

            return orderByReverse;
        }

	    protected override string BuildSelectSQL(string tableName, string tableAlias, string[] columnList, string[] columnAliasList, string[] joinList, string whereClause, string orderBy, int startRow, int maxRows, bool quoteColumns, bool unOrdered)
	    {
	        string sqlColumns;
	        string sqlFromTable;
	        string sqlJoins;
	        string sqlWhere;
	        string sqlOrderBy;

            if (quoteColumns)
                columnList = QuoteFieldList(columnList);

			string[] columnNames = new string[columnList.Length];

			for (int i = 0; i < columnList.Length; i++)
			{
				columnNames[i] = columnList[i];
				
				if (columnAliasList != null)
					columnNames[i] += " " + columnAliasList[i];
			}

	        sqlColumns = String.Join(",", columnNames);
	        sqlFromTable = " FROM " + QuoteTable(tableName) + " " + tableAlias;

            if (joinList != null && joinList.Length > 0)
                sqlJoins = " " + String.Join(" ", joinList);
            else
                sqlJoins = "";

            if (!string.IsNullOrEmpty(whereClause))
	            sqlWhere = " WHERE " + whereClause;
            else
	            sqlWhere = "";

            if (!string.IsNullOrEmpty(orderBy))
                sqlOrderBy = " ORDER BY " + orderBy;
            else
                sqlOrderBy = "";

            if (startRow > 1) // Paging implementation is dirty, but it works. Too bad VistaDB doesn't support LIMIT or ROW_NUMBER() functionality
            {
                if ((orderBy ?? "").Length < 1)
                    throw new CSException("When selecting a range, a sort order is required");

                int count = 0;

                orderBy = Regex.Replace(orderBy, @"[a-z]+\.\[[^\]]+\]",
                              delegate(Match m)
                                  {
                                      int found = -1;

                                      for (int i=0;i<columnList.Length;i++)
                                          if (columnList[i] == m.Value)
                                              found = i;

                                      if (found < 0)
                                      {
                                          string alias = "ORDER" + (++count);

                                          sqlColumns += "," + m.Value + " " + alias;

                                          return alias;
                                      }

                                      return columnAliasList[found];
                                  });


                string reverseOrderBy = ReverseOrderBy(orderBy);

                string innerSql = "select " + sqlColumns + sqlFromTable + sqlJoins + sqlWhere;

                return "select " + string.Join(",", columnAliasList) + " from (select top " + maxRows + " * from (select top " + (startRow + maxRows - 1) + " * from (" + innerSql + ") t2 order by " + orderBy + ") t1 order by " + reverseOrderBy + ") t0" + (unOrdered ? "" : (" ORDER BY " + orderBy));
            }
	        
            string sql = "SELECT";

	        if (maxRows > 0)
	            sql += " TOP " + maxRows;

	        return sql + " " + sqlColumns + sqlFromTable + sqlJoins + sqlWhere + (unOrdered ? "" : sqlOrderBy);
	    }

	    protected override string BuildInsertSQL(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string[] sequences, string identityField)
        {
            string sql;

            if (columnList.Length > 0)
            {
                sql = String.Format("insert into {0} ({1}) values ({2})",
                                    QuoteTable(tableName),
                                    String.Join(",", QuoteFieldList(columnList)),
                                    String.Join(",", valueList)
                                    );
            }
            else
            {
                sql = String.Format("insert into {0} default values",QuoteTable(tableName));
            }

            if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
                sql += String.Format(";SELECT {0} from {1} where {2} = @@IDENTITY", String.Join(",",QuoteFieldList(primaryKeys)),QuoteTable(tableName),identityField);

            return sql;
        }

		protected override string QuoteField(string fieldName) 
		{
			int dotIdx = fieldName.IndexOf('.');

			if (dotIdx > 0)
				return fieldName.Substring(0, dotIdx + 1) + "[" + fieldName.Substring(dotIdx + 1) + "]";
			else
				return "[" + fieldName + "]";
		}

		protected override string QuoteTable(string tableName) { return "[" + tableName.Replace(".", "].[") + "]"; }

		protected override string NativeFunction(string functionName, ref string[] parameters)
        {
            switch (functionName.ToUpper())
            {
                default: return functionName.ToUpper();
            }
        }

		protected override bool SupportsNestedTransactions
		{
			get { return false; }
		}

	    protected override bool SupportsSequences
	    {
	        get { return false; }
	    }

	    protected override bool SupportsMultipleStatements
	    {
	        get { return true; }
	    }

	    protected override bool RequiresSeperateIdentityGet
	    {
	        get { return false; }
	    }
	}
}

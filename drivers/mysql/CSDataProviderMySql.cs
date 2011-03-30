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
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Data;

namespace Vici.CoolStorage
{
	public class CSDataProviderMySql : CSDataProvider
	{
        
        
		public CSDataProviderMySql(string connectionString) : base(connectionString)
		{
            
		}

		protected override IDbConnection CreateConnection()
		{
			MySqlConnection conn = new MySqlConnection(ConnectionString);

			conn.Open();

			return conn;
		}

        protected override void ClearConnectionPool()
        {
            MySqlConnection.ClearAllPools();
        }

		protected override CSDataProvider Clone()
		{
			return new CSDataProviderMySql(ConnectionString);
		}

		protected override IDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
		{
			MySqlCommand mySqlCommand = (MySqlCommand)Connection.CreateCommand();

			mySqlCommand.Transaction = (MySqlTransaction)CurrentTransaction;


            if (sqlQuery.StartsWith("!"))
            {
                mySqlCommand.CommandType = CommandType.StoredProcedure;
                mySqlCommand.CommandText = sqlQuery.Substring(1);
            }
            else
            {
                mySqlCommand.CommandType = CommandType.Text;
                mySqlCommand.CommandText = sqlQuery;
            }

			mySqlCommand.CommandText = Regex.Replace(sqlQuery, @"@(?<name>[a-z0-9A-Z_]+)", "?${name}");

			if (parameters != null && !parameters.IsEmpty)
				foreach (CSParameter parameter in parameters)
				{
					IDbDataParameter dataParameter = mySqlCommand.CreateParameter();

					dataParameter.ParameterName = "?" + parameter.Name.Substring(1);
					dataParameter.Direction = ParameterDirection.Input;
					dataParameter.Value = ConvertParameter(parameter.Value);

					mySqlCommand.Parameters.Add(dataParameter);
				}

			return mySqlCommand;
		}

		protected override string QuoteField(string fieldName) { return "`" + fieldName.Replace(".", "`.`") + "`"; }
		protected override string QuoteTable(string tableName) { return "`" + tableName.Replace(".", "`.`") + "`"; }

		protected override string NativeFunction(string functionName, ref string[] parameters)
		{
			switch (functionName.ToUpper())
			{
				case "LEN": return "LENGTH";
				default: return functionName.ToUpper();
			}
		}

        protected override string BuildSelectSQL(string tableName, string tableAlias, string[] columnList, string[] columnAliasList, string[] joinList, string whereClause, string orderBy, int startRow, int maxRows, bool quoteColumns, bool unOrdered)
        {
            string sql = "select";

            if (quoteColumns)
                columnList = QuoteFieldList(columnList);

            string[] columnNames = new string[columnList.Length];

            for (int i = 0; i < columnList.Length; i++)
            {
                columnNames[i] = columnList[i];

                if (columnAliasList != null)
                    columnNames[i] += " " + columnAliasList[i];
            }

            sql += " " + String.Join(",", columnNames);

            sql += " from " + QuoteTable(tableName) + " " + tableAlias;

            if (joinList != null && joinList.Length > 0)
                foreach (string joinExpression in joinList)
                    sql += " " + joinExpression;

            if (!string.IsNullOrEmpty(whereClause))
                sql += " where " + whereClause;

            if (!string.IsNullOrEmpty(orderBy))
                sql += " order by " + orderBy;

            if (maxRows > 0)
            {
                sql += " limit ";

                if (startRow > 1)
                    sql += (startRow-1) + ",";

                sql += maxRows;
            }

            return sql;

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
                sql = String.Format("insert into {0} () values ()", QuoteTable(tableName));
            }

            if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
                sql += String.Format(";SELECT {0} from {1} where {2} = last_insert_id()", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName), identityField);

            return sql;
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

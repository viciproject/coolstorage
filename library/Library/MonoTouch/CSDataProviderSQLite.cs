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
using Vici.Core;
using Mono.Data.Sqlite;
using System.Data;

namespace Vici.CoolStorage
{
    public class CSDataProviderSQLite : CSDataProvider
    {
        public CSDataProviderSQLite(string connectionString)
            : base(connectionString)
        {
        }

        protected override IDbConnection CreateConnection()
        {
			
            SqliteConnection conn = new SqliteConnection(ConnectionString);

            conn.Open();
            
            return conn;
        }

        protected override void ClearConnectionPool()
        {
            SqliteConnection.ClearAllPools();
        }

        protected internal override CSDataProvider Clone()
        {
            return new CSDataProviderSQLite(ConnectionString);
        }

        protected override IDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
        {
            SqliteCommand sqlCommand = (SqliteCommand) Connection.CreateCommand();

            sqlCommand.Transaction = (SqliteTransaction)CurrentTransaction;

            sqlCommand.CommandType = CommandType.Text;
 
            sqlCommand.CommandText = Regex.Replace(sqlQuery, @"@(?<name>[a-z0-9A-Z_]+)", "@${name}");

            if (parameters != null && !parameters.IsEmpty)
                foreach (CSParameter parameter in parameters)
                {
                    IDbDataParameter dataParameter = sqlCommand.CreateParameter();

                    dataParameter.ParameterName = "@" + parameter.Name.Substring(1);
                    dataParameter.Direction = ParameterDirection.Input;
                    dataParameter.Value = ConvertParameter(parameter.Value);

                    sqlCommand.Parameters.Add(dataParameter);
                }

            return sqlCommand;
        }

        protected internal override string QuoteField(string fieldName) { return "\"" + fieldName.Replace(".","\".\"") + "\""; }
        protected internal override string QuoteTable(string tableName) { return "\"" + tableName + "\""; }

        protected internal override string NativeFunction(string functionName, ref string[] parameters)
        {
            switch (functionName.ToUpper())
            {
                case "LEN": return "LENGTH";
                case "LEFT": return "SUBSTR(" + parameters[0] + ",1," + parameters[1] + ")";
                default: return functionName.ToUpper();
            }
        }

		protected internal override DataTable GetSchemaTable (string tableName)
		{
			DataTable dataTable = new DataTable();
			
			dataTable.Columns.Add("IsKey",typeof(bool));
			dataTable.Columns.Add("AllowDBNull",typeof(bool));
			dataTable.Columns.Add("ColumnName",typeof(string));
			dataTable.Columns.Add("DataType",typeof(Type));
			dataTable.Columns.Add("IsReadOnly",typeof(bool));
			dataTable.Columns.Add("ColumnSize",typeof(int));
			dataTable.Columns.Add("IsAutoIncrement",typeof(bool));
			
			List<string> autoColumns = new List<string>();
			
			using (SqliteCommand cmd = (SqliteCommand) CreateCommand("select sql from sqlite_master where type='table' and name=@tablename",
			                                                         new CSParameterCollection("@tablename",tableName)))
			{
				string sql = (string) cmd.ExecuteScalar();
			
				Regex regex = new Regex(@"[\(,]\s*(?<column>[a-z0-9_]+).*?AUTOINCREMENT",RegexOptions.IgnoreCase);
				
				Match m = regex.Match(sql);
				
				if (m.Success)
				{
					autoColumns.Add(m.Groups["column"].Value.ToUpper());
				}

			}

			
			using (SqliteCommand cmd = (SqliteCommand) CreateCommand("pragma table_info (" + tableName + ")",null))
			{
				using (SqliteDataReader reader = cmd.ExecuteReader()) 
				{
					while(reader.Read())
					{
						DataRow row = dataTable.NewRow();

						string columnName = (string) reader["name"];
						
						row["IsKey"] = reader["pk"].Convert<bool>();
						row["AllowDBNull"] = !reader["notnull"].Convert<bool>();
						row["ColumnName"] = columnName;
						row["IsReadOnly"] = false;
						row["ColumnSize"] = 1000;
						
						
						Type dataType = null;
						
						string dbType = (string) reader["type"];
						
						int paren = dbType.IndexOf('(');
						
						if (paren > 0)
							dbType = dbType.Substring(0,paren);
						
						dbType = dbType.ToUpper();
						
						switch (dbType) 
						{
						case "TEXT": dataType = typeof(string); break;
						case "VARCHAR": dataType = typeof(string); break;
						case "INTEGER": dataType = typeof(int); break;
						case "BOOL": dataType = typeof(bool); break;
						case "DOUBLE": dataType = typeof(double); break;
						case "FLOAT": dataType = typeof(double); break;
						case "REAL": dataType = typeof(double); break;
						case "CHAR": dataType = typeof(string); break;
						case "BLOB": dataType = typeof(byte[]); break;
						case "NUMERIC": dataType = typeof(decimal); break;
						case "DATETIME": dataType = typeof(DateTime); break;
						
						}

						row["DataType"] = dataType;
						
						row["IsAutoIncrement"] = autoColumns.Contains(columnName.ToUpper());
							
							
						dataTable.Rows.Add(row);
					}
				}
			}
			
			

			
			return dataTable;
			
		}

		
        protected internal override string BuildSelectSQL(string tableName, string tableAlias, string[] columnList, string[] columnAliasList, string[] joinList, string whereClause, string orderBy, int startRow, int maxRows, bool quoteColumns, bool unOrdered)
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
                sql += " limit " + maxRows;

            if (startRow > 1)
                sql += " offset " + (startRow - 1);

            return sql;

        }

        protected internal override string BuildInsertSQL(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string[] sequences, string identityField)
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
                sql = String.Format("insert into {0} default values", QuoteTable(tableName));
            }

            if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
                sql += String.Format(";SELECT {0} from {1} where {2} = last_insert_rowid()", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName), identityField);

            return sql;
        }

        protected internal override bool SupportsNestedTransactions
        {
            get { return false; }
        }

        protected internal override bool SupportsSequences
        {
            get { return false; }
        }

        protected internal override bool SupportsMultipleStatements
        {
            get { return true; }
        }

        protected internal override bool RequiresSeperateIdentityGet
        {
            get { return false; }
        }
    }
}

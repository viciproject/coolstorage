#region License
//=============================================================================
// Vici CoolStorage - .NET Object Relational Mapping Library 
//
// Copyright (c) 2004-2011 Philippe Leybaert
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
using Vici.Core;


#endregion

using System;
using System.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Data.Sqlite;

namespace Vici.CoolStorage
{
    public class CSDataProviderSqlite : CSDataProvider
    {
        public CSDataProviderSqlite(string connectionString)
            : base(connectionString)
        {
        }

        public CSDataProviderSqlite(string fileName, bool useDateTimeTicks)
            : base("Data Source=" + fileName + ";DateTimeFormat=" + (useDateTimeTicks ? "Ticks" : "ISO8601"))
        {
        }

        protected override object NullFieldValue()
        {
            return DBNull.Value;
        }

        protected override ICSDbConnection CreateConnection()
        {
            var connection = new SqliteConnection(ConnectionString);

            connection.Open();

            return new CSSqliteConnection(connection);
        }

        protected override void ClearConnectionPool()
        {
            SqliteConnection.ClearAllPools();
        }

        protected override CSDataProvider Clone()
        {
            return new CSDataProviderSqlite(ConnectionString);
        }

        protected override ICSDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
        {
            SqliteCommand sqlCommand = ((CSSqliteCommand)Connection.CreateCommand()).Command;

            if (CurrentTransaction != null)
                sqlCommand.Transaction = ((CSSqliteTransaction)CurrentTransaction).Transaction;

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

            return new CSSqliteCommand(sqlCommand);
        }

        protected override string QuoteField(string fieldName) { return "\"" + fieldName.Replace(".", "\".\"") + "\""; }
        protected override string QuoteTable(string tableName) { return "\"" + tableName + "\""; }

        protected override string NativeFunction(string functionName, ref string[] parameters)
        {
            switch (functionName.ToUpper())
            {
                case "LEN": return "LENGTH";
                case "LEFT": return "SUBSTR(" + parameters[0] + ",1," + parameters[1] + ")";
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
                sql += " limit " + maxRows;

            if (startRow > 1)
                sql += " offset " + (startRow - 1);

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
                sql = String.Format("insert into {0} default values", QuoteTable(tableName));
            }

            if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
                sql += String.Format(";SELECT {0} from {1} where {2} = last_insert_rowid()", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName), identityField);

            return sql;
        }

        protected override CSSchemaColumn[] GetSchemaColumns(string tableName)
        {
            using (ICSDbConnection newConn = CreateConnection())
            {
				var schemaColumns = new List<CSSchemaColumn>();

				DataTable schemaTable = ((CSSqliteConnection)newConn).GetSchema(tableName);

                bool hasHidden = schemaTable.Columns.Contains("IsHidden");
                bool hasIdentity = schemaTable.Columns.Contains("IsIdentity");
                bool hasAutoincrement = schemaTable.Columns.Contains("IsAutoIncrement");

                foreach (DataRow schemaRow in schemaTable.Rows)
                {
                    var schemaColumn = new CSSchemaColumn();

                    if (hasHidden && !schemaRow.IsNull("IsHidden") && (bool)schemaRow["IsHidden"])
                        schemaColumn.Hidden = true;

                    schemaColumn.IsKey = (bool)schemaRow["IsKey"];
                    schemaColumn.AllowNull = (bool)schemaRow["AllowDBNull"];
                    schemaColumn.Name = (string)schemaRow["ColumnName"];
                    schemaColumn.ReadOnly = (bool)schemaRow["IsReadOnly"];
                    schemaColumn.DataType = (Type)schemaRow["DataType"];
                    schemaColumn.Size = (int)schemaRow["ColumnSize"];

                    if (hasAutoincrement && !schemaRow.IsNull("IsAutoIncrement") && (bool)schemaRow["IsAutoIncrement"])
                        schemaColumn.Identity = true;

                    if (hasIdentity && !schemaRow.IsNull("IsIdentity") && (bool)schemaRow["IsIdentity"])
                        schemaColumn.Identity = true;

                    schemaColumns.Add(schemaColumn);
                }

                return schemaColumns.ToArray();
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

        private class CSSqliteConnection : ICSDbConnection
        {
            public SqliteConnection Connection;

            public CSSqliteConnection(SqliteConnection connection)
            {
                Connection = connection;
            }

            public void Close()
            {
                Connection.Close();
            }

			public DataTable GetSchema(string tableName)
			{
				DataTable dataTable = new DataTable();

				dataTable.Columns.Add("IsKey",typeof(bool));
				dataTable.Columns.Add("AllowDBNull",typeof(bool));
				dataTable.Columns.Add("ColumnName",typeof(string));
				dataTable.Columns.Add("DataType",typeof(Type));
				dataTable.Columns.Add("IsReadOnly",typeof(bool));
				dataTable.Columns.Add("ColumnSize",typeof(int));
				dataTable.Columns.Add("IsAutoIncrement",typeof(bool));

				var autoColumns = new List<string>();

				using (var sqliteCommand = Connection.CreateCommand())
				{
					sqliteCommand.CommandText = "select sql from sqlite_master where type='table' and name=@tablename";
					sqliteCommand.Parameters.Add(new SqliteParameter("tablename",tableName));
					
					string sql = (string) sqliteCommand.ExecuteScalar();
	
					var regex = new Regex(@"[\(,]\s*((?<column>[A-Za-z][A-Za-z0-9]*)|""(?<column>[^""]+)"")\s+.*?AUTOINCREMENT",RegexOptions.IgnoreCase);
	
					var match = regex.Match(sql);
	
					if (match.Success)
					{
						autoColumns.Add(match.Groups["column"].Value.ToUpper());
					}
				}

                //TODO: implement workaround shown above
				using (SqliteCommand cmd = Connection.CreateCommand())
				{
					cmd.CommandText = "pragma table_info (" + tableName + ")";
					
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

            public bool IsOpenAndReady()
            {
                return Connection.State == ConnectionState.Open;
            }

            public bool IsClosed()
            {
                return Connection.State == ConnectionState.Closed;
            }

            public ICSDbTransaction BeginTransaction(CSIsolationLevel isolationLevel)
            {
                return new CSSqliteTransaction(Connection.BeginTransaction((IsolationLevel) isolationLevel));
            }

            public ICSDbTransaction BeginTransaction()
            {
                return new CSSqliteTransaction(Connection.BeginTransaction());
            }

            public ICSDbCommand CreateCommand()
            {
                return new CSSqliteCommand(Connection.CreateCommand());
            }

            public void Dispose()
            {
                Connection.Dispose();
            }
        }

        private class CSSqliteCommand : ICSDbCommand
        {
            public readonly SqliteCommand Command;

            public CSSqliteCommand(SqliteCommand command)
            {
                Command = command;
            }

            public string CommandText
            {
                get { return Command.CommandText; }
                set { Command.CommandText = value; }
            }

            public int CommandTimeout
            {
                get { return Command.CommandTimeout; }
                set { Command.CommandTimeout = value; }
            }

            public ICSDbReader ExecuteReader(CSCommandBehavior commandBehavior)
            {
                return new CSSqliteReader(Command.ExecuteReader((CommandBehavior) commandBehavior));
            }

            public ICSDbReader ExecuteReader()
            {
                return new CSSqliteReader(Command.ExecuteReader());
            }

            public int ExecuteNonQuery()
            {
                return Command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                Command.Dispose();
            }
        }

        private class CSSqliteTransaction : ICSDbTransaction
        {
            public readonly SqliteTransaction Transaction;

            public CSSqliteTransaction(SqliteTransaction transaction)
            {
                Transaction = transaction;
            }

            public void Dispose()
            {
                Transaction.Dispose();
            }

            public void Commit()
            {
                Transaction.Commit();
            }

            public void Rollback()
            {
                Transaction.Rollback();
            }
        }

        public class CSSqliteReader : ICSDbReader
        {
            public SqliteDataReader Reader;

            public CSSqliteReader(SqliteDataReader reader)
            {
                Reader = reader;
            }

            public void Dispose()
            {
                Reader.Dispose();
            }

            public DataTable GetSchemaTable()
            {
                return Reader.GetSchemaTable();
            }

            public int FieldCount
            {
                get { return Reader.FieldCount; }
            }

            public string GetName(int i)
            {
                return Reader.GetName(i);
            }

            public bool Read()
            {
                return Reader.Read();
            }

            public bool IsClosed
            {
                get { return Reader.IsClosed; }
            }

            public object this[int i]
            {
                get { return Reader[i]; }
            }
        }

    }
}

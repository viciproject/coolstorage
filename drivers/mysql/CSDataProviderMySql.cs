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

        protected override ICSDbConnection CreateConnection()
        {
            MySqlConnection conn = new MySqlConnection(ConnectionString);

            conn.Open();

            return new CSSqlConnection(conn);
        }

        protected override void ClearConnectionPool()
        {
            MySqlConnection.ClearAllPools();
        }

		protected override CSDataProvider Clone()
		{
			return new CSDataProviderMySql(ConnectionString);
		}

		protected override ICSDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
		{
            MySqlCommand mySqlCommand = ((CSSqlCommand)Connection.CreateCommand()).Command;

            if (CurrentTransaction != null)
                mySqlCommand.Transaction = ((CSSqlTransaction)CurrentTransaction).Transaction;

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

            return new CSSqlCommand(mySqlCommand);
		}

        protected override CSSchemaColumn[] GetSchemaColumns(string tableName)
        {
            using (ICSDbConnection newConn = CreateConnection())
            {
                ICSDbCommand dbCommand = newConn.CreateCommand();

                dbCommand.CommandText = "select * from " + QuoteTable(tableName);

                using (CSSqlReader dataReader = (CSSqlReader)dbCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                {
                    List<CSSchemaColumn> columns = new List<CSSchemaColumn>();

                    DataTable schemaTable = dataReader.Reader.GetSchemaTable();

                    bool hasHidden = schemaTable.Columns.Contains("IsHidden");
                    bool hasIdentity = schemaTable.Columns.Contains("IsIdentity");
                    bool hasAutoincrement = schemaTable.Columns.Contains("IsAutoIncrement");

                    foreach (DataRow schemaRow in schemaTable.Rows)
                    {
                        CSSchemaColumn schemaColumn = new CSSchemaColumn();

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

                        columns.Add(schemaColumn);
                    }

                    return columns.ToArray();
                }
            }
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

        private class CSSqlConnection : ICSDbConnection
        {
            public readonly MySqlConnection Connection;

            public CSSqlConnection(MySqlConnection connection)
            {
                Connection = connection;
            }

            public void Close()
            {
                Connection.Close();
            }

            public bool IsOpenAndReady()
            {
                return Connection.State == ConnectionState.Open;
            }

            public bool IsClosed()
            {
                return Connection.State == ConnectionState.Closed;
            }

            public ICSDbTransaction BeginTransaction(IsolationLevel isolationLevel)
            {
                return new CSSqlTransaction(Connection.BeginTransaction(isolationLevel));
            }

            public ICSDbTransaction BeginTransaction()
            {
                return new CSSqlTransaction(Connection.BeginTransaction());
            }

            public ICSDbCommand CreateCommand()
            {
                return new CSSqlCommand(Connection.CreateCommand());
            }

            public void Dispose()
            {
                Connection.Dispose();
            }
        }

        private class CSSqlCommand : ICSDbCommand
        {
            public readonly MySqlCommand Command;

            public CSSqlCommand(MySqlCommand command)
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

            public ICSDbReader ExecuteReader(CommandBehavior commandBehavior)
            {
                return new CSSqlReader(Command.ExecuteReader(commandBehavior));
            }

            public ICSDbReader ExecuteReader()
            {
                return new CSSqlReader(Command.ExecuteReader());
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

        private class CSSqlTransaction : ICSDbTransaction
        {
            public readonly MySqlTransaction Transaction;

            public CSSqlTransaction(MySqlTransaction transaction)
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

        private class CSSqlReader : ICSDbReader
        {
            public readonly MySqlDataReader Reader;

            public CSSqlReader(MySqlDataReader reader)
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

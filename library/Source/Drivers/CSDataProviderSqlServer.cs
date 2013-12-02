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
using System.Data.SqlClient;
using Vici.Core;

namespace Vici.CoolStorage
{
	public class CSDataProviderSqlServer : CSDataProvider
	{
	    private int _serverVersion = 0;

		public CSDataProviderSqlServer(string connectionString) : base(connectionString)
		{
		}

	    protected internal override object NullFieldValue()
	    {
	        return DBNull.Value;
	    }

	    protected override ICSDbConnection CreateConnection()
		{
			var sqlConnection = new SqlConnection(ConnectionString);

			sqlConnection.Open();

		    _serverVersion = Convert.ToInt32(sqlConnection.ServerVersion.Substring(0, 2));

			return new CSSqlConnection(sqlConnection);
		}

        protected override void ClearConnectionPool()
        {
            SqlConnection.ClearAllPools();
        }

		public CSDataProviderSqlServer(string serverName, string databaseName)
			: this("Initial Catalog=" + databaseName + ";Data Source=" + serverName + ";Integrated Security=true;")
		{
		}

		protected internal override CSDataProvider Clone()
		{
			return new CSDataProviderSqlServer(ConnectionString);
		}

		protected override ICSDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
		{
			SqlCommand sqlCommand = ((CSSqlCommand) Connection.CreateCommand()).Command;

            if (CurrentTransaction != null)
			    sqlCommand.Transaction = ((CSSqlTransaction) CurrentTransaction).Transaction;

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

			return new CSSqlCommand(sqlCommand);
		}
        
        protected internal override CSSchemaColumn[] GetSchemaColumns(string tableName)
        {
            using (ICSDbConnection newConn = CreateConnection())
            {
                ICSDbCommand dbCommand = newConn.CreateCommand();

                dbCommand.CommandText = "select * from " + QuoteTable(tableName);

                using (var dataReader = (CSSqlReader) dbCommand.ExecuteReader(CSCommandBehavior.SchemaOnly | CSCommandBehavior.KeyInfo))
                {
                    var schemaColumns = new List<CSSchemaColumn>();

                    DataTable schemaTable = dataReader.Reader.GetSchemaTable();

                    bool hasHidden = schemaTable.Columns.Contains("IsHidden");
                    bool hasIdentity = schemaTable.Columns.Contains("IsIdentity");
                    bool hasAutoincrement = schemaTable.Columns.Contains("IsAutoIncrement");

                    foreach (DataRow schemaRow in schemaTable.Rows)
                    {
                        var schemaColumn = new CSSchemaColumn();

                        if (hasHidden && !schemaRow.IsNull("IsHidden") && (bool)schemaRow["IsHidden"])
                            schemaColumn.Hidden = true;

                        if (hasIdentity && !schemaRow.IsNull("IsIdentity") && (bool)schemaRow["IsIdentity"])
                            schemaColumn.Identity = true;

                        schemaColumn.IsKey = (bool)schemaRow["IsKey"];
                        schemaColumn.AllowNull = (bool)schemaRow["AllowDBNull"];
                        schemaColumn.Name = (string)schemaRow["ColumnName"];
                        schemaColumn.ReadOnly = (bool)schemaRow["IsReadOnly"];
                        schemaColumn.DataType = (Type)schemaRow["DataType"];
                        schemaColumn.Size = (int)schemaRow["ColumnSize"];

                        if (hasAutoincrement && !schemaRow.IsNull("IsAutoIncrement") && (bool)schemaRow["IsAutoIncrement"])
                            schemaColumn.Identity = true;


                        schemaColumns.Add(schemaColumn);
                    }

                    return schemaColumns.ToArray();
                }
            }
        }

	    protected internal override string BuildSelectSQL(string tableName, string tableAlias, string[] columnList, string[] columnAliasList, string[] joinList, string whereClause, string orderBy, int startRow, int maxRows, bool quoteColumns, bool unOrdered)
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


            if (startRow > 1)
            {
                if ((orderBy ?? "").Length < 1)
                    throw new CSException("When selecting a range, a sort order is required");

                if (_serverVersion < 9)
                    throw new CSException("Paging is not supported on SQL Server 2000 or earlier versions");

                return "select " 
                        + String.Join(",", columnAliasList) 
                        + " from (select row_number() over (ORDER BY " + orderBy + ") rownumber, " 
                                + sqlColumns 
                                + sqlFromTable 
                                + sqlJoins 
                                + sqlWhere
                         + ")  " 
                         + tableAlias + "XXX" 
                         + " where rownumber between " + startRow + " and " + (startRow + maxRows - 1) + (unOrdered ? "" : " order by rownumber");
            }
            else
            {
                string sql = "SELECT";

                if (maxRows > 0)
                    sql += " TOP " + maxRows;

                return sql + " " + sqlColumns + sqlFromTable + sqlJoins + sqlWhere + (unOrdered ? "" : sqlOrderBy);
            }
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
                sql += String.Format(";SELECT {0} from {1} where {2} = SCOPE_IDENTITY()",
                                     String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName), identityField);

            return sql;
        }

	    protected internal override string QuoteField(string fieldName) 
		{
			int dotIdx = fieldName.IndexOf('.');

			if (dotIdx > 0)
				return fieldName.Substring(0, dotIdx + 1) + "[" + fieldName.Substring(dotIdx + 1) + "]";
			else
				return "[" + fieldName + "]";
		}

		protected internal override string QuoteTable(string tableName) { return "[" + tableName.Replace(".", "].[") + "]"; }

        protected internal override string NativeFunction(string functionName, ref string[] parameters)
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

        protected internal override bool SupportsSequences
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
            public readonly SqlConnection Connection;

            public CSSqlConnection(SqlConnection connection)
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

            public ICSDbTransaction BeginTransaction(CSIsolationLevel isolationLevel)
            {
                return new CSSqlTransaction(Connection.BeginTransaction(isolationLevel.Native()));
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
            public readonly SqlCommand Command;

            public CSSqlCommand(SqlCommand command)
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
                return new CSSqlReader(Command.ExecuteReader(commandBehavior.Native()));
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
            public readonly SqlTransaction Transaction;

            public CSSqlTransaction(SqlTransaction transaction)
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
            public readonly SqlDataReader Reader;

            public CSSqlReader(SqlDataReader reader)
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

    public static class CSEnumConverter
    {
        public static IsolationLevel Native(this CSIsolationLevel isolationLevel)
        {
            return (IsolationLevel) (int) isolationLevel;
        }

        public static CommandBehavior Native(this CSCommandBehavior commandBehavior)
        {
            return (CommandBehavior) (int) commandBehavior;
        }
    }
}

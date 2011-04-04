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
using System.Data;
using System.Data.OleDb;
using System.Text;
using System.Text.RegularExpressions;

namespace Vici.CoolStorage
{
    public class CSDataProviderAccess : CSDataProvider
    {
        public CSDataProviderAccess(string fileName)
            : base(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileName + ";")
        {
        }

        protected override ICSDbConnection CreateConnection()
        {
            OleDbConnection conn = new OleDbConnection(ConnectionString);

            conn.Open();
            
            return new CSAccessConnection(conn);
        }

        protected internal override CSDataProvider Clone()
        {
            return new CSDataProviderAccess(ConnectionString);
        }

        protected override ICSDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
        {
            OleDbCommand dbCommand = ((CSAccessCommand)Connection.CreateCommand()).Command;

            dbCommand.Transaction = ((CSAccessTransaction)CurrentTransaction).Transaction;

            foreach (Match m in Regex.Matches(sqlQuery, "(?<!@)@[a-z_0-9]+", RegexOptions.IgnoreCase))
            {
                dbCommand.Parameters.AddWithValue(m.Value, ConvertParameter(parameters[m.Value].Value));
            }

            sqlQuery = Regex.Replace(sqlQuery, "(?<!@)@[a-z_0-9]+", "?", RegexOptions.IgnoreCase);

            dbCommand.CommandType = CommandType.Text;
            dbCommand.CommandText = sqlQuery;

            return new CSAccessCommand(dbCommand);
        }

        protected internal override string QuoteField(string fieldName) { return "[" + fieldName + "]"; }
        protected internal override string QuoteTable(string tableName) { return "[" + tableName + "]"; }

        protected internal override string NativeFunction(string functionName, ref string[] parameters)
        {
            switch (functionName.ToUpper())
            {
                default: return functionName.ToUpper();
            }
        }

        protected internal override bool SupportsNestedTransactions
        {
            get { return false; }
        }

        protected internal override string BuildGetKeys(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string identityField)
        {
            int id;

            using (ICSDbReader reader = CreateReader("SELECT @@IDENTITY", null))
            {
                reader.Read();

                id = (int)reader[0];
            }

            if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
                return String.Format("SELECT {0} from {1} where {2} = " + id, String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName), identityField);

            return "";
        }

        protected internal override string BuildInsertSQL(string tableName, string[] columnList, string[] valueList,
                                                          string[] primaryKeys, string[] sequences, string identityField)
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

            return sql;

        }

        protected internal override string BuildSelectSQL(string tableName, string tableAlias, string[] columnList, string[] columnAliasList, string[] joinList, string whereClause, string orderBy, int startRow, int maxRows, bool quoteColumns, bool unOrdered)
        {
            StringBuilder sql = new StringBuilder(100);

            sql.Append("SELECT");

            if (maxRows > 0)
                sql.Append(" TOP " + maxRows);

            if (quoteColumns)
                columnList = QuoteFieldList(columnList);

            string[] columnNames = new string[columnList.Length];

            for (int i = 0; i < columnList.Length; i++)
            {
                columnNames[i] = columnList[i];

                if (columnAliasList != null)
                    columnNames[i] += " AS " + columnAliasList[i];
            }

            sql.Append(' ');
            sql.Append(String.Join(",", columnNames));
            sql.Append(" FROM ");

            if (joinList != null)
                sql.Append('(', joinList.Length);

            sql.Append(QuoteTable(tableName));
            sql.Append(' ');
            sql.Append(tableAlias);

            if (joinList != null && joinList.Length > 0)
                foreach (string joinExpression in joinList)
                    sql.Append(" " + joinExpression + ")");

            if (!string.IsNullOrEmpty(whereClause))
                sql.Append(" WHERE " + whereClause);

            if (!string.IsNullOrEmpty(orderBy))
                sql.Append(" ORDER BY " + orderBy);

            return sql.ToString();
        }

        protected internal override bool SupportsSequences
        {
            get { return false; }
        }

        protected internal override bool SupportsMultipleStatements
        {
            get { return false; }
        }

        protected internal override bool RequiresSeperateIdentityGet
        {
            get { return true; }
        }

        private class CSAccessConnection : ICSDbConnection
        {
            public readonly OleDbConnection Connection;

            public CSAccessConnection(OleDbConnection connection)
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
                return new CSAccessTransaction(Connection.BeginTransaction(isolationLevel));
            }

            public ICSDbTransaction BeginTransaction()
            {
                return new CSAccessTransaction(Connection.BeginTransaction());
            }

            public ICSDbCommand CreateCommand()
            {
                return new CSAccessCommand(Connection.CreateCommand());
            }

            public void Dispose()
            {
                Connection.Dispose();
            }
        }

        private class CSAccessCommand : ICSDbCommand
        {
            public readonly OleDbCommand Command;

            public CSAccessCommand(OleDbCommand command)
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
                return new CSAccessReader(Command.ExecuteReader(commandBehavior));
            }

            public ICSDbReader ExecuteReader()
            {
                return new CSAccessReader(Command.ExecuteReader());
            }

            public void Dispose()
            {
                Command.Dispose();
            }

            public int ExecuteNonQuery()
            {
                return Command.ExecuteNonQuery();
            }
        }

        private class CSAccessTransaction : ICSDbTransaction
        {
            public readonly OleDbTransaction Transaction;

            public CSAccessTransaction(OleDbTransaction transaction)
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

        private class CSAccessReader : ICSDbReader
        {
            public readonly OleDbDataReader Reader;

            public CSAccessReader(OleDbDataReader reader)
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

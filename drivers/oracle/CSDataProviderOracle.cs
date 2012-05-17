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
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace Vici.CoolStorage
{
    public class CSDataProviderOracle : CSDataProvider
    {
        public CSDataProviderOracle(string connectionString) : base(connectionString)
        {
            
        }

        protected override ICSDbConnection CreateConnection()
        {
            OracleConnection conn = new OracleConnection(ConnectionString);

            conn.Open();

            return new CSOracleConnection(conn);
        }

        protected override void ClearConnectionPool()
        {
            OracleConnection.ClearAllPools();
        }

        protected override CSDataProvider Clone()
        {
            return new CSDataProviderOracle(ConnectionString);
        }

        protected override ICSDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
        {
            OracleCommand oracleCommand = ((CSOracleCommand)Connection.CreateCommand()).Command;

            if (CurrentTransaction != null)
                oracleCommand.Transaction = ((CSOracleTransaction)CurrentTransaction).Transaction;

            oracleCommand.BindByName = true;

            if (sqlQuery.StartsWith("!"))
            {
                oracleCommand.CommandType = CommandType.StoredProcedure;
                oracleCommand.CommandText = sqlQuery.Substring(1);
            }
            else
            {
                oracleCommand.CommandType = CommandType.Text;
                oracleCommand.CommandText = sqlQuery;
            }

            oracleCommand.CommandText = Regex.Replace(sqlQuery, @"@(?<name>[a-z0-9A-Z_]+)", ":${name}");

            if (parameters != null && !parameters.IsEmpty)
                foreach (CSParameter parameter in parameters)
                {
                    OracleParameter dataParameter = oracleCommand.CreateParameter();

                    dataParameter.ParameterName = ":" + parameter.Name.Substring(1);
                    dataParameter.Direction = ParameterDirection.Input;

                    if (parameter.Value is Guid)
                        dataParameter.Value = ((Guid)parameter.Value).ToByteArray();
                    else if (parameter.Value is Boolean)
                        dataParameter.Value = ((Boolean) parameter.Value) ? 1 : 0;
                    else
                        dataParameter.Value = ConvertParameter(parameter.Value);

                    oracleCommand.Parameters.Add(dataParameter);
                }

            return new CSOracleCommand(oracleCommand);
        }

        protected override CSSchemaColumn[] GetSchemaColumns(string tableName)
        {
            using (ICSDbConnection newConn = CreateConnection())
            {
                ICSDbCommand dbCommand = newConn.CreateCommand();

                dbCommand.CommandText = "select * from " + QuoteTable(tableName);

                using (CSOracleReader dataReader = (CSOracleReader)dbCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
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

        protected override string QuoteField(string fieldName)
        {
            int dotIdx = fieldName.IndexOf('.');

            if (dotIdx >= 0)
                return fieldName.Substring(0, dotIdx + 1) + '"' + fieldName.Substring(dotIdx + 1).ToUpper() + '"';
            else
                return '"' + fieldName + '"';
        }

        protected override string QuoteTable(string tableName) { return "\"" + tableName.Replace(".", "\".\"").ToUpper() + "\""; } 

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

            if (maxRows > 0 || startRow > 1)
                return CreatePaged(sql, startRow, maxRows);

            return sql;
        }

        private string CreatePaged(string sql, int startRow, int maxRows)
        {
            startRow = Math.Max(1, startRow);

            if (startRow > 1)
                return string.Format("select * from (select p.*, rownum rnum from ({0}) p where rownum < {2}) where rnum >= {1}",sql,startRow,startRow + (maxRows > 0 ? maxRows : 9999999999));
            else
                return string.Format("select * from ({0}) where rownum <= {1}", sql, maxRows);
        }

        protected override ICSDbReader ExecuteInsert(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string[] sequences, string identityField, CSParameterCollection parameters)
        {
            string sql = "";

            if (columnList.Length > 0)
            {
                List<string> list = new List<string>();

                for (int i = 0; i < valueList.Length; i++)
                {
                    if (sequences != null && sequences[i] != null)
                        list.Add(QuoteField(sequences[i]) + ".nextval");
                    else
                        list.Add(valueList[i]);
                }

                sql += String.Format("insert into {0} ({1}) values ({2})",
                                    QuoteTable(tableName),
                                    String.Join(",", QuoteFieldList(columnList)),
                                    String.Join(",", list.ToArray())
                                    );
            }
            else
            {
                sql += String.Format("insert into {0} () values ()", QuoteTable(tableName));
            }

            sql += " RETURNING rowid INTO :IDVAL";


            long logId = Log(sql, parameters);

            OracleString rowid;

            try
            {
                using (CSOracleCommand cmd = (CSOracleCommand)CreateCommand(sql, parameters))
                {
                    OracleParameter parameter = new OracleParameter(":IDVAL", OracleDbType.Varchar2, 18, "ROWID");

                    parameter.Direction = ParameterDirection.ReturnValue;

                    cmd.Command.Parameters.Add(parameter);

                    cmd.ExecuteNonQuery();

                    rowid = (OracleString)parameter.Value;

                }
            }
            finally
            {
                LogEnd(logId);
            }

            if (primaryKeys == null || primaryKeys.Length == 0)
                return null;

            sql = String.Format("SELECT {0} from {1} where rowid = :IDVAL", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName));

            using (CSOracleCommand cmd = (CSOracleCommand)CreateCommand(sql, null))
            {
                cmd.Command.Parameters.Add(new OracleParameter(":IDVAL", rowid));

                return cmd.ExecuteReader();
            }
        }

        protected override string BuildInsertSQL(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string[] sequences, string identityField)
        {
            string sql = "BEGIN DECLARE IDVAL ROWID; BEGIN ";

            if (columnList.Length > 0)
            {
                List<string> list = new List<string>();

                for (int i=0;i<valueList.Length;i++)
                {
                    if (sequences[i] != null)
                        list.Add(QuoteField(sequences[i]) + ".nextval");
                    else
                        list.Add(valueList[i]);
                }

                sql += String.Format("insert into {0} ({1}) values ({2})",
                                    QuoteTable(tableName),
                                    String.Join(",", QuoteFieldList(columnList)),
                                    String.Join(",", list.ToArray())
                                    );
            }
            else
            {
                sql += String.Format("insert into {0} () values ()", QuoteTable(tableName));
            }

            if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
            {
                int idx = Array.IndexOf(columnList, identityField);

                if (idx >= 0)
                    sql += String.Format(" RETURNING rowid INTO IDVAL;SELECT {0} from {1} where rowid = IDVAL", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName));
            }

            sql += "; END; END;";

            return sql;
        }

        protected override bool SupportsNestedTransactions
        {
            get { return false; }
        }

        protected override bool SupportsSequences
        {
            get { return true; }
        }

        protected override bool SupportsMultipleStatements
        {
            get { return true; }
        }

        protected override bool RequiresSeperateIdentityGet
        {
            get { return false; }
        }

        private class CSOracleConnection : ICSDbConnection
        {
            public readonly OracleConnection Connection;

            public CSOracleConnection(OracleConnection connection)
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
                return new CSOracleTransaction(Connection.BeginTransaction(isolationLevel));
            }

            public ICSDbTransaction BeginTransaction()
            {
                return new CSOracleTransaction(Connection.BeginTransaction());
            }

            public ICSDbCommand CreateCommand()
            {
                return new CSOracleCommand(Connection.CreateCommand());
            }

            public void Dispose()
            {
                Connection.Dispose();
            }
        }

        private class CSOracleCommand : ICSDbCommand
        {
            public readonly OracleCommand Command;

            public CSOracleCommand(OracleCommand command)
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
                return new CSOracleReader(Command.ExecuteReader(commandBehavior));
            }

            public ICSDbReader ExecuteReader()
            {
                return new CSOracleReader(Command.ExecuteReader());
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

        private class CSOracleTransaction : ICSDbTransaction
        {
            public readonly OracleTransaction Transaction;

            public CSOracleTransaction(OracleTransaction transaction)
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

        private class CSOracleReader : ICSDbReader
        {
            public readonly OracleDataReader Reader;

            public CSOracleReader(OracleDataReader reader)
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using Community.CsharpSqlite.SQLiteClient;
using Vici.Core;

namespace Vici.CoolStorage.SqliteWP7
{
    public class CSDataProviderSqliteWP7 : CSDataProvider
    {
        public CSDataProviderSqliteWP7(string connectionString)
            : base(connectionString)
        {
        }

        public CSDataProviderSqliteWP7(string fileName, bool useDateTimeTicks)
            : base("Data Source=" + fileName + ";DateTimeFormat=" + (useDateTimeTicks ? "Ticks":"ISO8601"))
        {
        }

        protected override ICSDbConnection CreateConnection()
        {
            SqliteConnection conn = new SqliteConnection(ConnectionString);

            conn.Open();
            
            return new CSSqliteConnection(conn);
        }

        protected override void ClearConnectionPool()
        {
            //SqliteConnection.ClearAllPools();
        }

        protected override CSDataProvider Clone()
        {
            return new CSDataProviderSqliteWP7(ConnectionString);
        }

        protected override ICSDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
        {
            SqliteCommand sqlCommand = ((CSSqliteCommand)Connection.CreateCommand()).Command;

            if (CurrentTransaction != null)
                sqlCommand.Transaction = ((CSSqliteTransaction)CurrentTransaction).Transaction;

            if (sqlQuery.ToUpper().StartsWith("DELETE ") || sqlQuery.ToUpper().StartsWith("SELECT ") || sqlQuery.ToUpper().StartsWith("UPDATE ") || sqlQuery.ToUpper().StartsWith("INSERT ") || sqlQuery.ToUpper().StartsWith("CREATE "))
                sqlCommand.CommandType = CommandType.Text;
            else
                sqlCommand.CommandType = CommandType.StoredProcedure;

            sqlCommand.CommandText = Regex.Replace(sqlQuery, @"@(?<name>[a-z0-9A-Z_]+)", "@${name}");

            if (parameters != null && !parameters.IsEmpty)
                foreach (CSParameter parameter in parameters)
                {
                    SqliteParameter dataParameter = sqlCommand.CreateParameter();

                    dataParameter.ParameterName = "@" + parameter.Name.Substring(1);
                    dataParameter.Direction = ParameterDirection.Input;
                    dataParameter.Value = ConvertParameter(parameter.Value);

                    sqlCommand.Parameters.Add(dataParameter);
                }

            return new CSSqliteCommand(sqlCommand);
        }

        protected override string QuoteField(string fieldName) { return "\"" + fieldName.Replace(".","\".\"") + "\""; }
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


        protected override CSSchemaColumn[] GetSchemaColumns(string tableName)
        {
            List<string> autoColumns = new List<string>();

            using (
                CSSqliteCommand cmd = (CSSqliteCommand) CreateCommand("select sql from sqlite_master where type='table' and name=@tablename", new CSParameterCollection("@tablename", tableName)))
            {
                string sql = (string) cmd.Command.ExecuteScalar();

                Regex regex = new Regex(@"[\(,]\s*(?<column>[a-z0-9_]+).*?AUTOINCREMENT", RegexOptions.IgnoreCase);

                Match m = regex.Match(sql);

                if (m.Success)
                {
                    autoColumns.Add(m.Groups["column"].Value.ToUpper());
                }

            }

            List<CSSchemaColumn> columns = new List<CSSchemaColumn>();

            using (CSSqliteCommand cmd = (CSSqliteCommand) CreateCommand("pragma table_info (" + tableName + ")", null))
            {
                using (CSSqliteReader reader = (CSSqliteReader) cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CSSchemaColumn column = new CSSchemaColumn();

                        string columnName = (string) reader.Reader["name"];

                        column.Name = columnName;
                        column.IsKey = reader.Reader["pk"].Convert<bool>();
                        column.AllowNull = !reader.Reader["notnull"].Convert<bool>();
                        column.ReadOnly = false;
                        column.Size = 1000;

                        Type dataType = null;

                        string dbType = (string) reader.Reader["type"];

                        int paren = dbType.IndexOf('(');

                        if (paren > 0)
                            dbType = dbType.Substring(0, paren);

                        dbType = dbType.ToUpper();

                        switch (dbType)
                        {
                            case "TEXT":
                                dataType = typeof (string);
                                break;
                            case "VARCHAR":
                                dataType = typeof (string);
                                break;
                            case "INTEGER":
                                dataType = typeof (int);
                                break;
                            case "BOOL":
                                dataType = typeof (bool);
                                break;
                            case "DOUBLE":
                                dataType = typeof (double);
                                break;
                            case "FLOAT":
                                dataType = typeof (double);
                                break;
                            case "REAL":
                                dataType = typeof (double);
                                break;
                            case "CHAR":
                                dataType = typeof (string);
                                break;
                            case "BLOB":
                                dataType = typeof (byte[]);
                                break;
                            case "NUMERIC":
                                dataType = typeof (decimal);
                                break;
                            case "DATETIME":
                                dataType = typeof (DateTime);
                                break;

                        }

                        column.DataType = dataType;
                        column.Identity = autoColumns.Contains(columnName.ToUpper());

                        columns.Add(column);
                    }
                }

                return columns.ToArray();

            }
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
                return new CSSqliteTransaction(Connection.BeginTransaction());
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
            public SqliteCommand Command;

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

            public ICSDbReader ExecuteReader(CommandBehavior commandBehavior)
            {
                return new CSSqliteReader(Command.ExecuteReader(commandBehavior));
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
            public SqliteTransaction Transaction;

            public CSSqliteTransaction(SqliteTransaction transaction)
            {
                Transaction = transaction;
            }

            public void Dispose()
            {
                //Transaction.Dispose();
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

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
//using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;
using System.IO;
using System.Text;

namespace Vici.CoolStorage
{
    public interface ICSDbConnection : IDisposable
    {
        void Close();
        bool IsOpenAndReady();
        bool IsClosed();
        ICSDbTransaction BeginTransaction(IsolationLevel isolationLevel);
        ICSDbTransaction BeginTransaction();
        ICSDbCommand CreateCommand();
    }

    public interface ICSDbReader : IDisposable
    {
        int FieldCount { get; }
        bool IsClosed { get; }
        string GetName(int i);
        bool Read();
        object this[int i] { get; }
    }

    public interface ICSDbCommand : IDisposable
    {
        string CommandText { get; set; }
        int CommandTimeout { get; set; }
        ICSDbReader ExecuteReader(CommandBehavior commandBehavior);
        ICSDbReader ExecuteReader();
        int ExecuteNonQuery();
    }

    public interface ICSDbTransaction : IDisposable
    {
        void Commit();
        void Rollback();
    }

    /// <summary>
    /// Represents a database connection (abstract)
    /// </summary>
    public abstract class CSDataProvider : IDisposable
    {
        private static readonly object _staticLock = new object();
        private static string _lastQuery;
        private static int _numQueries;

        public static int QueryCount
        {
            get { lock (_staticLock) { return _numQueries; } }
            set { lock (_staticLock) { _numQueries = value; } }
        }

        public static string LastQuery
        {
            get { return _lastQuery; }
        }

        protected internal virtual string BuildDeleteSQL(string tableName, string joinList, string whereClause)
        {
            return "delete from " + QuoteTable(tableName) + " " + (joinList ?? "") + " where " + whereClause;
        }

        protected internal virtual string BuildUpdateSQL(string tableName, string[] columnList, string[] valueList, string whereClasuse)
        {
            string sql = "update " + QuoteTable(tableName) + " set ";

            for (int i = 0; i < columnList.Length; i++)
            {
                if (i > 0)
                    sql += ",";

                sql += QuoteField(columnList[i]) + "=" + valueList[i];
            }

            sql += " where " + whereClasuse;

            return sql;
        }

        protected abstract ICSDbConnection CreateConnection();
        protected abstract ICSDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters);
        protected virtual void ClearConnectionPool() {}

        protected internal abstract string BuildInsertSQL(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string[] sequences, string identityField);
        protected internal abstract string BuildSelectSQL(string tableName, string tableAlias, string[] columnList, string[] columnAliasList, string[] joinList, string whereClause, string orderBy, int startRow, int maxRows, bool quoteColumns, bool unOrdered);
        
        protected internal abstract string QuoteField(string fieldName);
        protected internal abstract string QuoteTable(string tableName);
        
        protected internal abstract CSDataProvider Clone();
        protected internal abstract string NativeFunction(string functionName, ref string[] parameters);
        protected internal abstract bool SupportsNestedTransactions { get; }
        protected internal abstract bool SupportsSequences { get; }
        protected internal abstract bool SupportsMultipleStatements { get; }
        protected internal abstract bool RequiresSeperateIdentityGet { get; }

        protected internal virtual string BuildGetKeys(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string identityField)
        {
            throw new NotSupportedException();
        }

        private ICSDbConnection _dbConnection;
        private ICSDbTransaction _currentTransaction;
        private TransactionScope _currentTransactionScope;
        private int _transactionDepth;
        private readonly string _connectionString;

        private readonly Stack<ICSDbTransaction> _transactionStack = new Stack<ICSDbTransaction>();
        private readonly Stack<TransactionScope> _transactionScopeStack = new Stack<TransactionScope>();
        private readonly Stack<bool> _newTransactionStack = new Stack<bool>();

        protected CSDataProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected string ConnectionString
        {
            get { return _connectionString; }
        }

        protected ICSDbConnection Connection
        {
            get
            {
                if (_dbConnection == null)
                    _dbConnection = CreateConnection();

                if (!_dbConnection.IsOpenAndReady())
                {
                    _dbConnection.Dispose();

                    ClearConnectionPool();

                    _dbConnection = CreateConnection();
                }

                return _dbConnection;
            }
        }

        internal void CloseConnection()
        {
            if (_dbConnection != null && !_dbConnection.IsClosed())
                _dbConnection.Close();

            _dbConnection = null;
        }

        protected static object ConvertParameter(object value)
        {
            if (value == null)
                return DBNull.Value;

            if (value is Enum)
                return Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()),null);

            if (value is CSObject)
                return ((CSObject)value).PrimaryKeyValue;

            return value;
        }

        protected internal ICSDbCommand CreateCommandInternal(string sqlQuery, CSParameterCollection parameters)
        {
            ICSDbCommand command = CreateCommand(sqlQuery, parameters);

            if (CSConfig.CommandTimeout.HasValue)
                command.CommandTimeout = CSConfig.CommandTimeout.Value;

            return command;
        }

        protected internal ICSDbReader CreateReader(string sqlQuery, CSParameterCollection parameters)
        {
            long logId = Log(sqlQuery, parameters);

            try
            {
                using (ICSDbCommand dbCommand = CreateCommandInternal(sqlQuery, parameters))
                    return dbCommand.ExecuteReader();
            }
            catch (Exception ex)
            {
                throw new CSSQLException("Error executing query. Possible syntax error", ex, sqlQuery, parameters);
            }
            finally
            {
                LogEnd(logId);

                CSNameGenerator.Reset();
            }
        }

        protected internal virtual ICSDbReader ExecuteInsert(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string[] sequences, string identityField, CSParameterCollection parameters)
        {
            string sqlQuery = BuildInsertSQL(tableName, columnList, valueList, primaryKeys, sequences, identityField);

            if (RequiresSeperateIdentityGet)
            {
                ExecuteNonQuery(sqlQuery, parameters);

                sqlQuery = BuildGetKeys(tableName, columnList, valueList, primaryKeys, identityField);
            }

            if (sqlQuery.Length > 0)
            {
                if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
                    return CreateReader(sqlQuery, parameters);
                else
                    ExecuteNonQuery(sqlQuery, parameters);
            }

            return null;
        }

        protected internal int ExecuteNonQuery(string sqlQuery, CSParameterCollection parameters)
        {
            long logId = Log(sqlQuery, parameters);

            
            try
            {
                using (ICSDbCommand dbCommand = CreateCommandInternal(sqlQuery, parameters))
                    dbCommand.ExecuteNonQuery();

                return 1;
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
            catch (Exception ex)
            {
                throw new CSSQLException("ExecuteNonQuery exception", ex, sqlQuery, parameters);
            }
            finally
            {
                LogEnd(logId);

                CSNameGenerator.Reset();
            }
        }

        internal object GetScalar(string sqlQuery, CSParameterCollection parameters)
        {
            long logId = Log(sqlQuery, parameters);

            try
            {
                using (ICSDbCommand dbCommand = CreateCommandInternal(sqlQuery, parameters))
                {
                    using (ICSDbReader reader = dbCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            object r = reader[0];

                            return (r is DBNull) ? null : r;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new CSSQLException("GetScalar exception", ex, sqlQuery, parameters);
            }
            finally
            {
                LogEnd(logId);

                CSNameGenerator.Reset();
            }
        }

        internal void BeginTransaction()
        {
            _transactionDepth++;

            if (CSConfig.UseTransactionScope)
                _transactionScopeStack.Push(_currentTransactionScope);
            else
                _transactionStack.Push(_currentTransaction);

            _newTransactionStack.Push(false);
        }

        internal void BeginTransaction(IsolationLevel isolationLevel)
        {
            _transactionDepth++;

            if (SupportsNestedTransactions)
            {
                if (CSConfig.UseTransactionScope)
                {
                    _currentTransactionScope = new TransactionScope();

                    _transactionScopeStack.Push(_currentTransactionScope);
                }
                else
                {
                    _currentTransaction = Connection.BeginTransaction(isolationLevel);

                    _transactionStack.Push(_currentTransaction);
                }

                _newTransactionStack.Push(true);
            }
            else
            {
                if (CSConfig.UseTransactionScope)
                {
                    if (_currentTransactionScope != null)
                    {
                        _newTransactionStack.Push(false);
                    }
                    else
                    {
                        _currentTransactionScope = new TransactionScope();

                        _newTransactionStack.Push(true);
                    }

                    _transactionScopeStack.Push(_currentTransactionScope);
                }
                else
                {
                    if (_currentTransaction != null)
                    {
                        _newTransactionStack.Push(false);
                    }
                    else
                    {
                        _currentTransaction = Connection.BeginTransaction();

                        _newTransactionStack.Push(true);
                    }

                    _transactionStack.Push(_currentTransaction);
                }
            }
        }

        internal void Commit()
        {
            bool wasNewTransaction = _newTransactionStack.Pop();

            if (CSConfig.UseTransactionScope)
            {
                TransactionScope transactionScope = _transactionScopeStack.Pop();

                if (wasNewTransaction && transactionScope != null)
                {
                    transactionScope.Complete();
                    transactionScope.Dispose();
                }

                if (_transactionScopeStack.Count > 0)
                    _currentTransactionScope = _transactionScopeStack.Peek();
                else
                    _currentTransactionScope = null;
            }
            else
            {
                ICSDbTransaction transaction = _transactionStack.Pop();

                if (wasNewTransaction && transaction != null)
                {
                    transaction.Commit();
                }

                if (_transactionStack.Count > 0)
                    _currentTransaction = _transactionStack.Peek();
                else
                    _currentTransaction = null;
            }

            _transactionDepth--;

            if (_transactionDepth == 0)
                CloseConnection();
        }

        internal void Rollback()
        {
            bool wasNewTransaction = _newTransactionStack.Pop();

            if (CSConfig.UseTransactionScope)
            {
                TransactionScope transactionScope = _transactionScopeStack.Pop();

                if (wasNewTransaction && transactionScope != null)
                {
                    transactionScope.Dispose();
                }

                if (_transactionScopeStack.Count > 0)
                    _currentTransactionScope = _transactionScopeStack.Peek();
                else
                    _currentTransactionScope = null;
            }
            else
            {
                ICSDbTransaction transaction = _transactionStack.Pop();

                if (wasNewTransaction && transaction != null)
                {
                    transaction.Rollback();
                }

                if (_transactionStack.Count > 0)
                    _currentTransaction = _transactionStack.Peek();
                else
                    _currentTransaction = null;
            }

            _transactionDepth--;

            if (_transactionDepth == 0)
                CloseConnection();
        }

        protected ICSDbTransaction CurrentTransaction
        {
            get
            {
                return _currentTransaction;
            }
        }

        protected internal abstract CSSchemaColumn[] GetSchemaColumns(string tableName);

        private static readonly object _logLock = new object();
        private static long _lastLogId;
        private static readonly Dictionary<long, DateTime> _logTimings = new Dictionary<long, DateTime>();

        protected static long Log(string cmd, CSParameterCollection parameters)
        {
            _numQueries++;
            _lastQuery = cmd;

            long logId;

            lock (_logLock)
            {
                logId = ++_lastLogId;

                if (!CSConfig.Logging)
                    return logId;

                _logTimings.Add(logId, DateTime.Now);

                try
                {
                    using (StreamWriter writer = File.AppendText(CSConfig.LogFileName))
                    {
                        writer.WriteLine("{0} | {2:000000} | {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"), cmd, logId);

                        if (parameters != null && parameters.Count > 0)
                        {
                            StringBuilder p = new StringBuilder();

                            foreach (CSParameter csParameter in parameters)
                            {
                                string value = "<null>";

                                if (csParameter.Value != null)
                                    value = csParameter.Value + "(" + csParameter.Value.GetType().Name + ")";

                                if (value.Length > 30)
                                    value = value.Substring(0, 30) + "...";

                                p.Append(csParameter.Name + "=" + ((csParameter.Value is string) ? "\"" : "") + value + ((csParameter.Value is string) ? "\"" : "") + " | ");
                            }

                            writer.WriteLine("{0} | {2:000000} | {1}", new string(' ', 23), p, logId);
                        }
                    }
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch 
                {
                    // Couldn't care less
                }
                // ReSharper restore EmptyGeneralCatchClause
            }

            return logId;
        }

        protected static void LogEnd(long logId)
        {
            DateTime startTime;

            if (!_logTimings.TryGetValue(logId, out startTime))
                return;

            TimeSpan timeSpan = DateTime.Now - startTime;

            lock (_logLock)
            {
                _logTimings.Remove(logId);

                try
                {
                    using (StreamWriter writer = File.AppendText(CSConfig.LogFileName))
                    {
                        writer.WriteLine("{0} | {1:000000} | TIMING: {2:#,##0} ms", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"), logId, timeSpan.TotalMilliseconds);
                    }
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch
                {
                    // Couldn't care less
                }
                // ReSharper restore EmptyGeneralCatchClause
            }
        }

        protected string[] QuoteFieldList(string[] fields)
        {
            string[] newList = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
                newList[i] = QuoteField(fields[i]);

            return newList;
        }

        #region IDisposable Members

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_dbConnection != null && !_dbConnection.IsClosed())
                _dbConnection.Close();

            _dbConnection = null;
            _disposed = true;
        }

        #endregion
    }
}

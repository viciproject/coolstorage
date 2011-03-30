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
using System.Data;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace Vici.CoolStorage
{
	public class CSDataProviderOracle : CSDataProvider
	{
		public CSDataProviderOracle(string connectionString) : base(connectionString)
		{
            
		}

		protected override IDbConnection CreateConnection()
		{
			OracleConnection conn = new OracleConnection(ConnectionString);

			conn.Open();

			return conn;
		}

		protected override CSDataProvider Clone()
		{
			return new CSDataProviderOracle(ConnectionString);
		}

		protected override IDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
		{
            OracleCommand mySqlCommand = (OracleCommand)Connection.CreateCommand();

			mySqlCommand.Transaction = (OracleTransaction)CurrentTransaction;
		    mySqlCommand.BindByName = true;

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

			mySqlCommand.CommandText = Regex.Replace(sqlQuery, @"@(?<name>[a-z0-9A-Z_]+)", ":${name}");

			if (parameters != null && !parameters.IsEmpty)
				foreach (CSParameter parameter in parameters)
				{
					OracleParameter dataParameter = mySqlCommand.CreateParameter();

					dataParameter.ParameterName = ":" + parameter.Name.Substring(1);
					dataParameter.Direction = ParameterDirection.Input;

                    if (parameter.Value is Guid)
                        dataParameter.Value = ((Guid)parameter.Value).ToByteArray();
                    else if (parameter.Value is Boolean)
                        dataParameter.Value = ((Boolean) parameter.Value) ? 1 : 0;
                    else
                        dataParameter.Value = ConvertParameter(parameter.Value);

					mySqlCommand.Parameters.Add(dataParameter);
				}

			return mySqlCommand;
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

        protected override IDataReader ExecuteInsert(string tableName, string[] columnList, string[] valueList, string[] primaryKeys, string[] sequences, string identityField, CSParameterCollection parameters)
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
                using (IDbCommand cmd = CreateCommand(sql, parameters))
                {
                    OracleParameter parameter = new OracleParameter(":IDVAL", OracleDbType.Varchar2, 18, "ROWID");

                    parameter.Direction = ParameterDirection.ReturnValue;

                    cmd.Parameters.Add(parameter);

                    cmd.ExecuteNonQuery();

                    rowid = (OracleString) parameter.Value;

                }
            }
            finally
            {
                LogEnd(logId);
            }

            if (primaryKeys == null || primaryKeys.Length == 0)
                return null;

            sql = String.Format("SELECT {0} from {1} where rowid = :IDVAL", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName));
            
            using (IDbCommand cmd = CreateCommand(sql,null))
            {
                cmd.Parameters.Add(new OracleParameter(":IDVAL", rowid));

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
                    sql += String.Format(" RETURNING rowid INTO :IDVAL;SELECT {0} from {1} where rowid = :IDVAL", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName));
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
	}
}

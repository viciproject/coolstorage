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
using IBM.Data.DB2;
using System.Data;

namespace Vici.CoolStorage
{
	public class CSDataProviderDB2 : CSDataProvider
	{
		public CSDataProviderDB2(string connectionString) : base(connectionString)
		{
		}

		protected override IDbConnection CreateConnection()
		{
			DB2Connection conn = new DB2Connection(ConnectionString);

			conn.Open();

			return conn;
		}

		protected override CSDataProvider Clone()
		{
			return new CSDataProviderDB2(ConnectionString);
		}

		protected override IDbCommand CreateCommand(string sqlQuery, CSParameterCollection parameters)
		{
            DB2Command dbCommand = (DB2Command)Connection.CreateCommand();

            dbCommand.Transaction = (DB2Transaction)CurrentTransaction;

            if (parameters != null && !parameters.IsEmpty)
            {
				int paramNum = 1;

                foreach (Match m in Regex.Matches(sqlQuery, "@[a-z_0-9]+", RegexOptions.IgnoreCase))
                {
                    if (parameters[m.Value] == null)
                        throw new CSException("Parameter " + m.Value + " undefined");

                    dbCommand.Parameters.Add(new DB2Parameter("@P" + (paramNum++), ConvertParameter(parameters[m.Value].Value)));
                }

                sqlQuery = Regex.Replace(sqlQuery, "@[a-z_0-9]+", "?", RegexOptions.IgnoreCase);
            }

            if (sqlQuery.StartsWith("!"))
            {
                dbCommand.CommandType = CommandType.StoredProcedure;
                dbCommand.CommandText = sqlQuery.Substring(1);
            }
            else
            {
                dbCommand.CommandType = CommandType.Text;
                dbCommand.CommandText = sqlQuery;
            }

            return dbCommand;
        }

		protected override string QuoteField(string fieldName) { return /*"\"" + */fieldName/* + "\""*/; }
		protected override string QuoteTable(string tableName) { return /*"\"" + */tableName/*.Replace(".", "\".\"")*//* + "\""*/ ; }

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

            if (whereClause != null && whereClause.Length > 0)
                sqlWhere = " WHERE " + whereClause;
            else
                sqlWhere = "";

            if (orderBy != null && orderBy.Length > 0)
                sqlOrderBy = " ORDER BY " + orderBy;
            else
                sqlOrderBy = "";


            if (startRow > 1)
            {
                if ((orderBy ?? "").Length < 1)
                    throw new CSException("When selecting a range, a sort order is required");

                return
                    "with orderedTable as (select row_number() over (ORDER BY " + orderBy + ") rownumber, " + sqlColumns + sqlFromTable + sqlJoins + sqlWhere +
                    ") select " + String.Join(",", columnAliasList) + " from orderedTable where rownumber between " +
                    startRow + " and " + (startRow + maxRows - 1) + " order by rownumber";
            }
            else
            {
                string sql = "SELECT";

                if (maxRows > 0)
                    sql += " TOP " + maxRows;

                return sql + " " + sqlColumns + sqlFromTable + sqlJoins + sqlWhere + sqlOrderBy;
            }
        }

		protected override bool SupportsNestedTransactions
		{
			get { return false; }
		}

	    protected override string BuildInsertSQL(string tableName, string[] columnList, string[] valueList,
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
                sql = String.Format("insert into {0} () values ()", QuoteTable(tableName));
            }

            if (primaryKeys != null && primaryKeys.Length > 0 && identityField != null)
                sql += String.Format(";SELECT {0} from {1} where {2} = (SELECT identity_val_local() from sysibm.sysdummy1 fetch first 1 rows only)", String.Join(",", QuoteFieldList(primaryKeys)), QuoteTable(tableName), identityField);

            return sql;
	        
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

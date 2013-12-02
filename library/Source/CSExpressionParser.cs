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

namespace Vici.CoolStorage
{
	internal class CSExpressionParser
	{
		private abstract class Token
		{
		}

		private class LiteralToken : Token
		{
			public readonly string Literal;

			public LiteralToken(string literal)
			{
				Literal = literal;
			}
		}

		private class DBFieldToken : Token
		{
			public readonly string FieldName;

			public DBFieldToken(string fieldName)
			{
				FieldName = fieldName;
			}
		}

		private class DBTableToken : Token
		{
			public readonly string TableName;

			public DBTableToken(string tableName)
			{
				TableName = tableName;
			}
		}

		private class OperatorToken : Token
		{
			public readonly string Operator;

			public OperatorToken(string @operator)
			{
				Operator = @operator;
			}
		}

		private class CommaToken : Token
		{
		}

		private class FunctionToken : Token
		{
			public readonly string Function;

			public FunctionToken(string function)
			{
				Function = function.ToUpper();
			}
		}

		private class ScalarFunctionToken : Token
		{
			public readonly string Function;

			public ScalarFunctionToken(string function)
			{
				Function = function.ToUpper();
			}
		}

		private class VariableToken : Token
		{
			public readonly string Variable;

			public VariableToken(string variable)
			{
				Variable = variable;
			}
		}

		private class LeftParenToken : Token
		{
		}

		private class RightParenToken : Token
		{
		}

		private static readonly Regex _regex;
		private static readonly Regex _regexVar;

		static CSExpressionParser()
		{
			string[] tokenExpressions = new[]
	            {
	                @"(?<literal>'[^']*')",
					@"(?<keyword>\$[a-z0-9]+)",
	                @"(?<oper>\<\>|\<=|\>=|\|\||\bis\b|\blike\b|\bwhere\b|\band\b|\bor\b|\bnot\b|\bin\b|[\+\-\*/=<>&])",
					@"(?<comma>\,)",
	                @"(?<param>@[a-z0-9_]+)",
					@"(?<dbfield>\{[a-z0-9_\.]+\})",
					@"(?<dbtable>\[[a-z0-9_\.]+\])",
					@"(?<constant>\bnull\b)",
	                @"(?<num>\d+(\.\d+)?)",
					@"(?<func>(count|min|max|countdistinct|avg|sum|has|hasno)(?=\s*\())",
	                @"(?<scalarfunc>(len|space|replace|left|right|substring|ltrim|rtrim|lower|upper|abs|sqrt)(?=\s*\())",
	                @"(?<var>[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]+(\.[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]+)*)",
	                @"(?<leftparen>[\(])",
	                @"(?<rightparen>[\)])"
	            };

			_regex = new Regex(string.Join("|", tokenExpressions), RegexOptions.IgnoreCase);
			_regexVar = new Regex(@"((?<Object>[a-z][a-z_0-9]+)\.)*(?<Object>[a-z][a-z_0-9]+)+", RegexOptions.IgnoreCase);
		}

        internal static string ParseFilter(string expressionText, CSSchema schema, string tableAlias, CSJoinList joins)
        {
            return Parse(schema, expressionText, tableAlias, joins);
        }

        internal static string ParseOrderBy(string expression, CSSchema schema, string tableAlias, CSJoinList joinList)
        {
            //joinList = new CSJoinList();

            if (expression.Trim().Length < 1)
                return expression;

            string parsedExpression = "";
            string[] terms = expression.Split(',');

            foreach (string term in terms)
            {
                if (term.Length < 1)
                    continue;

                bool ascending = true;
                string fieldName = term;

                if (term.EndsWith("-"))
                    ascending = false;

                if (term.EndsWith("+") || term.EndsWith("-"))
                    fieldName = term.Substring(0, term.Length - 1);

                if (parsedExpression.Length > 1)
                    parsedExpression += ",";

                parsedExpression += ParseFilter(fieldName, schema, tableAlias, joinList);

                if (!ascending)
                    parsedExpression += " DESC";
            }

            return parsedExpression;
        }

		//public static string Parse(Type t, string input)
		//{
		//    CSJoinList joins;

		//    return Parse(CSSchema.Get(t), input, null, out joins);
		//}

		private static List<Token> PreParse(string input)
		{
			List<Token> tokens = new List<Token>();

			foreach (Match match in _regex.Matches(input))
			{
				if (match.Groups["literal"].Success)
					tokens.Add(new LiteralToken(match.Value));

				else if (match.Groups["oper"].Success)
					tokens.Add(new OperatorToken(match.Value));

				else if (match.Groups["param"].Success)
					tokens.Add(new LiteralToken(match.Value));

				else if (match.Groups["keyword"].Success)
					tokens.Add(new LiteralToken(match.Value.Substring(1)));

				else if (match.Groups["dbfield"].Success)
					tokens.Add(new DBFieldToken(match.Value.Substring(1,match.Length-2)));

				else if (match.Groups["dbtable"].Success)
					tokens.Add(new DBTableToken(match.Value.Substring(1,match.Length-2)));

				else if (match.Groups["comma"].Success)
					tokens.Add(new CommaToken());

				else if (match.Groups["num"].Success)
					tokens.Add(new LiteralToken(match.Value));

				else if (match.Groups["constant"].Success)
					tokens.Add(new LiteralToken(match.Value));

				else if (match.Groups["var"].Success)
					tokens.Add(new VariableToken(match.Value));

				else if (match.Groups["scalarfunc"].Success)
					tokens.Add(new ScalarFunctionToken(match.Value));

				else if (match.Groups["func"].Success)
					tokens.Add(new FunctionToken(match.Value));

				else if (match.Groups["leftparen"].Success)
					tokens.Add(new LeftParenToken());

				else if (match.Groups["rightparen"].Success)
					tokens.Add(new RightParenToken());
			}

			return tokens;
		}

		private static QueryExpression ParseScalarFunction(QueryExpression subQuery, List<Token> tokens, int startToken, int numTokens, ScalarFunctionToken functionToken)
		{
			string[] parameters = new string[0];

			subQuery.Expression += subQuery.Table.Schema.DB.NativeFunction(functionToken.Function,ref parameters) + "(";

			subQuery = Parse(subQuery, tokens, startToken, numTokens);

			subQuery.Expression += ")";

			return subQuery;
		}
		
		private static QueryExpression ParseFunction(QueryExpression subQuery, List<Token> tokens, int startToken, int numTokens, FunctionToken functionToken)
		{
			if (!(tokens[startToken] is VariableToken))
				throw new CSExpressionException("Function call " + functionToken.Function + " called with incorrect parameters");

            QueryExpression newQuery = (QueryExpression) EvalVariable(((VariableToken) tokens[startToken]).Variable, subQuery);

			string fieldExpression;

			switch (functionToken.Function)
			{
				case "HAS": fieldExpression = "*"; break;
				case "COUNT": fieldExpression = "count(*)"; break;
				case "COUNTDISTINCT": fieldExpression = "count(distinct " + newQuery.FieldName + ")"; break;

				default: fieldExpression = functionToken.Function + "(" + newQuery.FieldName + ")"; break;
			}

		    if (numTokens > 1)
		    {
                if (!(tokens[startToken + 1] is OperatorToken))
                    throw new CSExpressionException("Expected WHERE");

                OperatorToken whereToken = (OperatorToken) tokens[startToken + 1];
		        
		        if (whereToken.Operator.ToUpper() != "WHERE")
                    throw new CSExpressionException("Expected WHERE");

		        if (numTokens < 3)
                    throw new CSExpressionException("Expected expression after WHERE");

                newQuery.Expression += " AND ";
		        
                newQuery = Parse(newQuery, tokens, startToken + 2, numTokens - 2);
            }
		    
            string selectSql = newQuery.Table.Schema.DB.BuildSelectSQL(newQuery.Table.TableName, newQuery.Table.TableAlias, new[] { fieldExpression }, null, newQuery.Joins.BuildJoinExpressions(), newQuery.Expression, null, 1, 0, false, false);

		    if (functionToken.Function == "HAS")
                subQuery.Expression += "EXISTS ";
		    
            subQuery.Expression += "(" + selectSql + ")";
	    
			return subQuery;
		}

		private static QueryExpression Parse(QueryExpression subQuery, List<Token> tokens, int startToken, int numTokens)
		{
			for (int i = startToken; i < startToken + numTokens; i++)
			{
				Token token = tokens[i];

				if (token is FunctionToken || token is ScalarFunctionToken)
				{
					if (!(tokens[i + 1] is LeftParenToken))
						throw new CSExpressionException("Function name not followed by left parentheses");

					int braceLevel = 1;

					for (int j = i + 2; j < startToken + numTokens; j++)
					{
						if (tokens[j] is LeftParenToken)
							braceLevel++;

						if (tokens[j] is RightParenToken)
						{
							braceLevel--;

							if (braceLevel == 0)
							{
								// Here we have all tokens that are part of the function call
								// first item = tokens[i+2]
								// last item = tokens[j-1]

								if (token is FunctionToken)
									subQuery = ParseFunction(subQuery, tokens, i + 2, j - i - 2, (FunctionToken) token);
								else
									subQuery = ParseScalarFunction(subQuery, tokens, i + 2, j - i - 2, (ScalarFunctionToken) token);

								i = j;

								break;
							}
						}
					}

					continue;
				}

				if (token is VariableToken)
				{
					string result = (string) EvalVariable((token as VariableToken).Variable, subQuery);

                    subQuery.Expression += " " + result + " ";
				    
					continue;
				}

				if (token is LiteralToken)
				{
					subQuery.Expression += " " + (token as LiteralToken).Literal + " ";

					continue;
				}

				if (token is DBFieldToken)
				{
					subQuery.Expression += " " + subQuery.Table.Schema.DB.QuoteField((token as DBFieldToken).FieldName) + " ";
				}

				if (token is DBTableToken)
				{
					subQuery.Expression += " " + subQuery.Table.Schema.DB.QuoteTable((token as DBTableToken).TableName) + " ";
				}

				if (token is LeftParenToken)
				{
					subQuery.Expression += " (";

					continue;
				}

				if (token is RightParenToken)
				{
					subQuery.Expression += ") ";

					continue;
				}

				if (token is OperatorToken)
				{
					subQuery.Expression += " " + (token as OperatorToken).Operator + " ";

					continue;
				}

				if (token is CommaToken)
				{
					subQuery.Expression += ",";

					continue;
				}
			}

			return subQuery;
		}

		internal static string Parse(CSSchema schema, string input, string tableAlias, CSJoinList joins)
		{
			if (input == "")
				return input;

			List<Token> tokens = PreParse(input);

			QueryExpression rootQuery = new QueryExpression(schema,tableAlias);

			rootQuery.Joins = joins;

            QueryExpression finalQuery = Parse(rootQuery, tokens, 0, tokens.Count);

			return finalQuery.Expression;
		}

		private class QueryExpression
		{
			public QueryExpression(CSSchema schema, string tableAlias)
			{
				Table = new CSTable(schema, tableAlias);
			}

			public QueryExpression(CSSchema schema)
			{
				Table = new CSTable(schema);
			}

			public readonly CSTable Table;
			public CSJoinList Joins;
			public string FieldName;
            public string Expression = "";
		}

		private static object EvalVariable(string var, QueryExpression parentQuery)
		{
			Match match = _regexVar.Match(var);

			if (!match.Success)
				throw new CSExpressionException("Object [" + var + "] is unknown");

			List<string> fieldNames = new List<string>();

			foreach (Capture capture in match.Groups["Object"].Captures)
				fieldNames.Add(capture.Value);

			QueryExpression subExpression = null;
		    
			CSTable currentTable = parentQuery.Table;

			string fieldName = null;

			for (int i = 0; i < fieldNames.Count; i++)
			{
				bool isLast = (i == fieldNames.Count - 1);

				CSJoin currentJoin = null;
				CSSchemaField schemaField = currentTable.Schema.Fields[fieldNames[i]];

				if (schemaField == null)
					throw new CSException("Error in expression. [" + fieldNames[i] + "] is not a valid field name");

				if (schemaField.Relation != null)
				{
					switch (schemaField.Relation.RelationType)
					{
						case CSSchemaRelationType.OneToMany:
							{
								if (subExpression != null)
									throw new CSExpressionException("Error in expression [" + var + "]: Multiple *ToMany relations");

								subExpression = new QueryExpression(schemaField.Relation.ForeignSchema);
								subExpression.Joins = new CSJoinList();

								subExpression.Expression = currentTable.TableAlias + "." + currentTable.Schema.DB.QuoteField(schemaField.Relation.LocalKey) + "=" + subExpression.Table.TableAlias + "." + currentTable.Schema.DB.QuoteField(schemaField.Relation.ForeignKey);
							}
							break;

						case CSSchemaRelationType.ManyToMany:
							{
								if (subExpression != null)
									throw new CSExpressionException("Error in expression [" + var + "]: Multiple *ToMany relations");

								subExpression = new QueryExpression(schemaField.Relation.ForeignSchema);
								subExpression.Joins = new CSJoinList();

								currentJoin = new CSJoin();

								currentJoin.LeftSchema = schemaField.Relation.ForeignSchema;
								currentJoin.LeftTable = subExpression.Table.TableName;
								currentJoin.LeftAlias = subExpression.Table.TableAlias;
								currentJoin.RightTable = schemaField.Relation.LinkTable;
								currentJoin.RightAlias = CSNameGenerator.NextTableAlias;
								currentJoin.LeftColumn = schemaField.Relation.ForeignKey;
								currentJoin.RightColumn = schemaField.Relation.ForeignLinkKey;

								currentJoin.Type = CSJoinType.Inner;

								subExpression.Joins.Add(currentJoin);

								subExpression.Expression = currentJoin.RightAlias + "." + currentTable.Schema.DB.QuoteField(schemaField.Relation.LocalLinkKey) + "=" + currentTable.TableAlias + "." + currentTable.Schema.DB.QuoteField(schemaField.Relation.LocalKey);

								currentJoin = null;
							}
							break;

						case CSSchemaRelationType.ManyToOne:
							{
								if (!isLast)
								{
									if (subExpression != null) // meaning we already encountered a *toMany relation
									{
										currentJoin = new CSJoin(schemaField.Relation, subExpression.Table.TableAlias);

										if (subExpression.Joins.Contains(currentJoin))
											currentJoin = subExpression.Joins.GetExistingJoin(currentJoin);
										else
											subExpression.Joins.Add(currentJoin);
									}
									else
									{
										currentJoin = new CSJoin(schemaField.Relation, currentTable.TableAlias);

										if (parentQuery.Joins.Contains(currentJoin))
											currentJoin = parentQuery.Joins.GetExistingJoin(currentJoin);
										else
											parentQuery.Joins.Add(currentJoin);
									}
								}
							}
							break;
					}

					if (!isLast)
					{
						if (currentJoin != null)
							currentTable = new CSTable(schemaField.Relation.ForeignSchema, currentJoin.RightAlias);
						else
						{
							if (subExpression != null)
								currentTable = new CSTable(schemaField.Relation.ForeignSchema, subExpression.Table.TableAlias);
							else
								currentTable = new CSTable(schemaField.Relation.ForeignSchema);

						}
					}
					else
					{
						fieldName = currentTable.Schema.DB.QuoteField(currentTable.TableAlias + "." + schemaField.Relation.LocalKey);
					}
				}
				else
				{
					fieldName = currentTable.Schema.DB.QuoteField(currentTable.TableAlias + "." + schemaField.MappedColumn.Name);
				}
			}

			if (subExpression != null)
			{
			    subExpression.FieldName = fieldName;

                return subExpression;
			}
			else
			{
				//if (fieldName != null)
					return fieldName;
			}

			
		}
	}
}

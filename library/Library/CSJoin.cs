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

namespace Vici.CoolStorage
{
	internal enum CSJoinType
	{
		Inner, Left, Right
	}

    internal sealed class CSJoin : IEquatable<CSJoin>
	{
		private string _leftTable;
		private string _rightTable;
        private string _leftAlias;
		private string _rightAlias;
        
		private CSSchema _leftSchema;
		private CSSchema _rightSchema;

		public CSJoinType Type;
		public string LeftColumn;
		public string RightColumn;

		public CSJoin()
		{
            _leftAlias = CSNameGenerator.NextTableAlias;
            _rightAlias = CSNameGenerator.NextTableAlias;
		}

		public CSJoin(CSRelation relation, string sourceAlias)
		{
            Type = CSJoinType.Left;
		    
            _leftSchema = relation.Schema;
			_rightSchema = relation.ForeignSchema;

			_leftAlias = sourceAlias;
            _rightAlias = CSNameGenerator.NextTableAlias;

		    LeftColumn = relation.LocalKey;
			RightColumn = relation.ForeignKey;
		}

		public string LeftTable 
		{ 
			get 
			{
				if (_leftTable != null)
					return _leftTable;
				else if (_leftSchema != null)
					return _leftSchema.TableName;
				else
					return null;
			} 
			set 
			{ 
				_leftTable = value; 
			} 
		}

		public string RightTable
		{
			get
			{
				if (_rightTable != null)
					return _rightTable;
				else if (_rightSchema != null)
					return _rightSchema.TableName;
				else
					return null;
			}
			set
			{
				_rightTable = value;
			}
		}

		public CSSchema LeftSchema 
		{ 
			get { return _leftSchema; } 
			set { _leftSchema = value; } 
		}

		public CSSchema RightSchema 
		{ 
			get { return _rightSchema; } 
			set { _rightSchema = value; } 
		}
        
        public string LeftAlias
        {
            get { return _leftAlias; }
            set { _leftAlias = value; }
        }

        public string RightAlias
        {
            get { return _rightAlias; }
            set { _rightAlias = value; }
        }

		public string JoinExpression
		{
			get
			{
				CSSchema leftSchema = _leftSchema;
				CSSchema rightSchema = _rightSchema;

				if (leftSchema == null)
					leftSchema = rightSchema;
				if (rightSchema == null)
					rightSchema = leftSchema;

				string expr = "";

				switch (Type)
				{
					case CSJoinType.Left: expr = "LEFT JOIN "; break;
					case CSJoinType.Right: expr = "RIGHT JOIN "; break;
					case CSJoinType.Inner: expr = "INNER JOIN "; break;
				}

				expr += rightSchema.DB.QuoteTable(RightTable) + " " + _rightAlias + " ON ";

				expr += _leftAlias + "." + leftSchema.DB.QuoteField(LeftColumn) + " = " + _rightAlias + "." + rightSchema.DB.QuoteField(RightColumn);

				return expr;
			}
		}

        public bool Equals(CSJoin other)
		{
			return other.LeftTable == LeftTable && other.RightTable == RightTable && other.LeftColumn == LeftColumn && other.RightColumn == RightColumn;
		}
	}
}

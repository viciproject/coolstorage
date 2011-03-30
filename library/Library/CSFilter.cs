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

namespace Vici.CoolStorage
{
	[Serializable]
	public sealed class CSFilter
	{
		private static readonly CSFilter _staticFilterNone = new CSFilter();

		private readonly string _expression;
		private readonly CSParameterCollection _parameters;

		public CSFilter()
		{
			_expression = "";
			_parameters = new CSParameterCollection();
		}

		public CSFilter(CSFilter sourceFilter)
		{
			_expression = sourceFilter._expression;
			_parameters = new CSParameterCollection(sourceFilter._parameters);
		}

		public CSFilter(string expression)
		{
			_expression = expression;
			_parameters = new CSParameterCollection();
		}

        public CSFilter(string expression, CSParameterCollection parameters)
        {
            _expression = expression;
            _parameters = new CSParameterCollection(parameters);
        }

		public CSFilter(string expression, params CSParameter[] parameters)
		{
			_expression = expression;
			_parameters = new CSParameterCollection(parameters);
		}

        public CSFilter(string expression, string paramName, object paramValue)
        {
            _expression = expression;
            _parameters = new CSParameterCollection(paramName, paramValue);
        }

		public CSFilter(string expression, string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			_expression = expression;
			_parameters = new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2);
		}

		public CSFilter(string expression, string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			_expression = expression;
			_parameters = new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3);
		}

		public CSFilter(CSFilter filter1, string andOr, CSFilter filter2)
		{
            if (filter1.IsBlank && filter2.IsBlank)
            {
                _expression = "";
                _parameters = new CSParameterCollection();
            }
            else if (filter1.IsBlank)
            {
                _expression = "(" + filter2.Expression + ")";
                _parameters = new CSParameterCollection(filter2.Parameters);
                return;
            }
            else if (filter2.IsBlank)
            {
                _expression = "(" + filter1.Expression + ")";
                _parameters = new CSParameterCollection(filter1.Parameters);
            }
            else
            {
                _expression = "(" + filter1._expression + ") " + andOr + " (" + filter2.Expression + ")";

                _parameters = new CSParameterCollection(filter1.Parameters);
                _parameters.Add(filter2.Parameters);
            }
		}

		public static CSFilter None
		{
			get
			{
				return _staticFilterNone;
			}
		}

		internal CSParameterCollection Parameters
		{
			get
			{
				return _parameters;
			}
		}

		internal string Expression
		{
			get
			{
				return _expression;
			}
		}

		public bool IsBlank
		{
			get
			{
				return _expression.Trim().Length < 1;
			}
		}

		public CSFilter Or(CSFilter filterOr)
		{
			return new CSFilter(this, "OR", filterOr);
		}

        public CSFilter Or(string expression)
        {
            return new CSFilter(this, "OR", new CSFilter(expression));
        }

		public CSFilter Or(string expression, string paramName , object paramValue)
		{
			return new CSFilter(this, "OR", new CSFilter(expression, paramName, paramValue));
		}

		public CSFilter Or(string expression, CSParameterCollection parameters)
		{
			return new CSFilter(this, "OR", new CSFilter(expression, parameters));
		}

		public CSFilter And(CSFilter filterOr)
		{
			return new CSFilter(this, "AND", filterOr);
		}

        public CSFilter And(string expression)
        {
            return new CSFilter(this, "AND", new CSFilter(expression));
        }

		public CSFilter And(string expression, string paramName, object paramValue)
		{
			return new CSFilter(this, "AND", new CSFilter(expression, paramName, paramValue));
		}

		public CSFilter And(string expression, CSParameterCollection parameters)
		{
			return new CSFilter(this, "AND", new CSFilter(expression, parameters));
		}

		public static CSFilter operator|(CSFilter filter1 , CSFilter filter2)
		{
			return new CSFilter(filter1, "OR", filter2);
		}

		public static CSFilter operator&(CSFilter filter1, CSFilter filter2)
		{
			return new CSFilter(filter1, "AND", filter2);
		}

		public static CSFilter operator &(CSFilter filter1, string filter2)
		{
			return new CSFilter(filter1, "AND", new CSFilter(filter2));
		}

		public static CSFilter operator |(CSFilter filter1, string filter2)
		{
			return new CSFilter(filter1, "OR", new CSFilter(filter2));
		}
	}
}

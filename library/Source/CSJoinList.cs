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
using System.Collections;
using System.Collections.Generic;

namespace Vici.CoolStorage
{
    internal sealed class CSJoinList : IEnumerable<CSJoin>
    {
        private readonly List<CSJoin> _joins = new List<CSJoin>();

        public CSJoinList(params IEnumerable<CSJoin>[] joinLists)
        {
            if (joinLists.Length < 1)
                return;

            foreach (IEnumerable<CSJoin> joinList in joinLists)
                Combine(joinList);
        }

		public bool Contains(CSJoin join)
		{
			return _joins.Contains(join);
		}

		public CSJoin GetExistingJoin(CSJoin join)
		{
		    int i = _joins.IndexOf(join);

		    return i >= 0 ? _joins[i] : null;
		}

        public void Add(CSJoin join)
        {
            if (!_joins.Contains(join))
                _joins.Add(join);
        }

        public void Combine(IEnumerable<CSJoin> joins)
        {
            foreach (CSJoin join in joins)
            {
                if (!_joins.Contains(join))
                    _joins.Add(join);
            }
        }

        public CSJoin[] ToArray()
        {
            return _joins.ToArray();
        }

        public IEnumerator<CSJoin> GetEnumerator()
        {
            return _joins.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _joins.GetEnumerator();
        }

        public string[] BuildJoinExpressions()
        {
            string[] s = new string[_joins.Count];

            for (int i = 0; i < _joins.Count;i++ )
                s[i] = _joins[i].JoinExpression;

            return s;
        }
    }
}
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
	public abstract class CSTypedQuery<T> where T : class, new()
	{
		public static T[] Run()
		{
			return Run(CSParameterCollection.Empty);
		}

		public static T[] Run(CSParameterCollection parameters)
		{
			return CSDatabase.RunQuery<T>(null,parameters);
		}

		public static T[] Run(string paramName, object paramValue)
		{
			return Run(new CSParameterCollection(paramName, paramValue));
		}

		public static T[] Run(string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			return Run(new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
		}

		public static T[] Run(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			return Run(new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
		}

		public static T[] Run(params CSParameter[] parameters)
		{
			return Run(new CSParameterCollection(parameters));
		}

        public static T[] Run(object parameters)
        {
            return Run(new CSParameterCollection(parameters));
        }

		public static T RunSingle()
		{
			return RunSingle(CSParameterCollection.Empty);
		}

		public static T RunSingle(CSParameterCollection parameters)
		{
			return CSDatabase.RunSingleQuery<T>(null, parameters);
		}

		public static T RunSingle(params CSParameter[] parameters)
		{
			return RunSingle(new CSParameterCollection(parameters));
		}

        public static T RunSingle(object parameters)
        {
            return RunSingle(new CSParameterCollection(parameters));
        }

        public static T RunSingle(string paramName, object paramValue)
		{
			return RunSingle(new CSParameterCollection(paramName, paramValue));
		}

		public static T RunSingle(string paramName1, object paramValue1, string paramName2, object paramValue2)
		{
			return RunSingle(new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2));
		}

		public static T RunSingle(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
		{
			return RunSingle(new CSParameterCollection(paramName1, paramValue1, paramName2, paramValue2, paramName3, paramValue3));
		}
	}
}

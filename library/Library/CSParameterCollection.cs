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
using System.Reflection;
using System.Runtime.Serialization;

namespace Vici.CoolStorage
{
	[Serializable]
	public class CSParameterCollection : IEnumerable<CSParameter>
	{
		public static CSParameterCollection Empty = new CSParameterCollection();

		private readonly List<CSParameter> _parameterList = new List<CSParameter>();

		[NonSerialized]
		private Dictionary<string, CSParameter> _parameterMap = new Dictionary<string, CSParameter>();

		[OnDeserialized]
		private void AfterDeserialization(StreamingContext context)
		{
			_parameterMap = new Dictionary<string, CSParameter>();

			foreach (CSParameter parameter in _parameterList)
				_parameterMap.Add(parameter.Name, parameter);
		}

		public CSParameterCollection()
		{
		}

        public CSParameterCollection(object o)
        {
            var members = o.GetType().GetMembers(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            foreach (var member in members)
            {
                object value;

                if (member is FieldInfo)
                    value = ((FieldInfo)member).GetValue(o);
                else if (member is PropertyInfo)
                    value = ((PropertyInfo)member).GetValue(o, null);
                else
                    continue;

                Add('@' + member.Name, value);
            }
        }

		public CSParameterCollection(string paramName,object paramValue)
		{
			Add(paramName, paramValue);
		}

        public CSParameterCollection(string paramName1, object paramValue1,string paramName2,object paramValue2)
        {
            Add(paramName1, paramValue1);
            Add(paramName2, paramValue2);
        }

        public CSParameterCollection(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3)
        {
            Add(paramName1, paramValue1);
            Add(paramName2, paramValue2);
            Add(paramName3, paramValue3);
        }

        public CSParameterCollection(string paramName1, object paramValue1, string paramName2, object paramValue2, string paramName3, object paramValue3 ,params object[] otherParameters)
        {
            Add(paramName1, paramValue1);
            Add(paramName2, paramValue2);
            Add(paramName3, paramValue3);

            if ((otherParameters.Length % 2) != 0)
                throw new CSException("Bad parameter list !");

            for(int i=0; i < otherParameters.Length ; i+=2)
            {
                if (otherParameters[i] == null)
                    throw new CSException("Parameter name cannot be null");

                if (!(otherParameters[i] is string))
                    throw new CSException("Parameter name should be a string");

                Add((string) otherParameters[i], otherParameters[i+1]);
            }
        }

		public CSParameterCollection(CSParameterCollection sourceCollection)
		{
		    Add(sourceCollection);
        }

        public CSParameterCollection(params CSParameter[] parameters)
        {
			foreach (CSParameter param in parameters)
			{
				_parameterList.Add(param);
				_parameterMap.Add(param.Name, param);
			}
        }

		public CSParameter Add()
		{
		    return Add('@' + CSNameGenerator.NextParameterName);
		}

        public CSParameter Add(string parameterName)
        {
            CSParameter param = new CSParameter(parameterName);

        	_parameterMap.Add(param.Name, param);
			_parameterList.Add(param);

            return param;
        }

        public void Add(string paramName,object paramValue)
        {
            CSParameter param = new CSParameter(paramName,paramValue);

            _parameterMap.Add(param.Name, param);
            _parameterList.Add(param);
        }

        public void Add(CSParameter parameter)
        {
            Add(parameter.Name, parameter.Value);
        }

		public void Add(CSParameterCollection parameters)
		{
            if (parameters != null)
            {
                foreach (CSParameter param in parameters)
                    Add(param.Name, param.Value);
            }
		}

		public CSParameter this[string name]
		{
			get
			{
				CSParameter parameter;

				_parameterMap.TryGetValue(name, out parameter);
				
				return parameter;
			}
		}
	    
	    public int Count
	    {
	        get
	        {
                return _parameterList.Count;
            }       
	    }

		public bool IsEmpty
		{
			get
			{
				return (_parameterList.Count == 0);
			}
		}

		IEnumerator<CSParameter> IEnumerable<CSParameter>.GetEnumerator()
		{
			return _parameterList.GetEnumerator();
		}

		public IEnumerator GetEnumerator()
		{
			return _parameterList.GetEnumerator();
		}
	}
}

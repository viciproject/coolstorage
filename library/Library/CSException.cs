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
using System.Runtime.Serialization;

namespace Vici.CoolStorage
{
#if !WINDOWS_PHONE
    [Serializable]
#endif
    public class CSException : Exception
    {
        public CSException()
        {
        }

        public CSException(string message) 
            : base(message)
        {
        }

        public CSException(string message , Exception innerException) 
            : base(message,innerException)
        {
        }

#if !WINDOWS_PHONE
        public CSException(SerializationInfo info,StreamingContext context) : base(info,context)
        {
        }
#endif
    }

    public sealed class CSSQLException : CSException
    {
        public CSSQLException(string message, Exception innerException, string sqlQuery, IEnumerable<CSParameter> parameters)
            : base(message, innerException)
        {
            Data.Add("sqlQuery", sqlQuery);

            if (parameters == null) return;

            foreach (var p in parameters)
                Data.Add(string.Format("Parameter {0} ({1})", p.Name, p.Value.GetType().Name), p.Value);
        }
    }

    public class CSObjectNotFoundException : CSException
    {
        public CSObjectNotFoundException(Type type , object key) : base(String.Format("Object with key [{0}] of type [{1}] does not exist",key,type.Name))
        {
        }
    }

    public class CSOptimisticLockException : CSException
    {
        public CSOptimisticLockException() : base("Optimistic lock error")
        {
        }
    }

    public class CSValidationException : CSException
    {
    }
    
    public class CSExpressionException : CSException
    {
        public CSExpressionException(string message)
            : base(message)
        {
        }
    }
}

using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class QueryExpressionAttribute : Attribute
    {
        private readonly string _query;

        public QueryExpressionAttribute(string sqlQuery)
        {
            _query = sqlQuery;
        }

        public string Query
        {
            get { return _query; }
        }
    }
}
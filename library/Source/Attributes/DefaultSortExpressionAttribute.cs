using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultSortExpressionAttribute : Attribute
    {
        private readonly string _expression;

        public DefaultSortExpressionAttribute(string sortExpression)
        {
            _expression = sortExpression;
        }

        public string Expression
        {
            get { return _expression; }
        }
    }
}
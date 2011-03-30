using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NullValueAttribute : Attribute
    {
        private readonly object _nullValue;

        public NullValueAttribute(Int32 i)
        {
            _nullValue = i;
        }

        public NullValueAttribute(Double d)
        {
            _nullValue = d;
        }

        public NullValueAttribute(Boolean b)
        {
            _nullValue = b;
        }

        public NullValueAttribute(DateTime dt)
        {
            _nullValue = dt;
        }

        public NullValueAttribute(string s)
        {
            _nullValue = s;
        }

        public object NullValue
        {
            get { return _nullValue; }
        }
    }
}
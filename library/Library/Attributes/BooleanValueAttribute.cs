using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BooleanValueAttribute : Attribute
    {
        private readonly object _trueValue;
        private readonly object _falseValue;

        public BooleanValueAttribute(string trueValue, string falseValue)
        {
            _trueValue = trueValue;
            _falseValue = falseValue;
        }

        public BooleanValueAttribute(string trueValue)
        {
            _trueValue = trueValue;
        }

        public BooleanValueAttribute(int trueValue, int falseValue)
        {
            _trueValue = trueValue;
            _falseValue = falseValue;
        }

        public BooleanValueAttribute(int trueValue)
        {
            _trueValue = trueValue;
        }

        public object TrueValue
        {
            get { return _trueValue; }
        }

        public object FalseValue
        {
            get { return _falseValue; }
        }
    }
}
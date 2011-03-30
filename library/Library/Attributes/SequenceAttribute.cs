using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed  class SequenceAttribute : Attribute
    {
        private readonly string _sequenceName;
        private bool _identity;

        public SequenceAttribute(string sequenceName)
        {
            _sequenceName = sequenceName;
        }

        public SequenceAttribute(string sequenceName, bool identity)
        {
            _sequenceName = sequenceName;
            _identity = identity;
        }

        public bool Identity
        {
            get { return _identity; }
            set { _identity = value; }
        }

        public string SequenceName
        {
            get { return _sequenceName; }
        }
    }
}
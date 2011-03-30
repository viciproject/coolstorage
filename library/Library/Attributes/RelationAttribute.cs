using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class RelationAttribute : Attribute
    {
        private string _localKey;
        private string _foreignKey;

        public string LocalKey
        {
            get { return _localKey; }
            set { _localKey = value; }
        }

        public string ForeignKey
        {
            get { return _foreignKey; }
            set { _foreignKey = value; }
        }
    }
}
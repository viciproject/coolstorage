using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ManyToManyAttribute : RelationAttribute
    {
        private string _localLinkKey;
        private string _foreignLinkKey ;
        private string _linkTable;
        private bool   _pure;

        public ManyToManyAttribute(string linkTable)
        {
            _linkTable = linkTable;
        }

        public bool Pure
        {
            get { return _pure; }
            set { _pure = value; }
        }

        public string LocalLinkKey
        {
            get { return _localLinkKey; }
            set	{ _localLinkKey = value; }
        }

        public string ForeignLinkKey
        {
            get { return _foreignLinkKey; }
            set { _foreignLinkKey = value; }
        }

        public string LinkTable
        {
            get { return _linkTable; }
            set { _linkTable = value; }
        }
    }
}
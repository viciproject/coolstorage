using System;
using System.Linq;

namespace Vici.CoolStorage
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DefaultSortAttribute : Attribute
    {
        private readonly CSSort _sortDirection = CSSort.Ascending;

        public DefaultSortAttribute()
        {
        }

        public DefaultSortAttribute(CSSort sortDirection)
        {
            _sortDirection = sortDirection;
        }


        public CSSort SortDirection
        {
            get { return _sortDirection; }
        }
    }
}
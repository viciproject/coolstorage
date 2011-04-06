using System.Collections.Generic;

namespace Community.CsharpSqlite.SQLiteClient
{
    public class Hashtable : Dictionary<string,object>
    {
        public Hashtable(IEqualityComparer<string> comparer) : base(comparer)
        {
        }

        public Hashtable()
        {
        }

        public new object this[string key]
        {
            get 
            { 
                object value;

                if (TryGetValue(key, out value))
                    return value;
                
                return null;
            }

            set { base[key] = value; }
        }

        public bool Contains(string key)
        {
            return ContainsKey(key);
        }
    }
}
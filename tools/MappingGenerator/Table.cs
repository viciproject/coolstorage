using System.Collections.Generic;
using System.Linq;

namespace MappingGenerator
{
    class Table
    {
        public string TableName { get; set; }
        public string ClassName { get; set; }
        public List<Field> Fields = new List<Field>();

        public Field GetField(string name)
        {
            return (Fields.Where(field => field.Name == name)).FirstOrDefault();
        }

        public Field PrimaryKey
        {
            get { return (Fields.Where(field => field.IsPrimaryKey)).FirstOrDefault(); }
        }
    }
}
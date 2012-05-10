using System;

namespace MappingGenerator
{
    internal class Field
    {
        public string Name { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsIdentity { get; set; }
        public Table Table { get; set; }
        public Type Type { get; set; }
        public int Size { get; set; }
        public int Scale { get; set; }
        public bool AllowNull { get; set; }
        public bool IsUnique { get; set; }
        public object DefaultValue { get; set; }
        public bool IsExpression { get; set; }

        public string CompilerTypeName { get { return Type.CompilerTypeName(); } }
    }
}
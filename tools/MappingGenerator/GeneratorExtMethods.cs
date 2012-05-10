using System;
using System.Collections.Generic;

namespace MappingGenerator
{
    public static class GeneratorExtMethods
    {
        private static readonly Dictionary<Type, string> _compilerTypeNames = new Dictionary<Type, string>();

        static GeneratorExtMethods()
        {
            _compilerTypeNames[typeof(byte)] = "byte";
            _compilerTypeNames[typeof(char)] = "char";
            _compilerTypeNames[typeof(sbyte)] = "sbyte";
            _compilerTypeNames[typeof(short)] = "short";
            _compilerTypeNames[typeof(ushort)] = "ushort";
            _compilerTypeNames[typeof(int)] = "int";
            _compilerTypeNames[typeof(uint)] = "uint";
            _compilerTypeNames[typeof(long)] = "long";
            _compilerTypeNames[typeof(ulong)] = "ulong";
            _compilerTypeNames[typeof(decimal)] = "decimal";
            _compilerTypeNames[typeof(double)] = "double";
            _compilerTypeNames[typeof(float)] = "float";
            _compilerTypeNames[typeof(string)] = "string";
            _compilerTypeNames[typeof(bool)] = "bool";
            _compilerTypeNames[typeof(object)] = "object";
        }

        public static string CompilerTypeName(this Type type)
        {
            bool nullable = false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                nullable = true;
                type = type.GetGenericArguments()[0];
            }

            if (type.IsGenericType)
            {
                List<string> typeNames = new List<string>();

                foreach (Type t in type.GetGenericArguments())
                {
                    typeNames.Add(t.CompilerTypeName());
                }

                string genericName = type.GetGenericTypeDefinition().FullName;

                return genericName.Substring(0, genericName.Length - 2) + "<" + string.Join(",", typeNames.ToArray()) + ">";
            }

            string typeName;

            if (!_compilerTypeNames.TryGetValue(type, out typeName))
            {
                typeName = type.FullName.Replace('+', '.');

                if (typeName.StartsWith("System.") && typeName.IndexOf('.') == typeName.LastIndexOf('.'))
                    typeName = typeName.Substring(7);
            }

            return typeName + (nullable ? "?" : "");
        }

        
    }
}
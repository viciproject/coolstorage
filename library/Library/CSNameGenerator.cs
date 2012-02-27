using System;
using System.Collections.Generic;

namespace Vici.CoolStorage
{
    internal static class CSNameGenerator
    {
        [ThreadStatic] private static int _currentTableAlias;
        [ThreadStatic] private static int _currentFieldAlias;
        [ThreadStatic] private static int _currentParamCounter;

        private static readonly string[] _tableAliases;
        private static readonly string[] _reservedWords = new[] { "is", "as", "in", "on", "to", "at", "go", "by", "of", "or", "if", "no" };
        
        static CSNameGenerator()
        {
            List<string> aliasList = new List<string>();

            for (char c1 = 'a'; c1 < 'z'; c1++)
            {
                for (char c2 = 'a'; c2 < 'z'; c2++)
                {
                    string alias = c1.ToString() + c2;

                    foreach (string reservedWord in _reservedWords)
                        if (alias == reservedWord)
                            alias = "";

                    if (alias.Length > 0)
                        aliasList.Add(alias);
                }
            }

            _tableAliases = aliasList.ToArray();
        }

        internal static string NextTableAlias
        {
            get
            {
                _currentTableAlias = _currentTableAlias % _tableAliases.Length;

                return _tableAliases[_currentTableAlias++];
            }
        }

        internal static string NextFieldAlias
        {
            get
            {
                _currentFieldAlias = _currentFieldAlias % _tableAliases.Length;

                return "f" + _tableAliases[_currentFieldAlias++];
            }
        }

        public static string NextParameterName
        {
            get
            {
                string paramName = "P" + (++_currentParamCounter);

                if (_currentParamCounter >= 999)
                    _currentParamCounter = 0;

                return paramName;
            }
        }

        internal static void Reset()
        {
            _currentFieldAlias = 0;
            _currentTableAlias = 0;
            _currentParamCounter = 0;
        }
    }
}
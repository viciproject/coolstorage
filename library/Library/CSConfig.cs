#region License
//=============================================================================
// Vici CoolStorage - .NET Object Relational Mapping Library 
//
// Copyright (c) 2004-2009 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading;

namespace Vici.CoolStorage
{
    public static partial class CSConfig
    {
        internal const string DEFAULT_CONTEXTNAME = "_DEFAULT_";

        private static bool? _useTransactionScope;
        private static int? _commandTimeout;
        private static bool _doLogging;
        private static string _logFileName;

        static CSConfig()
        {
            ReadConfig();
        }

        private static void ReadConfig()
        {
#if !MONOTOUCH && !WINDOWS_PHONE && !SILVERLIGHT
            NameValueCollection configurationSection = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("CoolStorage");

            if (configurationSection == null)
                return;

            if (configurationSection["UseTransactionScope"] != null)
                _useTransactionScope = (configurationSection["UseTransactionScope"].ToUpper() == "TRUE");

            int commandTimeout;

            if (configurationSection["CommandTimeout"] != null && int.TryParse(configurationSection["CommandTimeout"], out commandTimeout))
                _commandTimeout = commandTimeout;

            if (configurationSection["Logging"] != null)
                _doLogging = (configurationSection["Logging"].ToUpper() == "TRUE");

            if (configurationSection["LogFile"] != null)
                _logFileName = configurationSection["LogFile"];
#endif
        }

        public static bool UseTransactionScope
        {
            get
            {
                return _useTransactionScope ?? false;
            }
            set
            {
                _useTransactionScope = value;
            }
        }

        public static int? CommandTimeout
        {
            get
            {
                return _commandTimeout;
            }
            set
            {
                _commandTimeout = value;
            }
        }

        public static bool Logging
        {
            get { return _doLogging; }
            set { _doLogging = value; }
        }

        public static string LogFileName
        {
            get { return _logFileName; }
            set { _logFileName = value; }
        }

        internal static readonly Dictionary<string, string> ColumnMappingOverrideMap = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        private static readonly Dictionary<string, CSDataProvider> _globalDbMap = new Dictionary<string, CSDataProvider>(StringComparer.CurrentCultureIgnoreCase);
        private static readonly Dictionary<string, bool> _globalDbMapChanged = new Dictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);

        [ThreadStatic]
        private static ThreadData _threadData;


        /// <summary>
        /// Determines whether a database connection has been specified.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance has DB; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasDB()
        {
            return HasDB(DEFAULT_CONTEXTNAME);
        }

        /// <summary>
        /// Determines whether the database connection for the given context has been specified.
        /// </summary>
        /// <param name="contextName">Name of the context.</param>
        /// <returns>
        /// 	<c>true</c> if the specified context name has DB; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasDB(string contextName)
        {
            lock (_globalDbMap)
            {
                return _globalDbMap.ContainsKey(contextName);
            }
        }

        internal static CSDataProvider GetDB(string strContext)
        {
            if (_threadData == null)
                _threadData = new ThreadData();

            return _threadData.GetDB(strContext);
        }

        public static void SetDB(CSDataProvider db)
        {
            SetDB(db, DEFAULT_CONTEXTNAME);
        }

        public static void SetDB(CSDataProvider db, string contextName)
        {
            lock (_globalDbMap)
            {
                _globalDbMap[contextName] = db;
                _globalDbMapChanged[contextName] = true;
            }
        }
		
        public static void ChangeTableMapping(Type type, string tableName, string contextName)
        {
            CSSchema.ChangeMapTo(type, tableName, contextName);
        }

        public static void ChangeColumnMapping(Type type, string propertyName, string columnName)
        {
            lock (ColumnMappingOverrideMap)
            {
                PropertyInfo propInfo = type.GetProperty(propertyName);

                if (propInfo == null)
                    throw new CSException("ChangeColumnMapping() : Property [" + propertyName + "] undefined");

                ColumnMappingOverrideMap[propInfo.DeclaringType.Name + ":" + propInfo.Name] = columnName;
            }
        }


        private class ThreadData
        {
            private readonly Thread _callingThread;
            private readonly Dictionary<string, CSDataProvider> _threadDbMap = new Dictionary<string, CSDataProvider>(StringComparer.CurrentCultureIgnoreCase);

            internal ThreadData()
            {
                _callingThread = Thread.CurrentThread;

                Thread cleanupThread = new Thread(CleanupBehind);

                cleanupThread.IsBackground = true;

                cleanupThread.Start();
            }

            internal CSDataProvider GetDB(string contextName)
            {
                lock (_globalDbMap)
                {
                    if (_globalDbMapChanged.ContainsKey(contextName) && _globalDbMapChanged[contextName])
                    {
                        _globalDbMapChanged[contextName] = false;

                        if (_threadDbMap.ContainsKey(contextName))
                        {
                            _threadDbMap[contextName].Dispose();
                            _threadDbMap.Remove(contextName);
                        }
                    }
                }

                if (_threadDbMap.ContainsKey(contextName))
                    return _threadDbMap[contextName];

                lock (_globalDbMap)
                {
                    if (!_globalDbMap.ContainsKey(contextName))
                    {
#if !MONOTOUCH && !WINDOWS_PHONE && !SILVERLIGHT
                        NameValueCollection configurationSection = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("CoolStorage");

                        if (configurationSection != null)
                        {
                            string key = (contextName == DEFAULT_CONTEXTNAME) ? "Connection" : ("Connection." + contextName);

                            string value = configurationSection[key];

                            if (value.IndexOf('/') > 0)
                            {
                                string dbType = value.Substring(0, value.IndexOf('/')).Trim();
                                string connString = value.Substring(value.IndexOf('/') + 1).Trim();

                                string typeName = "Vici.CoolStorage." + dbType;

                                Type type = Type.GetType(typeName);

                                if (type == null)
                                    throw new CSException("Unable to load type <" + typeName + ">");

                                _globalDbMap[contextName] = (CSDataProvider)Activator.CreateInstance(type, new object[] { connString });
                            }
                        }
#endif

                        if (!_globalDbMap.ContainsKey(contextName))
                            throw new CSException("GetDB(): context [" + contextName + "] not found");
                    }
                }

                CSDataProvider db = _globalDbMap[contextName];

                db = db.Clone();

                _threadDbMap[contextName] = db;

                return db;
            }

            private void CleanupBehind()
            {
                _callingThread.Join();

                foreach (CSDataProvider db in _threadDbMap.Values)
                    db.Dispose();
            }
        }

    }
}

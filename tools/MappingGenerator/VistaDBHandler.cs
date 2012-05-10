using System.Collections.Generic;
using VistaDB;
using VistaDB.DDA;
using VistaDB.Provider;

namespace MappingGenerator
{
    class VistaDBHandler : MetaHandler, IMetaDataProvider
    {
        public Table[] GetMetaData(string server, string database, string login, string password, string fn)
        {
            List<Table> tables = new List<Table>();

            IVistaDBDatabase vistaDB = VistaDBEngine.Connections.OpenDDA().OpenDatabase(fn, VistaDBDatabaseOpenMode.NonexclusiveReadWrite, null);

            string[] tableNames = (string[])vistaDB.EnumTables().ToArray(typeof(string));

            vistaDB.Close();

            var conn = new VistaDBConnection("Data Source=" + fn);

            foreach (string tableName in tableNames)
            {
                string className = tableName;

                if (tableName.StartsWith("tbl"))
                    className = tableName.Substring(3);

                tables.Add(GetTable(conn, tableName, className));
            }

            conn.Close();

            return tables.ToArray();
        }
    }
}
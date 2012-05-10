using System.Collections.Generic;
using System.Data.SqlClient;

namespace MappingGenerator
{
    class SQLDBHandler : MetaHandler, IMetaDataProvider
    {
        public Table[] GetMetaData(string server, string database, string login, string password, string fn)
        {
            List<Table> tables = new List<Table>();
            List<string> tableNames = new List<string>();

            string connectionString = "";

            if (!string.IsNullOrEmpty(server))
                connectionString += "Server=" + server;
            else
                connectionString += "Server=(local)";

            connectionString += ";Database=" + database;

            if (!string.IsNullOrEmpty(login))
                connectionString += ";User ID=" + login + ";Password=" + password;
            else
                connectionString += ";Integrated security=true";

            var conn = new SqlConnection(connectionString);

            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select * from information_schema.tables";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["TABLE_TYPE"].ToString() == "BASE TABLE")
                            tableNames.Add(reader["TABLE_SCHEMA"] + "." + reader["TABLE_NAME"] );
                    }
                }
            }

            foreach (string tableName in tableNames)
            {
                string className = tableName.Substring(tableName.IndexOf('.')+1);

                if (className.StartsWith("tbl"))
                    className = className.Substring(3);

                tables.Add(GetTable(conn, tableName, className));
            }

            conn.Close();

            return tables.ToArray();
        }
    }
}
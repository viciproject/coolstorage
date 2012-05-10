using System;
using System.Data;

namespace MappingGenerator
{
    class MetaHandler
    {
        protected Table GetTable(IDbConnection conn, string tableName, string className)
        {
            Table tableObject = new Table { TableName = tableName, ClassName = className };

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select * from [" + tableName.Replace(".","].[") + "]";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
                {
                    var schemaTable = reader.GetSchemaTable();

                    foreach (DataRow row in schemaTable.Rows)
                    {
                        var field = new Field();

                        field.Table = tableObject;
                        field.Name = row["ColumnName"].ToString();
                        field.IsPrimaryKey = (bool?)row["IsKey"] ?? false;
                        field.IsReadOnly = ((bool?)row["IsReadOnly"] ?? false) || ((bool?)row["IsAutoIncrement"] ?? false);
                        field.Type = (Type)row["DataType"];
                        field.AllowNull = (bool?) row["AllowDBNull"] ?? false;

                        if (field.Type.IsValueType && field.AllowNull)
                        {
                            field.Type = typeof (Nullable<>).MakeGenericType(field.Type);
                        }

                        tableObject.Fields.Add(field);
                    }

                }

            }

            return tableObject;
        }
    }
}
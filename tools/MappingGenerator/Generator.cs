using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace MappingGenerator
{
	class Generator
	{
	    private IMetaDataProvider provider;

	    public Generator(string dbType)
	    {
            switch (dbType.ToUpper())
            {
                case "VISTADB":
                    provider = new VistaDBHandler();
                    break;

                case "SQLSERVER":
                    provider = new SQLDBHandler();
                    break;
            }

	    }

	    public void CreateMappingFiles(string directory, string ns, string server, string database, string login, string password, string fn)
        {
            if (!Directory.Exists(Path.Combine(directory, @"Fields")))
	            Directory.CreateDirectory(Path.Combine(directory, @"Fields"));
            if (!Directory.Exists(Path.Combine(directory, @"Relations")))
                Directory.CreateDirectory(Path.Combine(directory, @"Relations"));
            if (!Directory.Exists(Path.Combine(directory, @"Extra")))
                Directory.CreateDirectory(Path.Combine(directory, @"Extra"));

	        Table[] tableObjects = provider.GetMetaData(server, database, login, password, fn);

            foreach (var table in tableObjects)
            {
                var otm = new List<Table>();
                var mto = new List<Table>();

                foreach (Field field in table.Fields)
                {
                    if (field.IsPrimaryKey)
                        otm.AddRange(from t in tableObjects where t != table && t.GetField(field.Name) != null select t);
                    else
                        mto.AddRange(from t in tableObjects where t.PrimaryKey != null && t.PrimaryKey.Name == field.Name select t);
                }

                using (var writer = File.CreateText(Path.Combine(directory,@"Fields\" + table.ClassName + ".cs")))
                {
                    writer.WriteLine("using System;");
                    writer.WriteLine("using Activa.CoolStorage;");
                    writer.WriteLine();
                    
                    if (ns != null)
                    {
                        writer.WriteLine("namespace " + ns);
                        writer.WriteLine("{");
                    }

                    writer.WriteLine("\t[MapTo(\"" + table.TableName + "\")]");
                    writer.Write("\tpublic abstract partial class " + table.ClassName + " : CSObject<" + table.ClassName);

                    if (table.PrimaryKey != null)
                    {
                        writer.Write("," + table.PrimaryKey.CompilerTypeName);
                    }

                    writer.WriteLine(">");

                    writer.WriteLine("\t{");

                    foreach (Field field in table.Fields)
                    {
                        writer.Write("\t\tpublic abstract " + field.CompilerTypeName + " ");
                        writer.Write(field.Name);

                        writer.Write(" { get; ");

                        if (!field.IsReadOnly)
                            writer.Write("set; ");

                        writer.WriteLine("}");
                    }

                    writer.WriteLine("\t}");

                    if (ns != null)
                        writer.WriteLine("}");

                }

                using (var writer = File.CreateText(Path.Combine(directory, @"Relations\" + table.ClassName + ".cs")))
                {
                    writer.WriteLine("using System;");
                    writer.WriteLine("using Activa.CoolStorage;");
                    writer.WriteLine();

                    if (ns != null)
                    {
                        writer.WriteLine("namespace " + ns);
                        writer.WriteLine("{");
                    }

                    writer.WriteLine("\tpublic partial class " + table.ClassName);
                    writer.WriteLine("\t{");

                    foreach (Table t in otm)
                    {
                        writer.WriteLine("\t\t[OneToMany]");
                        writer.WriteLine("\t\tpublic abstract CSList<" + t.ClassName + "> " + t.ClassName + "s { get; }");
                    }

                    foreach (Table t in mto)
                    {
                        writer.WriteLine("\t\t[ManyToOne]");
                        writer.WriteLine("\t\tpublic abstract " + t.ClassName + " " + t.ClassName + " { get; set; }");
                    }

                    writer.WriteLine("\t}");

                    if (ns != null)
                        writer.WriteLine("}");
                }

                if (!File.Exists(Path.Combine(directory,@"Extra\" + table.ClassName + ".cs")))
                {
                    using (var writer = File.CreateText(Path.Combine(directory,@"Extra\" + table.ClassName + ".cs")))
                    {
                        writer.WriteLine("using System;");
                        writer.WriteLine("using Activa.CoolStorage;");
                        writer.WriteLine();

                        if (ns != null)
                        {
                            writer.WriteLine("namespace " + ns);
                            writer.WriteLine("{");
                        }

                        writer.WriteLine("\tpublic partial class " + table.ClassName);
                        writer.WriteLine("\t{");
                        writer.WriteLine("\t}");

                        if (ns != null)
                            writer.WriteLine("}");
                    }
                }
            }

        }



	}
}

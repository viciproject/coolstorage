using System;
using System.Collections.Generic;

namespace MappingGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Write(@"Usage:

MappingGenerator <DBType> <BaseDirectory> [/file=FileName] [/server=Server] [/database=Database] [/login=Login] [/password=Password] [/namespace=Namespace]

DBType: SQLSERVER, VISTADB
");
                return;
            }

            string baseDirectory = args[1];
            string dbType = args[0];
            string fn = null;
            string server = null;
            string db = null;
            string login = null;
            string password = null;
            string ns = null;

            for (int i=2;i<args.Length;i++)
            {
                string param = args[i];

                if (param.ToLower().StartsWith("/file="))
                    fn = param.Substring(6);

                if (param.ToLower().StartsWith("/server="))
                    server = param.Substring(8);

                if (param.ToLower().StartsWith("/database="))
                    db = param.Substring(10);

                if (param.ToLower().StartsWith("/login="))
                    login = param.Substring(7);

                if (param.ToLower().StartsWith("/password="))
                    password = param.Substring(10);

                if (param.ToLower().StartsWith("/namespace="))
                    ns = param.Substring(11);
            }

            new Generator(dbType).CreateMappingFiles(baseDirectory, ns,server,db,login,password,fn);


            /*			var tableSchema = database.NewTable("tblUser");

                        tableSchema.AddColumn("UserID", VistaDBType.Int);
                        tableSchema.AddColumn("Name", VistaDBType.VarChar,100);
                        tableSchema.AddColumn("CreationTime", VistaDBType.DateTime);

                        tableSchema.DefineIdentity("UserID","1","1");
                        tableSchema.DefineDefaultValue("CreationTime","getdate()",false,"");
                        tableSchema.DefineIndex("UserID", "UserID", true, true);
                        tableSchema.DefineIndex("Test", "Name;DESC(CreationTime)", false, false);
                        tableSchema.DefineColumnAttributes("CreationTime", false, false, false, false, null, null);

                        database.CreateTable(tableSchema, false, false);
            */
            //var database = VistaDBEngine.Connections.OpenDDA().OpenDatabase(fn, VistaDBDatabaseOpenMode.NonexclusiveReadWrite, null);

            //			string[] tables = (string[])database.EnumTables().ToArray(typeof(string));

            //CreateDB(fn);

            //CreateMappingFiles(fn);

/*            ClassifiedType ct = ClassifiedType.New();

            ct.Name = "Auto";

            ct.FieldDefinitions.AddRange(new[]
			                             	{
												FieldDefinition.New("Merk",FieldType.STRING),
												FieldDefinition.New("Kleur",FieldType.STRING),
												FieldDefinition.New("Prijs",FieldType.DECIMAL),
												FieldDefinition.New("Bouwjaar",FieldType.INT)
											});

            ct.Save();


            for (int i = 0; i < 20; i++)
            {
                Classified c = Classified.New();
                c.ClassifiedType = ct;
                c.UserID = 0;

                c.CreateFields();

                c.FindField("Merk").StringValue = "Porsche";
                c.Save();
            }

            var classifieds = Classified.List("has(Fields where FieldDefinition.Name = @Name and StringValue=@Value)", "@Name", "Merk", "@Value", "Porsche");

            */

            /*
                        Console.Read();

                        var xmlDoc = XDocument.Load(args[0]);
                        var xmlRoot = xmlDoc.Root;

                        var ns = xmlRoot.Name.Namespace;

                        var projGuid = xmlRoot.Elements(ns + "PropertyGroup").Elements(ns + "ProjectGuid").First().Value;


                        var itemGroups = GetItemGroups(xmlDoc);
                        var itemGroups2 = GetItemGroups(XDocument.Load(args[0]));
			
                        xmlRoot.Add(itemGroups2);
                        //itemGroups.Last().AddAfterSelf(itemGroups2);

                        xmlDoc.Save("c:\\test.xml");

                        foreach (var itemGroup in itemGroups)
                            Console.WriteLine(itemGroup);


                        Console.Read();
             */
        }

        /*
        static IEnumerable<XElement> GetItemGroups(XDocument xmlDoc)
        {
            var xmlRoot = xmlDoc.Root;

            if (xmlRoot == null)
                return null;

            var ns = xmlRoot.Name.Namespace;

            var itemGroups =
                    from
                        itemGroup
                    in
                        (from itemGroup in xmlRoot.Elements(ns + "ItemGroup")
                         where itemGroup.Descendants().Any(y =>
                             new[] { "Compile", "Content", "EmbeddedResource", "None" }
                     .Any(localName => y.Name.LocalName == localName))
                         select itemGroup)
                    select
                        itemGroup;


            return itemGroups;
        }
        */
        
        private const string _createScript = @"

CREATE TABLE [tblClassified]
(
	[ClassifiedID] int NOT NULL IDENTITY (1,1),
	[ClassifiedTypeID] int NOT NULL,
	[CreationTime] datetime NOT NULL DEFAULT 'getdate()',
	[PublishTime] datetime NOT NULL DEFAULT 'getdate()',
	[Published] bit NOT NULL default '0',
	[UserID] int NOT NULL,

	CONSTRAINT [PK_Classified] PRIMARY KEY NONCLUSTERED ([ClassifiedID] ASC)
)

CREATE TABLE [tblFieldDefinition]
(
	[FieldDefinitionID] int NOT NULL IDENTITY (1,1),
	[ClassifiedTypeID] int NOT NULL,
	[DataType] varchar(20) NOT NULL,
	[Required] bit NOT NULL default '0',
	[Name] varchar(100) NOT NULL,
	[Prompt] varchar(100) NOT NULL,
	[Header] varchar(100) NOT NULL,
	[Description] varchar(200) NULL,
	[DefaultValue] varchar(300) NULL,
	[ValidationExpression] varchar(100) NULL,
	[MinLength] int NOT NULL default '300',
	[MaxLength] int NULL,
	[MinValue] decimal(18,2) NULL,
	[MaxValue] decimal(18,2) NULL,

	CONSTRAINT [PK_FieldDefinition] PRIMARY KEY NONCLUSTERED ([FieldDefinitionID] ASC)
)

CREATE TABLE [tblField]
(
	[FieldID] int NOT NULL IDENTITY (1,1),
	[FieldDefinitionID] int NOT NULL,
	[ClassifiedID] int NOT NULL,
	[StringValue] varchar(300) NULL,
	[NumericValue] decimal(18,2) NULL,

	CONSTRAINT [PK_Field] PRIMARY KEY NONCLUSTERED ([FieldID] ASC)
)

CREATE TABLE [tblClassifiedType]
(
	[ClassifiedTypeID] int NOT NULL IDENTITY (1,1),
	[Name] varchar(100) NOT NULL,

	CONSTRAINT [PK_ClassifiedType] PRIMARY KEY NONCLUSTERED ([ClassifiedTypeID] ASC)
)

CREATE NONCLUSTERED INDEX [IX_Classified_UserID] ON [tblClassified] ([UserID] ASC)
CREATE NONCLUSTERED INDEX [IX_FieldDefinition_Name] ON [tblFieldDefinition] ([Name] ASC)
CREATE NONCLUSTERED INDEX [IX_Field_FieldDefinitionID] ON [tblField] ([FieldDefinitionID] ASC)
CREATE CLUSTERED INDEX [IX_Field_ClassifiedID] ON [tblField] ([ClassifiedID] ASC)


";

    }
}

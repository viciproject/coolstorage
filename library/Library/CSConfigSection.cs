using System.Configuration;

namespace Vici.CoolStorage
{
	/// <summary>
	/// Configuration for CoolStorage
	/// </summary>
	public class CSConfigSection : ConfigurationSection
	{
		private const string SECTIONNAME = "coolStorage";
		private const string USETRANSACTIONSCOPE = "UseTransactionScope";
		private const string COMMANDTIMEOUT = "commandTimeout";
		private const string CONNECTIONS = "connections";
		private const string DEFAULTCONNECTION = "defaultConnection";
		private const string ENABLELOGGING = "enableLogging";
		private const string LOGFILENAME = "logFilename";

		/// <summary>
		/// Gets or sets whether transactions should be used
		/// </summary>
		[ConfigurationProperty(USETRANSACTIONSCOPE, DefaultValue = false)]
		public bool UseTransactionScope
		{
			get { return (bool)this[USETRANSACTIONSCOPE]; }
			set { this[USETRANSACTIONSCOPE] = value; }
		}

		/// <summary>
		/// Gets or sets the command timeout seconds
		/// </summary>
		[ConfigurationProperty(COMMANDTIMEOUT)]
		[IntegerValidator(MinValue = 0)]
		public int CommandTimeout
		{
			get { return (int)this[COMMANDTIMEOUT]; }
			set { this[COMMANDTIMEOUT] = value; }
		}

		/// <summary>
		/// Gets or sets the collection of connections
		/// </summary>
		[ConfigurationProperty(CONNECTIONS)]
		public CSConnectionsCollection Connections
		{
			get { return (CSConnectionsCollection)this[CONNECTIONS]; }
			set { this[CONNECTIONS] = value; }
		}

		/// <summary>
		/// Gets or sets the default connection
		/// </summary>
		[ConfigurationProperty(DEFAULTCONNECTION, IsRequired = true)]
		public string DefaultConnection
		{
			get { return (string)this[DEFAULTCONNECTION]; }
			set { this[DEFAULTCONNECTION] = value; }
		}

		/// <summary>
		/// Gets or sets whether logging is enabled
		/// </summary>
		[ConfigurationProperty(ENABLELOGGING, DefaultValue = false)]
		public bool EnableLogging
		{
			get { return (bool)this[ENABLELOGGING]; }
			set { this[ENABLELOGGING] = value; }
		}

		/// <summary>
		/// Gets or sets the log file name
		/// </summary>
		[ConfigurationProperty(LOGFILENAME)]
		public string LogFilename
		{
			get { return (string)this[LOGFILENAME]; }
			set { this[LOGFILENAME] = value; }
		}

		/// <summary>
		/// Gets the configuration instance
		/// </summary>
		/// <returns></returns>
		public static CSConfigSection Instance()
		{
			return (CSConfigSection)ConfigurationManager.GetSection(SECTIONNAME);
		}
	}

	/// <summary>
	/// A coolstorage context
	/// </summary>
	public class CSConnectionElement : ConfigurationElement
	{
		private const string NAME = "name";
		private const string CONTEXTNAME = "contextName";
		private const string CONNECTIONSTRING = "connectionString";
		private const string PROVIDERTYPE = "type";

		/// <summary>
		/// Gets or sets the name for the connection. Required.
		/// </summary>
		[ConfigurationProperty(NAME, IsKey = true, IsRequired = true)]
		public string Name
		{
			get { return (string)this[NAME]; }
			set { this[NAME] = value; }
		}

		/// <summary>
		/// Gets or sets the context name
		/// </summary>
		[ConfigurationProperty(CONTEXTNAME)]
		public string ContextName
		{
			get { return (string)this[CONTEXTNAME]; }
			set { this[CONTEXTNAME] = value; }
		}

		/// <summary>
		/// Gets or sets connection string name. required
		/// </summary>
		[ConfigurationProperty(CONNECTIONSTRING, IsRequired = true)]
		public string ConnectionString
		{
			get { return (string)this[CONNECTIONSTRING]; }
			set { this[CONNECTIONSTRING] = value; }
		}

		/// <summary>
		/// Gets or sets the type of data provider
		/// </summary>
		[ConfigurationProperty(PROVIDERTYPE, IsRequired = true)]
		public string ProviderType
		{
			get { return (string)this[PROVIDERTYPE]; }
			set { this[PROVIDERTYPE] = value; }
		}

		public CSConnectionElement()
			: base()
		{ }

		public CSConnectionElement(string elementName)
			: this()
		{
			Name = elementName;
		}

	}

	public class CSConnectionsCollection : ConfigurationElementCollection
	{
		public new string AddElementName
		{
			get { return base.AddElementName; }
			set { base.AddElementName = value; }
		}

		public new string ClearElementName
		{
			get { return base.ClearElementName; }
			set { base.ClearElementName = value; }
		}

		public new string RemoveElementName
		{
			get { return base.RemoveElementName; }
			set { base.RemoveElementName = value; }
		}

		public CSConnectionElement this[int index]
		{
			get { return (CSConnectionElement)BaseGet(index); }
			set
			{
				if (BaseGet(index) != null) BaseRemoveAt(index);
				BaseAdd(index, value);
			}
		}

		public new CSConnectionElement this[string name]
		{
			get { return (CSConnectionElement)BaseGet(name); }
		}

		public override ConfigurationElementCollectionType CollectionType
		{
			get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
		}

		public CSConnectionsCollection()
			: base()
		{ }

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((CSConnectionElement)element).Name;
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new CSConnectionElement();
		}

		protected override ConfigurationElement CreateNewElement(string elementName)
		{
			return new CSConnectionElement(elementName);
		}

		public int IndexOf(CSConnectionElement connection)
		{
			return BaseIndexOf(connection);
		}

		public void Add(CSConnectionElement connection)
		{
			BaseAdd(connection);
		}

		protected override void BaseAdd(ConfigurationElement element)
		{
			base.BaseAdd(element, false);
		}

		public void Remove(CSConnectionElement connection)
		{
			if (BaseIndexOf(connection) >= 0) BaseRemove(connection.Name);
		}

		public void RemoveAt(int index)
		{
			BaseRemoveAt(index);
		}

		public void Remove(string key)
		{
			BaseRemove(key);
		}

		public void Clear()
		{
			BaseClear();
		}
	}

}
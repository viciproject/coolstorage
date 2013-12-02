namespace System
{
	public sealed class DBNull
	{
		// Fields
		public static readonly DBNull Value = new DBNull ();

		// Private constructor
		private DBNull ()
		{
		}

		public override string ToString ()
		{
			return String.Empty;
		}
	}
}
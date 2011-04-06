namespace Community.CsharpSqlite.SQLiteClient
{
    public enum DataRowVersion
    {
        Original = 256,
        Current = 512,
        Proposed = 1024,
        Default = 1536,
    }
}
namespace MappingGenerator
{
    internal interface IMetaDataProvider
    {
        Table[] GetMetaData(string server, string database, string login, string password, string fn);
    }
}
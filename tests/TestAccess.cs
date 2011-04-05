using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Vici.CoolStorage.UnitTests
{
    [TestFixture]
    public class TestAccess : CommonTests
    {
        [TestFixtureSetUp]
        public void SetupServer()
        {
            string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\Data\\coolstorage.mdb"));

            if (File.Exists(path))
                File.Delete(path);

            ADOX.CatalogClass cat = new ADOX.CatalogClass();

            cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + path + ";Jet OLEDB:Engine Type=5");

            CSConfig.SetDB(new CSDataProviderAccess(path));

            CSDatabase.ExecuteNonQuery(
    "CREATE TABLE tblCustomers (CustomerID COUNTER PRIMARY KEY,Name TEXT(50) NOT NULL)");

            CSDatabase.ExecuteNonQuery(
                @"CREATE INDEX tblCustomers_Name ON tblCustomers (Name)");

            CSDatabase.ExecuteNonQuery(
                @"CREATE TABLE tblCustomerPaymentMethodLinks (
            	                CustomerID integer NOT NULL,
            	                PaymentMethodID integer NOT NULL,
                                primary key (CustomerID,PaymentMethodID)
                                )");



            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblOrderItems (
            	OrderItemID counter PRIMARY KEY,
            	OrderID integer NOT NULL,
            	Qty integer NOT NULL,
            	Price double NOT NULL,
            	Description TEXT(200) NOT NULL
                )
            ");

            CSDatabase.ExecuteNonQuery(
                @"CREATE INDEX tblOrderItems_OrderID ON tblOrderItems (OrderID)");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblOrders (
            	OrderID counter PRIMARY KEY,
            	[Date] datetime NOT NULL DEFAULT DATE()+TIME(),
            	CustomerID integer NOT NULL,
            	SalesPersonID integer NULL,
            	DataState text(50))");

            CSDatabase.ExecuteNonQuery(
    @"CREATE INDEX tblOrders_CustomerID ON tblOrders (CustomerID)");

            CSDatabase.ExecuteNonQuery(
@"CREATE INDEX tblOrders_SalesPersonID ON tblOrders (SalesPersonID)");

            CSDatabase.ExecuteNonQuery(
                @"CREATE TABLE tblPaymentMethods (
            	PaymentMethodID counter primary key,
            	Name text(50) NOT NULL,
            	MonthlyCost integer NOT NULL
             )");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblSalesPeople (
            	SalesPersonID counter primary key,
            	Name text(50) NOT NULL,
            	SalesPersonType integer NULL)
             ");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblCoolData (
            	CoolDataID guid NOT NULL PRIMARY KEY,
            	Name text(50) NULL)");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblColdData (Name text(50) NULL)");

            cat.let_ActiveConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + path);

            ADOX.Column column = new ADOX.Column();

            column.Name = "ColdDataID";
            column.Type = ADOX.DataTypeEnum.adGUID;
            column.ParentCatalog = cat;
            column.Properties["AutoIncrement"].Value = false;
            column.Properties["Fixed Length"].Value = true;
            column.Properties["Jet OLEDB:AutoGenerate"].Value = true;
            column.Properties["Jet OLEDB:Allow Zero Length"].Value = true;

            cat.Tables["tblColdData"].Columns.Append(column, ADOX.DataTypeEnum.adGUID, 0);


            CSDatabase.ExecuteNonQuery("ALTER TABLE tblColdData ADD CONSTRAINT PK_COLD_DATA PRIMARY KEY (ColdDataID)");
        }

    }
}

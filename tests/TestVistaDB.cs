using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VistaDB.DDA;

namespace Vici.CoolStorage.UnitTests
{
    [TestFixture]
    public class TestVistaDB : CommonTests
    {
        [TestFixtureSetUp]
        public void SetupServer()
        {
            string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\Data\\coolstorage.vdb3"));

            if (File.Exists(path))
                File.Delete(path);

            IVistaDBDatabase database = VistaDBEngine.Connections.OpenDDA().CreateDatabase(path, false, null, 0, 0, false);

            database.Close();

            CSConfig.SetDB(new CSDataProviderVistaDB(@"Data Source=" + path));

            CSDatabase.ExecuteNonQuery(_sqlCreateTables);
        }

        private const string _sqlCreateTables =
            @"
CREATE TABLE [tblCustomerPaymentMethodLinks]
(
	[CustomerID] [int] NOT NULL,
	[PaymentMethodID] [int] NOT NULL,
		
	CONSTRAINT [PK_tblCustomerPaymentMethodLinks] PRIMARY KEY CLUSTERED ([CustomerID] ASC,[PaymentMethodID] ASC)
)

CREATE TABLE [tblOrders]
(
	[OrderID] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL DEFAULT ""getdate()"",
	[CustomerID] [int] NOT NULL,
	[SalesPersonID] [int] NULL,
	[DataState] [varchar](50) NULL,

    CONSTRAINT [PK_tblOrders] PRIMARY KEY CLUSTERED ([OrderID] ASC)
)

CREATE TABLE [tblOrderItems]
(
	[OrderItemID] [int] IDENTITY(1,1) NOT NULL,
	[OrderID] [int] NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [float] NOT NULL,
	[Description] [varchar](200) NOT NULL,

    CONSTRAINT [PK_tblOrderItems] PRIMARY KEY CLUSTERED ([OrderItemID] ASC)
)

CREATE TABLE [tblCustomers]
(
	[CustomerID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,

	CONSTRAINT [PK_tblCustomers] PRIMARY KEY CLUSTERED ([CustomerID] ASC)
)

CREATE TABLE [tblSalesPeople]
(
	[SalesPersonID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[SalesPersonType] [bigint] NULL,
        
	CONSTRAINT [PK_tblSalesPeople] PRIMARY KEY CLUSTERED ([SalesPersonID] ASC)
)

CREATE TABLE [tblPaymentMethods]
(
	[PaymentMethodID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[MonthlyCost] [int] NOT NULL,

	CONSTRAINT [PK_tblPaymentMethods] PRIMARY KEY CLUSTERED ([PaymentMethodID] ASC)
)

CREATE TABLE [tblCoolData]
(
	[CoolDataID] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) NULL,
        
	CONSTRAINT [PK_tblCoolData] PRIMARY KEY CLUSTERED ([CoolDataID] ASC)
)

CREATE TABLE [tblColdData]
(
	[ColdDataID] [uniqueidentifier] NOT NULL DEFAULT ""newid()"" ,
	[Name] [varchar](50) NULL,
        
	CONSTRAINT [PK_tblColdData] PRIMARY KEY CLUSTERED ([ColdDataID] ASC)
)

CREATE NONCLUSTERED INDEX [IX_tblOrderItems_OrderID] ON [tblOrderItems] ([OrderID] ASC)
CREATE NONCLUSTERED INDEX [IX_tblOrderItems_Price] ON [tblOrderItems] ([Price] ASC)
CREATE NONCLUSTERED INDEX [IX_tblOrders_CustomerID] ON [tblOrders] ([CustomerID] ASC)
CREATE NONCLUSTERED INDEX [IX_tblOrders_SalesPersonID] ON [tblOrders] ([SalesPersonID] ASC)
";
    }

}

using System;
using System.Collections.Generic;
using Vici.CoolStorage;
using NUnit.Framework;

namespace Vici.CoolStorage.UnitTests
{
    [TestFixture]
    public class TestSQLServer : CommonTests
    {
        [TestFixtureSetUp]
        public void SetupServer()
        {
            CSConfig.SetDB(new CSDataProviderSqlServer("Initial Catalog=cstest;Data Source=DBSERV;User ID=nunit;PWD=nunit;"));
            

            CSDatabase.ExecuteNonQuery(_sqlCreateTables);
        }


        private const string _sqlCreateTables = @"
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblCustomerPaymentMethodLinks]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblCustomerPaymentMethodLinks](
	[CustomerID] [int] NOT NULL,
	[PaymentMethodID] [int] NOT NULL,
 CONSTRAINT [PK_tblCustomerPaymentMethodLinks] PRIMARY KEY CLUSTERED 
(
	[CustomerID] ASC,
	[PaymentMethodID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblOrders]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblOrders](
	[OrderID] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL CONSTRAINT [DF_tblOrders_Date]  DEFAULT (getdate()),
	[CustomerID] [int] NOT NULL,
	[SalesPersonID] [int] NULL,
	[DataState] [varchar](50) NULL,
 CONSTRAINT [PK_tblOrders] PRIMARY KEY CLUSTERED 
(
	[OrderID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblOrderItems]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblOrderItems](
	[OrderItemID] [int] IDENTITY(1,1) NOT NULL,
	[OrderID] [int] NOT NULL,
	[Qty] [int] NOT NULL,
	[Price] [float] NOT NULL,
	[Description] [varchar](200) NOT NULL,
 CONSTRAINT [PK_tblOrderItems] PRIMARY KEY CLUSTERED 
(
	[OrderItemID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END


IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tblOrderItems]') AND name = N'IX_tblOrderItems_OrderID')
CREATE NONCLUSTERED INDEX [IX_tblOrderItems_OrderID] ON [dbo].[tblOrderItems] 
(
	[OrderID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]


IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tblOrderItems]') AND name = N'IX_tblOrderItems_Price')
CREATE NONCLUSTERED INDEX [IX_tblOrderItems_Price] ON [dbo].[tblOrderItems] 
(
	[Price] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblCustomers]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblCustomers](
	[CustomerID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
 CONSTRAINT [PK_tblCustomers] PRIMARY KEY CLUSTERED 
(
	[CustomerID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblSalesPeople]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblSalesPeople](
	[SalesPersonID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[SalesPersonType] [bigint] NULL,
 CONSTRAINT [PK_tblSalesPeople] PRIMARY KEY CLUSTERED 
(
	[SalesPersonID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblPaymentMethods]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblPaymentMethods](
	[PaymentMethodID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[MonthlyCost] [int] NOT NULL,
 CONSTRAINT [PK_tblPaymentMethods] PRIMARY KEY CLUSTERED 
(
	[PaymentMethodID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblCoolData]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblCoolData](
	[CoolDataID] [uniqueidentifier] NOT NULL,
	[Name] [varchar](50) NULL,
 CONSTRAINT [PK_tblCoolData] PRIMARY KEY CLUSTERED 
(
	[CoolDataID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

SET ANSI_NULLS ON

SET QUOTED_IDENTIFIER ON

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tblColdData]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tblColdData](
	[ColdDataID] [uniqueidentifier] NOT NULL CONSTRAINT [DF_tblColdData_ColdDataID]  DEFAULT (newid()),
	[Name] [varchar](50) NULL CONSTRAINT [DF_tblColdData_Name]  DEFAULT (newid()),
 CONSTRAINT [PK_tblColdData] PRIMARY KEY CLUSTERED 
(
	[ColdDataID] ASC
)WITH (IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

";
    }

}

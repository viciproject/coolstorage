using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Vici.CoolStorage.UnitTests
{
    [TestFixture]
    public class TestMySQL : CommonTests
    {
        [TestFixtureSetUp]
        public void SetupServer()
        {
            CSConfig.SetDB(new CSDataProviderMySql("Server=192.168.1.41;Database=cstest;UID=nunit;PWD=nunit"));

            CSDatabase.ExecuteNonQuery("drop table if exists tblCustomers");
            CSDatabase.ExecuteNonQuery("drop table if exists tblCustomerPaymentMethodLinks");
            CSDatabase.ExecuteNonQuery("drop table if exists tblOrderItems");
            CSDatabase.ExecuteNonQuery("drop table if exists tblOrders");
            CSDatabase.ExecuteNonQuery("drop table if exists tblSalesPeople");
            CSDatabase.ExecuteNonQuery("drop table if exists tblCoolData");
            CSDatabase.ExecuteNonQuery("drop table if exists tblPaymentMethods");


            CSDatabase.ExecuteNonQuery(
    "CREATE TABLE tblCustomers (CustomerID INTEGER PRIMARY KEY AUTO_INCREMENT,Name VARCHAR(50) NOT NULL)");

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
            	OrderItemID INTEGER PRIMARY KEY AUTO_INCREMENT,
            	OrderID integer NOT NULL,
            	Qty integer NOT NULL,
            	Price real NOT NULL,
            	Description varchar(200) NOT NULL
                )
            ");

            CSDatabase.ExecuteNonQuery(
                @"CREATE INDEX tblOrderItems_OrderID ON tblOrderItems (OrderID)");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblOrders (
            	OrderID INTEGER PRIMARY KEY AUTO_INCREMENT,
            	Date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            	CustomerID integer NOT NULL,
            	SalesPersonID integer NULL,
            	DataState varchar(50))");

            CSDatabase.ExecuteNonQuery(
    @"CREATE INDEX tblOrders_CustomerID ON tblOrders (CustomerID)");

            CSDatabase.ExecuteNonQuery(
@"CREATE INDEX tblOrders_SalesPersonID ON tblOrders (SalesPersonID)");

            CSDatabase.ExecuteNonQuery(
                @"CREATE TABLE tblPaymentMethods (
            	PaymentMethodID integer primary key auto_increment,
            	Name varchar(50) NOT NULL,
            	MonthlyCost integer NOT NULL
             )");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblSalesPeople (
            	SalesPersonID integer primary key auto_increment,
            	Name varchar(50) NOT NULL,
            	SalesPersonType integer NULL)
             ");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblCoolData (
            	CoolDataID varchar(50) PRIMARY KEY,
            	Name varchar(50) NULL)");

//            CSDatabase.ExecuteNonQuery(
//@"CREATE TABLE tblColdData (
//            	ColdDataID varchar(50) PRIMARY KEY,
//            	Name varchar(50) NULL)");
//
//
//            CSDatabase.ExecuteNonQuery(
//
//                @"CREATE TRIGGER
//newid
//BEFORE INSERT ON
//tblColdData
//FOR EACH ROW
//SET NEW.id = UUID()"
//
//                );

        }
        
    }
}

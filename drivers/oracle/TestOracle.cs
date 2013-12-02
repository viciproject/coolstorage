using NUnit.Framework;

namespace Vici.CoolStorage.UnitTests
{
     [TestFixture]
    public class TestOracle : CommonTests
    {
        private delegate void VoidAction();

        private void DontCare(VoidAction action)
        {
            try
            {
                action();
            }
            catch
            {

            }
        }

        [TestFixtureSetUp]
        public void SetupServer()
        {
            CSConfig.SetDB(new CSDataProviderOracle("user id=CS;password=ABCDEFG;data source=dev-cweber/xe"));
            /*
            -- Script to create the user with necessary roles
            -- USER SQL
            CREATE USER CS IDENTIFIED BY ABCDEFG 
            DEFAULT TABLESPACE "SYSTEM"
            TEMPORARY TABLESPACE "TEMP";

            -- ROLES
            GRANT "CONNECT" TO CS ;
            GRANT "DBA" TO "CS" ;
            ALTER USER "CS" DEFAULT ROLE "DBA","CONNECT";
            */

            DontCare(() => CSDatabase.ExecuteNonQuery("drop table tblCustomers"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop table tblCustomerPaymentMethodLinks"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop table tblOrderItems"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop table tblOrders"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop table tblSalesPeople"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop table tblCoolData"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop table tblPaymentMethods"));

            DontCare(() => CSDatabase.ExecuteNonQuery("drop sequence Customer_seq"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop sequence Order_seq"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop sequence SalesPerson_seq"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop sequence PaymentMethod_seq"));
            DontCare(() => CSDatabase.ExecuteNonQuery("drop sequence OrderItem_seq"));


            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblCustomers (
    CustomerID INTEGER PRIMARY KEY,
    Name VARCHAR2(50) NOT NULL
)");

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
    OrderItemID INTEGER PRIMARY KEY,
    OrderID integer NOT NULL,
    Qty integer NOT NULL,
    Price float NOT NULL,
    Description varchar2(200) NOT NULL
)");

            CSDatabase.ExecuteNonQuery(
@"CREATE INDEX tblOrderItems_OrderID ON tblOrderItems (OrderID)");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblOrders (
    OrderID INTEGER PRIMARY KEY,
    ""DATE"" DATE DEFAULT sysdate,
    CustomerID integer NOT NULL,
    SalesPersonID integer NULL,
    DataState varchar2(50)
)");

            CSDatabase.ExecuteNonQuery(
@"CREATE INDEX tblOrders_CustomerID ON tblOrders (CustomerID)");

            CSDatabase.ExecuteNonQuery(
@"CREATE INDEX tblOrders_SalesPersonID ON tblOrders (SalesPersonID)");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblPaymentMethods (
    PaymentMethodID integer primary key,
    Name varchar2(50) NOT NULL,
    MonthlyCost integer NOT NULL
)");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblSalesPeople (
    SalesPersonID integer primary key,
    Name varchar2(50) NOT NULL,
    SalesPersonType integer NULL
)");

            CSDatabase.ExecuteNonQuery(
@"CREATE TABLE tblCoolData (
    CoolDataID RAW(16) PRIMARY KEY,
    Name varchar2(50) NULL
)");

            CSDatabase.ExecuteNonQuery(@"create sequence Customer_seq start with 1 increment by 1 nomaxvalue ");
            CSDatabase.ExecuteNonQuery(@"create sequence Order_seq start with 1 increment by 1 nomaxvalue ");
            CSDatabase.ExecuteNonQuery(@"create sequence SalesPerson_seq start with 1 increment by 1 nomaxvalue ");
            CSDatabase.ExecuteNonQuery(@"create sequence PaymentMethod_seq start with 1 increment by 1 nomaxvalue ");
            CSDatabase.ExecuteNonQuery(@"create sequence OrderItem_seq start with 1 increment by 1 nomaxvalue ");

        }

    }
}

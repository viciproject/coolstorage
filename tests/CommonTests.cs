using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Vici.CoolStorage.UnitTests.Data;
using NUnit.Framework;

namespace Vici.CoolStorage.UnitTests
{
    public class CommonTests
    {
        private const int numIterations = 1;
		
        public void SetupTestData()
        {
            DeleteData();

            int nCustomers = 5;
            int nOrders = 20;

            Customer[] customers = new Customer[nCustomers];

            for (int i = 0; i < nCustomers; i++)
            {
                Customer customer = Customer.New();

                customer.Name = "Customer " + (i + 1);
                customer.Save();

                customers[i] = customer;
            }

            for (int i = 0; i < nOrders; i++)
            {
                Order order = Order.New();

                order.Customer = customers[i % nCustomers];


                for (int j = 0; j < i + 1; j++)
                {
                    order.OrderItems.Add(OrderItem.New("Item " + (i+1) + "/" + (j+1),(short)(j*i+1),(j*34.0+i)));
                }

                order.Save();
            }
        }

        [SetUp]
        public virtual void DeleteData()
        {
            CSDatabase.ExecuteNonQuery("delete from tblOrderItems");
            CSDatabase.ExecuteNonQuery("delete from tblOrders");
            CSDatabase.ExecuteNonQuery("delete from tblCustomers");
            CSDatabase.ExecuteNonQuery("delete from tblSalesPeople");
            CSDatabase.ExecuteNonQuery("delete from tblPaymentMethods");
        }

        [Test]
        public void PrefetchMany()
        {
            Random rnd = new Random();

            Dictionary<int, List<int>> testMapCustomers = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> testMapOrders = new Dictionary<int, List<int>>();

            const int numCustomers = 25;

            for (int i = 0; i < numCustomers; i++)
            {
                Customer customer = Customer.New();

                customer.Name = "Customer" + rnd.Next();

                customer.Save();

                testMapCustomers[customer.CustomerID] = new List<int>();

                int numOrders = rnd.Next(2, 4);

                for (int j = 0; j < numOrders; j++)
                {
                    Order order = Order.New();

                    order.OrderDate = DateTime.Today;
                    order.Customer = customer;

                    order.Save();

                    testMapCustomers[customer.CustomerID].Add(order.OrderID);

                    testMapOrders[order.OrderID] = new List<int>();

                    int numItems = rnd.Next(4, 7);

                    for (int k = 0; k < numItems; k++)
                    {
                        OrderItem item = OrderItem.New("Test " + k, (short)(k * 4), k * 5 + 1);

                        item.Order = order;
                        item.Save();

                        testMapOrders[order.OrderID].Add(item.OrderItemID);
                    }

                }

                customer.Save();
            }

            int numExpectedQueries = 0;

            CSDataProvider.QueryCount = 0;

            Stopwatch sw1 = Stopwatch.StartNew();

            numExpectedQueries = 1;

            foreach (Customer customer in Customer.List())
            {
                numExpectedQueries++;

                Assert.AreEqual(customer.Orders.Count, testMapCustomers[customer.CustomerID].Count);

                foreach (Order order in customer.Orders)
                {
                    numExpectedQueries++;

                    Assert.AreEqual(order.OrderItems.Count, testMapOrders[order.OrderID].Count);
                }
            }

            sw1.Stop();

            Assert.AreEqual(numExpectedQueries, CSDataProvider.QueryCount);

            CSDataProvider.QueryCount = 0;

            numExpectedQueries = 2;

            Stopwatch sw2 = Stopwatch.StartNew();

            foreach (Customer customer in Customer.List().WithPrefetch("Orders"))
            {
                Assert.AreEqual(customer.Orders.Count, testMapCustomers[customer.CustomerID].Count);

                foreach (Order order in customer.Orders)
                {
                    numExpectedQueries++;

                    Assert.AreEqual(order.OrderItems.Count, testMapOrders[order.OrderID].Count);
                }
            }

            sw2.Stop();

            Assert.AreEqual(numExpectedQueries, CSDataProvider.QueryCount);

            CSDataProvider.QueryCount = 0;

            numExpectedQueries = 3;

            Stopwatch sw3 = Stopwatch.StartNew();

            foreach (Customer customer in Customer.List().WithPrefetch("Orders", "Orders.OrderItems"))
            {
                Assert.AreEqual(customer.Orders.Count, testMapCustomers[customer.CustomerID].Count);

                foreach (Order order in customer.Orders)
                {
                    Assert.AreEqual(order.OrderItems.Count, testMapOrders[order.OrderID].Count);
                }
            }

            sw3.Stop();

            Assert.AreEqual(numExpectedQueries, CSDataProvider.QueryCount);

//            Assert.IsTrue(sw1.ElapsedMilliseconds > sw2.ElapsedMilliseconds);
//            Assert.IsTrue(sw2.ElapsedMilliseconds > sw3.ElapsedMilliseconds);

        }


        [Test]
        public void ManyToOne()
        {
            SetupTestData();

            Customer customer = new CSList<Customer>()[0];
            SalesPerson salesPerson = SalesPerson.New();
            salesPerson.Name = "Test";
            salesPerson.Save();

            Order order = Order.New();

            order.SalesPerson = null;
            order.Customer = customer;

            order.Save();

            int id = order.OrderID;

            order = Order.Read(id);

            Assert.AreEqual(order.Customer, customer);

            order.SalesPerson = salesPerson;
            order.Save();

            order = Order.Read(id);

            Assert.AreEqual(salesPerson, order.SalesPerson);

            order.SalesPerson = null;
            order.Save();

            order = Order.Read(id);

            Assert.IsNull(order.SalesPerson);
            Assert.IsNull(order.SalesPersonID);
        }

        [Test]
        public void CreateObject()
        {
            Customer customer = Customer.New();

            customer.Name = "BLABLA1";
            customer.Save();

            Assert.IsTrue(customer.CustomerID > 0);

            Customer customer2 = Customer.Read(customer.CustomerID);

            Assert.AreEqual(customer.Name, customer2.Name);
        }

        [Test]
        public void CreateObjectNonAbstract()
        {
            Customer2 customer = Customer2.New();

            customer.Name = "BLABLA1";
            customer.Save();

            Assert.IsTrue(customer.CustomerID > 0);

            Customer2 customer2 = Customer2.Read(customer.CustomerID);

            Assert.AreEqual(customer.Name, customer2.Name);
        }

        [Test]
        public void CreateOrderWithNewCustomer()
        {
            for (int i = 0 ; i < numIterations ; i++)
            {
                Order order = Order.New();

                order.DataState = "test";

                order.Customer = Customer.New();
                order.Customer.Name = "me";

                Assert.IsTrue(order.Save());

                Order order2 = Order.Read(order.OrderID);

                Assert.AreEqual(order2.Customer.Name,order.Customer.Name);
                Assert.AreEqual(order2.Customer.CustomerID,order.Customer.CustomerID);
                Assert.AreEqual(order2.Customer.CustomerID, order.CustomerID);

                Assert.AreEqual(order2.Customer.Orders[0].Customer,order.Customer);

                Assert.IsTrue(order2.Customer.Delete());
                Assert.IsTrue(order2.Delete());
            }
        }

        [Test]
        public void CreateOrderWithExistingCustomer()
        {
            for (int i = 0 ; i < numIterations ; i++)
            {
                Customer cust = Customer.New();
                cust.Name = "TestCust";
                cust.Save();

                cust = Customer.Read(cust.CustomerID);

                Order order = Order.New();

                order.Customer = cust;

                if ((i % 2) == 0)
                    order.Customer = cust;

                Assert.IsTrue(order.Save());

                order = Order.Read(order.OrderID);

                Assert.AreEqual(order.Customer.Name,cust.Name);
                Assert.AreEqual(order.Customer.CustomerID,cust.CustomerID);
                Assert.AreEqual(order.CustomerID, cust.CustomerID);

                Assert.AreEqual((order.Customer.Orders[0]).Customer,cust);

                order.Customer.Name = "TestCust2";
                order.Save();

                order = Order.Read(order.OrderID);

                Assert.AreEqual(order.CustomerID, cust.CustomerID);

                Assert.AreEqual("TestCust2",order.Customer.Name);

                Assert.IsTrue(order.Customer.Delete());
                Assert.IsTrue(order.Delete());
            }
        }

        [Test]
        public void CreateOrderWithNewItems()
        {
            for (int i = 0 ; i < numIterations ; i++)
            {
                DeleteData();

                Order order = Order.New();

                order.Customer = Customer.New();
                order.Customer.Name = "test";

                order.OrderItems.Add(OrderItem.New("test",5,200.0));
                order.OrderItems.Add(OrderItem.New("test", 3, 45.0));

                order.Save();

                order = Order.Read(order.OrderID);

                double totalPrice = Convert.ToDouble(order.OrderItems.GetScalar("Qty * Price",CSAggregate.Sum));

                Assert.AreEqual(2,order.OrderItems.Count,"Order items not added");
                Assert.AreEqual(1135.0,totalPrice,"Incorrect total amount");

                order.OrderItems.Add(OrderItem.New("test", 2, 1000.0));

                Assert.IsTrue(order.Save());

                order = Order.Read(order.OrderID);

                totalPrice = Convert.ToDouble(order.OrderItems.GetScalar("Qty * Price",CSAggregate.Sum));

                Assert.AreEqual(3,order.OrderItems.Count,"Order item not added");
                Assert.AreEqual(3135.0,totalPrice,"Total price incorrect");

                order.OrderItems.DeleteAll();

                order = Order.Read(order.OrderID);

                Assert.AreEqual(0,order.OrderItems.Count,"Order items not deleted");

                Assert.IsTrue(order.Delete());
            }

        }

        [Test]
        public void RandomCreation()
        {
            Random rnd = new Random();

            Customer cust = Customer.New();
            cust.Name = "Blabla";

            double total = 0.0;

            for (int i = 0 ; i < 5 ; i++)
            {
                Order order = Order.New();

                order.Customer = cust;

                for (int j = 0 ; j < 20 ; j++)
                {
                    int qty = rnd.Next(1,10);
                    double price = rnd.NextDouble() * 500.0;

                    order.OrderItems.Add(OrderItem.New("test",(short)qty,price));

                    total += qty * price;
                }

                order.Save();
            }

            CSList<Order> orders = new CSList<Order>();

            Assert.AreEqual(5,orders.Count);

            double total2 = Convert.ToDouble(OrderItem.GetScalar("Qty*Price",CSAggregate.Sum));

            Assert.AreEqual(total,total2,0.000001);

            foreach (Order order in orders)
            {
                Assert.AreEqual(cust,order.Customer);
                Assert.AreEqual(20,order.OrderItems.Count);
                Assert.AreEqual(cust.Name,order.Customer.Name);

                order.OrderItems[rnd.Next(0,19)].Delete();

                Assert.AreEqual(19,order.OrderItems.Count);
            }

            total2 = Convert.ToDouble(OrderItem.GetScalar("Qty*Price", CSAggregate.Sum));

            if (total <= total2)
                Assert.Fail();

            Assert.AreEqual(95, new CSList<OrderItem>().Count);
        }

        [Test]
        public void ManyToMany()
        {
            for (int i = 0 ; i < numIterations ; i++)
            {
                DeleteData();

                Customer cust1 = Customer.New();
                Customer cust2 = Customer.New();
                Customer cust3 = Customer.New();

                cust1.Name = "Cust1";
                cust2.Name = "Cust2";
                cust3.Name = "Cust3";

                SalesPerson sp1 = SalesPerson.New();
                SalesPerson sp2 = SalesPerson.New();

                sp1.Name = "SP1";
                sp1.SalesPersonType = SalesPersonType.External;

                sp2.Name = "SP2";
                sp2.SalesPersonType = SalesPersonType.Internal;

                cust1.Save();
                cust2.Save();
                cust3.Save();
                sp1.Save();
                sp2.Save();

                sp1 = SalesPerson.Read(sp1.ID);

                Assert.AreEqual(SalesPersonType.External, sp1.SalesPersonType);

                Order order;

                order = Order.New();
                order.SalesPerson = sp1;
                order.Customer = cust1;
                order.Save();

                order = Order.New();
                order.SalesPerson = sp2;
                order.Customer = cust1;
                order.Save();

                order = Order.New();
                order.SalesPerson = sp2;
                order.Customer = cust2;
                order.Save();

                order = Order.New();
                order.SalesPerson = sp1;
                order.Customer = cust3;
                order.Save();

                cust1 = Customer.Read(cust1.CustomerID);
                cust2 = Customer.Read(cust2.CustomerID);
                cust3 = Customer.Read(cust3.CustomerID);

                Assert.AreEqual(2,cust1.SalesPeople.Count);
                Assert.AreEqual(1,cust2.SalesPeople.Count);
                Assert.AreEqual(1,cust3.SalesPeople.Count);

                Assert.AreEqual(2, cust1.SalesPeople.GetScalar("*", CSAggregate.Count));
                Assert.AreEqual(1, cust2.SalesPeople.GetScalar("*", CSAggregate.Count));
                Assert.AreEqual(1, cust3.SalesPeople.GetScalar("*", CSAggregate.Count));

                cust1 = Customer.Read(cust1.CustomerID);
                cust2 = Customer.Read(cust2.CustomerID);
                cust3 = Customer.Read(cust3.CustomerID);

                Assert.AreEqual(2,cust1.SalesPeople.Count);
                Assert.AreEqual(1,cust2.SalesPeople.Count);
                Assert.AreEqual(1,cust3.SalesPeople.Count);

                Assert.AreEqual(2, cust1.SalesPeople.GetScalar("*",CSAggregate.Count));
                Assert.AreEqual(1, cust2.SalesPeople.GetScalar("*", CSAggregate.Count));
                Assert.AreEqual(1, cust3.SalesPeople.GetScalar("*", CSAggregate.Count));

            }
        }

        [Test]
        public void PureManyToMany()
        {
            PaymentMethod[] methods = new PaymentMethod[] { PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New() };

            methods[0].Name = "Bank";			methods[0].MonthlyCost = 5;
            methods[1].Name = "Credit Card";	methods[1].MonthlyCost = 50;
            methods[2].Name = "PayPal";			methods[2].MonthlyCost = 10;
            methods[3].Name = "Cash";			methods[3].MonthlyCost = 20;
            methods[4].Name = "Bancontact";		methods[4].MonthlyCost = 100;

            foreach (PaymentMethod method in methods)
                method.Save();

            for (int i = 0; i < 10; i++)
            {
                Customer customer = Customer.New();

                customer.Name = "customer" + (i + 1);

                Random rnd = new Random();
                int nMethods = 0;

                for (int j = 0; j < 5 || nMethods < 1; j++)
                {
                    if ((rnd.Next() % 2) == 0)
                    {
                        Assert.IsNotNull(PaymentMethod.Read(methods[j%5].PaymentMethodID));

                        customer.PaymentMethods.Add(methods[j%5]);
                        nMethods++;
                    }
                }

                customer.Save();

                int customerID = customer.CustomerID;

                customer = Customer.Read(customerID);

                Assert.AreEqual(nMethods, customer.PaymentMethods.Count);

                customer.PaymentMethods.Remove(customer.PaymentMethods[0]);

                customer.Save();

                Assert.AreEqual(nMethods-1, customer.PaymentMethods.Count);

                customer = Customer.Read(customerID);

                Assert.AreEqual(nMethods-1, customer.PaymentMethods.Count);
            }
        }


        [Test]
        public void ReadUniqueKey()
        {
            for (int i = 0 ; i < numIterations * 10 ; i++)
            {
                Order order = Order.New();

                order.Customer = Customer.New();
                order.Customer.Name = "cust" + (i + 1);

                order.Save();
            }

            Random rnd = new Random();

            for (int i = 0 ; i < numIterations * 20 ; i++)
            {
                int n = rnd.Next(1,numIterations * 10);

                Customer cust = Customer.ReadUsingUniqueField("Name","cust" + n);
				
                Assert.IsNotNull(cust);
                Assert.AreEqual("cust" + n,cust.Name);
            }
        }

        [Test]
        [ExpectedException(typeof(CSObjectNotFoundException))]
        public void ObjectNotFound()
        {
            Order order = Order.New();

            order.Customer = Customer.New();
            order.Customer.Name = "me";

            order.Save();


            int orderID = order.OrderID;

            Order order2 = Order.Read(orderID);
			
            Assert.IsNotNull(order2);

            Order.Read(orderID+4234);
        }

        [Test]
        public void ObjectNotFoundSafe()
        {
            Order order = Order.New();

            order.Customer = Customer.New();
            order.Customer.Name = "me";

            order.Save();

            int orderID = order.OrderID;

            Order order2 = Order.ReadSafe(orderID);

            Assert.IsNotNull(order2);

            order2 = Order.ReadSafe(orderID + 4234);

            Assert.IsNull(order2);
        }

        [Test]
        public void DeleteFromCollections()
        {
            Order order = Order.New();

            order.Customer = Customer.New();
            order.Customer.Name = "me";


            for (int i = 0; i < 100; i++)
            {
                OrderItem item = order.OrderItems.AddNew();

                item.Description = "test" + (i + 1);
                item.Price = i;
                item.Qty = (short) i;
            }

            order.Save();

            order = Order.Read(order.OrderID);

            Assert.AreEqual(order.OrderItems.Count, 100);

            foreach (OrderItem item in order.OrderItems)
            {
                if ((item.Qty % 5) == 0)
                    item.MarkForDelete();
            }

            order.Save();

            Assert.AreEqual(order.OrderItems.Count, 80);

            order = Order.Read(order.OrderID);

            Assert.AreEqual(order.OrderItems.Count, 80);

            CSList<OrderItem> items = new CSList<OrderItem>(order.OrderItems);

            items.AddFilter("Qty < 50");

            Assert.AreEqual(40, items.Count);

            items.DeleteAll();

            order = Order.Read(order.OrderID);

            Assert.AreEqual(40, order.OrderItems.Count);

            order.OrderItems.AddFilter("Qty >= 80");

            Assert.AreEqual(16, order.OrderItems.Count);

            order.OrderItems.DeleteAll();

            Assert.AreEqual(0, order.OrderItems.Count);

            order = Order.Read(order.OrderID);

            Assert.AreEqual(24, order.OrderItems.Count);


        }

        [Test]
        public void NullableFields()
        {
            Order order = Order.New();

            order.Customer = Customer.New();
            order.Customer.Name = "blabla";
			
            order.Save();

            int orderId = order.OrderID;

            order = Order.Read(orderId);

            Assert.IsNull(order.SalesPersonID);

            order.SalesPerson = SalesPerson.New();
            order.SalesPerson.Name = "Salesperson";

            order.Save();

            order = Order.Read(orderId);

            Assert.IsNotNull(order.SalesPersonID);

            order.SalesPersonID = null;

            order.Save();

            order = Order.Read(orderId);

            Assert.IsNull(order.SalesPersonID);
        }

        [Test]
        public void OrderBy()
        {

            Order.List().DeleteAll();

            Order order;

            order = Order.New();
            order.Customer = Customer.New();
            order.Customer.Name = "Alain";
            order.OrderDate = new DateTime(2005, 1, 10);
            order.Save();

            order = Order.New();
            order.Customer = Customer.New();
            order.Customer.Name = "Luc";
            order.OrderDate = new DateTime(2005, 1, 6);
            order.Save();

            order = Order.New();
            order.Customer = Customer.New();
            order.Customer.Name = "Gerard";
            order.OrderDate = new DateTime(2005, 1, 8);
            order.Save();

            CSList<Order> orders = Order.List();

            orders.OrderBy = "OrderDate";

            Assert.AreEqual(new DateTime(2005,1,6),orders[0].OrderDate);
            Assert.AreEqual(new DateTime(2005, 1, 10),orders[2].OrderDate);

            orders = new OrderCollection();

            orders.OrderBy = "Customer.Name";

            Assert.AreEqual(orders[0].Customer.Name, "Alain");
            Assert.AreEqual(orders[2].Customer.Name, "Luc");

            CSList<OrderItem> orderItems = new CSList<OrderItem>("Order.Customer.Name=''").OrderedBy("Order.Customer.Name");

            Assert.AreEqual(0, orderItems.Count);

            Assert.AreEqual("Luc", Order.GetScalar("Customer.Name", CSAggregate.Max,"Customer.Name<>'Alain'"));
            Assert.AreEqual("Alain", Order.GetScalar("Customer.Name", CSAggregate.Min));

#if !SQLITE
            Assert.AreEqual(new DateTime(2005, 1, 10), Order.GetScalar<DateTime>("OrderDate", CSAggregate.Max));
            Assert.AreEqual(new DateTime(2005, 1, 6), Order.GetScalar<DateTime>("OrderDate", CSAggregate.Min));
#endif
        }

        [Test]
        public void ComplexFilters()
        {
            PaymentMethod[] methods = new PaymentMethod[] { PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New() };

            methods[0].Name = "Bank";			methods[0].MonthlyCost = 5;
            methods[1].Name = "Credit Card";	methods[1].MonthlyCost = 50;
            methods[2].Name = "PayPal";			methods[2].MonthlyCost = 10;
            methods[3].Name = "Cash";			methods[3].MonthlyCost = 20;
            methods[4].Name = "Bancontact";		methods[4].MonthlyCost = 100;


            foreach (PaymentMethod method in methods)
                method.Save();

            Order order = Order.New();

            order.Customer = Customer.New();
            order.Customer.Name = "test";
            order.Customer.PaymentMethods.Add(methods[2]);
            order.Customer.PaymentMethods.Add(methods[4]);

            order.OrderItems.Add(OrderItem.New("test", 5, 200.0));
            order.OrderItems.Add(OrderItem.New("test", 3, 45.0));

            order.Save();

            order = Order.New();

            order.Customer = Customer.New();
            order.Customer.Name = "blabla";

            order.OrderItems.Add(OrderItem.New("test", 15, 100.0));
            order.OrderItems.Add(OrderItem.New("test2", 6, 35.0));

            order.SalesPerson = SalesPerson.New();
            order.SalesPerson.Name = "SalesPerson1";

            order.Save();

		
            Assert.AreEqual(1, new OrderCollection("count(OrderItems where Price > 100) = 1").Count);

            Assert.AreEqual(2, new CSList<OrderItem>("Order.Customer.Name = 'blabla'").Count);
            Assert.AreEqual(2, new CSList<OrderItem>("len(Order.Customer.Name) = 6").Count);
            Assert.AreEqual(2, new CSList<OrderItem>("len(Order.Customer.Name) = @len", new { len=6 }).Count);
            //Assert.AreEqual(2, new CSList<OrderItem>("left(Order.Customer.Name,3) = 'bla'").Count);
//			Assert.AreEqual(1, new CSList<Order>("countdistinct(OrderItems.Description) = 1").Count);
//			Assert.AreEqual(1, new CSList<Order>("countdistinct(OrderItems.Description) = 2").Count);
            Assert.AreEqual(1, new OrderCollection("max(OrderItems.Price) = 200").Count);
            Assert.AreEqual(1, new OrderCollection("sum(OrderItems.Price) = 245").Count);
            Assert.AreEqual(2, new CSList<Order>("count(OrderItems) = 2").Count);
            Assert.AreEqual(2, new CSList<Order>("has(OrderItems)").Count);
            Assert.AreEqual(1, Customer.List("len(Name)=4").Count);
            //Assert.AreEqual(1, new CSList<Customer>("count(PaymentMethods)>0").Count);
            Assert.AreEqual(1, new CSList<Customer>("sum(PaymentMethods.MonthlyCost)=110").Count);
            Assert.AreEqual(1, new CSList<Customer>("sum(PaymentMethods.MonthlyCost where Name='PayPal')=10").Count);
//			Assert.AreEqual(1, new CSList<Customer>("count(PaymentMethods where PaymentMethodID = @MethodID) > 0", "@MethodID", methods[2].PaymentMethodID).Count);
//			Assert.AreEqual(1, new CSList<Customer>("count(PaymentMethods where Name = @MethodName) > 0", "@MethodName", methods[2].Name).Count);
            Assert.AreEqual(1, new CSList<Customer>("has(PaymentMethods)").Count);


            //Assert.AreEqual(200.0, (double)Customer.GetScalar("Orders.OrderItems.Price", CSAggregate.Max));
        }

        [Test]
        public void Transactions()
        {
            Customer customer = Customer.New();

            customer.Name = "Test1";
            customer.Save();

            Assert.AreEqual(1, new CSList<Customer>().Count);

            using (CSTransaction transaction = new CSTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                Customer customer2 = Customer.New();
                customer2.Name = "Test2";
                customer2.Save();

                transaction.Commit();
            }

            Assert.AreEqual(2, new CSList<Customer>().Count);

            using (/*CSTransaction transaction = */new CSTransaction(IsolationLevel.ReadUncommitted))
            {
                Customer customer3 = Customer.New();
                customer3.Name = "Test3";
                customer3.Save();

                Assert.AreEqual(3, new CSList<Customer>().Count);

                Customer customer4 = Customer.New();
                customer4.Name = "Test4";
                customer4.Save();

                Assert.AreEqual(4, new CSList<Customer>().Count);

                // We don't do a commit, so a rollback will be performed
            }

            Assert.AreEqual(2, new CSList<Customer>().Count);

        }

        [Test]
        public void ReadFirst()
        {
            Customer customer = Customer.New();

            customer.Name = "Bob";
            customer.Save();

            customer = Customer.New();
            customer.Name = "Mike";
            customer.Save();

            customer = Customer.ReadFirst("Name=@Name", "@Name", "Bob");

            Assert.AreEqual(customer.Name, "Bob");
        }

        [Test]
        public void CompositeKey()
        {
            Customer customer = Customer.New();
            customer.Name = "Blabla";
            customer.Save();

            int customerID = customer.CustomerID;

            PaymentMethod[] methods = new PaymentMethod[] { PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New(), PaymentMethod.New() };

            methods[0].Name = "Bank"; methods[0].MonthlyCost = 5;
            methods[1].Name = "Credit Card"; methods[1].MonthlyCost = 50;
            methods[2].Name = "PayPal"; methods[2].MonthlyCost = 10;
            methods[3].Name = "Cash"; methods[3].MonthlyCost = 20;
            methods[4].Name = "Bancontact"; methods[4].MonthlyCost = 100;

            foreach (PaymentMethod method in methods)
                method.Save();

            CustomerPaymentMethodLink link = CustomerPaymentMethodLink.New();
            link.CustomerID = customerID;
            link.PaymentMethodID = methods[0].PaymentMethodID;
            link.Save();

            link = CustomerPaymentMethodLink.Read(customerID, methods[0].PaymentMethodID);

            Assert.IsNotNull(link);
            Assert.AreEqual(customerID, link.CustomerID);
            Assert.AreEqual(methods[0].PaymentMethodID, link.PaymentMethodID);
			
        }

        [Test]
        public void ObjectEvents()
        {
            int customerCreated = 0;

            ObjectEventHandler<Customer> eventDelegate = delegate(Customer obj, EventArgs e) { customerCreated = obj.CustomerID; };

            Customer.AnyObjectCreated += eventDelegate;

            Customer customer = Customer.New();
            customer.Name = "Blabla";
            customer.Save();

            Assert.AreEqual(customer.CustomerID,customerCreated);

            Customer.AnyObjectCreated -= eventDelegate;
        }

        [Test]
        public void ObjectParamaters()
        {
            DeleteData();
            SetupTestData();

            foreach (Customer customer in new CSList<Customer>())
            {
                Assert.AreEqual(4,Order.List("Customer=@Customer", "@Customer", customer).Count);
            }
        }

        [Test]
        public void ListPredicates()
        {
            DeleteData();

            Customer customer = Customer.New();

            customer.Name = "Philippe";
            customer.Save();

            customer = Customer.New();

            customer.Name = "Dirk";
            customer.Save();

            customer = Customer.New();

            customer.Name = "Paul";
            customer.Save();

            CSList<Customer> customers = Customer.List().FilteredBy(delegate(Customer c) { return c.Name.StartsWith("P"); });

            Assert.AreEqual(2, customers.Count);

            customers = customers.FilteredBy(delegate(Customer c) { return c.Name.EndsWith("e"); });

            Assert.AreEqual(1, customers.Count);
        }

        [Test]
        public void DefaultSort()
        {
            Random rnd = new Random();

            Customer cust = Customer.New();
            cust.Name = "Blabla";

            double total = 0.0;

            for (int i = 0; i < 5; i++)
            {
                Order order = Order.New();

                order.Customer = cust;

                for (int j = 0; j < 50; j++)
                {
                    int qty = rnd.Next(1, 10);
                    double price = rnd.NextDouble() * 500.0;

                    order.OrderItems.Add(OrderItem.New("test", (short)qty, price));

                    total += qty * price;
                }

                order.Save();

                order = Order.Read(order.OrderID);

                double lastPrice = 0.0;

                foreach (OrderItem orderItem in order.OrderItems)
                {
                    Assert.IsTrue(orderItem.Price >= lastPrice);

                    lastPrice = orderItem.Price;
                }

                int lastQty = 0;

                foreach (OrderItem orderItem in order.OrderItems.OrderedBy("Qty"))
                {
                    Assert.IsTrue(orderItem.Qty >= lastQty);

                    lastQty = orderItem.Qty;
                }

            }


        }


        [QueryExpression("select Name,count(*) as NumOrders from tblCustomers inner join tblOrders on tblOrders.CustomerID=tblCustomers.CustomerID group by Name order by Name")]
        internal class TestQueryClass
        {
            public string Name;
            public int NumOrders;
        }


        [QueryExpression("select Name,count(*) as NumOrders from tblCustomers inner join tblOrders on tblOrders.CustomerID=tblCustomers.CustomerID group by Name order by Name")]
        internal class TestQuery : CSTypedQuery<TestQuery>
        {
            public string Name;
            public int NumOrders;
        }

        [Test]
        public void TypedQuery()
        {
            DeleteData();
            SetupTestData();

            TestQueryClass[] items = CSDatabase.RunQuery<TestQueryClass>();
			
            Assert.AreEqual(5, items.Length);
            Assert.AreEqual(4, items[3].NumOrders);
            Assert.AreEqual("Customer 1", items[0].Name);
            Assert.AreEqual("Customer 2", items[1].Name);
            Assert.AreEqual("Customer 3", items[2].Name);
            Assert.AreEqual("Customer 4", items[3].Name);

            TestQueryClass item = CSDatabase.RunSingleQuery<TestQueryClass>();

            Assert.IsNotNull(item);
            Assert.AreEqual("Customer 1", item.Name);

            Assert.AreEqual(5, TestQuery.Run().Length);
        }

        [Test][Ignore]
        public void Serialization()
        {
            DeleteData();
            SetupTestData();

            Customer customer = Customer.New();

            customer.Name = "Test";
            customer.Save();

            CSList<Customer> customers = new CSList<Customer>();

            int nCustomers = customers.Count;

            Customer refCustomer = customers[nCustomers-1];

            using (Stream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();

                formatter.Serialize(stream, customer);
                formatter.Serialize(stream, customers);

                stream.Seek(0, SeekOrigin.Begin);

                customer = (Customer)formatter.Deserialize(stream);
                customers = (CSList<Customer>)formatter.Deserialize(stream);
            }

            Assert.AreEqual("Test",customer.Name);
            Assert.AreEqual(nCustomers, customers.Count);
            Assert.AreEqual(refCustomer,customers[nCustomers-1]);
        }

        [Test]
        public void Paging()
        {
            if (GetType().Name == "TestAccess")
                return;



            for (int i=1;i<=70;i++)
            {
                Customer customer = Customer.New();
                Order order = Order.New();

                customer.Name = "Customer" + i.ToString("0000");
                customer.Save();

                order.Customer = customer;
                order.OrderItems.Add(OrderItem.New("test",4,10));
                order.Save();
            }

            CSList<Order> orders;

            CSList<Customer> customers = Customer.OrderedList("Name").Range(11, 10);

            Assert.AreEqual(10,customers.Count);
            Assert.AreEqual("Customer0011", customers[0].Name);
            Assert.AreEqual("Customer0020",customers[9].Name);

            orders = Order.OrderedList("Customer.Name , OrderID").Range(51, 10);

            Assert.AreEqual(10, orders.Count);
            Assert.AreEqual("Customer0051", orders[0].Customer.Name);
            Assert.AreEqual("Customer0060", orders[9].Customer.Name);




        }

        [Test]
        public void Paging2()
        {
                Customer customer = Customer.New();
            customer.Name = "Customer";
                customer.Save();
            for (int i = 1; i <= 5; i++)
            {
                Order order = Order.New();


                order.Customer = customer;
                order.OrderItems.Add(OrderItem.New("test", 4, 10));
                order.Save();
            }

            CSList<Order> orders = Order.List("has(OrderItems where Qty > 1)").OrderedBy("OrderID").Range(2, 2);

            Assert.AreEqual(2,orders.Count);

        }

        [Test]
        public void ClientGeneratedGuid()
        {

            CoolData coolData = CoolData.New();

            coolData.Name = "Blabla";
            coolData.Save();

            Assert.IsNotNull(coolData.CoolDataID);

            Guid id = coolData.CoolDataID;

            coolData = CoolData.Read(id);

            Assert.AreEqual(coolData.CoolDataID,id);
        }

//        [Test]
//        public void ServerGeneratedGuid()
//        {
//            if (GetType() != typeof(TestSQLServer))
//                return;
//                
//            ColdData coldData = ColdData.New();
//
//            coldData.Name = "Blabla";
//            coldData.Save();
//
//            Assert.IsNotNull(coldData.ColdDataID);
//        }
//
    }
}
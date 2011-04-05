using System;
using System.Runtime.Serialization;
using Vici.CoolStorage;

namespace Vici.CoolStorage.UnitTests.Data
{
	[MapTo("tblOrders")]
	public partial class Order : CSObject<Order,int> 
	{
		public int OrderID { get { return (int) GetField("OrderID"); } }
			
		public int CustomerID 
		{ 
			get { return (int) GetField("CustomerID"); } 
			set { SetField("CustomerID",value); } 
		}

        public int? SalesPersonID { get { return (int?) GetField("SalesPersonID"); } set { SetField("SalesPersonID",value); } }

		[MapTo("Date")]
		public DateTime OrderDate { get { return (DateTime) GetField("OrderDate"); } set { SetField("OrderDate",value); } }

		public string DataState { get { return (string) GetField("DataState"); } set { SetField("DataState",value); } }

		[ManyToOne]	[Prefetch] public SalesPerson SalesPerson { get { return (SalesPerson) GetField("SalesPerson"); } set { SetField("SalesPerson",value); } }
		[ManyToOne] [Prefetch] public Customer Customer { get { return (Customer) GetField("Customer"); } set { SetField("Customer",value); } }
		[OneToMany] public CSList<OrderItem> OrderItems { get { return (CSList<OrderItem>) GetField("OrderItems"); } }
	}

	[MapTo("tblOrderItems")]
	public partial class OrderItem : CSObject<OrderItem,int>
	{
        public static OrderItem New(string description,short quantity,double price)
		{
            OrderItem item = New();

			item.Description = description;
			item.Qty = quantity;
			item.Price = price;

            return item;
		}

 		public int OrderItemID { get { return (int) GetField("OrderItemID"); } }
		public int OrderID { get { return (int) GetField("OrderID"); } set { SetField("OrderID",value); } }
		public short Qty { get { return (short) GetField("Qty"); } set { SetField("Qty",value); } }
		[DefaultSort] public double Price { get { return (double) GetField("Price"); } set { SetField("Price",value); } }
		public string Description { get { return (string) GetField("Description"); } set { SetField("Description",value); } }

		[ManyToOne] public Order Order { get { return (Order) GetField("Order"); } set { SetField("Order",value); } }
	}


	public enum SalesPersonType { Internal, External };

	[MapTo("tblSalesPeople")]
	public partial class SalesPerson : CSObject<SalesPerson,int>
	{
        [MapTo("SalesPersonID")]
		public int ID { get { return (int) GetField("ID"); } }
        public string Name { get { return (string) GetField("Name"); } set { SetField("Name",value); } }
		public SalesPersonType? SalesPersonType { get { return (SalesPersonType?) GetField("SalesPersonType"); } set { SetField("SalesPersonType",value); } }
		public int? Test { get { return (int?) GetField("Test"); } set { SetField("Test",value); } }

        [OneToMany] public OrderCollection Orders { get { return (OrderCollection) GetField("Orders"); } }
	}

    [MapTo("tblCustomers")]
    public class Customer2 : CSObject<Customer2, int>
    {
        public int CustomerID { get { return (int) GetField("CustomerID");  } }
        public string Name { get { return (string) GetField("Name");  } set { SetField("Name",value); } }

        [ManyToMany("tblCustomerPaymentMethodLinks", Pure = true)]
        public CSList<PaymentMethod> PaymentMethods { get { return (CSList<PaymentMethod>) GetField("PaymentMethods");  } }
        [OneToMany]
        public OrderCollection Orders { get { return (OrderCollection) GetField("Orders"); } }
        [ManyToMany("tblOrders")]
        public CSList<SalesPerson> SalesPeople { get { return (CSList<SalesPerson>) GetField("SalesPeople");  } }
    }

	[MapTo("tblCustomers")]
	public partial class Customer : CSObject<Customer,int>
	{
        public int CustomerID { get { return (int) GetField("CustomerID");  } }
        public string Name { get { return (string) GetField("Name");  } set { SetField("Name",value); } }

        [ManyToMany("tblCustomerPaymentMethodLinks", Pure = true)]
        public CSList<PaymentMethod> PaymentMethods { get { return (CSList<PaymentMethod>) GetField("PaymentMethods");  } }
        [OneToMany]
        public OrderCollection Orders { get { return (OrderCollection) GetField("Orders"); } }
        [ManyToMany("tblOrders")]
        public CSList<SalesPerson> SalesPeople { get { return (CSList<SalesPerson>) GetField("SalesPeople");  } }
 	}

	[MapTo("tblPaymentMethods")]
	public partial class PaymentMethod : CSObject<PaymentMethod, int>
	{
        public  int PaymentMethodID { get { return (int) GetField("PaymentMethodID"); }  }
		public  string Name { get { return (string) GetField("Name"); } set { SetField("Name",value); } }
		public  int MonthlyCost { get { return (int) GetField("MonthlyCost"); } set { SetField("MonthlyCost",value); } }

		[ManyToMany("tblCustomerPaymentMethodLinks", Pure = true)]
		public  CSList<Customer> Customers { get { return (CSList<Customer>) GetField("Customers"); } }
	}

	[MapTo("tblCustomerPaymentMethodLinks")]
	public  partial class CustomerPaymentMethodLink : CSObject<CustomerPaymentMethodLink, int,int>
	{
		public  int CustomerID { get { return (int) GetField("CustomerID"); } set { SetField("CustomerID",value); } }
		public  long PaymentMethodID { get { return (long) GetField("PaymentMethodID"); } set { SetField("PaymentMethodID",value); } }
/*
		public static new CustomerPaymentMethodLink Read(int customerID, int paymentMethodID)
		{
			return CustomerPaymentMethodLink.Read(customerID, paymentMethodID);
		}
*/		
	}

    [Serializable]
	public partial class OrderCollection : CSList<Order>
	{
		public OrderCollection() { }
		public OrderCollection(string filter) : base(filter) { }
		public new OrderCollection OrderedBy(string orderBy) { return (OrderCollection) base.OrderedBy(orderBy); }
		public new OrderCollection FilteredBy(string filter) { return (OrderCollection) base.FilteredBy(filter); }
	}


    [MapTo("tblCoolData")]
    public class CoolData : CSObject<CoolData, Guid>
    {
        [ClientGenerated]
        public Guid CoolDataID
        {
            get { return (Guid) GetField("CoolDataID"); }
            set { SetField("CoolDataID", value); }
        }

        public string Name { get { return (string)GetField("Name"); } set { SetField("Name", value); } }
    } 

    [MapTo("tblColdData")]
    public  partial class ColdData : CSObject<ColdData, Guid>
    {
        [ServerGenerated]
        public  Guid ColdDataID { get { return (Guid) GetField("ColdDataID"); } }

        public  string Name { get { return (string) GetField("Name"); } set { SetField("Name",value); } }
    }


}
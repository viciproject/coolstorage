using System;
using System.Runtime.Serialization;
using Vici.CoolStorage;

namespace Vici.CoolStorage.UnitTests.Data
{
	[MapTo("tblOrders")]
	public abstract partial class BaseOrder<ObjectType> : CSObject<ObjectType,int> 
		where ObjectType : CSObject<ObjectType>
	{
        [Sequence("Order_seq", Identity = true)]
		public abstract int OrderID { get; }
		public abstract int CustomerID { get; set; }

        public abstract int? SalesPersonID { get;set; }

		[MapTo("Date")]
		public abstract DateTime OrderDate { get; set; }

		public abstract string DataState { get; set; }

		[ManyToOne]	[Prefetch] public abstract SalesPerson SalesPerson { get; set; }
		[ManyToOne] [Prefetch] public abstract Customer Customer { get; set; }
		[OneToMany] public abstract CSList<OrderItem> OrderItems { get; set; }
	}

	public abstract partial class Order : BaseOrder<Order>
	{
	}

	[MapTo("tblOrderItems")]
	public abstract partial class OrderItem : CSObject<OrderItem,int>
	{
        public static OrderItem New(string description,short quantity,double price)
		{
            OrderItem item = New();

			item.Description = description;
			item.Qty = quantity;
			item.Price = price;

            return item;
		}

        [Sequence("OrderItem_seq", Identity = true)]
		public abstract int OrderItemID { get; }
		public abstract int OrderID { get;set; }
		public abstract short Qty { get; set; }
		[DefaultSort] public abstract double Price { get; set; }
		public abstract string Description { get; set; }

		[ManyToOne] public abstract Order Order { get; set; }
	}


	public enum SalesPersonType { Internal, External };

	[MapTo("tblSalesPeople")]
	public abstract partial class SalesPerson : CSObject<SalesPerson,int>
	{
        [MapTo("SalesPersonID")]
        [Sequence("SalesPerson_seq", Identity = true)]
		public abstract int ID { get; }
        public abstract string Name { get;set;}
		public abstract SalesPersonType? SalesPersonType { get; set; }
		public abstract int? Test { get; set; }

        [OneToMany] public abstract OrderCollection Orders { get; }
	}

    [MapTo("tblCustomers")]
    public class Customer2 : CSObject<Customer2, int>
    {
       // [Sequence("Customer_seq", Identity = true)]
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
	public abstract partial class Customer : CSObject<Customer,int>
	{
        //[Sequence("Customer_seq", Identity = true)]
		public abstract int CustomerID { get; }
		public abstract string Name { get; set; }

		[ManyToMany("tblCustomerPaymentMethodLinks",Pure=true)]
		public abstract CSList<PaymentMethod> PaymentMethods { get; }
		[OneToMany]
		public abstract OrderCollection Orders { get; }
		[ManyToMany("tblOrders")]
		public abstract CSList<SalesPerson> SalesPeople { get; }
 	}

	[MapTo("tblPaymentMethods")]
	public abstract partial class PaymentMethod : CSObject<PaymentMethod, int>
	{
        [Sequence("PaymentMethod_seq", Identity = true)]
        public abstract int PaymentMethodID { get; }
		public abstract string Name { get; set; }
		public abstract int MonthlyCost { get; set; }

		[ManyToMany("tblCustomerPaymentMethodLinks", Pure = true)]
		public abstract CSList<Customer> Customers { get; }
	}

	[MapTo("tblCustomerPaymentMethodLinks")]
	public abstract partial class CustomerPaymentMethodLink : CSObject<CustomerPaymentMethodLink, int,int>
	{
		public abstract int CustomerID { get; set; }
		public abstract long PaymentMethodID { get; set; }

		public static new CustomerPaymentMethodLink Read(int customerID, int paymentMethodID)
		{
			return CSObject<CustomerPaymentMethodLink, int, int>.Read(customerID, paymentMethodID);
		}
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
    public abstract partial class ColdData : CSObject<ColdData, Guid>
    {
        [ServerGenerated]
        public abstract Guid ColdDataID { get; }

        public abstract string Name { get; set; }
    }


}
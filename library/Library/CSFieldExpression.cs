using System;
using System.Collections.Generic;

namespace Activa.CoolStorage
{
    /*
    public class ParameterCollection
    {
        private static int _nextVarNum = 1;

        public Dictionary<string, object> Parameters =
            new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        public static ParameterCollection Merge(ParameterCollection p1, ParameterCollection p2)
        {
            ParameterCollection pc = new ParameterCollection();

            foreach (string key in p1.Parameters.Keys)
                pc.Parameters[key] = p1.Parameters[key];

            foreach (string key in p2.Parameters.Keys)
                pc.Parameters[key] = p2.Parameters[key];

            return pc;
        }

        public string Add(object value)
        {
            string varName = "@Var" + _nextVarNum++;

            Parameters[varName] = value;

            return varName;
        }
    }


    public class FieldExpression
    {
        public ParameterCollection Parameters;

        private string _expr = "";

        public FieldExpression()
        {
            Parameters = new ParameterCollection();
        }

        public FieldExpression(string expr)
            : this()
        {
            _expr = expr;
        }

        public FieldExpression(string expr, ParameterCollection p1)
        {
            _expr = expr;

            Parameters = p1;
        }

        public FieldExpression(string expr, ParameterCollection p1, ParameterCollection p2)
        {
            _expr = expr;

            Parameters = ParameterCollection.Merge(p1, p2);
        }


        public static FieldExpression And(FieldExpression expr1, FieldExpression expr2)
        {
            return new FieldExpression("(" + expr1 + " and " + expr2 + ")", expr1.Parameters, expr2.Parameters);
        }

        public static FieldExpression Or(FieldExpression expr1, FieldExpression expr2)
        {
            return new FieldExpression("(" + expr1 + " or " + expr2 + ")", expr1.Parameters, expr2.Parameters);
        }

        public static FieldExpression GreaterThan(FieldExpression expr1, FieldExpression expr2)
        {
            return new FieldExpression("(" + expr1 + " > " + expr2 + ")", expr1.Parameters, expr2.Parameters);
        }

        public static FieldExpression LessThan(FieldExpression expr1, FieldExpression expr2)
        {
            return new FieldExpression("(" + expr1 + " < " + expr2 + ")", expr1.Parameters, expr2.Parameters);
        }

        public static FieldExpression GreaterThanOrEqual(FieldExpression expr1, FieldExpression expr2)
        {
            return new FieldExpression("(" + expr1 + " >= " + expr2 + ")", expr1.Parameters, expr2.Parameters);
        }

        public static FieldExpression LessThanOrEqual(FieldExpression expr1, FieldExpression expr2)
        {
            return new FieldExpression("(" + expr1 + " <= " + expr2 + ")", expr1.Parameters, expr2.Parameters);
        }

        public new FieldExpression Equals(object value)
        {
            if (value == null)
            {
                return new FieldExpression("(" + _expr + " is null)");
            }

            ParameterCollection p = new ParameterCollection();

            return new FieldExpression("(" + _expr + " = " + p.Add(value) + ")", p);
        }

        public FieldExpression NotEquals(object value)
        {
            if (value == null)
            {
                return new FieldExpression("(" + _expr + " is not null)");
            }

            ParameterCollection p = new ParameterCollection();

            return new FieldExpression("(" + _expr + " <> " + p.Add(value) + ")", p);
        }

        public static FieldExpression operator ==(FieldExpression expr, string field)
        {
            return expr.Equals(field);
        }

        public static FieldExpression operator ==(FieldExpression expr, object field)
        {
            return expr.Equals(field);
        }

        public static FieldExpression operator !=(FieldExpression expr, string field)
        {
            return expr.NotEquals(field);
        }

        public static FieldExpression operator !=(FieldExpression expr, object value)
        {
            return expr.NotEquals(value);
        }

        public static FieldExpression operator &(FieldExpression expr1, FieldExpression expr2)
        {
            return And(expr1, expr2);
        }

        public static FieldExpression operator |(FieldExpression expr1, FieldExpression expr2)
        {
            return Or(expr1, expr2);
        }

        public static FieldExpression operator <(FieldExpression expr1, FieldExpression expr2)
        {
            return LessThan(expr1, expr2);
        }

        public static FieldExpression operator >(FieldExpression expr1, FieldExpression expr2)
        {
            return GreaterThan(expr1, expr2);
        }

        public override string ToString()
        {
            return _expr;
        }

        public string Dump()
        {
            string s = _expr;

            foreach (string varName in Parameters.Parameters.Keys)
                s += ", " + varName + "=" + Parameters.Parameters[varName];

            return s;
        }
    }

    public class FieldName : FieldExpression
    {
        public FieldName(string fieldName)
            : base(fieldName)
        {
        }

        public FieldName Of(FieldName fieldName)
        {
            return new FieldName(fieldName + "." + this);
        }
    }

    public class Customer
    {
        public class F
        {
            public static readonly FieldName Name = new FieldName("Name");
        }
    }

    public class Order
    {
        public class F
        {
            public static readonly FieldName Date = new FieldName("Date");
            public static readonly FieldName Customer = new FieldName("Customer");
        }
    }

    public class OrderItem
    {
        public class F
        {
            public static readonly FieldName Order = new FieldName("Order");
        }
    }

    public class fieldtest
    {
        public static readonly FieldName Field1 = new FieldName("Field1");

        public void Test()
        {
            FieldExpression expr1 = (Field1 == "@Test") & (Field1 == "Test");
            FieldExpression expr2 = (Field1 == "Test") | (Field1 == null);
            FieldExpression expr3 = (Customer.F.Name == 5);
            FieldExpression expr4 = (Customer.F.Name.Of(Order.F.Customer.Of(OrderItem.F.Order))) == "Name";

            Console.WriteLine(expr1.Dump());
            Console.WriteLine(expr2.Dump());
            Console.WriteLine(expr3.Dump());
            Console.WriteLine(expr4.Dump());
        }
    }*/
}

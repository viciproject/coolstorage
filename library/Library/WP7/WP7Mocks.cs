using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

#if WINDOWS_PHONE

namespace System.Data
{
    public class TransactionScope : IDisposable
    {
        public void Complete()
        {
            
        }

        public void Dispose()
        {
            
        }
    }

    public enum IsolationLevel
    {
        Unspecified = -1,
        Chaos = 16,
        ReadUncommitted = 256,
        ReadCommitted = 4096,
        RepeatableRead = 65536,
        Serializable = 1048576,
        Snapshot = 16777216,
    }

    [Flags]
    public enum CommandBehavior
    {
        Default = 0,
        SingleResult = 1,
        SchemaOnly = 2,
        KeyInfo = 4,
        SingleRow = 8,
        SequentialAccess = 16,
        CloseConnection = 32,
    }

}

namespace System.Transactions
{


}


namespace Vici.CoolStorage
{
    public static class WP7Mocks
    {
        public static int RemoveAll<T>(this List<T> list, Predicate<T> match)
        {
            int count = 0;

            for (int i = list.Count-1; i >= 0 ; i--)
            {
                if (match(list[i]))
                {
                    list.RemoveAt(i);
                }
            }

            return count;
        }
    }

    public class SerializableAttribute : Attribute
    {
        
    }

    public class NonSerializedAttribute : Attribute
    {
        
    }

    public class SerializationInfo
    {
        public object GetValue(string datastate, Type type)
        {
            return null;
        }

        public void AddValue(string datastate, object value)
        {
        }
    }


    public interface ISerializable
    {
        
    }

    public interface ITypedList
    {
        
    }

    public interface IBindingList
    {
        
    }

    public interface IListSource
    {
        
    }
}
#endif



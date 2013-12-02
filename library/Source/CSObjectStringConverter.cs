using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Vici.Core;

namespace Vici.CoolStorage
{
    public class CSObjectStringConverter : IStringConverter
    {
#if !PCL
        public bool TryConvert(Type objectType, out object obj, NameValueCollection clientData)
        {
            obj = null;

            return false;
        }
#endif
        public bool TryConvert(string value, Type objectType, out object obj)
        {
            if (!objectType.Inspector().IsSubclassOf(typeof(CSObject)))
            {
                obj = null;

                return false;
            }

            MethodInfo csObjectConstructor = null;
            Type type = objectType.GetTypeInfo().BaseType;

            while (type != typeof(CSObject))
            {
                if (type.GetTypeInfo().IsGenericType)
                {
                    Type genericType = type.GetGenericTypeDefinition();

                    if (genericType == typeof(CSObject<,>))
                    {
                        Type[] types = type.Inspector().GetGenericArguments();

                        if (types.Length == 2)
                        {
                            csObjectConstructor = objectType.Inspector().GetMethod("Read",
                                                                       BindingFlags.Static | BindingFlags.Public |
                                                                       BindingFlags.FlattenHierarchy);

                            objectType = types[1];

                            break;
                        }
                    }
                }

                type = type.GetTypeInfo().BaseType;
            }

            object key = value.Convert(objectType);

            if (csObjectConstructor != null)
            {
                try
                {
                    obj = csObjectConstructor.Invoke(null, new[] { key });

                    return true;
                }
                catch (TargetInvocationException invokeException)
                {
                    if (invokeException.InnerException is CSObjectNotFoundException)
                    {
                        obj = null;

                        return true;
                    }
                    else
                        throw;
                }
            }

            obj = null;

            return false;
        }
    }
}
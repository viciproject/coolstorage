#region License
//=============================================================================
// Vici CoolStorage - .NET Object Relational Mapping Library 
//
// Copyright (c) 2004-2009 Philippe Leybaert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//=============================================================================
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
#if !MONOTOUCH && !WINDOWS_PHONE
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
#endif

namespace Vici.CoolStorage
{
    internal class CSFactory
	{
#if !MONOTOUCH && !WINDOWS_PHONE
		private static readonly object _syncObject = new object();

		private static readonly Dictionary<Type,OpCode> _opCodeMap;
	    private static readonly Dictionary<Type, Type> _classMap;

		static CSFactory()
		{
            _opCodeMap = new Dictionary<Type, OpCode>();
            _classMap = new Dictionary<Type, Type>();

			_opCodeMap[typeof(Byte)] = OpCodes.Ldind_U1;
			_opCodeMap[typeof(Char)] = OpCodes.Ldind_I1;
			_opCodeMap[typeof(Int16)] = OpCodes.Ldind_I2;
			_opCodeMap[typeof(Int32)] = OpCodes.Ldind_I4;
			_opCodeMap[typeof(Int64)] = OpCodes.Ldind_I8;
			_opCodeMap[typeof(UInt16)] = OpCodes.Ldind_U2;
			_opCodeMap[typeof(UInt32)] = OpCodes.Ldind_U4;
			_opCodeMap[typeof(UInt64)] = OpCodes.Ldind_I8;
			_opCodeMap[typeof(Single)] = OpCodes.Ldind_R4;
			_opCodeMap[typeof(Double)] = OpCodes.Ldind_R8;
		}


        private static Type GetObjectType(Type baseType)
        {
			Type type;

            if (!_classMap.TryGetValue(baseType, out type))
			{
				lock (_syncObject)
				{
                    if (!_classMap.TryGetValue(baseType, out type))
					{
						type = CreateObjectClass(baseType);

						_classMap.Add(baseType , type);
					}
				}
			}

            return type;
        }
#endif
        private static T CreateObject<T>() where T : CSObject<T>
        {
			if (typeof(T).IsAbstract)
            {
#if MONOTOUCH || WINDOWS_PHONE
				throw new NotSupportedException("Mapping classes should not be declared abstract");
#else
                return (T) Activator.CreateInstance(GetObjectType(typeof(T)));
#endif
			}
			else
			{
				return Activator.CreateInstance<T>();
			}
        }

		private static CSObject CreateObject(Type baseType)
		{
			if (baseType.IsAbstract)
            {
#if MONOTOUCH || WINDOWS_PHONE
				throw new NotSupportedException("Mapping classes should not be declared abstract");
#else
                return (CSObject) Activator.CreateInstance(GetObjectType(baseType));
#endif
			}
			else
			{
				return (CSObject) Activator.CreateInstance(baseType);
			}
		}


#if !MONOTOUCH && !WINDOWS_PHONE

		private static Type CreateObjectClass(Type baseType)
		{
			MethodInfo getFieldMethod = typeof(CSObject).GetMethod("GetField",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,new[] { typeof(string) }, null);
			MethodInfo setFieldMethod = typeof(CSObject).GetMethod("SetField",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,new[] { typeof(string),typeof(object) }, null);
			MethodInfo deserializeMethod = typeof(CSObject).GetMethod("Deserialize", BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public);

			AssemblyName assemblyName = new AssemblyName();

			assemblyName.Name = "Vici.CoolStorage.Assemblies." + baseType.Name;

            // Required for partial trust:
            CustomAttributeBuilder[] assemblyAttributes = new[] 
                { 
                    new CustomAttributeBuilder(typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes), new object[0])
                };

			AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName , AssemblyBuilderAccess.Run, assemblyAttributes);

			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

			TypeBuilder typeBuilder = moduleBuilder.DefineType("Vici.CoolStorage.Generated." + baseType.Name , TypeAttributes.Public | TypeAttributes.Sealed , baseType);

			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(SerializableAttribute).GetConstructor(new Type[0]), new object[0]));

			typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);
			ConstructorBuilder serializationConstructor = typeBuilder.DefineConstructor(MethodAttributes.Family, CallingConventions.Standard, new[] { typeof(SerializationInfo), typeof(StreamingContext) });

			ILGenerator constructorIL = serializationConstructor.GetILGenerator();

			ConstructorInfo baseConstructor = baseType.GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public,null,Type.EmptyTypes,null);

			constructorIL.Emit(OpCodes.Ldarg_0);
			constructorIL.Emit(OpCodes.Call, baseConstructor);
			constructorIL.Emit(OpCodes.Nop);
			constructorIL.Emit(OpCodes.Nop);
			constructorIL.Emit(OpCodes.Ldarg_0);
			constructorIL.Emit(OpCodes.Ldarg_1);
			constructorIL.Emit(OpCodes.Ldarg_2);
			constructorIL.EmitCall(OpCodes.Call, deserializeMethod, null);
			constructorIL.Emit(OpCodes.Nop);
			constructorIL.Emit(OpCodes.Nop);
			constructorIL.Emit(OpCodes.Ret);

			foreach (PropertyInfo baseProperty in baseType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				MethodInfo baseGetMethod = baseProperty.GetGetMethod();
				MethodInfo baseSetMethod = baseProperty.GetSetMethod();

				MethodBuilder newGetMethod = null;
				MethodBuilder newSetMethod = null;

				if (baseGetMethod != null && baseGetMethod.IsAbstract)
				{
					newGetMethod = typeBuilder.DefineMethod(baseGetMethod.Name , MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig , baseProperty.PropertyType , null);

					ILGenerator ilGen = newGetMethod.GetILGenerator();

					ilGen.Emit(OpCodes.Ldarg_0);
					ilGen.Emit(OpCodes.Ldstr , baseProperty.Name);
					ilGen.EmitCall(OpCodes.Call,getFieldMethod,null);

					ilGen.Emit(OpCodes.Unbox_Any, baseProperty.PropertyType);

					ilGen.Emit(OpCodes.Ret);
				}

				if (baseSetMethod != null && baseSetMethod.IsAbstract)
				{
					newSetMethod = typeBuilder.DefineMethod(baseSetMethod.Name , MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null , new[] {baseProperty.PropertyType});

					ILGenerator ilGen = newSetMethod.GetILGenerator();

					ilGen.Emit(OpCodes.Ldarg_0);
					ilGen.Emit(OpCodes.Ldstr , baseProperty.Name);
					ilGen.Emit(OpCodes.Ldarg_1);

					if (baseProperty.PropertyType.IsValueType)
					{
						ilGen.Emit(OpCodes.Box,baseProperty.PropertyType);
					}

					ilGen.EmitCall(OpCodes.Call,setFieldMethod,null);

					ilGen.Emit(OpCodes.Ret);
				}

				if (newSetMethod != null || newGetMethod != null)
				{
					PropertyBuilder newProperty = typeBuilder.DefineProperty(baseProperty.Name , PropertyAttributes.None , baseProperty.PropertyType , new[] { baseProperty.PropertyType });
					
					if (newSetMethod != null)
					{
						newProperty.SetSetMethod(newSetMethod);
						typeBuilder.DefineMethodOverride(newSetMethod,baseSetMethod);
					}

					if (newGetMethod != null)
					{
						newProperty.SetGetMethod(newGetMethod);
						typeBuilder.DefineMethodOverride(newGetMethod,baseGetMethod);
					}
					
				}
			}

			return typeBuilder.CreateType();
		}
#endif

        internal static T New<T>() where T:CSObject<T>
        {
            return CreateObject<T>();
        }

        internal static T ReadSafe<T>(params object[] p) where T : CSObject<T>
        {
            T csObject = CreateObject<T>();

            if (csObject.Read(p))
                return csObject;
            
            return null;
        }

  		internal static T Read<T>(params object[] p) where T:CSObject<T>
		{
  		    T csObject = ReadSafe<T>(p);

            if (csObject != null)
                return csObject;

			throw new CSObjectNotFoundException(typeof(T), p[0]);
		}

		internal static CSObject New(Type type)
		{
			return CreateObject(type);
		}

	}
}

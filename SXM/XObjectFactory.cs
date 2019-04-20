using System;
using System.Reflection;
using System.Threading;
using System.Reflection.Emit;

namespace SXM
{
	/// <summary>
	/// Factory class for building transactional objects. Uses Reflection.Emit to intercept
	/// calls to properties (get methods must be thread-safe).
	/// TO DO: should intercept calls to other methods.
	/// </summary>
	public class XObjectFactory
	{
		Type type;
		Type proxyType;
		TypeBuilder typeBuilder;

		// object fields
		FieldBuilder sxmObjectField;	// object synchronization
		
		// external calls
		MethodInfo openReadMethod = typeof(SynchState).GetMethod("OpenRead");
		MethodInfo openWriteMethod = typeof(SynchState).GetMethod("OpenWrite");

		/// <summary>
		/// Takes type to intercept and returns a factory for creating
		/// transactional instances of that type.
		/// </summary>
		/// <param name="type"></param>
		public XObjectFactory(Type type)
		{
			this.type = type;

			// make sure class is well-formed
			AssertAtomicAttribute();
			AssertFieldsArePrivate();
			//EnsureConstructorsArePrivate();

			// standard reflection.emit preliminaries
			AppDomain appDomain = AppDomain.CurrentDomain;
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Name = "SXMAssembly"; 
			AssemblyBuilder assemblyBuilder = 
				appDomain.DefineDynamicAssembly(assemblyName,
				AssemblyBuilderAccess.Run);
			ModuleBuilder moduleBuilder
				= assemblyBuilder.DefineDynamicModule(assemblyName.Name);

			// build a new type that inherits from the argument type
			typeBuilder
				= moduleBuilder.DefineType("TX_" + type.Name, TypeAttributes.Public, type);

			//	STMObject sxmObject;
			sxmObjectField = typeBuilder.DefineField("sxmObject",
				typeof(SynchState),
				FieldAttributes.Public);

			// make constructors
			foreach (ConstructorInfo constructor in type.GetConstructors())
			{
				MakeConstructor(typeBuilder, constructor);
			}

			// intercept properties
			foreach (PropertyInfo property in type.GetProperties()) 
			{
				InterceptProperty(property);
			}

			// intercept methods
			/*
			foreach (MethodInfo method in type.GetMethods()) 
			{
				InterceptMethod(method);
			}
			*/

			// make type Cloneable
			MakeCloneable();

			// bake the type
			proxyType = typeBuilder.CreateType();
		}


		/// <summary>
		/// Create a new object of proxy type
		/// </summary>
		/// <returns></returns>
		public object Create(params object[] args) 
		{
			return Activator.CreateInstance(proxyType, args);
		}

		/// <summary>
		/// Define no-arg constructor for proxy type
		/// public X_type();
		/// </summary>
		private void MakeConstructor(TypeBuilder typeBuilder, ConstructorInfo baseCtor) 
		{
			// duplicate args for base constructor
			ParameterInfo[] ctorParams = baseCtor.GetParameters();
			Type[] args = new Type[ctorParams.Length];
			for (int i = 0; i < args.Length; i++)
			{
				args[i] = ctorParams[i].ParameterType;
			}
			// build constructor
			ConstructorBuilder constructor = typeBuilder.DefineConstructor(
				baseCtor.Attributes, baseCtor.CallingConvention, args);
			if (constructor == null) 
			{
				throw new ProxyException(type, "Class {0}: constructor does not exist", type.Name);
			}
			// call these methods later
			MethodInfo allocateDataSlot = typeof(Thread).GetMethod("AllocateDataSlot");
			ConstructorInfo synchStateCtor
				= typeof(SynchState).GetConstructor(new Type[] {typeof(ICloneable)});
			#region|	base(...);
			ILGenerator constructorIL = constructor.GetILGenerator();
			constructorIL.Emit(OpCodes.Ldarg_0);
			for (int i = 0; i < args.Length; i++) 
			{
				constructorIL.Emit(OpCodes.Ldarg, i+1);
			}
			constructorIL.Emit(OpCodes.Call, baseCtor);
			#endregion
			#region|	sxmObject = new STMObject(new type());
			constructorIL.Emit(OpCodes.Ldarg_0);
			constructorIL.Emit(OpCodes.Ldflda, sxmObjectField);
			constructorIL.Emit(OpCodes.Ldarg_0);
			constructorIL.Emit(OpCodes.Call, synchStateCtor);
			#endregion
			#region|	return
			constructorIL.Emit(OpCodes.Ret);
			#endregion
		}


		private void InterceptMethod(MethodInfo method) 
		{
			Console.WriteLine("intercepting {0}", method.Name);
		}

		/// <summary>
		/// Intercept a property call.
		/// </summary>
		/// <param name="property"></param>
		private void InterceptProperty(PropertyInfo property) 
		{
			MethodInfo _getMethod = property.GetGetMethod();
			ParameterInfo[] _parameters = _getMethod.GetParameters();

			// intercept get method, if it exists
			if (property.CanRead) 
			{
				// method to intercept
				MethodInfo getMethod = property.GetGetMethod();
				// don't intercept private methods
				if (getMethod.IsPrivate) 
				{
					return;
				}
				if (!getMethod.IsVirtual) 
				{
					throw new ProxyException(type,
						"Property or Indexer get method must be declared virtual: {0}.{1}",
						type.Name, property.Name);
				}
				if (HasWriterAttribute(getMethod)) 
				{
					InterceptWriter(getMethod);
				} 
				else 
				{
					InterceptReader(getMethod);	// default
				}
			}

			// intercept set method, if it exists
			if (property.CanWrite) 
			{
				MethodInfo setMethod = property.GetSetMethod();
				// don't intercept private methods
				if (setMethod.IsPrivate) 
				{
					return;
				}
				if (!setMethod.IsVirtual) 
				{
					throw new ProxyException(type,
						"Property or Indexer set method must be declared virtual: {0}",
						property.Name);
				}
				InterceptWriter(setMethod);
			}
		}

		/// <summary>
		/// Intercept method that modifies object
		/// </summary>
		/// <param name="method">method to intercept</param>
		void InterceptWriter(MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();
			Type[] args = new Type[parameters.Length];
			for (int i = 0; i < args.Length; i++) 
			{
				args[i] = parameters[i].ParameterType;
			}
			// intercepting method
			MethodBuilder myMethod = typeBuilder.DefineMethod(method.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				method.ReturnType,
				args);
			ILGenerator myMethodIL = myMethod.GetILGenerator();
			#region   target = (type)this.stmObject.OpenWrite();
			myMethodIL.Emit(OpCodes.Ldarg_0);
			myMethodIL.Emit(OpCodes.Ldflda, sxmObjectField);
			#endregion
			#region	return target.method(...);
			myMethodIL.Emit(OpCodes.Call, openWriteMethod);
			for (int i = 0; i < args.Length; i++)
			{
				myMethodIL.Emit(OpCodes.Ldarg, i + 1);
			}
			myMethodIL.Emit(OpCodes.Call, method);
			myMethodIL.Emit(OpCodes.Ret);
			#endregion
		}

		/// <summary>
		/// Intercept method that does not modify object
		/// </summary>
		/// <param name="method">method to intercept</param>
		void InterceptReader(MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();
			Type[] args = new Type[parameters.Length];
			for (int i = 0; i < args.Length; i++) 
			{
				args[i] = parameters[i].ParameterType;
			}
			// intercepting method
			MethodBuilder myMethod = typeBuilder.DefineMethod(method.Name,
				MethodAttributes.Public | MethodAttributes.Virtual,
				method.ReturnType,
				args);
			// code for intercepting method
			ILGenerator myMethodIL = myMethod.GetILGenerator();
			#region	target = (type)this.stmObject.OpenRead();
			myMethodIL.Emit(OpCodes.Ldarg_0);
			myMethodIL.Emit(OpCodes.Ldflda, sxmObjectField);
			myMethodIL.Emit(OpCodes.Call, openReadMethod);
			#endregion
			#region		return target.method(...);
			for (int i = 0; i < args.Length; i++)
			{
				myMethodIL.Emit(OpCodes.Ldarg, i + 1);
			}
			myMethodIL.Emit(OpCodes.Call, method);
			myMethodIL.Emit(OpCodes.Ret);
			#endregion
		}

		/// <summary>
		/// If type does not implement ICloneable, add the interface and give it a Clone() method
		/// (just call inherited MemberwiseClone). Otherwise do nothing.
		/// </summary>
		void MakeCloneable() 
		{
			// If I don't already implement ICloneable ...
			if (type.GetInterface("ICloneable") == null) 
			{
				MethodInfo cloneMethod = type.GetMethod("MemberwiseClone",
					BindingFlags.NonPublic | BindingFlags.Instance);
				typeBuilder.AddInterfaceImplementation(typeof(System.ICloneable));
				MethodBuilder myMethod = typeBuilder.DefineMethod("Clone",
					MethodAttributes.Public | MethodAttributes.Virtual,
					typeof(object),
					new Type[] {});
				ILGenerator myMethodIL = myMethod.GetILGenerator();
				#region|	return MemberwiseClone();
				myMethodIL.Emit(OpCodes.Ldarg_0);
				myMethodIL.Emit(OpCodes.Call, cloneMethod);
				myMethodIL.Emit(OpCodes.Ret);
				#endregion
			} 
		}

		void AssertFieldsArePrivate()
		{
			foreach (FieldInfo field in type.GetFields()) {
				if (!field.IsPrivate) 
				{
					throw new ProxyException(type,"Field must be declared private: {0}.{1}", type.Name, field.Name);
				} 
			Console.WriteLine("Field: {0}.{1}", type.Name, field.Name);
		}
		}

		void EnsureConstructorsArePrivate()
		{
			foreach (ConstructorInfo ctor in type.GetConstructors())
				if (!ctor.IsPrivate) 
				{
					throw new ProxyException(type, "Constructor must be declared private: {0}.{1}", type.Name, ctor.Name);
				}
		}

		private void AssertAtomicAttribute()
		{
			foreach (object attribute in type.GetCustomAttributes(true))
			{
				if (attribute is AtomicAttribute)
				{
					return;
				}
			}
			throw new ProxyException(type, "Class {0} does not have Atomic attribute", type.Name);
		}

		private bool HasReaderAttribute(MethodInfo method) 
		{
			foreach (object attribute in method.GetCustomAttributes(true))
			{
				if (attribute is ReaderAttribute)
				{
					return true;
				}
			}
			return false;
		}

		private bool HasWriterAttribute(MethodInfo method) 
		{
			foreach (object attribute in method.GetCustomAttributes(true))
			{
				if (attribute is WriterAttribute)
				{
					return true;
				}
			}
			return false;
		}
	}
}

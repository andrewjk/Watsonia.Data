using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Watsonia.Data
{
	internal static class DynamicProxyFactory
	{
		private static AssemblyBuilder _assemblyBuilder = null;
		private static ModuleBuilder _moduleBuilder = null;
		private static string _exportPath = null;

		private static readonly Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();

		static DynamicProxyFactory()
		{
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Name = "Watsonia.Data.DynamicProxies";

			_assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			_moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyBuilder.GetName().Name, false);
		}

		internal static void SetAssemblyPath(string path)
		{
			// Remove any previously created types so that they will be re-created
			_cachedTypes.Clear();

			AssemblyName newAssemblyName = new AssemblyName();
			newAssemblyName.Name = System.IO.Path.GetFileNameWithoutExtension(path);

			_assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(newAssemblyName, AssemblyBuilderAccess.RunAndSave);
			string fileName = Path.GetFileName(path);
			_moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyBuilder.GetName().Name, fileName);

			_exportPath = path;
		}

		internal static void SaveAssembly()
		{
			string folder = Path.GetDirectoryName(_exportPath);
			string fileName = Path.GetFileName(_exportPath);

			_assemblyBuilder.Save(fileName);

			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			File.Move(fileName, _exportPath);
		}

		internal static Type GetDynamicProxyType(Type parentType, Database database)
		{
			// A new type needs to be made for each type in each database
			// This is because each database may have different naming conventions and primary/foreign key types
			string proxyTypeName = database.DatabaseName + parentType.Name + "Proxy";
			if (!_cachedTypes.ContainsKey(proxyTypeName))
			{
				_cachedTypes.Add(proxyTypeName, CreateType(proxyTypeName, parentType, database));
			}
			return _cachedTypes[proxyTypeName];
		}

		internal static IDynamicProxy GetDynamicProxy(Type parentType, Database database)
		{
			Type proxyType = GetDynamicProxyType(parentType, database);
			IDynamicProxy proxy = (IDynamicProxy)proxyType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			proxy.StateTracker.Database = database;
			return proxy;
		}

		public static T GetDynamicProxy<T>(Database database)
		{
			Type parentType = typeof(T);
			IDynamicProxy proxy = GetDynamicProxy(parentType, database);
			return (T)proxy;
		}

		private static Type CreateType(string typeName, Type parent, Database database)
		{
			System.Diagnostics.Trace.WriteLine("Creating " + typeName, "Dynamic Proxy");

			DynamicProxyTypeMembers members = new DynamicProxyTypeMembers();
			members.PrimaryKeyColumnName = database.Configuration.GetPrimaryKeyColumnName(parent);
			members.PrimaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(parent);

			TypeBuilder type = _moduleBuilder.DefineType(typeName,
				TypeAttributes.Public |
				TypeAttributes.Class |
				TypeAttributes.AutoClass |
				TypeAttributes.AnsiClass |
				TypeAttributes.BeforeFieldInit |
				TypeAttributes.AutoLayout,
				parent);
			type.AddInterfaceImplementation(typeof(IDynamicProxy));

			// Implement INotifyPropertyChanging
			FieldBuilder propertyChangingEventField = CreatePropertyChangingEvent(type);
			members.OnPropertyChangingMethod = CreateOnPropertyChangingMethod(type, propertyChangingEventField);

			// Implement INotifyPropertyChanged
			FieldBuilder propertyChangedEventField = CreatePropertyChangedEvent(type);
			members.OnPropertyChangedMethod = CreateOnPropertyChangedMethod(type, propertyChangedEventField);

			// Add the properties
			AddProperties(type, parent, members, database);

			// Add some methods
			CreateSetValuesFromReaderMethod(type, members);
			CreateGetHashCodeMethod(type, members);
			CreateEqualsMethod(type, members);

			// You'd think it would be easy to override the == and != operators by defining static op_Equality
			// and op_Inequality methods but you'd be wrong.  Apparently C# resolves operator calls at compile
			// time, so overrides added at runtime will never be called.  Maybe this will change one day...
			//CreateEqualityOperator(type, parent, members);
			//CreateInequalityOperator(type, parent, members);

			// Implement IDataErrorInfo
			CreateErrorProperty(type, members);
			CreateErrorItemProperty(type, members);

			// Add the constructor
			AddConstructor(type, parent, members);

			Type t = type.CreateType();

			return t;
		}

		private static void AddConstructor(TypeBuilder type, Type parent, DynamicProxyTypeMembers members)
		{
			ConstructorInfo c = parent.GetConstructor(
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				Type.EmptyTypes,
				null);
			if (c == null || c.IsPrivate)
			{
				throw new InvalidOperationException(
					string.Format("An accessible empty constructor was not found on type {0}", parent.FullName));
			}

			ConstructorBuilder constructor = type.DefineConstructor(
				c.Attributes | MethodAttributes.Public,		// Make it public, dammit
				c.CallingConvention,
				Type.EmptyTypes);

			MethodInfo stateTrackerIsLoadingMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"set_IsLoading", new Type[] { typeof(bool) });

			MethodInfo stateTrackerChangedFieldsMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"get_ChangedFields", Type.EmptyTypes);

			MethodInfo stringListContainsMethod = typeof(List<>).MakeGenericType(typeof(string)).GetMethod(
				"Contains", new Type[] { typeof(string) });

			ILGenerator gen = constructor.GetILGenerator();

			// base.ctor()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, c);

			// this.StateTracker.IsLoading = true;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// Set the default values
			foreach (string propertyName in members.DefaultValues.Keys)
			{
				Label endIfShouldSetDefaultValueLabel = gen.DefineLabel();

				// if (!this.StateTracker.ChangedFields.Contains("Property"))
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
				gen.Emit(OpCodes.Callvirt, stateTrackerChangedFieldsMethod);
				gen.Emit(OpCodes.Ldstr, propertyName);
				gen.Emit(OpCodes.Callvirt, stringListContainsMethod);
				gen.Emit(OpCodes.Brtrue_S, endIfShouldSetDefaultValueLabel);

				object value = members.DefaultValues[propertyName];
				bool isNullable = false;
				Type propertyType = members.GetPropertyMethods[propertyName].ReturnType;
				if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					isNullable = true;
					propertyType = propertyType.GetGenericArguments()[0];
				}

				if (propertyType == typeof(string))
				{
					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldstr, (string)value);
					gen.Emit(OpCodes.Callvirt, members.SetPropertyMethods[propertyName]);
				}
				else if (propertyType == typeof(DateTime))
				{
					ConstructorInfo dateTimeConstructor = typeof(DateTime).GetConstructor(
						new Type[] { typeof(long), typeof(DateTimeKind) });

					DateTime dateTimeValue = (DateTime)value;

					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldc_I8, dateTimeValue.Ticks);
					gen.Emit(OpCodes.Ldc_I4, (int)dateTimeValue.Kind);
					gen.Emit(OpCodes.Newobj, dateTimeConstructor);
					if (isNullable)
					{
						ConstructorInfo nullableDateTimeConstructor = typeof(Nullable<>).MakeGenericType(typeof(DateTime)).GetConstructor(
							new Type[] { typeof(DateTime) });
						gen.Emit(OpCodes.Newobj, nullableDateTimeConstructor);
					}
					gen.Emit(OpCodes.Callvirt, members.SetPropertyMethods[propertyName]);
				}
				else if (propertyType.IsValueType)
				{
					gen.Emit(OpCodes.Ldarg_0);
					switch (Type.GetTypeCode(propertyType))
					{
						case TypeCode.Byte:
						{
							gen.Emit(OpCodes.Ldc_I4, (int)((byte)value));
							break;
						}
						case TypeCode.SByte:
						{
							gen.Emit(OpCodes.Ldc_I4, (int)((sbyte)value));
							break;
						}
						case TypeCode.Int16:
						{
							gen.Emit(OpCodes.Ldc_I4, (int)((short)value));
							break;
						}
						case TypeCode.UInt16:
						{
							gen.Emit(OpCodes.Ldc_I4, (int)((ushort)value));
							break;
						}
						case TypeCode.Int32:
						{
							gen.Emit(OpCodes.Ldc_I4, (int)value);
							break;
						}
						case TypeCode.UInt32:
						{
							gen.Emit(OpCodes.Ldc_I4, unchecked((int)((uint)value)));
							break;
						}
						case TypeCode.Int64:
						{
							gen.Emit(OpCodes.Ldc_I8, Convert.ToInt64(value));
							break;
						}
						case TypeCode.UInt64:
						{
							gen.Emit(OpCodes.Ldc_I8, unchecked((long)((ulong)value)));
							break;
						}
						case TypeCode.Boolean:
						{
							if ((bool)value)
							{
								gen.Emit(OpCodes.Ldc_I4_1);
							}
							else
							{
								gen.Emit(OpCodes.Ldc_I4_0);
							}
							break;
						}
						case TypeCode.Char:
						{
							gen.Emit(OpCodes.Ldc_I4, (int)((char)value));
							break;
						}
						case TypeCode.Single:
						{
							gen.Emit(OpCodes.Ldc_R4, (float)value);
							break;
						}
						case TypeCode.Double:
						{
							gen.Emit(OpCodes.Ldc_R8, (double)value);
							break;
						}
						case TypeCode.Decimal:
						{
							Decimal decimalValue = Convert.ToDecimal(value);
							int[] words = Decimal.GetBits(decimalValue);
							int power = (words[3] >> 16) & 0xff;
							int sign = words[3] >> 31;

							if (power == 0 && decimalValue <= int.MaxValue && decimalValue >= int.MinValue)
							{
								ConstructorInfo decimalConstructor = typeof(Decimal).GetConstructor(
									new Type[1] { typeof(int) });

								gen.Emit(OpCodes.Ldc_I4, (int)decimalValue);
								gen.Emit(OpCodes.Newobj, decimalConstructor);
							}
							else
							{
								ConstructorInfo decimalConstructor = typeof(Decimal).GetConstructor(
									new Type[5] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) });

								gen.Emit(OpCodes.Ldc_I4, words[0]);
								gen.Emit(OpCodes.Ldc_I4, words[1]);
								gen.Emit(OpCodes.Ldc_I4, words[2]);
								gen.Emit(OpCodes.Ldc_I4, sign);
								gen.Emit(OpCodes.Ldc_I4, power);
								gen.Emit(OpCodes.Newobj, decimalConstructor);
							}
							break;
						}
					}
					if (isNullable)
					{
						ConstructorInfo nullableConstructor = typeof(Nullable<>).MakeGenericType(propertyType).GetConstructor(
							new Type[] { propertyType });
						gen.Emit(OpCodes.Newobj, nullableConstructor);
					}
					gen.Emit(OpCodes.Callvirt, members.SetPropertyMethods[propertyName]);
				}
				else
				{
					// Not much we can do here, we just have to silently ignore the troublesome default value
					// We don't want to throw an exception because then users would have to remove the default value attribute
					// to get things to work
					System.Diagnostics.Trace.WriteLine(string.Format("Unsupported default value type for {0}: {1}", propertyName, propertyType));
				}

				gen.MarkLabel(endIfShouldSetDefaultValueLabel);
			}

			// this.StateTracker.IsLoading = false;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// return;
			gen.Emit(OpCodes.Ret);
		}

		private static FieldBuilder CreatePropertyChangingEvent(TypeBuilder type)
		{
			// public event PropertyChangingEventHandler PropertyChanging;
			FieldBuilder eventField = type.DefineField(
				"PropertyChanging",
				typeof(PropertyChangingEventHandler),
				FieldAttributes.Private);
			EventBuilder eventBuilder = type.DefineEvent(
				"PropertyChanging",
				EventAttributes.None,
				typeof(PropertyChangingEventHandler));

			eventBuilder.SetAddOnMethod(CreateAddRemoveEventMethod(type, eventField, typeof(INotifyPropertyChanging), typeof(PropertyChangingEventHandler), true));
			eventBuilder.SetRemoveOnMethod(CreateAddRemoveEventMethod(type, eventField, typeof(INotifyPropertyChanging), typeof(PropertyChangingEventHandler), false));

			return eventField;
		}

		private static FieldBuilder CreatePropertyChangedEvent(TypeBuilder type)
		{
			// public event PropertyChangedEventHandler PropertyChanged;
			FieldBuilder eventField = type.DefineField(
				"PropertyChanged",
				typeof(PropertyChangedEventHandler),
				FieldAttributes.Private);
			EventBuilder eventBuilder = type.DefineEvent(
				"PropertyChanged",
				EventAttributes.None,
				typeof(PropertyChangedEventHandler));

			eventBuilder.SetAddOnMethod(CreateAddRemoveEventMethod(type, eventField, typeof(INotifyPropertyChanged), typeof(PropertyChangedEventHandler), true));
			eventBuilder.SetRemoveOnMethod(CreateAddRemoveEventMethod(type, eventField, typeof(INotifyPropertyChanged), typeof(PropertyChangedEventHandler), false));

			return eventField;
		}

		private static MethodBuilder CreateAddRemoveEventMethod(TypeBuilder type, FieldBuilder eventField, Type declaringType, Type eventHandlerType, bool isAdd)
		{
			string prefix = "remove_";
			string delegateAction = "Remove";
			if (isAdd)
			{
				prefix = "add_";
				delegateAction = "Combine";
			}

			MethodBuilder addremoveMethod = type.DefineMethod(
				prefix + eventField.Name,
				MethodAttributes.Public |
				MethodAttributes.SpecialName |
				MethodAttributes.NewSlot |
				MethodAttributes.HideBySig |
				MethodAttributes.Virtual |
				MethodAttributes.Final,
				null,
				new[] { eventHandlerType });
			addremoveMethod.SetImplementationFlags(MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);

			ILGenerator gen = addremoveMethod.GetILGenerator();

			// PropertyChanged += value; // PropertyChanged -= value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, eventField);
			gen.Emit(OpCodes.Ldarg_1);
			gen.EmitCall(OpCodes.Call,
				typeof(Delegate).GetMethod(
				delegateAction,
				new[] { typeof(Delegate), typeof(Delegate) }),
				null);
			gen.Emit(OpCodes.Castclass, eventHandlerType);
			gen.Emit(OpCodes.Stfld, eventField);
			gen.Emit(OpCodes.Ret);

			MethodInfo intAddRemoveMethod = declaringType.GetMethod(prefix + eventField.Name);
			type.DefineMethodOverride(addremoveMethod, intAddRemoveMethod);

			return addremoveMethod;
		}

		private static MethodBuilder CreateOnPropertyChangingMethod(TypeBuilder type, FieldBuilder propertyChangeEventField)
		{
			MethodBuilder onPropertyChangingMethod = type.DefineMethod(
				"OnPropertyChanging",
				MethodAttributes.Family | MethodAttributes.Virtual,
				null,
				new Type[] { typeof(string) });

			ParameterBuilder propertyName = onPropertyChangingMethod.DefineParameter(1, ParameterAttributes.None, "propertyName");

			ILGenerator gen = onPropertyChangingMethod.GetILGenerator();

			LocalBuilder handler = gen.DeclareLocal(typeof(PropertyChangingEventHandler));
			LocalBuilder flag = gen.DeclareLocal(typeof(bool));
			Label exitLabel = gen.DefineLabel();

			// PropertyChangingEventHandler changed = PropertyChanging;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, propertyChangeEventField);

			// if (changed != null)
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, exitLabel);

			// changed(this, new PropertyChangingEventArgs(propertyName);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Newobj, typeof(PropertyChangingEventArgs).GetConstructor(new[] { typeof(string) }));
			gen.EmitCall(OpCodes.Callvirt, typeof(PropertyChangingEventHandler).GetMethod("Invoke"), null);

			// return;
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ret);

			return onPropertyChangingMethod;
		}

		private static MethodBuilder CreateOnPropertyChangedMethod(TypeBuilder type, FieldBuilder propertyChangeEventField)
		{
			MethodBuilder onPropertyChangedMethod = type.DefineMethod(
				"OnPropertyChanged",
				MethodAttributes.Family | MethodAttributes.Virtual,
				null,
				new Type[] { typeof(string) });

			ParameterBuilder propertyName = onPropertyChangedMethod.DefineParameter(1, ParameterAttributes.None, "propertyName");

			ILGenerator gen = onPropertyChangedMethod.GetILGenerator();

			LocalBuilder handler = gen.DeclareLocal(typeof(PropertyChangedEventHandler));
			LocalBuilder flag = gen.DeclareLocal(typeof(bool));
			Label exitLabel = gen.DefineLabel();

			// PropertyChangedEventHandler changed = PropertyChanged;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, propertyChangeEventField);

			// if (changed != null)
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, exitLabel);

			// changed(this, new PropertyChangedEventArgs(propertyName);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Newobj, typeof(PropertyChangedEventArgs).GetConstructor(new[] { typeof(string) }));
			gen.EmitCall(OpCodes.Callvirt, typeof(PropertyChangedEventHandler).GetMethod("Invoke"), null);

			// return;
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ret);

			return onPropertyChangedMethod;
		}

		private static void AddProperties(TypeBuilder type, Type parentType, DynamicProxyTypeMembers members, Database database)
		{
			// The StateTracker property must be created first as it is called by the overridden properties
			CreateStateTrackerProperty(type, members);

			// Override the properties on the type
			foreach (PropertyInfo property in database.Configuration.PropertiesToMap(parentType))
			{
				// Check whether the property is a related item
				if (database.Configuration.ShouldMapType(property.PropertyType))
				{
					// Just store this in a list for now.  It will need to be created after we know which associated ID properties
					// we need to override and which ones we need to create
					members.BaseItemProperties.Add(property);
					continue;
				}

				CreateOveriddenFieldProperty(type, parentType, property, members, database);
			}

			// Create the primary key property (e.g. ID or TableID) if it didn't already get created
			if (members.GetPrimaryKeyMethod == null)
			{
				CreateFieldProperty(type, parentType, members.PrimaryKeyColumnName, members.PrimaryKeyColumnType, members, database);
			}

			// Create the PrimaryKeyValue property which just wraps the primary key property
			CreatePrimaryKeyValueProperty(type, members);

			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			PropertyInfo isNewProperty = parentType.GetProperty("IsNew", flags);
			if (isNewProperty != null)
			{
				if (isNewProperty.PropertyType != typeof(bool))
				{
					throw new InvalidOperationException(
						string.Format("The IsNew property on {0} must be of type {1}", parentType.FullName, typeof(bool).FullName));
				}
				CreateOverriddenProperty(type, isNewProperty, members, database);
			}
			else
			{
				CreateProperty(type, "IsNew", typeof(bool), members, database);
			}

			PropertyInfo hasChangesProperty = parentType.GetProperty("HasChanges", flags);
			if (hasChangesProperty != null)
			{
				if (hasChangesProperty.PropertyType != typeof(bool))
				{
					throw new InvalidOperationException(
						string.Format("The HasChanges property on {0} must be of type {1}", parentType.FullName, typeof(bool).FullName));
				}
				CreateOverriddenProperty(type, hasChangesProperty, members, database);
			}
			else
			{
				CreateProperty(type, "HasChanges", typeof(bool), members, database);
			}

			PropertyInfo isValidProperty = parentType.GetProperty("IsValid", flags);
			if (isValidProperty != null)
			{
				if (isValidProperty.PropertyType != typeof(bool))
				{
					throw new InvalidOperationException(
						string.Format("The IsValid property on {0} must be of type {1}", parentType.FullName, typeof(bool).FullName));
				}
			}
			CreateIsValidProperty(type, members);

			PropertyInfo validationErrorsProperty = parentType.GetProperty("ValidationErrors", flags);
			if (validationErrorsProperty != null)
			{
				if (validationErrorsProperty.PropertyType != typeof(List<ValidationError>))
				{
					throw new InvalidOperationException(
						string.Format("The ValidationErrors property on {0} must be of type {1}", parentType.FullName, "List<ValidationError>"));
				}
			}
			CreateValidationErrorsProperty(type, members);

			// Create the related item properties
			foreach (PropertyInfo property in members.BaseItemProperties)
			{
				CreateOverriddenItemProperty(type, property, members, database);
			}
		}

		private static void CheckCreatedProperty(PropertyInfo property, MethodBuilder getMethod, MethodBuilder setMethod, bool isOverridden, DynamicProxyTypeMembers members, Database database)
		{
			// Add the get and set methods to the members class so that we can access them while building the proxy
			members.GetPropertyMethods.Add(property.Name, getMethod);
			if (setMethod != null)
			{
				members.SetPropertyMethods.Add(property.Name, setMethod);
			}

			if (isOverridden)
			{
				// Add the default value to the members class
				object[] attributes = Attribute.GetCustomAttributes(property, typeof(DefaultValueAttribute), true);
				if (attributes.Length > 0)
				{
					DefaultValueAttribute dv = (DefaultValueAttribute)attributes[0];
					members.DefaultValues.Add(property.Name, dv.Value);
				}
				else if (property.PropertyType == typeof(string))
				{
					// Strings are always initialised to the empty string because nulls are horrible
					members.DefaultValues.Add(property.Name, "");
				}
			}

			// If it's the primary key property, make sure it is of the correct type and then add it to the members class
			if (property.Name == members.PrimaryKeyColumnName)
			{
				if (property.PropertyType != members.PrimaryKeyColumnType)
				{
					throw new InvalidOperationException(
						string.Format("The {0} property on {1} must be of type {2}", members.PrimaryKeyColumnName, property.DeclaringType.FullName, members.PrimaryKeyColumnType.FullName));
				}

				members.GetPrimaryKeyMethod = getMethod;
				members.SetPrimaryKeyMethod = setMethod;

				return;
			}

			// Check whether it's the ID of a related item
			if (database.Configuration.IsRelatedItemID(property))
			{
				members.GetRelatedItemIDMethods.Add(property.Name, getMethod);
				members.SetRelatedItemIDMethods.Add(property.Name, setMethod);
			}
		}

		private static void CreateProperty(TypeBuilder type, string propertyName, Type propertyType, DynamicProxyTypeMembers members, Database database, bool isReadOnly = false)
		{
			FieldBuilder field = type.DefineField(
				"_" + propertyName,
				propertyType,
				FieldAttributes.Private);

			PropertyBuilder property = type.DefineProperty(
				propertyName,
				PropertyAttributes.None,
				propertyType,
				null);

			MethodBuilder getMethod = CreatePropertyGetMethod(type, propertyName, propertyType, field);
			MethodBuilder setMethod = null;
			if (!isReadOnly)
			{
				setMethod = CreatePropertySetMethod(type, propertyName, propertyType, field);
			}

			// Map the get and set methods created above to their corresponding property methods
			property.SetGetMethod(getMethod);
			if (!isReadOnly)
			{
				property.SetSetMethod(setMethod);
			}

			CheckCreatedProperty(property, getMethod, setMethod, false, members, database);
		}

		private static MethodBuilder CreatePropertyGetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField)
		{
			MethodBuilder method = type.DefineMethod(
				"get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				propertyType,
				Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder value = gen.DeclareLocal(propertyType);

			// return _property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, privateField);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreatePropertySetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField)
		{
			MethodBuilder method = type.DefineMethod(
				"set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { propertyType });

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			// _property = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, privateField);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateOverriddenProperty(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database, bool isReadOnly = false)
		{
			FieldBuilder field = type.DefineField(
				"_" + property.Name,
				property.PropertyType,
				FieldAttributes.Private);

			PropertyBuilder newProperty = type.DefineProperty(
				property.Name,
				PropertyAttributes.None,
				property.PropertyType,
				null);

			MethodBuilder getMethod = CreateOverriddenPropertyGetMethod(type, property);
			MethodBuilder setMethod = null;
			if (!isReadOnly)
			{
				setMethod = CreateOverriddenPropertySetMethod(type, property);
			}

			// Map the get and set methods created above to their corresponding property methods
			newProperty.SetGetMethod(getMethod);
			if (!isReadOnly)
			{
				newProperty.SetSetMethod(setMethod);
			}

			CheckCreatedProperty(property, getMethod, setMethod, false, members, database);
		}

		private static MethodBuilder CreateOverriddenPropertyGetMethod(TypeBuilder type, PropertyInfo property)
		{
			MethodBuilder method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				property.PropertyType,
				Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder value = gen.DeclareLocal(property.PropertyType);

			Label exitLabel = gen.DefineLabel();

			// return base.Property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateOverriddenPropertySetMethod(TypeBuilder type, PropertyInfo property)
		{
			MethodBuilder method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { property.PropertyType });

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			// base.Property = value;
			gen.Emit(OpCodes.Nop);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, property.GetSetMethod());
			gen.Emit(OpCodes.Nop);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateFieldProperty(TypeBuilder type, Type parentType, string propertyName, Type propertyType, DynamicProxyTypeMembers members, Database database, bool isReadOnly = false)
		{
			FieldBuilder field = type.DefineField(
				"_" + propertyName,
				propertyType,
				FieldAttributes.Private);

			PropertyBuilder property = type.DefineProperty(
				propertyName,
				PropertyAttributes.None,
				propertyType,
				null);

			MethodBuilder getMethod = CreateFieldPropertyGetMethod(type, propertyName, propertyType, field);
			MethodBuilder setMethod = null;
			if (!isReadOnly)
			{
				setMethod = CreateFieldPropertySetMethod(type, parentType, propertyName, propertyType, field, members);
			}

			// Map the get and set methods created above to their corresponding property methods
			property.SetGetMethod(getMethod);
			if (!isReadOnly)
			{
				property.SetSetMethod(setMethod);
			}

			CheckCreatedProperty(property, getMethod, setMethod, false, members, database);
		}

		private static MethodBuilder CreateFieldPropertyGetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField)
		{
			MethodBuilder method = type.DefineMethod(
				"get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				propertyType,
				Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder value = gen.DeclareLocal(propertyType);

			// return _property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, privateField);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateFieldPropertySetMethod(TypeBuilder type, Type parentType, string propertyName, Type propertyType, FieldBuilder privateField, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { propertyType });

			MethodInfo baseOnPropertyChangingMethod = parentType.GetMethod(
				"On" + propertyName + "Changing",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { propertyType },
				null);

			ConstructorInfo actionStringConstructor = typeof(Action<>).MakeGenericType(typeof(string)).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo baseOnPropertyChangedMethod = parentType.GetMethod(
				"On" + propertyName + "Changed",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				Type.EmptyTypes,
				null);

			ConstructorInfo actionConstructor = typeof(Action).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo setPropertyValueMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"SetPropertyValueByRef").MakeGenericMethod(propertyType);

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			ILGenerator gen = method.GetILGenerator();

			// this.StateTracker.SetPropertyValueByRef(ref _thing, value, "Thing", null, null);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldflda, privateField);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldstr, propertyName);
			if (baseOnPropertyChangingMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangingMethod);
				gen.Emit(OpCodes.Newobj, actionStringConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			if (baseOnPropertyChangedMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangedMethod);
				gen.Emit(OpCodes.Newobj, actionConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			gen.Emit(OpCodes.Callvirt, setPropertyValueMethod);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateOveriddenFieldProperty(TypeBuilder type, Type parentType, PropertyInfo property, DynamicProxyTypeMembers members, Database database, bool isReadOnly = false)
		{
			PropertyBuilder propertyBuilder = type.DefineProperty(
				property.Name,
				PropertyAttributes.None,
				property.PropertyType,
				null);

			MethodBuilder getMethod = null;
			MethodBuilder setMethod = null;

			// Check whether the property is a related collection
			Type enumeratedType;
			if (database.Configuration.IsRelatedCollection(property, out enumeratedType))
			{
				getMethod = CreateOverriddenCollectionPropertyGetMethod(type, property, enumeratedType, members);
				if (!isReadOnly)
				{
					setMethod = CreateOverriddenCollectionPropertySetMethod(type, property, members);
				}
			}

			if (getMethod == null)
			{
				getMethod = CreateOverriddenFieldPropertyGetMethod(type, property, members);
			}

			if (setMethod == null && !isReadOnly)
			{
				setMethod = CreateOverriddenFieldPropertySetMethod(type, property, members);
			}

			// Map the get and set methods created above to their corresponding property methods
			propertyBuilder.SetGetMethod(getMethod);
			if (!isReadOnly)
			{
				propertyBuilder.SetSetMethod(setMethod);
			}

			CheckCreatedProperty(property, getMethod, setMethod, true, members, database);
		}

		private static MethodBuilder CreateOverriddenCollectionPropertyGetMethod(TypeBuilder type, PropertyInfo property, Type enumeratedType, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				property.PropertyType,
				Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			MethodInfo loadCollectionMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"LoadCollection", new Type[] { typeof(string) }).MakeGenericMethod(enumeratedType);

			LocalBuilder list = gen.DeclareLocal(typeof(ICollection<>).MakeGenericType(enumeratedType));
			LocalBuilder flag = gen.DeclareLocal(typeof(bool));

			Label endIfLabel = gen.DefineLabel();
			Label exitLabel = gen.DefineLabel();

			// if (base.Property == null)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, endIfLabel);

			// base.Property = this.StateTracker.LoadCollection<PropertyType>();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, loadCollectionMethod);
			gen.Emit(OpCodes.Call, property.GetSetMethod());

			gen.MarkLabel(endIfLabel);

			// return base.Property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);

			gen.MarkLabel(exitLabel);

			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateOverriddenCollectionPropertySetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members)
		{
			MethodBuilder setValueMethod = CreateSetValueMethod(type, property);

			MethodBuilder method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			MethodInfo addLoadedCollectionMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"AddLoadedCollection", new Type[] { typeof(string) });

			ConstructorInfo setValueActionConstructor = typeof(Action<>).MakeGenericType(property.PropertyType).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo baseOnPropertyChangingMethod = property.DeclaringType.GetMethod(
				"On" + property.Name + "Changing",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { property.PropertyType },
				null);

			ConstructorInfo actionStringConstructor = typeof(Action<>).MakeGenericType(typeof(string)).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo baseOnPropertyChangedMethod = property.DeclaringType.GetMethod(
				"On" + property.Name + "Changed",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				Type.EmptyTypes,
				null);

			ConstructorInfo actionConstructor = typeof(Action).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo setPropertyValueMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"SetPropertyValueWithFunction").MakeGenericMethod(property.PropertyType);

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			ILGenerator gen = method.GetILGenerator();

			Label exitLabel = gen.DefineLabel();

			LocalBuilder tempVariable = gen.DeclareLocal(property.PropertyType);

			// this.StateTracker.AddLoadedCollection("Thing");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, addLoadedCollectionMethod);

			// this.StateTracker.SetPropertyValueWithFunction(base.Thing, value, "Thing", SetThing, base.OnThingChanging, base.OnThingChanged);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, setValueMethod);
			gen.Emit(OpCodes.Newobj, setValueActionConstructor);
			if (baseOnPropertyChangingMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangingMethod);
				gen.Emit(OpCodes.Newobj, actionStringConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			if (baseOnPropertyChangedMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangedMethod);
				gen.Emit(OpCodes.Newobj, actionConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			gen.Emit(OpCodes.Callvirt, setPropertyValueMethod);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateOverriddenFieldPropertyGetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				property.PropertyType,
				Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			// return base.Property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateOverriddenFieldPropertySetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members)
		{
			MethodBuilder setValueMethod = CreateSetValueMethod(type, property);

			MethodBuilder method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			ConstructorInfo setValueActionConstructor = typeof(Action<>).MakeGenericType(property.PropertyType).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo baseOnPropertyChangingMethod = property.DeclaringType.GetMethod(
				"On" + property.Name + "Changing",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { property.PropertyType },
				null);

			ConstructorInfo actionStringConstructor = typeof(Action<>).MakeGenericType(typeof(string)).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo baseOnPropertyChangedMethod = property.DeclaringType.GetMethod(
				"On" + property.Name + "Changed",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				Type.EmptyTypes,
				null);

			ConstructorInfo actionConstructor = typeof(Action).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo setPropertyValueMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"SetPropertyValueWithFunction").MakeGenericMethod(property.PropertyType);

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			ILGenerator gen = method.GetILGenerator();

			Label exitLabel = gen.DefineLabel();

			LocalBuilder tempVariable = gen.DeclareLocal(property.PropertyType);

			// this.StateTracker.SetPropertyValueWithFunction(base.Thing, value, "Thing", SetThing, base.OnThingChanging, base.OnThingChanged);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, setValueMethod);
			gen.Emit(OpCodes.Newobj, setValueActionConstructor);
			if (baseOnPropertyChangingMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangingMethod);
				gen.Emit(OpCodes.Newobj, actionStringConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			if (baseOnPropertyChangedMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangedMethod);
				gen.Emit(OpCodes.Newobj, actionConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			gen.Emit(OpCodes.Callvirt, setPropertyValueMethod);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateSetValueMethod(TypeBuilder type, PropertyInfo property)
		{
			MethodBuilder method = type.DefineMethod(
				"Set" + property.Name,
				MethodAttributes.Private | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, property.GetSetMethod());
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateOverriddenItemProperty(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			// If the ID property doesn't exist, create it
			string relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);
			if (!members.GetRelatedItemIDMethods.ContainsKey(relatedItemIDPropertyName))
			{
				Type primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);
				CreateFieldProperty(type, property.DeclaringType, relatedItemIDPropertyName, typeof(Nullable<>).MakeGenericType(primaryKeyColumnType), members, database);
			}

			if (!members.SetRelatedItemIDMethods.ContainsKey(relatedItemIDPropertyName))
			{
				// TODO: more informative message
				throw new InvalidOperationException();
			}

			PropertyBuilder propertyBuilder = type.DefineProperty(
				property.Name,
				PropertyAttributes.None,
				property.PropertyType,
				null);

			MethodBuilder getMethod = CreateOverriddenItemPropertyGetMethod(type, property, members, database);
			MethodBuilder setMethod = CreateOverriddenItemPropertySetMethod(type, property, members, database);

			// Map the get and set methods created above to their corresponding property methods
			propertyBuilder.SetGetMethod(getMethod);
			propertyBuilder.SetSetMethod(setMethod);
		}

		private static MethodBuilder CreateOverriddenItemPropertyGetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			MethodBuilder method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				property.PropertyType,
				Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			Type primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);

			MethodInfo getNullableHasValueMethod = typeof(Nullable<>).MakeGenericType(primaryKeyColumnType).GetMethod(
				"get_HasValue", Type.EmptyTypes);

			MethodInfo getNullableValueMethod = typeof(Nullable<>).MakeGenericType(primaryKeyColumnType).GetMethod(
				"get_Value", Type.EmptyTypes);

			// TODO: Cannot change that LoadItem to take an object parameter, it just won't work
			MethodInfo loadItemMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"LoadItem", new Type[] { typeof(long), typeof(string) }).MakeGenericMethod(property.PropertyType);

			LocalBuilder item = gen.DeclareLocal(property.PropertyType);
			LocalBuilder itemID = gen.DeclareLocal(typeof(Nullable<>).MakeGenericType(primaryKeyColumnType));
			LocalBuilder flag = gen.DeclareLocal(typeof(bool));

			Label label28 = gen.DefineLabel();
			Label label29 = gen.DefineLabel();
			Label label67 = gen.DefineLabel();
			Label exitLabel = gen.DefineLabel();

			string relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);

			// if (base.Item == null && this.ItemID.HasValue)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Brtrue_S, label28);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetRelatedItemIDMethods[relatedItemIDPropertyName]);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloca_S, 1);
			gen.Emit(OpCodes.Call, getNullableHasValueMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Br_S, label29);
			gen.MarkLabel(label28);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.MarkLabel(label29);
			gen.Emit(OpCodes.Stloc_2);
			gen.Emit(OpCodes.Ldloc_2);
			gen.Emit(OpCodes.Brtrue_S, label67);

			// base.Item = this.StateTracker.LoadItem<PropertyType>(this.ItemID.Value, "ItemID");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetRelatedItemIDMethods[relatedItemIDPropertyName]);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloca_S, 1);
			gen.Emit(OpCodes.Call, getNullableValueMethod);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, loadItemMethod);
			gen.Emit(OpCodes.Call, property.GetSetMethod());

			gen.MarkLabel(label67);

			// return base.Property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);

			gen.MarkLabel(exitLabel);

			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateOverriddenItemPropertySetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			MethodBuilder setValueMethod = CreateItemSetValueMethod(type, property, members, database);

			MethodBuilder method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			MethodInfo addLoadedItemMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"AddLoadedItem", new Type[] { typeof(string) });

			ConstructorInfo setValueActionConstructor = typeof(Action<>).MakeGenericType(property.PropertyType).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo baseOnPropertyChangingMethod = property.DeclaringType.GetMethod(
				"On" + property.Name + "Changing",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { property.PropertyType },
				null);

			ConstructorInfo actionStringConstructor = typeof(Action<>).MakeGenericType(typeof(string)).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo baseOnPropertyChangedMethod = property.DeclaringType.GetMethod(
				"On" + property.Name + "Changed",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				Type.EmptyTypes,
				null);

			ConstructorInfo actionConstructor = typeof(Action).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo setPropertyValueMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"SetPropertyValueWithFunction").MakeGenericMethod(property.PropertyType);

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			ILGenerator gen = method.GetILGenerator();

			// this.StateTracker.AddLoadedItem("Thing");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, addLoadedItemMethod);

			// this.StateTracker.SetPropertyValueWithFunction(base.Thing, value, "Thing", SetThing, base.OnThingChanging, base.OnThingChanged);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, setValueMethod);
			gen.Emit(OpCodes.Newobj, setValueActionConstructor);
			if (baseOnPropertyChangingMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangingMethod);
				gen.Emit(OpCodes.Newobj, actionStringConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			if (baseOnPropertyChangedMethod != null)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldftn, baseOnPropertyChangedMethod);
				gen.Emit(OpCodes.Newobj, actionConstructor);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}
			gen.Emit(OpCodes.Callvirt, setPropertyValueMethod);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateItemSetValueMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			MethodInfo setItemIDMethod = CreateItemSetIDMethod(type, property, members, database);

			MethodInfo itemPropertyChangedHandler = CreateItemPropertyChangedEventHandler(type, property, setItemIDMethod, members, database);

			MethodBuilder method = type.DefineMethod(
				"Set" + property.Name,
				MethodAttributes.Private | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			MethodInfo getIsNewMethod = typeof(IDynamicProxy).GetMethod(
				"get_IsNew", Type.EmptyTypes);

			ConstructorInfo eventHandlerConstructor = typeof(PropertyChangedEventHandler).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo addPropertyChangedMethod = typeof(INotifyPropertyChanged).GetMethod(
				"add_PropertyChanged",
				new Type[] { typeof(PropertyChangedEventHandler) });

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			Type primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);

			LocalBuilder variableProxy = gen.DeclareLocal(typeof(IDynamicProxy));
			LocalBuilder flag = gen.DeclareLocal(typeof(bool));
			LocalBuilder itemID = gen.DeclareLocal(typeof(Nullable<>).MakeGenericType(primaryKeyColumnType));

			string relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);

			Label label70 = gen.DefineLabel();
			Label label67 = gen.DefineLabel();
			Label label88 = gen.DefineLabel();

			// base.Thing = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, property.GetSetMethod());

			// if (value != null)
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, label70);

			// IDynamicProxy thingProxy = (IDynamicProxy)value;
			// SetThingID(thingProxy);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, typeof(IDynamicProxy));
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Call, setItemIDMethod);

			// if (thingProxy.IsNew)
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Callvirt, getIsNewMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, label67);

			// thingProxy.PropertyChanged += ThingProxy_PropertyChanged;
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, itemPropertyChangedHandler);
			gen.Emit(OpCodes.Newobj, eventHandlerConstructor);
			gen.Emit(OpCodes.Callvirt, addPropertyChangedMethod);

			gen.MarkLabel(label67);

			gen.Emit(OpCodes.Br_S, label88);
			gen.MarkLabel(label70);

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloca_S, 2);
			gen.Emit(OpCodes.Initobj, typeof(Nullable<long>));
			gen.Emit(OpCodes.Ldloc_2);
			gen.Emit(OpCodes.Call, members.SetRelatedItemIDMethods[relatedItemIDPropertyName]);

			gen.MarkLabel(label88);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateItemSetIDMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			MethodBuilder method = type.DefineMethod(
				"Set" + property.Name + "ID",
				MethodAttributes.Private | MethodAttributes.HideBySig,
				null,
				new Type[] { typeof(IDynamicProxy) });

			Type primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);

			MethodInfo getPrimaryKeyValueMethod = typeof(IDynamicProxy).GetMethod(
				"get_PrimaryKeyValue", Type.EmptyTypes);

			ConstructorInfo newNullableID = typeof(Nullable<>).MakeGenericType(primaryKeyColumnType).GetConstructor(
				new Type[] { primaryKeyColumnType });

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			string relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);

			// this.ThingID = (long)value.PrimaryKeyValue;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, getPrimaryKeyValueMethod);
			if (primaryKeyColumnType.IsValueType)
			{
				gen.Emit(OpCodes.Unbox_Any, primaryKeyColumnType);
			}
			gen.Emit(OpCodes.Newobj, newNullableID);
			gen.Emit(OpCodes.Call, members.SetRelatedItemIDMethods[relatedItemIDPropertyName]);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateItemPropertyChangedEventHandler(TypeBuilder type, PropertyInfo property, MethodInfo setItemIDMethod, DynamicProxyTypeMembers members, Database database)
		{
			MethodBuilder method = type.DefineMethod(
				property.Name + "Proxy_PropertyChanged",
				MethodAttributes.Private | MethodAttributes.HideBySig,
				null,
				new Type[] { typeof(object), typeof(PropertyChangedEventArgs) });

			MethodInfo getPropertyNameMethod = typeof(PropertyChangedEventArgs).GetMethod(
				"get_PropertyName", Type.EmptyTypes);

			MethodInfo stringEqualityMethod = typeof(String).GetMethod(
				"op_Equality", new Type[] { typeof(string), typeof(string) });

			ConstructorInfo eventHandlerConstructor = typeof(PropertyChangedEventHandler).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo removePropertyChangedMethod = typeof(INotifyPropertyChanged).GetMethod(
				"remove_PropertyChanged", new Type[] { typeof(PropertyChangedEventHandler) });

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder sender = method.DefineParameter(1, ParameterAttributes.None, "sender");
			ParameterBuilder e = method.DefineParameter(2, ParameterAttributes.None, "e");

			LocalBuilder thingProxy = gen.DeclareLocal(typeof(IDynamicProxy));
			LocalBuilder flag = gen.DeclareLocal(typeof(Boolean));

			Label exitLabel = gen.DefineLabel();

			// if (e.PropertyName == "ID")
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Callvirt, getPropertyNameMethod);
			gen.Emit(OpCodes.Ldstr, members.PrimaryKeyColumnName);
			gen.Emit(OpCodes.Call, stringEqualityMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, exitLabel);

			// IDynamicProxy thingProxy = (IDynamicProxy)sender;
			// SetThingID(thingProxy);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, typeof(IDynamicProxy));
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Call, setItemIDMethod);

			// TODO: How do you call the same method?
			//// thingProxy.PropertyChanged -= ThingProxy_PropertyChanged;
			//gen.Emit(OpCodes.Ldloc_0);
			//gen.Emit(OpCodes.Ldarg_0);
			//gen.Emit(OpCodes.Ldftn, method);
			//gen.Emit(OpCodes.Newobj, ctor5);
			//gen.Emit(OpCodes.Callvirt, removePropertyChangedMethod);

			gen.MarkLabel(exitLabel);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateStateTrackerProperty(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			// private DynamicProxyStateTracker _stateTracker;
			FieldBuilder stateTrackerField = type.DefineField(
				"_stateTracker",
				typeof(DynamicProxyStateTracker),
				FieldAttributes.Private);

			PropertyBuilder property = type.DefineProperty(
				"StateTracker",
				PropertyAttributes.None,
				typeof(DynamicProxyStateTracker),
				null);

			MethodBuilder getMethod = type.DefineMethod(
				"get_StateTracker",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				typeof(DynamicProxyStateTracker),
				Type.EmptyTypes);

			ConstructorInfo stateTrackerConstructor = typeof(DynamicProxyStateTracker).GetConstructor(
				Type.EmptyTypes);

			ConstructorInfo actionStringConstructor = typeof(Action<>).MakeGenericType(typeof(string)).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			MethodInfo setOnPropertyChangingMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"set_OnPropertyChanging", new Type[] { typeof(Action<>).MakeGenericType(typeof(string)) });

			MethodInfo setOnPropertyChangedMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"set_OnPropertyChanged", new Type[] { typeof(Action<>).MakeGenericType(typeof(string)) });

			MethodInfo setItemMethod = typeof(DynamicProxyStateTracker).GetMethod(
				 "set_Item", new Type[] { typeof(IDynamicProxy) });

			ILGenerator gen = getMethod.GetILGenerator();

			LocalBuilder stateTracker = gen.DeclareLocal(typeof(DynamicProxyStateTracker));
			LocalBuilder flag = gen.DeclareLocal(typeof(bool));

			Label endIfLabel = gen.DefineLabel();
			Label exitLabel = gen.DefineLabel();

			// if (_stateTracker == null)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, stateTrackerField);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, endIfLabel);

			// _stateTracker = new DynamicProxyStateTracker();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Newobj, stateTrackerConstructor);
			gen.Emit(OpCodes.Stfld, stateTrackerField);

			// _stateTracker.OnPropertyChanging = OnPropertyChanging;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, stateTrackerField);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, members.OnPropertyChangingMethod);
			gen.Emit(OpCodes.Newobj, actionStringConstructor);
			gen.Emit(OpCodes.Callvirt, setOnPropertyChangingMethod);

			// _stateTracker.OnPropertyChanged = OnPropertyChanged;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, stateTrackerField);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, members.OnPropertyChangedMethod);
			gen.Emit(OpCodes.Newobj, actionStringConstructor);
			gen.Emit(OpCodes.Callvirt, setOnPropertyChangedMethod);

			// _stateTracker.Item = this;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, stateTrackerField);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, setItemMethod);

			gen.MarkLabel(endIfLabel);

			// return _stateTracker;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, stateTrackerField);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);

			gen.MarkLabel(exitLabel);

			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			// Map the get method created above to its corresponding property method
			property.SetGetMethod(getMethod);

			members.GetStateTrackerMethod = getMethod;
		}

		private static void CreatePrimaryKeyValueProperty(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			PropertyBuilder property = type.DefineProperty(
				"PrimaryKeyValue",
				PropertyAttributes.None,
				typeof(object),
				null);

			MethodBuilder getMethod = CreatePrimaryKeyValuePropertyGetMethod(type, members);
			MethodBuilder setMethod = CreatePrimaryKeyValuePropertySetMethod(type, members);

			property.SetGetMethod(getMethod);
			property.SetSetMethod(setMethod);
		}

		private static MethodBuilder CreatePrimaryKeyValuePropertyGetMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"get_PrimaryKeyValue",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				typeof(object),
				Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder value = gen.DeclareLocal(typeof(object));

			Label exitLabel = gen.DefineLabel();

			// return this.ID;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetPrimaryKeyMethod);
			// Box the return value if it's a value type
			if (members.PrimaryKeyColumnType.IsValueType)
			{
				gen.Emit(OpCodes.Box, members.PrimaryKeyColumnType);
			}
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreatePrimaryKeyValuePropertySetMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"set_PrimaryKeyValue",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { typeof(object) });

			MethodInfo typeofMethod = typeof(Type).GetMethod(
				"GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

			MethodInfo changeTypeMethod = typeof(Convert).GetMethod(
				"ChangeType", new Type[] { typeof(object), typeof(Type) });

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder value = method.DefineParameter(1, ParameterAttributes.None, "value");

			// this.ID = (long)Convert.ChangeType(value, typeof(long));
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldtoken, members.PrimaryKeyColumnType);
			gen.Emit(OpCodes.Call, typeofMethod);
			gen.Emit(OpCodes.Call, changeTypeMethod);
			// Unbox the value if it's a value type
			if (members.PrimaryKeyColumnType.IsValueType)
			{
				gen.Emit(OpCodes.Unbox_Any, members.PrimaryKeyColumnType);
			}
			gen.Emit(OpCodes.Call, members.SetPrimaryKeyMethod);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateIsValidProperty(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			PropertyBuilder property = type.DefineProperty(
				"IsValid",
				PropertyAttributes.None,
				typeof(bool),
				null);

			MethodBuilder getMethod = type.DefineMethod(
				"get_IsValid",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				typeof(bool),
				Type.EmptyTypes);

			MethodInfo getIsValidMethod = typeof(DynamicProxyStateTracker).GetMethod("get_IsValid");

			ILGenerator gen = getMethod.GetILGenerator();

			// return this.StateTracker.IsValid;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Callvirt, getIsValidMethod);
			gen.Emit(OpCodes.Ret);

			// Map the get method to its corresponding property method
			property.SetGetMethod(getMethod);
		}

		private static void CreateValidationErrorsProperty(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			PropertyBuilder property = type.DefineProperty(
				"ValidationErrors",
				PropertyAttributes.None,
				typeof(List<ValidationError>),
				null);

			MethodBuilder getMethod = type.DefineMethod(
				"get_ValidationErrors",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				typeof(List<ValidationError>),
				Type.EmptyTypes);

			MethodInfo getValidationErrorsMethod = typeof(DynamicProxyStateTracker).GetMethod("get_ValidationErrors");

			ILGenerator gen = getMethod.GetILGenerator();

			// return this.StateTracker.ValidationErrors;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Callvirt, getValidationErrorsMethod);
			gen.Emit(OpCodes.Ret);

			// Map the get method to its corresponding property method
			property.SetGetMethod(getMethod);
		}

		private static void CreateSetValuesFromReaderMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"SetValuesFromReader",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { typeof(DbDataReader) });

			MethodInfo stateTrackerIsLoadingMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"set_IsLoading", new Type[] { typeof(Boolean) });

			MethodInfo readerGetNameMethod = typeof(DbDataReader).GetMethod(
				"GetName", new Type[] { typeof(Int32) });

			MethodInfo toUpperInvariantMethod = typeof(string).GetMethod(
				"ToUpperInvariant", Type.EmptyTypes);

			MethodInfo stringEqualityMethod = typeof(string).GetMethod(
				"op_Equality", new Type[] { typeof(string), typeof(string) });

			MethodInfo getValueMethod = typeof(DbDataReader).GetMethod(
				"GetValue", new Type[] { typeof(int) });

			MethodInfo getTypeMethod = typeof(Type).GetMethod(
				 "GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

			MethodInfo changeTypeMethod = typeof(TypeHelper).GetMethod(
				"ChangeType", new Type[] { typeof(Object), typeof(Type) });

			MethodInfo stateTrackerGetChangedFieldsMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"get_ChangedFields", Type.EmptyTypes);

			MethodInfo removeStringMethod = typeof(List<>).MakeGenericType(typeof(string)).GetMethod(
				"Remove", new Type[] { typeof(string) });

			MethodInfo getFieldCountMethod = typeof(DbDataReader).GetMethod(
				"get_FieldCount", Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			ParameterBuilder source = method.DefineParameter(1, ParameterAttributes.None, "source");

			LocalBuilder i = gen.DeclareLocal(typeof(int));
			LocalBuilder fieldName = gen.DeclareLocal(typeof(string));

			Label endIfIsNew = gen.DefineLabel();
			Label endFieldLoop = gen.DefineLabel();
			Label endSwitch = gen.DefineLabel();

			// Define the labels for each property in the switch statement
			Dictionary<string, Label> propertyLabels = new Dictionary<string, Label>();
			foreach (string key in members.SetPropertyMethods.Keys)
			{
				Label pl = gen.DefineLabel();
				propertyLabels.Add(key, pl);
			}

			Label startFieldLoop = gen.DefineLabel();

			// if (!this.IsNew)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetPropertyMethods["IsNew"]);
			gen.Emit(OpCodes.Brtrue_S, endIfIsNew);

			// this.StateTracker.IsLoading = true;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);
			gen.MarkLabel(endIfIsNew);

			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br, endFieldLoop);
			gen.MarkLabel(startFieldLoop);

			// switch (source.GetName(i).ToUpperInvariant())
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Callvirt, readerGetNameMethod);
			gen.Emit(OpCodes.Callvirt, toUpperInvariantMethod);
			gen.Emit(OpCodes.Dup);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Brfalse, endSwitch);

			foreach (string key in members.SetPropertyMethods.Keys)
			{
				// case "PROPERTY":
				gen.Emit(OpCodes.Ldloc_1);
				gen.Emit(OpCodes.Ldstr, key.ToUpperInvariant());
				gen.Emit(OpCodes.Call, stringEqualityMethod);
				gen.Emit(OpCodes.Brtrue, propertyLabels[key]);
			}

			gen.Emit(OpCodes.Br, endSwitch);

			int index = 0;
			foreach (string key in members.SetPropertyMethods.Keys)
			{
				gen.MarkLabel(propertyLabels[key]);

				Type propertyType = members.GetPropertyMethods[key].ReturnType;

				// this.Property = source.GetValue(i);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				gen.Emit(OpCodes.Ldloc_0);
				gen.Emit(OpCodes.Callvirt, getValueMethod);
				gen.Emit(OpCodes.Ldtoken, propertyType);
				gen.Emit(OpCodes.Call, getTypeMethod);
				gen.Emit(OpCodes.Call, changeTypeMethod);
				// Unbox the value if it's a value type
				if (propertyType.IsValueType)
				{
					gen.Emit(OpCodes.Unbox_Any, propertyType);
				}
				gen.Emit(OpCodes.Call, members.SetPropertyMethods[key]);

				// this.StateTracker.ChangedFields.Remove("Property");
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
				gen.Emit(OpCodes.Callvirt, stateTrackerGetChangedFieldsMethod);
				gen.Emit(OpCodes.Ldstr, key);
				gen.Emit(OpCodes.Callvirt, removeStringMethod);
				gen.Emit(OpCodes.Pop);

				index += 1;

				if (index < members.SetPropertyMethods.Count)
				{
					gen.Emit(OpCodes.Br, endSwitch);
				}
			}

			gen.MarkLabel(endSwitch);

			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Add);
			gen.Emit(OpCodes.Stloc_0);
			gen.MarkLabel(endFieldLoop);

			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, getFieldCountMethod);
			gen.Emit(OpCodes.Blt, startFieldLoop);

			// this.StateTracker.IsLoading = false;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// return;
			gen.Emit(OpCodes.Ret);
		}

		private static void CreateGetHashCodeMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"GetHashCode",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				typeof(int),
				Type.EmptyTypes);

			MethodInfo getHashCodeForItemMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"GetItemHashCode", Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder local = gen.DeclareLocal(typeof(int));
			Label exitLabel = gen.DefineLabel();

			// return this.StateTracker.GetHashCodeForItem();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Callvirt, getHashCodeForItemMethod);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);
		}

		private static void CreateEqualsMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"Equals",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				typeof(bool),
				new Type[] { typeof(object) });

			MethodInfo itemEqualsMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"ItemEquals", new Type[] { typeof(object) });

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder local = gen.DeclareLocal(typeof(bool));
			Label exitLabel = gen.DefineLabel();

			// return this.StateTracker.ItemEquals(obj);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, itemEqualsMethod);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);
		}

		private static void CreateErrorProperty(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"System.ComponentModel.IDataErrorInfo.get_Error",
				MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				typeof(string),
				null);
			type.DefineMethodOverride(method, typeof(IDataErrorInfo).GetMethod("get_Error"));

			MethodInfo getErrorTextMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"GetErrorText", Type.EmptyTypes);

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder local = gen.DeclareLocal(typeof(string));
			Label exitLabel = gen.DefineLabel();

			// return this.StateTracker.GetErrorText();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Callvirt, getErrorTextMethod);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);
		}

		private static void CreateErrorItemProperty(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			MethodBuilder method = type.DefineMethod(
				"System.ComponentModel.IDataErrorInfo.get_Item",
				MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				typeof(string),
				new Type[] { typeof(string) });
			type.DefineMethodOverride(method, typeof(IDataErrorInfo).GetMethod("get_Item"));

			MethodInfo getErrorTextMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"GetErrorText", new Type[] { typeof(string) });

			ParameterBuilder columnName = method.DefineParameter(1, ParameterAttributes.None, "columnName");

			ILGenerator gen = method.GetILGenerator();

			LocalBuilder local = gen.DeclareLocal(typeof(string));
			Label exitLabel = gen.DefineLabel();

			// 	return this.StateTracker.GetErrorText(columnName);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, getErrorTextMethod);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Br_S, exitLabel);
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);
		}
	}
}

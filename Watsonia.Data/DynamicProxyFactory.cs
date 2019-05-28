using System;
using System.Collections.Concurrent;
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
		private const string ProxyAssemblyName = "Watsonia.Data.DynamicProxies";

		private static readonly AssemblyBuilder _assemblyBuilder = null;
		private static readonly ModuleBuilder _moduleBuilder = null;
		//private static string _exportPath = null;

		private static readonly Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();
		private static readonly Dictionary<string, ConstructorInfo> _cachedConstructors = new Dictionary<string, ConstructorInfo>();
		private static readonly Dictionary<string, ChildParentMapping> _cachedChildParentMappings = new Dictionary<string, ChildParentMapping>();

		private static readonly object _cachedTypesLock = new object();
		private static readonly object _cachedConstructorsLock = new object();
		private static readonly object _cachedChildParentMappingsLock = new object();

		static DynamicProxyFactory()
		{
			var assemblyName = new AssemblyName(DynamicProxyFactory.ProxyAssemblyName);
			_assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			_moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyBuilder.GetName().Name/*, false*/);  // NOTE: emit was previously false
		}

		// NOTE: This is not supported as of .Net Standard 2.0:

		//internal static void SetAssemblyPath(string path)
		//{
		//	// Remove any previously created types so that they will be re-created
		//	_cachedTypes.Clear();
		//	_cachedChildParentMappings.Clear();

		//	var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(path));
		//	_assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

		//	string fileName = Path.GetFileName(path);
		//	_moduleBuilder = _assemblyBuilder.DefineDynamicModule(_assemblyBuilder.GetName().Name, fileName);

		//	_exportPath = path;
		//}

		//internal static void SaveAssembly()
		//{
		//	string folder = Path.GetDirectoryName(_exportPath);
		//	string fileName = Path.GetFileName(_exportPath);

		//	_assemblyBuilder.Save(fileName);

		//	if (!Directory.Exists(folder))
		//	{
		//		Directory.CreateDirectory(folder);
		//	}
		//	File.Move(fileName, _exportPath);
		//}

		internal static string GetDynamicTypeName(Type parentType, Database database, string suffix = "Proxy")
		{
			// A new type needs to be made for each type in each database
			// This is because each database may have different naming conventions and primary/foreign key types
			return DynamicProxyFactory.ProxyAssemblyName + "." + database.DatabaseName + parentType.Name + suffix;
		}

		internal static Type GetDynamicProxyType(Type parentType, Database database)
		{
			var proxyTypeName = GetDynamicTypeName(parentType, database);
			if (!_cachedTypes.ContainsKey(proxyTypeName))
			{
				lock (_cachedTypesLock)
				{
					if (!_cachedTypes.ContainsKey(proxyTypeName))
					{
						var type = CreateType(proxyTypeName, parentType, database);
						_cachedTypes.Add(proxyTypeName, type);
					}
				}
			}
			return _cachedTypes[proxyTypeName];
		}

		internal static ConstructorInfo GetDynamicProxyConstructor(Type parentType, Database database)
		{
			var proxyTypeName = GetDynamicTypeName(parentType, database);
			if (!_cachedConstructors.ContainsKey(proxyTypeName))
			{
				lock (_cachedConstructorsLock)
				{
					if (!_cachedConstructors.ContainsKey(proxyTypeName))
					{
						var proxyType = GetDynamicProxyType(parentType, database);
						var constructor = proxyType.GetConstructor(Type.EmptyTypes);
						_cachedConstructors.Add(proxyTypeName, constructor);
					}
				}
			}
			return _cachedConstructors[proxyTypeName];
		}

		private static Type GetValueBagType(Type parentType, Database database, DynamicProxyTypeMembers members)
		{
			var valueBagTypeName = GetDynamicTypeName(parentType, database, "ValueBag");
			if (!_cachedTypes.ContainsKey(valueBagTypeName))
			{
				lock (_cachedTypesLock)
				{
					if (!_cachedTypes.ContainsKey(valueBagTypeName))
					{
						var type = CreateValueBagType(valueBagTypeName, members);
						_cachedTypes.Add(valueBagTypeName, type);
					}
				}
			}
			return _cachedTypes[valueBagTypeName];
		}

		private static ChildParentMapping GetChildParentMapping(Type parentType, Database database)
		{
			var childParentMappingName = GetDynamicTypeName(parentType, database, "ChildParentMapping");
			if (!_cachedChildParentMappings.ContainsKey(childParentMappingName))
			{
				lock (_cachedChildParentMappingsLock)
				{
					if (!_cachedChildParentMappings.ContainsKey(childParentMappingName))
					{
						var mapping = LoadChildParentMapping(database);
						_cachedChildParentMappings.Add(childParentMappingName, mapping);
					}
				}
			}
			return _cachedChildParentMappings[childParentMappingName];
		}

		internal static IDynamicProxy GetDynamicProxy(Type parentType, Database database)
		{
			var proxyConstructor = GetDynamicProxyConstructor(parentType, database);
			var proxy = (IDynamicProxy)proxyConstructor.Invoke(Type.EmptyTypes);
			proxy.StateTracker.Database = database;
			return proxy;
		}

		internal static T GetDynamicProxy<T>(Database database)
		{
			var parentType = typeof(T);
			var proxy = GetDynamicProxy(parentType, database);
			return (T)proxy;
		}

		private static Type CreateType(string typeName, Type parentType, Database database)
		{
			System.Diagnostics.Trace.WriteLine("Creating " + typeName, "Dynamic Proxy");

			// Get the child parent mapping
			var childParentMapping = GetChildParentMapping(parentType, database);

			var members = new DynamicProxyTypeMembers();
			members.PrimaryKeyColumnName = database.Configuration.GetPrimaryKeyColumnName(parentType);
			members.PrimaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(parentType);

			var type = _moduleBuilder.DefineType(
				typeName,
				TypeAttributes.Public |
				TypeAttributes.Class |
				TypeAttributes.AutoClass |
				TypeAttributes.AnsiClass |
				TypeAttributes.BeforeFieldInit |
				TypeAttributes.AutoLayout,
				parentType,
				new Type[] { typeof(IDynamicProxy) });

			// Add the PrimaryKeyValueChanged event
			var primaryKeyValueChangedEventField = CreatePrimaryKeyValueChangedEvent(type);
			members.OnPrimaryKeyValueChangedMethod = CreateOnPrimaryKeyValueChangedMethod(type, primaryKeyValueChangedEventField);

			// Add the properties
			AddProperties(type, parentType, members, database, childParentMapping);

			// Create the ValueBag type, now that we know what properties there will be
			members.ValueBagType = GetValueBagType(parentType, database, members);

			// Add some methods
			members.ResetOriginalValuesMethod = CreateResetOriginalValuesMethod(type, parentType, members, database, childParentMapping);
			CreateSetValuesFromReaderMethod(type, members);
			CreateSetValuesFromBagMethod(type, members);
			CreateGetBagFromValuesMethod(type, members);

			CreateMethodToCallStateTrackerMethod(type, "GetHashCode", "GetItemHashCode", typeof(int), Type.EmptyTypes, members);
			CreateMethodToCallStateTrackerMethod(type, "Equals", "ItemEquals", typeof(bool), new Type[] { typeof(object) }, members);

			// You'd think it would be easy to override the == and != operators by defining static op_Equality
			// and op_Inequality methods but you'd be wrong.  Apparently C# resolves operator calls at compile
			// time, so overrides added at runtime will never be called.  Maybe this will change one day...
			//CreateEqualityOperator(type, parent, members);
			//CreateInequalityOperator(type, parent, members);

			// Add the constructor
			AddConstructor(type, parentType, members);

			return type.CreateTypeInfo();
		}

		private static void AddConstructor(TypeBuilder type, Type parentType, DynamicProxyTypeMembers members)
		{
			var c = parentType.GetConstructor(
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				Type.EmptyTypes,
				null);
			if (c == null || c.IsPrivate)
			{
				throw new InvalidOperationException(
					$"An accessible empty constructor was not found on type {parentType.FullName}");
			}

			var constructor = type.DefineConstructor(
				c.Attributes | MethodAttributes.Public,     // Make it public, dammit
				c.CallingConvention,
				Type.EmptyTypes);

			var stateTrackerIsLoadingMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"set_IsLoading", new Type[] { typeof(bool) });

			// TODO: Rather than abusing the SetFields (and requiring it be public!), I could
			// be checking whether the value == the default value? (In this case, I might need
			// to take the DefaultValue attribute into account)
			var stateTrackerSetFieldsMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"get_SetFields", Type.EmptyTypes);

			var stringListContainsMethod = typeof(List<>).MakeGenericType(typeof(string)).GetMethod(
				"Contains", new Type[] { typeof(string) });

			var gen = constructor.GetILGenerator();

			// base.ctor()
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, c);

			// this.StateTracker.IsLoading = true;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// Set the default values
			foreach (var propertyName in members.DefaultValues.Keys)
			{
				var endIfShouldSetDefaultValueLabel = gen.DefineLabel();

				// if (!this.StateTracker.ChangedFields.Contains("Property"))
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
				gen.Emit(OpCodes.Callvirt, stateTrackerSetFieldsMethod);
				gen.Emit(OpCodes.Ldstr, propertyName);
				gen.Emit(OpCodes.Callvirt, stringListContainsMethod);
				gen.Emit(OpCodes.Brtrue_S, endIfShouldSetDefaultValueLabel);

				var value = members.DefaultValues[propertyName];
				var isNullable = false;
				var propertyType = members.GetPropertyMethods[propertyName].ReturnType;
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
					var dateTimeConstructor = typeof(DateTime).GetConstructor(
						new Type[] { typeof(long), typeof(DateTimeKind) });

					var dateTimeValue = (DateTime)value;

					gen.Emit(OpCodes.Ldarg_0);
					gen.Emit(OpCodes.Ldc_I8, dateTimeValue.Ticks);
					gen.Emit(OpCodes.Ldc_I4, (int)dateTimeValue.Kind);
					gen.Emit(OpCodes.Newobj, dateTimeConstructor);
					if (isNullable)
					{
						var nullableDateTimeConstructor = typeof(Nullable<>).MakeGenericType(typeof(DateTime)).GetConstructor(
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
							var decimalValue = Convert.ToDecimal(value);
							var words = decimal.GetBits(decimalValue);
							var power = (words[3] >> 16) & 0xff;
							var sign = words[3] >> 31;

							if (power == 0 && decimalValue <= int.MaxValue && decimalValue >= int.MinValue)
							{
								var decimalConstructor = typeof(decimal).GetConstructor(
									new Type[1] { typeof(int) });

								gen.Emit(OpCodes.Ldc_I4, (int)decimalValue);
								gen.Emit(OpCodes.Newobj, decimalConstructor);
							}
							else
							{
								var decimalConstructor = typeof(decimal).GetConstructor(
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
						var nullableConstructor = typeof(Nullable<>).MakeGenericType(propertyType).GetConstructor(
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
					System.Diagnostics.Trace.WriteLine($"Unsupported default value type for {propertyName}: {propertyType}");
				}

				gen.MarkLabel(endIfShouldSetDefaultValueLabel);
			}

			// this.ResetOriginalValues();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.ResetOriginalValuesMethod);

			// this.StateTracker.IsLoading = false;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// return;
			gen.Emit(OpCodes.Ret);
		}

		private static FieldBuilder CreatePrimaryKeyValueChangedEvent(TypeBuilder type)
		{
			// public event PrimaryKeyValueChangedEventHandler PrimaryKeyValueChanged;
			var eventField = type.DefineField(
				"PrimaryKeyValueChanged",
				typeof(PrimaryKeyValueChangedEventHandler),
				FieldAttributes.Private);
			var eventBuilder = type.DefineEvent(
				"PrimaryKeyValueChanged",
				EventAttributes.None,
				typeof(PrimaryKeyValueChangedEventHandler));

			eventBuilder.SetAddOnMethod(CreateAddRemoveEventMethod(type, eventField, typeof(IDynamicProxy), typeof(PrimaryKeyValueChangedEventHandler), true));
			eventBuilder.SetRemoveOnMethod(CreateAddRemoveEventMethod(type, eventField, typeof(IDynamicProxy), typeof(PrimaryKeyValueChangedEventHandler), false));

			return eventField;
		}

		private static MethodBuilder CreateAddRemoveEventMethod(TypeBuilder type, FieldBuilder eventField, Type declaringType, Type eventHandlerType, bool isAdd)
		{
			var prefix = "remove_";
			var delegateAction = "Remove";
			if (isAdd)
			{
				prefix = "add_";
				delegateAction = "Combine";
			}

			var addremoveMethod = type.DefineMethod(
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

			var gen = addremoveMethod.GetILGenerator();

			// PropertyChanged += value; or
			// PropertyChanged -= value;
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

			var intAddRemoveMethod = declaringType.GetMethod(prefix + eventField.Name);
			type.DefineMethodOverride(addremoveMethod, intAddRemoveMethod);

			return addremoveMethod;
		}

		private static MethodBuilder CreateOnPrimaryKeyValueChangedMethod(TypeBuilder type, FieldBuilder primaryKeyValueChangedEventField)
		{
			var onPrimaryKeyValueChangedMethod = type.DefineMethod(
				"OnPrimaryKeyValueChanged",
				MethodAttributes.Family | MethodAttributes.Virtual,
				null,
				new Type[] { typeof(object) });

			onPrimaryKeyValueChangedMethod.DefineParameter(1, ParameterAttributes.None, "value");

			var gen = onPrimaryKeyValueChangedMethod.GetILGenerator();

			gen.DeclareLocal(typeof(PrimaryKeyValueChangedEventHandler));
			gen.DeclareLocal(typeof(bool));

			var exitLabel = gen.DefineLabel();

			// PrimaryKeyValueChangedEventHandler changed = PrimaryKeyValueChanged;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, primaryKeyValueChangedEventField);

			// if (changed != null)
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Ceq);
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Brtrue_S, exitLabel);

			// changed(this, new PrimaryKeyValueChangedEventArgs(value);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Newobj, typeof(EventArgs.PrimaryKeyValueChangedEventArgs).GetConstructor(new[] { typeof(object) }));
			gen.EmitCall(OpCodes.Callvirt, typeof(PrimaryKeyValueChangedEventHandler).GetMethod("Invoke"), null);

			// return;
			gen.MarkLabel(exitLabel);
			gen.Emit(OpCodes.Ret);

			return onPrimaryKeyValueChangedMethod;
		}

		private static void AddProperties(TypeBuilder type, Type parentType, DynamicProxyTypeMembers members, Database database, ChildParentMapping childParentMapping)
		{
			// The StateTracker property must be created first as it is called by the overridden properties
			CreateStateTrackerProperty(type, members);

			// Override the properties on the type
			foreach (var property in database.Configuration.PropertiesToMap(parentType))
			{
				// Check whether the property is a related item
				// TODO: Change that check to IsRelatedItem
				if (database.Configuration.ShouldMapTypeInternal(property.PropertyType))
				{
					// Just store this in a list for now.  It will need to be created after we know which associated ID properties
					// we need to override and which ones we need to create
					members.BaseItemProperties.Add(property);
					continue;
				}

				CreateOveriddenFieldProperty(type, property, members, database);
			}

			// Create the primary key property (e.g. ID or TableID) if it didn't already get created
			if (members.GetPrimaryKeyMethod == null)
			{
				CreateFieldProperty(type, members.PrimaryKeyColumnName, members.PrimaryKeyColumnType, members);
			}

			// Create the PrimaryKeyValue property which just wraps the primary key property
			CreatePrimaryKeyValueProperty(type, members);

			// Create the IsNew property
			var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			var isNewProperty = parentType.GetProperty("IsNew", flags);
			if (isNewProperty != null)
			{
				if (isNewProperty.PropertyType != typeof(bool))
				{
					throw new InvalidOperationException(
						$"The IsNew property on {parentType.FullName} must be of type {typeof(bool).FullName}");
				}
				CreateOverriddenProperty(type, isNewProperty, members);
			}
			else
			{
				CreateProperty(type, "IsNew", typeof(bool), members);
			}

			// Create the HasChanges property
			var hasChangesProperty = parentType.GetProperty("HasChanges", flags);
			if (hasChangesProperty != null)
			{
				if (hasChangesProperty.PropertyType != typeof(bool))
				{
					throw new InvalidOperationException(
						$"The HasChanges property on {parentType.FullName} must be of type {typeof(bool).FullName}");
				}
				CreateOverriddenProperty(type, hasChangesProperty, members);
			}
			else
			{
				CreateProperty(type, "HasChanges", typeof(bool), members);
			}

			// Create the IsValid property
			var isValidProperty = parentType.GetProperty("IsValid", flags);
			if (isValidProperty != null)
			{
				if (isValidProperty.PropertyType != typeof(bool))
				{
					throw new InvalidOperationException(
						$"The IsValid property on {parentType.FullName} must be of type {typeof(bool).FullName}");
				}
			}
			CreateIsValidProperty(type, members);

			// Create the ValidationErrors property
			var validationErrorsProperty = parentType.GetProperty("ValidationErrors", flags);
			if (validationErrorsProperty != null)
			{
				if (validationErrorsProperty.PropertyType != typeof(IList<ValidationError>))
				{
					throw new InvalidOperationException(
						$"The ValidationErrors property on {parentType.FullName} must be of type {"IList<ValidationError>"}");
				}
			}
			CreateValidationErrorsProperty(type, members);

			// Create the related item properties
			foreach (var property in members.BaseItemProperties)
			{
				CreateOverriddenItemProperty(type, property, members, database);
			}

			// Create the related item properties for parent-child relationships that don't exist as properties
			if (childParentMapping.ContainsKey(parentType))
			{
				foreach (var parent in childParentMapping[parentType])
				{
					// If the ID property doesn't exist, create it
					var relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(parentType, parent);
					if (!members.GetPropertyMethods.ContainsKey(relatedItemIDPropertyName))
					{
						var primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(parent);
						CreateFieldProperty(type, relatedItemIDPropertyName, typeof(Nullable<>).MakeGenericType(primaryKeyColumnType), members);
					}
				}
			}
		}

		private static void CheckCreatedProperty(PropertyInfo property, MethodBuilder getMethod, MethodBuilder setMethod, bool isOverridden, DynamicProxyTypeMembers members)
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
					var dv = (DefaultValueAttribute)attributes[0];
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
						$"The {members.PrimaryKeyColumnName} property on {property.DeclaringType.FullName} must be of type {members.PrimaryKeyColumnType.FullName}");
				}

				members.GetPrimaryKeyMethod = getMethod;
				members.SetPrimaryKeyMethod = setMethod;

				return;
			}
		}

		private static void CreateProperty(TypeBuilder type, string propertyName, Type propertyType, DynamicProxyTypeMembers members, bool isReadOnly = false)
		{
			var field = type.DefineField(
				"_" + propertyName,
				propertyType,
				FieldAttributes.Private);

			var property = type.DefineProperty(
				propertyName,
				PropertyAttributes.None,
				propertyType,
				null);

			var getMethod = CreatePropertyGetMethod(type, propertyName, propertyType, field);
			MethodBuilder setMethod = null;
			if (!isReadOnly)
			{
				setMethod = CreatePropertySetMethod(type, propertyName, propertyType, field, members);
			}

			// Map the get and set methods created above to their corresponding property methods
			property.SetGetMethod(getMethod);
			if (!isReadOnly)
			{
				property.SetSetMethod(setMethod);
			}

			CheckCreatedProperty(property, getMethod, setMethod, false, members);
		}

		private static MethodBuilder CreatePropertyGetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField)
		{
			var method = type.DefineMethod(
				"get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				propertyType,
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			gen.DeclareLocal(propertyType);

			// return _property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, privateField);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreatePropertySetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { propertyType });

			var gen = method.GetILGenerator();

			method.DefineParameter(1, ParameterAttributes.None, "value");

			// _property = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, privateField);

			// TODO: Ugh, this should only be fired if its value has changed
			if (propertyName == members.PrimaryKeyColumnName)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				if (members.PrimaryKeyColumnType.IsValueType)
				{
					gen.Emit(OpCodes.Box, members.PrimaryKeyColumnType);
				}
				gen.Emit(OpCodes.Call, members.OnPrimaryKeyValueChangedMethod);
			}

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateOverriddenProperty(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, bool isReadOnly = false)
		{
			type.DefineField(
				"_" + property.Name,
				property.PropertyType,
				FieldAttributes.Private);

			var newProperty = type.DefineProperty(
				property.Name,
				PropertyAttributes.None,
				property.PropertyType,
				null);

			var getMethod = CreateOverriddenPropertyGetMethod(type, property);
			MethodBuilder setMethod = null;
			if (!isReadOnly)
			{
				setMethod = CreateOverriddenPropertySetMethod(type, property, members);
			}

			// Map the get and set methods created above to their corresponding property methods
			newProperty.SetGetMethod(getMethod);
			if (!isReadOnly)
			{
				newProperty.SetSetMethod(setMethod);
			}

			CheckCreatedProperty(property, getMethod, setMethod, false, members);
		}

		private static MethodBuilder CreateOverriddenPropertyGetMethod(TypeBuilder type, PropertyInfo property)
		{
			var method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				property.PropertyType,
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			gen.DeclareLocal(property.PropertyType);

			var exitLabel = gen.DefineLabel();

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

		private static MethodBuilder CreateOverriddenPropertySetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { property.PropertyType });

			var gen = method.GetILGenerator();

			method.DefineParameter(1, ParameterAttributes.None, "value");

			// base.Property = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, property.GetSetMethod());

			// TODO: Ugh, this should only be fired if its value has changed
			if (property.Name == members.PrimaryKeyColumnName)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				if (members.PrimaryKeyColumnType.IsValueType)
				{
					gen.Emit(OpCodes.Box, members.PrimaryKeyColumnType);
				}
				gen.Emit(OpCodes.Call, members.OnPrimaryKeyValueChangedMethod);
			}

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateFieldProperty(TypeBuilder type, string propertyName, Type propertyType, DynamicProxyTypeMembers members, bool isReadOnly = false)
		{
			var field = type.DefineField(
				"_" + propertyName,
				propertyType,
				FieldAttributes.Private);

			var property = type.DefineProperty(
				propertyName,
				PropertyAttributes.None,
				propertyType,
				null);

			var getMethod = CreateFieldPropertyGetMethod(type, propertyName, propertyType, field);
			MethodBuilder setMethod = null;
			if (!isReadOnly)
			{
				setMethod = CreateFieldPropertySetMethod(type, propertyName, propertyType, field, members);
			}

			// Map the get and set methods created above to their corresponding property methods
			property.SetGetMethod(getMethod);
			if (!isReadOnly)
			{
				property.SetSetMethod(setMethod);
			}

			CheckCreatedProperty(property, getMethod, setMethod, false, members);

			members.ValueBagPropertyNames.Add(propertyName);
		}

		private static MethodBuilder CreateFieldPropertyGetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField)
		{
			var method = type.DefineMethod(
				"get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				propertyType,
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			gen.DeclareLocal(propertyType);

			// return _property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, privateField);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateFieldPropertySetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { propertyType });

			var checkOriginalValueMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"CheckOriginalValue").MakeGenericMethod(propertyType);

			method.DefineParameter(1, ParameterAttributes.None, "value");

			var gen = method.GetILGenerator();

			// _thing = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, privateField);

			// this.StateTracker.CheckOriginalValue("Thing", value);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldstr, propertyName);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, checkOriginalValueMethod);

			// TODO: Ugh, this should only be fired if its value has changed
			if (propertyName == members.PrimaryKeyColumnName)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				if (members.PrimaryKeyColumnType.IsValueType)
				{
					gen.Emit(OpCodes.Box, members.PrimaryKeyColumnType);
				}
				gen.Emit(OpCodes.Call, members.OnPrimaryKeyValueChangedMethod);
			}

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateOveriddenFieldProperty(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database, bool isReadOnly = false)
		{
			var propertyBuilder = type.DefineProperty(
				property.Name,
				PropertyAttributes.None,
				property.PropertyType,
				null);

			MethodBuilder getMethod = null;
			MethodBuilder setMethod = null;

			// Check whether the property is a related collection
			var isRelatedCollection = database.Configuration.IsRelatedCollection(property, out var enumeratedType);
			if (isRelatedCollection)
			{
				getMethod = CreateOverriddenCollectionPropertyGetMethod(type, property, enumeratedType, members);
				if (!isReadOnly)
				{
					setMethod = CreateOverriddenCollectionPropertySetMethod(type, property, members);
				}
			}

			if (getMethod == null)
			{
				getMethod = CreateOverriddenFieldPropertyGetMethod(type, property);
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

			CheckCreatedProperty(property, getMethod, setMethod, true, members);

			if (!isRelatedCollection)
			{
				members.ValueBagPropertyNames.Add(property.Name);
			}
		}

		private static MethodBuilder CreateOverriddenCollectionPropertyGetMethod(TypeBuilder type, PropertyInfo property, Type enumeratedType, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				property.PropertyType,
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			var loadCollectionMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"LoadCollection", new Type[] { typeof(string) }).MakeGenericMethod(enumeratedType);

			gen.DeclareLocal(typeof(IList<>).MakeGenericType(enumeratedType));
			gen.DeclareLocal(typeof(bool));

			var endIfLabel = gen.DefineLabel();
			var exitLabel = gen.DefineLabel();

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

			// If the property isn't an interface (e.g. it's a List<T>) then we need to try and instantiate
			// an instance and hope that it has a constructor that takes an IList<T>
			// TODO: Should probably throw an exception if it doesn't
			if (!property.PropertyType.IsInterface)
			{
				var listConstructor = property.PropertyType.GetConstructor(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					new Type[] { typeof(IList<>).MakeGenericType(enumeratedType) },
					null);
				gen.Emit(OpCodes.Newobj, listConstructor);
			}

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
			var method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			var addLoadedCollectionMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"AddLoadedCollection", new Type[] { typeof(string) });

			method.DefineParameter(1, ParameterAttributes.None, "value");

			var gen = method.GetILGenerator();

			// base.Thing = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, property.GetSetMethod());

			// this.StateTracker.AddLoadedCollection("Thing");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, addLoadedCollectionMethod);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateOverriddenFieldPropertyGetMethod(TypeBuilder type, PropertyInfo property)
		{
			var method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				property.PropertyType,
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			// return base.Property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateOverriddenFieldPropertySetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			var checkOriginalValueMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"CheckOriginalValue").MakeGenericMethod(property.PropertyType);

			method.DefineParameter(1, ParameterAttributes.None, "value");

			var gen = method.GetILGenerator();

			// base.Thing = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, property.GetSetMethod());

			// this.StateTracker.CheckOriginalValue("Thing", value);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, checkOriginalValueMethod);

			// TODO: Ugh, this should only be fired if its value has changed
			if (property.Name == members.PrimaryKeyColumnName)
			{
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldarg_1);
				if (members.PrimaryKeyColumnType.IsValueType)
				{
					gen.Emit(OpCodes.Box, members.PrimaryKeyColumnType);
				}
				gen.Emit(OpCodes.Call, members.OnPrimaryKeyValueChangedMethod);
			}

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateOverriddenItemProperty(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			// If the ID property doesn't exist, create it
			var relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);
			if (!members.GetPropertyMethods.ContainsKey(relatedItemIDPropertyName))
			{
				var primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);
				if (primaryKeyColumnType.IsValueType)
				{
					primaryKeyColumnType = typeof(Nullable<>).MakeGenericType(primaryKeyColumnType);
				}
				CreateFieldProperty(type, relatedItemIDPropertyName, primaryKeyColumnType, members);
			}

			if (!members.SetPropertyMethods.ContainsKey(relatedItemIDPropertyName))
			{
				// TODO: more informative message
				throw new InvalidOperationException();
			}

			var propertyBuilder = type.DefineProperty(
				property.Name,
				PropertyAttributes.None,
				property.PropertyType,
				null);

			var getMethod = CreateOverriddenItemPropertyGetMethod(type, property, members, database);
			var setMethod = CreateOverriddenItemPropertySetMethod(type, property, members, database);

			// Map the get and set methods created above to their corresponding property methods
			propertyBuilder.SetGetMethod(getMethod);
			propertyBuilder.SetSetMethod(setMethod);
		}

		private static MethodBuilder CreateOverriddenItemPropertyGetMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			var method = type.DefineMethod(
				"get_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				property.PropertyType,
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			var primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);
			var primaryKeyColumnIsValueType = primaryKeyColumnType.IsValueType;
			if (primaryKeyColumnIsValueType)
			{
				primaryKeyColumnType = typeof(Nullable<>).MakeGenericType(primaryKeyColumnType);
			}

			// TODO: Cannot change that LoadItem to take an object parameter, it just won't work
			var loadItemMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"LoadItem", new Type[] { typeof(long), typeof(string) }).MakeGenericMethod(property.PropertyType);

			gen.DeclareLocal(property.PropertyType);
			gen.DeclareLocal(primaryKeyColumnType);
			gen.DeclareLocal(typeof(bool));

			var label28 = gen.DefineLabel();
			var label29 = gen.DefineLabel();
			var label67 = gen.DefineLabel();
			var exitLabel = gen.DefineLabel();

			var relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);

			// if (base.Item == null && this.ItemID.HasValue)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Brtrue_S, label28);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetPropertyMethods[relatedItemIDPropertyName]);

			if (primaryKeyColumnIsValueType)
			{
				var getNullableHasValueMethod = primaryKeyColumnType.GetMethod(
					"get_HasValue", Type.EmptyTypes);
				gen.Emit(OpCodes.Stloc_1);
				gen.Emit(OpCodes.Ldloca_S, 1);
				gen.Emit(OpCodes.Call, getNullableHasValueMethod);
				gen.Emit(OpCodes.Ldc_I4_0);
			}
			else
			{
				gen.Emit(OpCodes.Ldnull);
			}

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
			gen.Emit(OpCodes.Call, members.GetPropertyMethods[relatedItemIDPropertyName]);
			gen.Emit(OpCodes.Stloc_1);
			if (primaryKeyColumnIsValueType)
			{
				var getNullableValueMethod = primaryKeyColumnType.GetMethod(
					"get_Value", Type.EmptyTypes);
				gen.Emit(OpCodes.Ldloca_S, 1);
				gen.Emit(OpCodes.Call, getNullableValueMethod);
			}
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
			MethodInfo setItemIDMethod = CreateItemSetIDMethod(type, property, members, database);

			MethodInfo itemPrimaryKeyValueChangedHandler = CreateItemPrimaryKeyValueChangedEventHandler(type, property, setItemIDMethod);

			var method = type.DefineMethod(
				"set_" + property.Name,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				null,
				new Type[] { property.PropertyType });

			var addLoadedItemMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"AddLoadedItem", new Type[] { typeof(string) });

			var eventHandlerConstructor = typeof(PrimaryKeyValueChangedEventHandler).GetConstructor(
				new Type[] { typeof(object), typeof(IntPtr) });

			var removePrimaryKeyValueChangedMethod = typeof(IDynamicProxy).GetMethod(
				"remove_PrimaryKeyValueChanged",
				new Type[] { typeof(PrimaryKeyValueChangedEventHandler) });

			var addPrimaryKeyValueChangedMethod = typeof(IDynamicProxy).GetMethod(
				"add_PrimaryKeyValueChanged",
				new Type[] { typeof(PrimaryKeyValueChangedEventHandler) });

			method.DefineParameter(1, ParameterAttributes.None, "value");

			var gen = method.GetILGenerator();

			var primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);
			if (primaryKeyColumnType.IsValueType)
			{
				primaryKeyColumnType = typeof(Nullable<>).MakeGenericType(primaryKeyColumnType);
			}

			var relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);

			gen.DeclareLocal(typeof(bool));
			gen.DeclareLocal(typeof(IDynamicProxy));
			gen.DeclareLocal(typeof(bool));
			gen.DeclareLocal(typeof(IDynamicProxy));
			gen.DeclareLocal(primaryKeyColumnType);

			var label47 = gen.DefineLabel();
			var label128 = gen.DefineLabel();
			var label138 = gen.DefineLabel();

			// if (base.Property != null)
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Cgt_Un);
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Brfalse_S, label47);

			// IDynamicProxy thingProxy = (IDynamicProxy)base.Property;
			// thingProxy.PrimaryKeyValueChanged -= ThingProxy_PrimaryKeyValueChanged;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, property.GetGetMethod());
			gen.Emit(OpCodes.Castclass, typeof(IDynamicProxy));
			gen.Emit(OpCodes.Stloc_1);
			gen.Emit(OpCodes.Ldloc_1);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, itemPrimaryKeyValueChangedHandler);
			gen.Emit(OpCodes.Newobj, eventHandlerConstructor);
			gen.Emit(OpCodes.Callvirt, removePrimaryKeyValueChangedMethod);

			gen.MarkLabel(label47);

			// base.Property = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Call, property.GetSetMethod());

			// if (value != null)
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldnull);
			gen.Emit(OpCodes.Cgt_Un);
			gen.Emit(OpCodes.Stloc_2);
			gen.Emit(OpCodes.Ldloc_2);
			gen.Emit(OpCodes.Brfalse_S, label128);

			// this.StateTracker.AddLoadedItem("Thing");
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldstr, property.Name);
			gen.Emit(OpCodes.Callvirt, addLoadedItemMethod);

			// IDynamicProxy thingProxy = (IDynamicProxy)value;
			// SetThingID(thingProxy);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, typeof(IDynamicProxy));
			gen.Emit(OpCodes.Stloc_3);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc_3);
			gen.Emit(OpCodes.Call, setItemIDMethod);

			// thingProxy.PrimaryKeyValueChanged += ThingProxy_PrimaryKeyValueChanged;
			gen.Emit(OpCodes.Ldloc_3);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldftn, itemPrimaryKeyValueChangedHandler);
			gen.Emit(OpCodes.Newobj, eventHandlerConstructor);
			gen.Emit(OpCodes.Callvirt, addPrimaryKeyValueChangedMethod);

			gen.Emit(OpCodes.Br_S, label138);

			gen.MarkLabel(label128);

			// this.PropertyID = null;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloca_S, 4);
			gen.Emit(OpCodes.Initobj, primaryKeyColumnType);
			gen.Emit(OpCodes.Ldloc, 4);
			gen.Emit(OpCodes.Call, members.SetPropertyMethods[relatedItemIDPropertyName]);

			gen.MarkLabel(label138);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateItemPrimaryKeyValueChangedEventHandler(TypeBuilder type, PropertyInfo property, MethodInfo setItemIDMethod)
		{
			var method = type.DefineMethod(
				property.Name + "Proxy_PrimaryKeyValueChanged",
				MethodAttributes.Private | MethodAttributes.HideBySig,
				null,
				new Type[] { typeof(object), typeof(EventArgs.PrimaryKeyValueChangedEventArgs) });

			//MethodInfo getPropertyNameMethod = typeof(PrimaryKeyValueChangedEventArgs).GetMethod(
			//	"get_PropertyName", Type.EmptyTypes);

			//MethodInfo stringEqualityMethod = typeof(string).GetMethod(
			//	"op_Equality", new Type[] { typeof(string), typeof(string) });

			//ConstructorInfo eventHandlerConstructor = typeof(PrimaryKeyValueChangedEventHandler).GetConstructor(
			//	new Type[] { typeof(object), typeof(IntPtr) });

			//MethodInfo removePrimaryKeyValueChangedMethod = typeof(INotifyPrimaryKeyValueChanged).GetMethod(
			//	"remove_PrimaryKeyValueChanged", new Type[] { typeof(PrimaryKeyValueChangedEventHandler) });

			var gen = method.GetILGenerator();

			method.DefineParameter(1, ParameterAttributes.None, "sender");
			method.DefineParameter(2, ParameterAttributes.None, "e");

			gen.DeclareLocal(typeof(IDynamicProxy));
			//LocalBuilder flag = gen.DeclareLocal(typeof(bool));

			//Label exitLabel = gen.DefineLabel();

			//// if (e.PropertyName == "ID")
			//gen.Emit(OpCodes.Ldarg_2);
			//gen.Emit(OpCodes.Callvirt, getPropertyNameMethod);
			//gen.Emit(OpCodes.Ldstr, members.PrimaryKeyColumnName);
			//gen.Emit(OpCodes.Call, stringEqualityMethod);
			//gen.Emit(OpCodes.Ldc_I4_0);
			//gen.Emit(OpCodes.Ceq);
			//gen.Emit(OpCodes.Stloc_1);
			//gen.Emit(OpCodes.Ldloc_1);
			//gen.Emit(OpCodes.Brtrue_S, exitLabel);

			// IDynamicProxy thingProxy = (IDynamicProxy)sender;
			// SetThingID(thingProxy);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, typeof(IDynamicProxy));
			gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Call, setItemIDMethod);

			//// TODO: How do you call the same method?
			////// thingProxy.PrimaryKeyValueChanged -= ThingProxy_PrimaryKeyValueChanged;
			////gen.Emit(OpCodes.Ldloc_0);
			////gen.Emit(OpCodes.Ldarg_0);
			////gen.Emit(OpCodes.Ldftn, method);
			////gen.Emit(OpCodes.Newobj, ctor5);
			////gen.Emit(OpCodes.Callvirt, removePrimaryKeyValueChangedMethod);

			//gen.MarkLabel(exitLabel);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateItemSetIDMethod(TypeBuilder type, PropertyInfo property, DynamicProxyTypeMembers members, Database database)
		{
			var method = type.DefineMethod(
				"Set" + property.Name + "ID",
				MethodAttributes.Private | MethodAttributes.HideBySig,
				null,
				new Type[] { typeof(IDynamicProxy) });

			var primaryKeyColumnType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);

			var getPrimaryKeyValueMethod = typeof(IDynamicProxy).GetMethod(
				"get_PrimaryKeyValue", Type.EmptyTypes);

			var gen = method.GetILGenerator();

			method.DefineParameter(1, ParameterAttributes.None, "value");

			var relatedItemIDPropertyName = database.Configuration.GetForeignKeyColumnName(property);

			// this.ThingID = (long)value.PrimaryKeyValue;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Callvirt, getPrimaryKeyValueMethod);
			if (primaryKeyColumnType.IsValueType)
			{
				gen.Emit(OpCodes.Unbox_Any, primaryKeyColumnType);
				var newNullableID = typeof(Nullable<>).MakeGenericType(primaryKeyColumnType).GetConstructor(
					new Type[] { primaryKeyColumnType });
				gen.Emit(OpCodes.Newobj, newNullableID);
			}
			else
			{
				gen.Emit(OpCodes.Castclass, primaryKeyColumnType);
			}
			gen.Emit(OpCodes.Call, members.SetPropertyMethods[relatedItemIDPropertyName]);

			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateStateTrackerProperty(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			// private DynamicProxyStateTracker _stateTracker;
			var stateTrackerField = type.DefineField(
				"_stateTracker",
				typeof(DynamicProxyStateTracker),
				FieldAttributes.Private);

			var property = type.DefineProperty(
				"StateTracker",
				PropertyAttributes.None,
				typeof(DynamicProxyStateTracker),
				null);

			var getMethod = type.DefineMethod(
				"get_StateTracker",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				typeof(DynamicProxyStateTracker),
				Type.EmptyTypes);

			var stateTrackerConstructor = typeof(DynamicProxyStateTracker).GetConstructor(
				Type.EmptyTypes);

			var setItemMethod = typeof(DynamicProxyStateTracker).GetMethod(
				 "set_Item", new Type[] { typeof(IDynamicProxy) });

			var gen = getMethod.GetILGenerator();

			gen.DeclareLocal(typeof(DynamicProxyStateTracker));
			gen.DeclareLocal(typeof(bool));

			var endIfLabel = gen.DefineLabel();
			var exitLabel = gen.DefineLabel();

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
			var property = type.DefineProperty(
				"PrimaryKeyValue",
				PropertyAttributes.None,
				typeof(object),
				null);

			var getMethod = CreatePrimaryKeyValuePropertyGetMethod(type, members);
			var setMethod = CreatePrimaryKeyValuePropertySetMethod(type, members);

			property.SetGetMethod(getMethod);
			property.SetSetMethod(setMethod);
		}

		private static MethodBuilder CreatePrimaryKeyValuePropertyGetMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"get_PrimaryKeyValue",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				typeof(object),
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			gen.DeclareLocal(typeof(object));

			var exitLabel = gen.DefineLabel();

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
			var method = type.DefineMethod(
				"set_PrimaryKeyValue",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { typeof(object) });

			var typeOfMethod = typeof(Type).GetMethod(
				"GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

			var changeTypeMethod = typeof(Convert).GetMethod(
				"ChangeType", new Type[] { typeof(object), typeof(Type) });

			var gen = method.GetILGenerator();

			method.DefineParameter(1, ParameterAttributes.None, "value");

			// this.ID = (long)Convert.ChangeType(value, typeof(long));
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldtoken, members.PrimaryKeyColumnType);
			gen.Emit(OpCodes.Call, typeOfMethod);
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
			var property = type.DefineProperty(
				"IsValid",
				PropertyAttributes.None,
				typeof(bool),
				null);

			var getMethod = type.DefineMethod(
				"get_IsValid",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				typeof(bool),
				Type.EmptyTypes);

			var getIsValidMethod = typeof(DynamicProxyStateTracker).GetMethod("get_IsValid");

			var gen = getMethod.GetILGenerator();

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
			var property = type.DefineProperty(
				"ValidationErrors",
				PropertyAttributes.None,
				typeof(IList<ValidationError>),
				null);

			var getMethod = type.DefineMethod(
				"get_ValidationErrors",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				typeof(IList<ValidationError>),
				Type.EmptyTypes);

			var getValidationErrorsMethod = typeof(DynamicProxyStateTracker).GetMethod("get_ValidationErrors");

			var gen = getMethod.GetILGenerator();

			// return this.StateTracker.ValidationErrors;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Callvirt, getValidationErrorsMethod);
			gen.Emit(OpCodes.Ret);

			// Map the get method to its corresponding property method
			property.SetGetMethod(getMethod);
		}

		private static Type CreateValueBagType(string typeName, DynamicProxyTypeMembers members)
		{
			var type = _moduleBuilder.DefineType(
				typeName,
				TypeAttributes.Public,
				typeof(object),
				new Type[] { typeof(IValueBag) });

			foreach (var key in members.ValueBagPropertyNames)
			{
				CreateValueBagProperty(type, key, members.GetPropertyMethods[key].ReturnType, members);
			}

			members.ValueBagType = type;

			return type.CreateTypeInfo();
		}

		private static void CreateValueBagProperty(TypeBuilder type, string propertyName, Type propertyType, DynamicProxyTypeMembers members)
		{
			var field = type.DefineField(
				"_" + propertyName,
				propertyType,
				FieldAttributes.Private);

			var property = type.DefineProperty(
				propertyName,
				PropertyAttributes.None,
				propertyType,
				null);

			var getMethod = CreateValueBagPropertyGetMethod(type, propertyName, propertyType, field);
			var setMethod = CreateValueBagPropertySetMethod(type, propertyName, propertyType, field);

			// Map the get and set methods created above to their corresponding property methods
			property.SetGetMethod(getMethod);
			property.SetSetMethod(setMethod);

			// Add the get and set methods to the members class so that we can access them while building the proxy
			members.GetValueBagPropertyMethods.Add(property.Name, getMethod);
			members.SetValueBagPropertyMethods.Add(property.Name, setMethod);
		}

		private static MethodBuilder CreateValueBagPropertyGetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField)
		{
			var method = type.DefineMethod(
				"get_" + propertyName,
				MethodAttributes.Public | MethodAttributes.HideBySig,
				propertyType,
				Type.EmptyTypes);

			var gen = method.GetILGenerator();

			gen.DeclareLocal(propertyType);

			// return _property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, privateField);
			//gen.Emit(OpCodes.Stloc_0);
			//gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateValueBagPropertySetMethod(TypeBuilder type, string propertyName, Type propertyType, FieldBuilder privateField)
		{
			var method = type.DefineMethod(
				"set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.HideBySig,
				null,
				new Type[] { propertyType });

			method.DefineParameter(1, ParameterAttributes.None, "value");

			var gen = method.GetILGenerator();

			// _property = value;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, privateField);
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static MethodBuilder CreateResetOriginalValuesMethod(TypeBuilder type, Type parentType, DynamicProxyTypeMembers members, Database database, ChildParentMapping childParentMapping)
		{
			// TODO: Should also clear ChangedFields here rather than in SetValuesFromReader

			var method = type.DefineMethod(
				"ResetOriginalValues",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				Type.EmptyTypes);

			var stateTrackerGetOriginalValuesMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"get_OriginalValues", Type.EmptyTypes);

			var dictionarySetItemMethod = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(object)).GetMethod(
				"set_Item", new Type[] { typeof(string), typeof(object) });

			var gen = method.GetILGenerator();

			// Set the original value for the primary key
			CreateSetOriginalValueCall(gen, members, database.Configuration.GetPrimaryKeyColumnName(parentType), database.Configuration.GetPrimaryKeyColumnType(parentType), stateTrackerGetOriginalValuesMethod, dictionarySetItemMethod);

			// Set the original values for each property
			foreach (var property in database.Configuration.PropertiesToMap(parentType))
			{
				// Check whether the property is a related collection
				if (database.Configuration.IsRelatedCollection(property))
				{
					continue;
				}

				var key = property.Name;
				var propertyType = property.PropertyType;

				// Check whether the property is a related item
				if (database.Configuration.IsRelatedItem(property))
				{
					key = database.Configuration.GetForeignKeyColumnName(property);
					propertyType = database.Configuration.GetPrimaryKeyColumnType(property.PropertyType);
					if (propertyType.IsValueType)
					{
						propertyType = typeof(Nullable<>).MakeGenericType(propertyType);
					}
				}

				CreateSetOriginalValueCall(gen, members, key, propertyType, stateTrackerGetOriginalValuesMethod, dictionarySetItemMethod);
			}

			// Set the original values for the related item properties for parent-child relationships
			// that don't exist as properties
			if (childParentMapping.ContainsKey(parentType))
			{
				foreach (var parent in childParentMapping[parentType])
				{
					var key = database.Configuration.GetForeignKeyColumnName(parentType, parent);
					var propertyType = database.Configuration.GetPrimaryKeyColumnType(parent);
					CreateSetOriginalValueCall(gen, members, key, propertyType, stateTrackerGetOriginalValuesMethod, dictionarySetItemMethod);
				}
			}

			// return;
			gen.Emit(OpCodes.Ret);

			return method;
		}

		private static void CreateSetOriginalValueCall(ILGenerator gen, DynamicProxyTypeMembers members, string key, Type propertyType, MethodInfo stateTrackerGetOriginalValuesMethod, MethodInfo dictionarySetItemMethod)
		{
			// this.StateTracker.OriginalValues["Property"] = this.Property;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Callvirt, stateTrackerGetOriginalValuesMethod);
			gen.Emit(OpCodes.Ldstr, key);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Callvirt, members.GetPropertyMethods[key]);
			if (propertyType.IsValueType)
			{
				gen.Emit(OpCodes.Box, propertyType);
			}
			gen.Emit(OpCodes.Callvirt, dictionarySetItemMethod);
		}

		private static void CreateSetValuesFromReaderMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"SetValuesFromReader",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { typeof(DbDataReader) });

			var stateTrackerIsLoadingMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"set_IsLoading", new Type[] { typeof(bool) });

			var readerGetNameMethod = typeof(DbDataReader).GetMethod(
				"GetName", new Type[] { typeof(int) });

			var toUpperInvariantMethod = typeof(string).GetMethod(
				"ToUpperInvariant", Type.EmptyTypes);

			var stringEqualityMethod = typeof(string).GetMethod(
				"op_Equality", new Type[] { typeof(string), typeof(string) });

			var isDBNullMethod = typeof(DbDataReader).GetMethod(
				"IsDBNull", new Type[] { typeof(int) });

			var getValueMethod = typeof(DbDataReader).GetMethod(
				"GetValue", new Type[] { typeof(int) });

			var getTypeMethod = typeof(Type).GetMethod(
				"GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

			var changeTypeMethod = typeof(TypeHelper).GetMethod(
				"ChangeType", new Type[] { typeof(object), typeof(Type) });

			MethodInfo getBooleanMethod = null;
			MethodInfo getDateTimeMethod = null;
			MethodInfo getDecimalMethod = null;
			MethodInfo getDoubleMethod = null;
			MethodInfo getInt16Method = null;
			MethodInfo getInt32Method = null;
			MethodInfo getInt64Method = null;
			MethodInfo getByteMethod = null;
			MethodInfo getStringMethod = null;
			MethodInfo getGuidMethod = null;

			var stateTrackerGetChangedFieldsMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"get_ChangedFields", Type.EmptyTypes);

			var removeStringMethod = typeof(List<>).MakeGenericType(typeof(string)).GetMethod(
				"Remove", new Type[] { typeof(string) });

			var getFieldCountMethod = typeof(DbDataReader).GetMethod(
				"get_FieldCount", Type.EmptyTypes);

			var gen = method.GetILGenerator();

			method.DefineParameter(1, ParameterAttributes.None, "source");

			gen.DeclareLocal(typeof(int));
			gen.DeclareLocal(typeof(string));

			// Define the locals for each nullable property in the switch statement
			// These will be referred to by their index later on so we need to store that too
			var localIndex = 2;
			var localIndexes = new Dictionary<string, int>();
			foreach (var key in members.SetPropertyMethods.Keys)
			{
				var propertyType = members.GetPropertyMethods[key].ReturnType;
				if (propertyType == typeof(bool?) ||
					propertyType == typeof(DateTime?) ||
					propertyType == typeof(decimal?) ||
					propertyType == typeof(double?) ||
					propertyType == typeof(short?) ||
					propertyType == typeof(int?) ||
					propertyType == typeof(long?) ||
					propertyType == typeof(byte?) ||
					propertyType == typeof(string))
				{
					var flag = gen.DeclareLocal(typeof(bool));
					var nullable = gen.DeclareLocal(propertyType);
					localIndexes.Add(key, localIndex);
					localIndex += 2;
				}
			}

			var endFieldLoop = gen.DefineLabel();
			var endSwitch = gen.DefineLabel();

			// Define the labels for each property in the switch statement
			var propertyLabels = new Dictionary<string, Label>();
			foreach (var key in members.SetPropertyMethods.Keys)
			{
				var pl = gen.DefineLabel();
				propertyLabels.Add(key, pl);
			}

			var startFieldLoop = gen.DefineLabel();

			// this.StateTracker.IsLoading = true;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

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

			foreach (var key in members.SetPropertyMethods.Keys)
			{
				// case "PROPERTY":
				gen.Emit(OpCodes.Ldloc_1);
				gen.Emit(OpCodes.Ldstr, key.ToUpperInvariant());
				gen.Emit(OpCodes.Call, stringEqualityMethod);
				gen.Emit(OpCodes.Brtrue, propertyLabels[key]);
			}

			gen.Emit(OpCodes.Br, endSwitch);

			var index = 0;
			foreach (var key in members.SetPropertyMethods.Keys)
			{
				gen.MarkLabel(propertyLabels[key]);

				var propertyType = members.GetPropertyMethods[key].ReturnType;

				if (propertyType == typeof(bool))
				{
					CreateSetValueFromReaderMethod(gen, getBooleanMethod, "GetBoolean", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(bool?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(bool), isDBNullMethod, getBooleanMethod, "GetBoolean", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(DateTime))
				{
					CreateSetValueFromReaderMethod(gen, getDateTimeMethod, "GetDateTime", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(DateTime?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(DateTime), isDBNullMethod, getDateTimeMethod, "GetDateTime", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(decimal))
				{
					CreateSetValueFromReaderMethod(gen, getDecimalMethod, "GetDecimal", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(decimal?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(decimal), isDBNullMethod, getDecimalMethod, "GetDecimal", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(double))
				{
					CreateSetValueFromReaderMethod(gen, getDoubleMethod, "GetDouble", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(double?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(double), isDBNullMethod, getDoubleMethod, "GetDouble", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(short))
				{
					CreateSetValueFromReaderMethod(gen, getInt16Method, "GetInt16", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(short?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(short), isDBNullMethod, getInt16Method, "GetInt16", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(int))
				{
					CreateSetValueFromReaderMethod(gen, getInt32Method, "GetInt32", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(int?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(int), isDBNullMethod, getInt32Method, "GetInt32", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(long))
				{
					CreateSetValueFromReaderMethod(gen, getInt64Method, "GetInt64", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(long?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(long), isDBNullMethod, getInt64Method, "GetInt64", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(byte))
				{
					CreateSetValueFromReaderMethod(gen, getByteMethod, "GetByte", members.SetPropertyMethods[key]);
				}
				else if (propertyType == typeof(byte?))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(byte), isDBNullMethod, getByteMethod, "GetByte", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(string))
				{
					CreateSetNullableValueFromReaderMethod(gen, propertyType, typeof(string), isDBNullMethod, getStringMethod, "GetString", members.SetPropertyMethods[key], localIndexes[key]);
				}
				else if (propertyType == typeof(Guid))
				{
					CreateSetValueFromReaderMethod(gen, getGuidMethod, "GetGuid", members.SetPropertyMethods[key]);
				}
				else
				{
					// TODO: Handle enums

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
				}

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

			// this.ResetOriginalValues();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.ResetOriginalValuesMethod);

			// this.StateTracker.IsLoading = false;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// return;
			gen.Emit(OpCodes.Ret);
		}

		private static void CreateSetValueFromReaderMethod(ILGenerator gen, MethodInfo getTypeValueMethod, string getTypeValueMethodName, MethodBuilder setPropertyMethod)
		{
			// this.Property = source.Get[Type](i);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldloc_0);
			if (getTypeValueMethod == null)
			{
				getTypeValueMethod = typeof(DbDataReader).GetMethod(getTypeValueMethodName, new Type[] { typeof(int) });
			}
			gen.Emit(OpCodes.Callvirt, getTypeValueMethod);
			gen.Emit(OpCodes.Callvirt, setPropertyMethod);
		}

		private static void CreateSetNullableValueFromReaderMethod(ILGenerator gen, Type propertyType, Type referenceType, MethodInfo isDBNullMethod, MethodInfo getTypeValueMethod, string getTypeValueMethodName, MethodBuilder setPropertyMethod, int localIndex)
		{
			var nullableTypeConstructor = propertyType.GetConstructor(new Type[] { referenceType });

			var endIsDBNull = gen.DefineLabel();
			var endIsNotDBNull = gen.DefineLabel();

			// if (source.IsDBNull(i))
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Callvirt, isDBNullMethod);
			gen.Emit(OpCodes.Stloc_S, localIndex);
			gen.Emit(OpCodes.Ldloc_S, localIndex);
			gen.Emit(OpCodes.Brfalse_S, endIsDBNull);

			// this.Property = null;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldloca_S, localIndex + 1);
			gen.Emit(OpCodes.Initobj, propertyType);
			gen.Emit(OpCodes.Ldloc_S, localIndex + 1);
			gen.Emit(OpCodes.Callvirt, setPropertyMethod);
			gen.Emit(OpCodes.Br_S, endIsNotDBNull);

			// else
			gen.MarkLabel(endIsDBNull);

			// this.Property = source.Get[Type](i);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldloc_0);
			if (getTypeValueMethod == null)
			{
				getTypeValueMethod = typeof(DbDataReader).GetMethod(getTypeValueMethodName, new Type[] { typeof(int) });
			}
			if (propertyType == typeof(string))
			{
				gen.Emit(OpCodes.Callvirt, getTypeValueMethod);
				gen.Emit(OpCodes.Callvirt, setPropertyMethod);
			}
			else
			{
				gen.Emit(OpCodes.Callvirt, getTypeValueMethod);
				gen.Emit(OpCodes.Newobj, nullableTypeConstructor);
				gen.Emit(OpCodes.Callvirt, setPropertyMethod);
			}

			gen.MarkLabel(endIsNotDBNull);
		}

		private static void CreateSetValuesFromBagMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"SetValuesFromBag",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				null,
				new Type[] { typeof(IValueBag) });

			var stateTrackerIsLoadingMethod = typeof(DynamicProxyStateTracker).GetMethod(
				"set_IsLoading", new Type[] { typeof(bool) });

			var gen = method.GetILGenerator();

			method.DefineParameter(1, ParameterAttributes.None, "bag");

			// Preparing locals
			gen.DeclareLocal(members.ValueBagType);

			// this.StateTracker.IsLoading = true;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// var itemBag = (ValueBagType)bag;
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, members.ValueBagType);
			gen.Emit(OpCodes.Stloc_0);

			foreach (var key in members.ValueBagPropertyNames)
			{
				// this.Property = itemBag.Property;
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldloc_0);
				gen.Emit(OpCodes.Callvirt, members.GetValueBagPropertyMethods[key]);
				gen.Emit(OpCodes.Callvirt, members.SetPropertyMethods[key]);
			}

			// this.ResetOriginalValues();
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.ResetOriginalValuesMethod);

			// this.StateTracker.IsLoading = false;
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Callvirt, stateTrackerIsLoadingMethod);

			// return;
			gen.Emit(OpCodes.Ret);
		}

		private static void CreateGetBagFromValuesMethod(TypeBuilder type, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				"GetBagFromValues",
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
				typeof(IValueBag),
				Type.EmptyTypes);

			var valueBagConstructor = members.ValueBagType.GetConstructor(Type.EmptyTypes);

			var gen = method.GetILGenerator();

			// Preparing locals
			gen.DeclareLocal(members.ValueBagType);
			gen.DeclareLocal(typeof(IValueBag));

			//// Preparing labels
			//Label label271 = gen.DefineLabel();

			// var itemBag = new ValueBagType();
			gen.Emit(OpCodes.Newobj, valueBagConstructor);
			gen.Emit(OpCodes.Stloc_0);

			foreach (var key in members.ValueBagPropertyNames)
			{
				// itemBag.Property = this.Property;
				gen.Emit(OpCodes.Ldloc_0);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Callvirt, members.GetPropertyMethods[key]);
				gen.Emit(OpCodes.Callvirt, members.SetValueBagPropertyMethods[key]);
			}

			//// Not really sure what this is doing...
			//gen.Emit(OpCodes.Ldloc_0);
			//gen.Emit(OpCodes.Stloc_1);
			//gen.Emit(OpCodes.Br_S, label271);

			//// End of method
			//gen.MarkLabel(label271);

			// return itemBag;
			gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);
		}

		private static void CreateMethodToCallStateTrackerMethod(TypeBuilder type, string methodName, string stateTrackerMethodName, Type methodReturnType, Type[] methodParameterTypes, DynamicProxyTypeMembers members)
		{
			var method = type.DefineMethod(
				methodName,
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
				methodReturnType,
				methodParameterTypes);

			var stateTrackerMethod = typeof(DynamicProxyStateTracker).GetMethod(
				stateTrackerMethodName, methodParameterTypes);

			var gen = method.GetILGenerator();

			if (methodReturnType != null)
			{
				gen.DeclareLocal(methodReturnType);
			}
			var exitLabel = gen.DefineLabel();

			// return this.StateTracker.Method(parameters);
			// TODO: This isn't going to work with more than one parameter...
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, members.GetStateTrackerMethod);
			if (methodParameterTypes.Length > 0)
			{
				gen.Emit(OpCodes.Ldarg_1);
			}
			gen.Emit(OpCodes.Callvirt, stateTrackerMethod);
			if (methodReturnType != null)
			{
				gen.Emit(OpCodes.Stloc_0);
			}
			gen.Emit(OpCodes.Br_S, exitLabel);
			gen.MarkLabel(exitLabel);
			if (methodReturnType != null)
			{
				gen.Emit(OpCodes.Ldloc_0);
			}
			gen.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Loads the child parent mapping.
		/// </summary>
		/// <param name="database">The database.</param>
		/// <returns>
		/// The child parent mapping.
		/// </returns>
		/// <remarks>
		/// Before we can build any proxies we first need to scan through the mapped types and build
		/// lists of types that exist in a one-to-many relationship in a parent type with a collection.
		/// </remarks>
		private static ChildParentMapping LoadChildParentMapping(Database database)
		{
			var result = new ChildParentMapping();
			foreach (var parentType in database.Configuration.TypesToMap())
			{
				// We only want to map parents that are tables
				if (!database.Configuration.IsTable(parentType))
				{
					continue;
				}

				foreach (var childProperty in database.Configuration.PropertiesToMap(parentType))
				{
					if (database.Configuration.IsRelatedCollection(childProperty))
					{
						// E.g. given Author.Books, Author is the parent and Book is the child
						// We will add Book as the key for the dictionary as we will want to add
						// the AuthorID column when creating its proxy
						var childPropertyType = TypeHelper.GetElementType(childProperty.PropertyType);
						if (!result.ContainsKey(childPropertyType))
						{
							result.Add(childPropertyType, new Stack<Type>());
						}
						var childTypes = result[childPropertyType];
						childTypes.Push(parentType);
					}
				}
			}
			return result;
		}
	}
}

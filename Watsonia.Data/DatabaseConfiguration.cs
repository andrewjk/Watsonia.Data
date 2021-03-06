﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Watsonia.Data.DataAnnotations;

namespace Watsonia.Data
{
	/// <summary>
	/// Contans information about how to access a database and how to map entities to database objects.
	/// </summary>
	public class DatabaseConfiguration
	{
		private IEnumerable<Assembly> _assembliesToMap;
		private IEnumerable<Type> _typesToMap;
		private readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertiesToMap = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();
		private readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertiesToLoadAndSave = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();
		private readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertiesToCascade = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();
		private readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _propertiesToCascadeDelete = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

		/// <summary>
		/// Gets or sets the provider used to access the type of database.
		/// </summary>
		/// <value>
		/// The data access provider.
		/// </value>
		public IDataAccessProvider DataAccessProvider { get; private set; }

		/// <summary>
		/// Gets the connection string used to access the database.
		/// </summary>
		/// <value>
		/// The connection string.
		/// </value>
		public virtual string ConnectionString { get; private set; }

		/// <summary>
		/// Gets the namespace in which entity classes are located.
		/// </summary>
		/// <value>
		/// The entity namespace.
		/// </value>
		public string EntityNamespace { get; set; } = "$";

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseConfiguration" /> class.
		/// </summary>
		/// <param name="provider">The provider used to access the type of database.</param>
		/// <param name="connectionString">The connection string used to access the database.</param>
		/// <param name="entityNamespace">The namespace in which entity classes are located.</param>
		public DatabaseConfiguration(IDataAccessProvider provider, string connectionString, string entityNamespace)
		{
			this.DataAccessProvider = provider;
			this.ConnectionString = connectionString;
			this.EntityNamespace = entityNamespace;
		}

		/// <summary>
		/// Gets the name of the schema for the supplied type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual string GetSchemaName(Type type)
		{
			return string.Empty;
		}

		/// <summary>
		/// Gets the name of the table for the supplied type.
		/// </summary>
		/// <remarks>
		/// For a Book item, this would return "Book" by default but might be overridden to return "Books" or something different.
		/// </remarks>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual string GetTableName(Type type)
		{
			return type.Name;
		}

		/// <summary>
		/// Gets the name of the view for the supplied type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual string GetViewName(Type type)
		{
			return type.Name;
		}

		/// <summary>
		/// Gets the name of the procedure for the supplied type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual string GetProcedureName(Type type)
		{
			return type.Name.Replace("Procedure", "");
		}

		/// <summary>
		/// Gets the name of the function for the supplied type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual string GetFunctionName(Type type)
		{
			return type.Name.Replace("Function", "");
		}

		/// <summary>
		/// Gets the name of the column for the supplied property.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public virtual string GetColumnName(PropertyInfo property)
		{
			if (IsRelatedItem(property))
			{
				return GetForeignKeyColumnName(property);
			}
			else
			{
				return property.Name;
			}
		}

		/// <summary>
		/// Gets the name of the primary key column for the table matching the supplied type.
		/// </summary>
		/// <remarks>
		/// For a Book item, this would return "ID" by default but might be overridden to return "BookID" or "BookCode" or something different.
		/// </remarks>
		/// <param name="tableType">The table type.</param>
		/// <returns></returns>
		public virtual string GetPrimaryKeyColumnName(Type tableType)
		{
			return "ID";
		}

		/// <summary>
		/// Gets the type of the primary key column for the table matching the supplied type.
		/// </summary>
		/// <remarks>
		/// By default the primary key type is a long but might be overridden to return a Guid or other type.
		/// </remarks>
		/// <param name="tableType">The table type.</param>
		/// <returns></returns>
		public virtual Type GetPrimaryKeyColumnType(Type tableType)
		{
			return typeof(long);
		}

		/// <summary>
		/// Gets the primary key value for a new item (i.e. one that doesn't yet exist in the database).
		/// </summary>
		/// <param name="tableType">The table type.</param>
		/// <returns></returns>
		public virtual object GetPrimaryKeyNewItemValue(Type tableType)
		{
			return -1;
		}

		/// <summary>
		/// Gets the name of the primary key constraint for the table matching the supplied type.
		/// </summary>
		/// <param name="tableType">The table type.</param>
		/// <returns></returns>
		public virtual string GetPrimaryKeyConstraintName(Type tableType)
		{
			return "PK_" + tableType.Name;
		}

		/// <summary>
		/// Gets the name of the foreign key column for the supplied property.
		/// </summary>
		/// <remarks>
		/// For a Book.Author property, this would return "AuthorID" by default.
		/// </remarks>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public virtual string GetForeignKeyColumnName(PropertyInfo property)
		{
			return property.Name + "ID";
		}

		/// <summary>
		/// Gets the name of the foreign key column in the table matching the supplied type and pointing to the foreign type.
		/// </summary>
		/// <remarks>
		/// For a Book type and an Author foreign type, this would return "AuthorID" by default.
		/// </remarks>
		/// <param name="tableType">The type.</param>
		/// <param name="foreignType">Type of the foreign.</param>
		/// <returns></returns>
		public virtual string GetForeignKeyColumnName(Type tableType, Type foreignType)
		{
			return GetTableName(foreignType) + "ID";
		}

		/// <summary>
		/// Gets the name of the foreign key constraint for the supplied property.
		/// </summary>
		/// <remarks>
		/// For a Book.Author property, this would return "Book_AuthorID" by default. 
		/// </remarks>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public virtual string GetForeignKeyConstraintName(PropertyInfo property)
		{
			return "FK_" + property.DeclaringType.Name + "_" + property.Name + "ID";
		}

		/// <summary>
		/// Gets the name of the foreign key constraint in the table matching the supplied type and pointing to the foreign type.
		/// </summary>
		/// <remarks>
		/// For a Book type and an Author foreign type, this would return "Book_AuthorID" by default.
		/// </remarks>
		/// <param name="tableType">The type.</param>
		/// <param name="foreignType">Type of the foreign.</param>
		/// <returns></returns>
		public virtual string GetForeignKeyConstraintName(Type tableType, Type foreignType)
		{
			return "FK_" + tableType.Name + "_" + foreignType.Name + "ID";
		}

		/// <summary>
		/// Gets the name of the default value constraint.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public virtual string GetDefaultValueConstraintName(string tableName, PropertyInfo property)
		{
			return "DF_" + tableName + "_" + property.Name;
		}

		/// <summary>
		/// Determines whether the supplied type is a table.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		///   <c>true</c> if the supplied type is a table; otherwise, <c>false</c>.
		/// </returns>
		public virtual bool IsTable(Type type)
		{
			return !type.Name.EndsWith("View") &&
				!type.Name.EndsWith("Procedure") &&
				!type.Name.EndsWith("Function");
		}
		
		/// <summary>
		/// Determines whether the supplied type is a view.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		///   <c>true</c> if the supplied type is a view; otherwise, <c>false</c>.
		/// </returns>
		public virtual bool IsView(Type type)
		{
			return type.Name.EndsWith("View");
		}

		/// <summary>
		/// Determines whether the supplied type is a stored procedure.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		///   <c>true</c> if the supplied type is a stored procedure; otherwise, <c>false</c>.
		/// </returns>
		public virtual bool IsProcedure(Type type)
		{
			return type.Name.EndsWith("Procedure");
		}

		/// <summary>
		/// Determines whether the supplied type is a user-defined function.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		///   <c>true</c> if the supplied type is a user-defined function; otherwise, <c>false</c>.
		/// </returns>
		public virtual bool IsFunction(Type type)
		{
			return type.Name.EndsWith("Function");
		}

		/// <summary>
		/// Determines whether the supplied property contains a related entity item.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>
		///   <c>true</c> if the supplied property contains a related entity item; otherwise, <c>false</c>.
		/// </returns>
		public bool IsRelatedItem(PropertyInfo property)
		{
			return IsRelatedItem(property.PropertyType);
		}

		/// <summary>
		/// Determines whether the supplied property contains a related entity item.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>
		///   <c>true</c> if the supplied property contains a related entity item; otherwise, <c>false</c>.
		/// </returns>
		public bool IsRelatedItem(Type type)
		{
			return (!type.IsValueType && ShouldMapTypeInternal(type));
		}

		/// <summary>
		/// Determines whether the specified property is the ID of a related item.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>
		///   <c>true</c> if the specified property is the ID of a related item; otherwise, <c>false</c>.
		/// </returns>
		public virtual bool IsRelatedItemID(PropertyInfo property)
		{
			var primaryKeyColumnType = typeof(long);
			return (property.PropertyType == typeof(Nullable<>).MakeGenericType(primaryKeyColumnType) && property.Name.EndsWith("ID"));
		}

		/// <summary>
		/// Determines whether the supplied property contains a collection of related entities.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>
		///   <c>true</c> if the supplied property contains a collection of related entities; otherwise, <c>false</c>.
		/// </returns>
		public bool IsRelatedCollection(PropertyInfo property)
		{
			return IsRelatedCollection(property, out _);
		}

		/// <summary>
		/// Determines whether the supplied property contains a collection of related entities.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="itemType">The type of the items contained in the collection.</param>
		/// <returns>
		///   <c>true</c> if the supplied property contains a collection of related entities; otherwise, <c>false</c>.
		/// </returns>
		public bool IsRelatedCollection(PropertyInfo property, out Type itemType)
		{
			itemType = null;

			var collectionType = property.PropertyType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)));
			if (collectionType != null)
			{
				var genericArgumentType = collectionType.GetGenericArguments()[0];
				if (genericArgumentType != null && ShouldMapTypeInternal(genericArgumentType))
				{
					itemType = genericArgumentType;
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Determines whether the supplied assembly should be looked in for types to map to the database.
		/// </summary>
		/// <param name="assembly">The assembly.</param>
		/// <returns></returns>
		internal bool ShouldMapAssemblyInternal(Assembly assembly)
		{
			// This assembly causes problems with some unit testing platforms
			if (assembly.FullName.StartsWith("Microsoft.VisualStudio"))
			{
				return false;
			}

			return ShouldMapAssembly(assembly);
		}

		/// <summary>
		/// Determines whether the supplied assembly should be looked in for types to map to the database.
		/// </summary>
		/// <param name="assembly">The assembly.</param>
		/// <returns></returns>
		public virtual bool ShouldMapAssembly(Assembly assembly)
		{
			return true;
		}

		/// <summary>
		/// Determines whether the class with the supplied type should be mapped to the database.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		internal bool ShouldMapTypeInternal(Type type)
		{
			// This can happen when there's a problem loading the types in an assembly
			if (type == null)
			{
				return false;
			}

			// Only map classes and don't map the database or configuration
			if (!type.IsClass ||
				typeof(Database).IsAssignableFrom(type) ||
				typeof(DatabaseConfiguration).IsAssignableFrom(type))
			{
				return false;
			}

			// Let the user specify what we should map
			return ShouldMapType(type);
		}

		/// <summary>
		/// Determines whether the class with the supplied type should be mapped to the database.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual bool ShouldMapType(Type type)
		{
			return (type.Namespace == this.EntityNamespace);
		}

		/// <summary>
		/// Gets a list of assemblies to look in for types to map.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Assembly> AssembliesToMap()
		{
			if (_assembliesToMap == null)
			{
				_assembliesToMap = GetAssembliesToMap();
			}
			return _assembliesToMap;
		}

		private IEnumerable<Assembly> GetAssembliesToMap()
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (ShouldMapAssemblyInternal(assembly))
				{
					yield return assembly;
				}
			}
		}

		/// <summary>
		/// Gets a list of classes within the current AppDomain to map to the database.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Type> TypesToMap()
		{
			if (_typesToMap == null)
			{
				_typesToMap = GetTypesToMap();
			}
			return _typesToMap;
		}

		private IEnumerable<Type> GetTypesToMap()
		{
			foreach (var assembly in AssembliesToMap())
			{
				foreach (var type in TypesToMap(assembly))
				{
					yield return type;
				}
			}
		}

		/// <summary>
		/// Gets a list of classes within the supplied assembly to map to the database.
		/// </summary>
		/// <param name="assembly">The assembly.</param>
		/// <returns></returns>
		public IEnumerable<Type> TypesToMap(Assembly assembly)
		{
			// HACK: This is hiding exceptions, should we have this be hidden behind a configuration switch?
			Type[] typesInAssembly;
			try
			{
				typesInAssembly = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				typesInAssembly = ex.Types;
			}
			foreach (var type in typesInAssembly)
			{
				if (ShouldMapTypeInternal(type))
				{
					yield return type;
				}
			}
		}

		/// <summary>
		/// Determines whether the supplied property should be mapped to the database.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		internal bool ShouldMapPropertyInternal(PropertyInfo property)
		{
			// Don't map the properties that we add to the proxy
			if (property.Name == "StateTracker" ||
				property.Name == "__PrimaryKeyValue")
			{
				return false;
			}

			// Or the SelectStatement property that we get the view statement from
			if (property.Name == "SelectStatement")
			{
				return false;
			}

			// Only map the property if it's readable, writeable, public and virtual
			if (!property.CanRead ||
				!property.CanWrite ||
				!property.GetGetMethod().IsPublic ||
				!property.GetSetMethod().IsPublic ||
				!property.GetGetMethod().IsVirtual)
			{
				return false;
			}

			// Let the user specify what we should map
			return ShouldMapProperty(property);
		}

		/// <summary>
		/// Determines whether the supplied property should be mapped to the database.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public virtual bool ShouldMapProperty(PropertyInfo property)
		{
			return true;
		}

		/// <summary>
		/// Gets a list of properties within the supplied type to map to the database.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public IEnumerable<PropertyInfo> PropertiesToMap(Type type)
		{
			return _propertiesToMap.GetOrAdd(type, GetPropertiesToMap(type));
		}

		private IEnumerable<PropertyInfo> GetPropertiesToMap(Type type)
		{
			var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			foreach (var property in type.GetProperties(flags))
			{
				if (ShouldMapPropertyInternal(property))
				{
					yield return property;
				}
			}
		}

		/// <summary>
		/// Determines whether to load and save the supplied property's value from the database.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public bool ShouldLoadAndSaveProperty(PropertyInfo property)
		{
			if (!ShouldMapPropertyInternal(property))
			{
				return false;
			}

			if (property.PropertyType.IsValueType)
			{
				// It's a value type so it should be loaded from the database
				return true;
			}

			if (IsRelatedItem(property))
			{
				// It's an object linked with a foreign key so it shouldn't be loaded directly from the database
				// It will be lazy-loaded later when it is accessed
				return false;
			}

			if (IsRelatedCollection(property))
			{
				// It's a related collection so it shouldn't be loaded directly from the database
				// The collection will be lazy-loaded later when it is accessed
				return false;
			}

			// Anything else should be loaded from the database
			return true;
		}

		/// <summary>
		/// Gets a list of properties within the supplied type to load and save from the database.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public IEnumerable<PropertyInfo> PropertiesToLoadAndSave(Type type)
		{
			return _propertiesToLoadAndSave.GetOrAdd(type, GetPropertiesToLoadAndSave(type));
		}

		private IEnumerable<PropertyInfo> GetPropertiesToLoadAndSave(Type type)
		{
			var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			foreach (var property in type.GetProperties(flags))
			{
				if (ShouldLoadAndSaveProperty(property))
				{
					yield return property;
				}
			}
		}

		/// <summary>
		/// Determines whether the value in the supplied property should be saved with its parent object.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		internal bool ShouldCascadeInternal(PropertyInfo property)
		{
			if (property.PropertyType.IsValueType)
			{
				// It's a value type so it can't be cascaded
				return false;
			}

			// Let the user specify what we should cascade
			return ShouldCascade(property);
		}

		/// <summary>
		/// Determines whether the value in the supplied property should be saved with its parent object.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public virtual bool ShouldCascade(PropertyInfo property)
		{
			// If it has a Cascade attribute then it should be cascaded
			var attribute = GetCustomAttribute<CascadeAttribute>(property);
			if (attribute != null)
			{
				return attribute.ShouldCascade;
			}

			// Everything else should be cascaded by default
			return true;
		}

		/// <summary>
		/// Gets a list of properties within the supplied type to save with the parent object.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public IEnumerable<PropertyInfo> PropertiesToCascade(Type type)
		{
			return _propertiesToCascade.GetOrAdd(type, GetPropertiesToCascade(type));
		}

		private IEnumerable<PropertyInfo> GetPropertiesToCascade(Type type)
		{
			var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			foreach (var property in type.GetProperties(flags))
			{
				if (ShouldCascadeInternal(property))
				{
					yield return property;
				}
			}
		}

		/// <summary>
		/// Determines whether the value in the supplied property should be deleted with its parent object.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		internal bool ShouldCascadeDeleteInternal(PropertyInfo property)
		{
			if (property.PropertyType.IsValueType)
			{
				// It's a value type so it can't be cascaded
				return false;
			}

			// Let the user specify what we should cascade
			return ShouldCascadeDelete(property);
		}

		/// <summary>
		/// Determines whether the value in the supplied property should be deleted with its parent object.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		public bool ShouldCascadeDelete(PropertyInfo property)
		{
			// If it has a CascadeDelete attribute then it should be cascaded
			var attribute = GetCustomAttribute<CascadeDeleteAttribute>(property);
			if (attribute != null)
			{
				return attribute.ShouldCascade;
			}

			// Nothing else should be cascaded by default
			return false;
		}

		/// <summary>
		/// Gets a list of properties within the supplied type to delete with the parent object.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public IEnumerable<PropertyInfo> PropertiesToCascadeDelete(Type type)
		{
			return _propertiesToCascadeDelete.GetOrAdd(type, GetPropertiesToCascadeDelete(type));
		}

		private IEnumerable<PropertyInfo> GetPropertiesToCascadeDelete(Type type)
		{
			var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			foreach (var property in type.GetProperties(flags))
			{
				if (ShouldCascadeDeleteInternal(property))
				{
					yield return property;
				}
			}
		}

		/// <summary>
		/// Gets whether items with the supplied type should be stored in the item cache.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual bool ShouldCacheType(Type type)
		{
			return true;
		}

		/// <summary>
		/// Gets the length of time in milliseconds to store items in the cache for the table with the supplied name.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual int GetCacheExpiryLength(Type type)
		{
			// 15 minutes
			return 900000;
		}

		/// <summary>
		/// Gets the maximum number of items to store in the cache for the table with the supplied name.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public virtual int GetCacheMaxItems(Type type)
		{
			return 10000;
		}

		/// <summary>
		/// Gets a custom attribute with type T on the supplied member if it exists.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="target">The target.</param>
		/// <returns>The custom attribute or null if one was not found.</returns>
		protected T GetCustomAttribute<T>(MemberInfo target) where T : Attribute
		{
			var attributes = target.GetCustomAttributes(typeof(T), false);
			if (attributes != null && attributes.Length > 0)
			{
				return (T)attributes[0];
			}
			else
			{
				return null;
			}
		}
	}
}

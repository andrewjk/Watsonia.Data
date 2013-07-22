using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Watsonia.Data
{
	/// <summary>
	/// Contans information about how to access a database and how to map entities to database objects.
	/// </summary>
	public class DatabaseConfiguration
	{
		private IDataAccessProvider _dataAccessProvider;

		/// <summary>
		/// Gets or sets the name of the provider used to access the type of database.
		/// </summary>
		/// <value>
		/// The name of the provider.
		/// </value>
		public virtual string ProviderName
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the provider used to access the type of database.
		/// </summary>
		/// <value>
		/// The data access provider.
		/// </value>
		public IDataAccessProvider DataAccessProvider
		{
			get
			{
				if (_dataAccessProvider == null)
				{
					// Set the provider based on the ProviderName that the user set, or just the first provider if they
					// didn't set anything
					foreach (IDataAccessProvider provider in DataAccessProviders.Providers)
					{
						if (string.IsNullOrEmpty(this.ProviderName) || this.ProviderName == provider.ProviderName)
						{
							_dataAccessProvider = provider;
							break;
						}
					}

					// If the user passed in a name and it didn't match anything, throw an exception
					// NOTE: We're checking this here rather than in DataAccessProviders as this gives a more meaningful
					// exception, rather than showing the user something wrapped in a TypeInitializationException
					if (_dataAccessProvider == null)
					{
						throw new InvalidOperationException("No data access provider found");
					}
				}
				return _dataAccessProvider;
			}
		}

		/// <summary>
		/// Gets the connection string used to access the database.
		/// </summary>
		/// <value>
		/// The connection string.
		/// </value>
		public virtual string ConnectionString
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the namespace in which entity classes are located.
		/// </summary>
		/// <value>
		/// The entity namespace.
		/// </value>
		private string EntityNamespace
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseConfiguration" /> class.
		/// </summary>
		/// <param name="connectionString">The connection string used to access the database.</param>
		/// <param name="entityNamespace">The namespace in which entity classes are located.</param>
		public DatabaseConfiguration(string connectionString, string entityNamespace)
		{
			this.ConnectionString = connectionString;
			this.EntityNamespace = entityNamespace;
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
			return foreignType.Name + "ID";
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
		public virtual string GetDefaultValueConstraintName(PropertyInfo property)
		{
			return "DF_" + property.DeclaringType.Name + "_" + property.Name;
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
			return (!property.PropertyType.IsValueType && ShouldMapTypeInternal(property.PropertyType));
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
			Type primaryKeyColumnType = typeof(long);
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
			Type itemType;
			return IsRelatedCollection(property, out itemType);
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

			Type collectionType = property.PropertyType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)));
			if (collectionType != null)
			{
				Type genericArgumentType = collectionType.GetGenericArguments()[0];
				if (genericArgumentType != null && ShouldMapTypeInternal(genericArgumentType))
				{
					itemType = genericArgumentType;
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Determines whether the class with the supplied type should be mapped to the database.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		internal bool ShouldMapTypeInternal(Type type)
		{
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
		/// Gets a list of classes within the supplied assembly to map to the database.
		/// </summary>
		/// <param name="assembly">The assembly.</param>
		/// <returns></returns>
		public IEnumerable<Type> TypesToMap(Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (this.ShouldMapTypeInternal(type))
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
				property.Name == "Database" ||
				property.Name == "PrimaryKeyValue" ||
				property.Name == "IsNew" ||
				property.Name == "HasChanges")
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
		internal bool ShouldMapProperty(PropertyInfo property)
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
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			foreach (PropertyInfo property in type.GetProperties(flags))
			{
				if (this.ShouldMapPropertyInternal(property))
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
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			foreach (PropertyInfo property in type.GetProperties(flags))
			{
				if (this.ShouldLoadAndSaveProperty(property))
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
		public bool ShouldCascade(PropertyInfo property)
		{
			if (IsRelatedItem(property))
			{
				// It's an object linked with a foreign key so it shouldn't be cascaded
				return false;
			}

			if (IsRelatedCollection(property))
			{
				// It's a related collection so changes to the item should be cascaded
				return true;
			}

			// If it has a Cascade attribute then it should be cascaded
			CascadeAttribute cascade = GetCustomAttribute<CascadeAttribute>(property);
			if (cascade != null && cascade.ShouldCascade)
			{
				return true;
			}

			// Nothing else should be cascaded by default
			return false;
		}

		/// <summary>
		/// Gets a list of properties within the supplied type to save with the parent object.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		public IEnumerable<PropertyInfo> PropertiesToCascade(Type type)
		{
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			foreach (PropertyInfo property in type.GetProperties(flags))
			{
				if (this.ShouldCascadeInternal(property))
				{
					yield return property;
				}
			}
		}

		/// <summary>
		/// Gets a custom attribute with type T on the supplied member if it exists.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="target">The target.</param>
		/// <returns>The custom attribute or null if one was not found.</returns>
		protected T GetCustomAttribute<T>(MemberInfo target) where T : Attribute
		{
			object[] attributes = target.GetCustomAttributes(typeof(T), false);
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

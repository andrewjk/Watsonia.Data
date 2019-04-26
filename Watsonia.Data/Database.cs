using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Watsonia.Data.EventArgs;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	/// <summary>
	/// Provides functionality for accessing and updating a database.
	/// </summary>
	public class Database : IDisposable
	{
		// TODO: Validation stuff

		public event EventHandler BeforeUpdateDatabase;
		public event EventHandler AfterUpdateDatabase;
		public event EventHandler AfterCreate;
		public event EventHandler AfterLoad;
		public event EventHandler BeforeRefresh;
		public event EventHandler AfterRefresh;
		public event EventHandler BeforeLoadCollection;
		public event EventHandler AfterLoadCollection;
		public event EventHandler BeforeLoadValue;
		public event EventHandler AfterLoadValue;
		public event EventHandler BeforeSave;
		public event EventHandler AfterSave;
		public event EventHandler BeforeInsert;
		public event EventHandler AfterInsert;
		public event EventHandler BeforeDelete;
		public event EventHandler AfterDelete;
		public event EventHandler BeforeExecute;
		public event EventHandler AfterExecute;
		public event EventHandler BeforeExecuteCommand;
		public event EventHandler AfterExecuteCommand;

		/// <summary>
		/// The cache.
		/// </summary>
		internal static DatabaseItemCache Cache { get; } = new DatabaseItemCache();

		/// <summary>
		/// Gets the configuration options used for mapping to and accessing the database.
		/// </summary>
		/// <value>
		/// The configuration.
		/// </value>
		public DatabaseConfiguration Configuration { get; private set; }

		/// <summary>
		/// Gets or sets the name of the database to use when creating proxy objects to avoid the same
		/// proxy class being used for different databases with different table and column names.
		/// </summary>
		/// <value>
		/// The name of the database.
		/// </value>
		internal string DatabaseName { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Database" /> class.
		/// </summary>
		/// <param name="provider">The provider used to access the type of database.</param>
		/// <param name="connectionString">The connection string used to access the database.</param>
		/// <param name="entityNamespace">The namespace in which entity classes are located.</param>
		public Database(IDataAccessProvider provider, string connectionString, string entityNamespace)
		{
			this.Configuration = new DatabaseConfiguration(provider, connectionString, entityNamespace);
			this.DatabaseName = this.GetType().Name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Database" /> class.
		/// </summary>
		/// <param name="configuration">The configuration options used for mapping to and accessing the database.</param>
		public Database(DatabaseConfiguration configuration)
		{
			this.Configuration = configuration;
			this.DatabaseName = this.GetType().Name;
		}

		/// <summary>
		/// Opens a connection to the database.
		/// </summary>
		/// <returns></returns>
		public DbConnection OpenConnection()
		{
			return this.Configuration.DataAccessProvider.OpenConnection(this.Configuration);
		}

		/// <summary>
		/// Ensures that the database is deleted.
		/// </summary>
		public void EnsureDatabaseDeleted()
		{
			this.Configuration.DataAccessProvider.EnsureDatabaseDeleted(this.Configuration);
		}

		/// <summary>
		/// Ensures that the database is created.
		/// </summary>
		public void EnsureDatabaseCreated()
		{
			this.Configuration.DataAccessProvider.EnsureDatabaseCreated(this.Configuration);
			this.UpdateDatabase();
		}

		/// <summary>
		/// Updates the database from the mapped entity classes.
		/// </summary>
		public void UpdateDatabase()
		{
			OnBeforeUpdateDatabase();

			var updater = new DatabaseUpdater();
			updater.UpdateDatabase(this.Configuration);

			OnAfterUpdateDatabase();
		}

		/// <summary>
		/// Gets the update script for the mapped entity classes.
		/// </summary>
		/// <returns>A string containing the update script.</returns>
		public string GetUpdateScript()
		{
			var updater = new DatabaseUpdater();
			return updater.GetUpdateScript(this.Configuration);
		}

		/// <summary>
		/// Gets the columns that exist in the database but not the mapped entity classes.
		/// </summary>
		/// <returns>
		/// A string containing the unmapped columns.
		/// </returns>
		public string GetUnmappedColumns()
		{
			var updater = new DatabaseUpdater();
			return updater.GetUnmappedColumns(this.Configuration);
		}

		// NOTE: This is not supported as of .Net Standard 2.0:

		///// <summary>
		///// Exports all mapped entity proxies to an assembly.
		///// </summary>
		///// <param name="path">The assembly path.</param>
		//public void ExportProxies(string path)
		//{
		//	DynamicProxyFactory.SetAssemblyPath(path);
		//	foreach (Type type in this.Configuration.TypesToMap())
		//	{
		//		DynamicProxyFactory.GetDynamicProxyType(type, this);
		//	}
		//	DynamicProxyFactory.SaveAssembly();
		//}

		/// <summary>
		/// Creates a proxy object for the type T.
		/// </summary>
		/// <typeparam name="T">The type of item to create a proxy for.</typeparam>
		/// <returns></returns>
		public T Create<T>()
		{
			var newItem = DynamicProxyFactory.GetDynamicProxy<T>(this);
			var proxy = (IDynamicProxy)newItem;
			//proxy.ID = -1;
			proxy.IsNew = true;

			OnAfterCreate(proxy);

			return newItem;
		}

		/// <summary>
		/// Creates a proxy object for the type T with values from the supplied item.
		/// </summary>
		/// <typeparam name="T">The type of item to create a proxy for.</typeparam>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		public T Create<T>(T item)
		{
			var newItem = DynamicProxyFactory.GetDynamicProxy<T>(this);
			var proxy = (IDynamicProxy)newItem;
			//proxy.ID = -1;
			proxy.IsNew = true;
			LoadValues(item, proxy);

			OnAfterCreate(proxy);

			return newItem;
		}

		/// <summary>
		/// Provides a queryable interface to a table in the database for use with LINQ.
		/// </summary>
		/// <typeparam name="T">The type of the class mapped to the database table.</typeparam>
		/// <returns></returns>
		public DatabaseQuery<T> Query<T>()
		{
			var queryParser = QueryParser.CreateDefault();
			var queryExecutor = new QueryExecutor<T>(this);
			var query = new DatabaseQuery<T>(queryParser, queryExecutor);

			// HACK: This is a bit horrible and circular, but necessary to get Include paths into the select
			// statement in the QueryExecutor.Execute methods
			queryExecutor.Query = query;

			return query;
		}

		/// <summary>
		/// Provides a queryable interface to a user-defined function in the database for use with LINQ.
		/// </summary>
		/// <typeparam name="T">The type of the class mapped to the database function.</typeparam>
		/// <returns></returns>
		public DatabaseQuery<T> QueryFunction<T>(params Parameter[] parameters)
		{
			var queryParser = QueryParser.CreateDefault();
			var queryExecutor = new QueryExecutor<T>(this);
			var query = new DatabaseQuery<T>(queryParser, queryExecutor);

			// HACK: This is a bit horrible and circular, but necessary to get Include paths into the select
			// statement in the QueryExecutor.Execute methods
			queryExecutor.Query = query;

			// HACK: This is also a bit horrible as parameters are only necessary for functions
			query.Parameters.AddRange(parameters);

			return query;
		}

		internal SelectStatement BuildSelectStatement(Expression expression)
		{
			// For testing
			var queryParser = QueryParser.CreateDefault();
			var queryModel = queryParser.GetParsedQuery(expression);

			// The type doesn't matter
			var queryExecutor = new QueryExecutor<int>(this);
			var query = new DatabaseQuery<int>(queryParser, queryExecutor);

			queryExecutor.Query = query;

			return queryExecutor.BuildSelectStatement(queryModel);
		}

		/// <summary>
		/// Loads the item from the database with the supplied ID.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <returns></returns>
		public T Load<T>(object id)
		{
			return LoadOrDefault<T>(id, true);
		}

		/// <summary>
		/// Loads the item from the database with the supplied ID.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <returns></returns>
		public T LoadOrDefault<T>(object id)
		{
			return LoadOrDefault<T>(id, false);
		}

		/// <summary>
		/// Loads the item from the database with the supplied ID.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <returns></returns>
		private T LoadOrDefault<T>(object id, bool throwIfNotFound)
		{
			var item = default(T);
			IDynamicProxy proxy = null;

			var tableName = this.Configuration.GetTableName(typeof(T));
			var primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(typeof(T));

			// First, check the cache
			var cacheKey = DynamicProxyFactory.GetDynamicTypeName(typeof(T), this);
			var cache = this.Configuration.ShouldCacheType(typeof(T)) ?
				Database.Cache.GetOrAdd(
				cacheKey,
				(string s) => new ItemCache(
					this.Configuration.GetCacheExpiryLength(typeof(T)),
					this.Configuration.GetCacheMaxItems(typeof(T)))) : null;
			if (cache != null && cache.ContainsKey(id))
			{
				// It's in there
				System.Diagnostics.Trace.WriteLine("Getting " + tableName + " with ID " + id + " from the cache", "Dynamic Proxy");

				item = Create<T>();
				proxy = (IDynamicProxy)item;
				proxy.SetValuesFromBag(cache.GetValues(id));
				proxy.IsNew = false;
				proxy.HasChanges = false;
			}
			else
			{
				// It's not in the cache, going to have to load it from the database
				var select = Select.From(tableName).Where(primaryKeyColumnName, SqlOperator.Equals, id);

				using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
				using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
				{
					command.Connection = connection;
					OnBeforeExecuteCommand(command);
					using (var reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							item = Create<T>();
							proxy = (IDynamicProxy)item;
							proxy.SetValuesFromReader(reader);
							proxy.IsNew = false;
							proxy.HasChanges = false;

							// Add or update it in the cache
							if (cache != null)
							{
								cache.AddOrUpdate(
									id,
									proxy.GetBagFromValues(),
									(key, existingValue) => proxy.GetBagFromValues());
							}
						}
						else if (throwIfNotFound)
						{
							throw new ItemNotFoundException($"The {typeof(T).Name} with ID {id} was not found in the database", id);
						}
					}
					OnAfterExecuteCommand(command);
				}
			}

			if (proxy != null)
			{
				OnAfterLoad(proxy);
			}

			return item;
		}

		/// <summary>
		/// Refreshes the supplied item from the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item">The item.</param>
		/// <exception cref="ArgumentException">item</exception>
		public void Refresh<T>(T item)
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, "item");
			}

			var proxy = (IDynamicProxy)item;

			OnBeforeRefresh(proxy);

			var newItem = Load<T>(proxy.PrimaryKeyValue);
			LoadValues(newItem, (IDynamicProxy)item);

			// Refresh any loaded children
			foreach (var collectionPropertyName in proxy.StateTracker.LoadedCollections)
			{
				var property = proxy.GetType().GetProperty(collectionPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

				var elementType = TypeHelper.GetElementType(property.PropertyType);
				var tableName = this.Configuration.GetTableName(elementType);
				var foreignKeyColumnName = this.Configuration.GetForeignKeyColumnName(elementType, typeof(T));
				var select = Select.From(tableName).Where(foreignKeyColumnName, SqlOperator.Equals, proxy.PrimaryKeyValue);

				// We know that this is an IList because we created it as an List in the DynamicProxyFactory
				RefreshCollection(select, elementType, (IList)property.GetValue(item, null));
			}

			OnAfterRefresh(proxy);
		}

		/// <summary>
		/// Loads a collection of items from the database using the supplied query.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="select">The select statement.</param>
		/// <returns></returns>
		public IList<T> LoadCollection<T>(SelectStatement select)
		{
			OnBeforeLoadCollection(select);

			var result = new List<T>();

			var itemType = GetCollectionItemType(typeof(T));

			using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var newItem = LoadItemInCollection<T>(reader, itemType);
						result.Add(newItem);
					}
				}
				OnAfterExecuteCommand(command);

				if (result.Count > 0 && select.IncludePaths.Count > 0)
				{
					LoadIncludePaths(select, result);
				}
			}

			OnAfterLoadCollection(result);

			return result;
		}

		private IList<IDynamicProxy> LoadCollection(SelectStatement select, Type itemType)
		{
			OnBeforeLoadCollection(select);

			var result = new List<IDynamicProxy>();

			using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var proxyConstructor = DynamicProxyFactory.GetDynamicProxyConstructor(itemType, this);
						var proxy = (IDynamicProxy)proxyConstructor.Invoke(Type.EmptyTypes);
						proxy.StateTracker.Database = this;
						proxy.SetValuesFromReader(reader);
						result.Add(proxy);
					}
				}
				OnAfterExecuteCommand(command);

				if (result.Count > 0 && select.IncludePaths.Count > 0)
				{
					LoadIncludePaths(select, result);
				}
			}

			OnAfterLoadCollection(result);

			return result;
		}

		private void LoadIncludePaths<T>(SelectStatement select, IList<T> result)
		{
			var parentType = typeof(T);
			if (typeof(IDynamicProxy).IsAssignableFrom(parentType))
			{
				parentType = parentType.BaseType;
			}

			// We need to ensure that paths are not loaded twice if the user has specified any compound paths
			// E.g. for "Books.Subject" on Author we need to remove "Books" if it's been specified
			// Otherwise the books collection would be loaded for "Books" AND "Books.Subject"
			var pathsToRemove = new List<string>();
			foreach (var path in select.IncludePaths)
			{
				var pathParts = path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < pathParts.Length - 1; i++)
				{
					var newPath = string.Join(".", pathParts.Take(i + 1));
					pathsToRemove.Add(newPath);
				}
			}

			var newPaths = select.IncludePaths.Except(pathsToRemove);
			foreach (var path in newPaths)
			{
				LoadIncludePath(select, result, parentType, path);
			}
		}

		private void LoadIncludePath<T>(SelectStatement parentQuery, IList<T> parentCollection, Type parentType, string path)
		{
			var firstProperty = path.Contains(".") ? path.Substring(0, path.IndexOf(".")) : path;
			var pathProperty = parentType.GetProperty(firstProperty);
			LoadCompoundChildItems(parentQuery, parentCollection, path, parentType, pathProperty);
		}

		private void LoadCompoundChildItems<T>(SelectStatement parentQuery, IList<T> parentCollection, string path, Type parentType, PropertyInfo pathProperty)
		{
			// Create arrays for each path and its corresponding properties and collections
			var pathParts = path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			var paths = pathParts.Select(p => new IncludePath(p)).ToArray();

			// Get the chain of properties in this path
			var propertyParentType = parentType;
			for (var i = 0; i < pathParts.Length; i++)
			{
				paths[i].Property = propertyParentType.GetProperty(pathParts[i]);
				propertyParentType = paths[i].Property.PropertyType;
				if (this.Configuration.IsRelatedCollection(paths[i].Property))
				{
					propertyParentType = TypeHelper.GetElementType(propertyParentType);
				}
			}

			// Build a statement to use as a subquery to get the IDs of the parent items
			Type itemType;
			SelectStatement childQuery;
			if (this.Configuration.IsRelatedCollection(pathProperty))
			{
				itemType = TypeHelper.GetElementType(pathProperty.PropertyType);
				childQuery = GetChildCollectionSubquery(parentQuery, parentType, itemType);
			}
			else if (this.Configuration.IsRelatedItem(pathProperty))
			{
				itemType = pathProperty.PropertyType;
				childQuery = GetChildItemSubquery(parentQuery, pathProperty, itemType);
			}
			else
			{
				throw new InvalidOperationException();
			}

			// Load the first child items
			// E.g. SELECT * FROM Book WHERE AuthorID IN (SELECT ID FROM Author WHERE LastName LIKE 'A%')
			var loadCollectionMethod = this.GetType().GetMethod("LoadCollection", new Type[] { typeof(SelectStatement) });
			var genericLoadCollectionMethod = loadCollectionMethod.MakeGenericMethod(itemType);
			var childCollection = genericLoadCollectionMethod.Invoke(this, new object[] { childQuery });
			paths[0].ChildCollection = (IEnumerable)childCollection;

			// For compound paths, load the other child items
			// E.g. SELECT Subject.*
			//		FROM Book INNER JOIN Subject ON Book.SubjectID = Subject.ID
			//		WHERE Book.AuthorID IN (SELECT ID FROM Author WHERE LastName LIKE 'A%')
			// We're doing a subquery for the first path part and joins for the rest, only because it
			// seems easier this way
			propertyParentType = itemType;
			for (var i = 1; i < pathParts.Length; i++)
			{
				paths[i].ChildCollection = LoadChildCollection(childQuery, propertyParentType, paths[i].Property, loadCollectionMethod);
				propertyParentType = paths[i].Property.PropertyType;
			}

			// Assign the child items to the appropriate parent items
			// TODO: There's a lot of scope for optimization here!
			SetChildItems(parentCollection, parentType, paths, 0);
		}

		private SelectStatement GetChildCollectionSubquery(SelectStatement parentQuery, Type parentType, Type itemType)
		{
			// Build a statement to use as a subquery to get the IDs of the parent items
			var parentTable = (Table)parentQuery.Source;
			var parentIDColumn = new Column(
				!string.IsNullOrEmpty(parentTable.Alias) ? parentTable.Alias : parentTable.Name,
				this.Configuration.GetPrimaryKeyColumnName(parentType));
			var selectParentItemIDs = Select.From(parentQuery.Source).Columns(parentIDColumn);
			if (parentQuery.SourceJoins.Count > 0)
			{
				selectParentItemIDs.SourceJoins.AddRange(parentQuery.SourceJoins);
			}
			if (parentQuery.Conditions.Count > 0)
			{
				selectParentItemIDs = selectParentItemIDs.Where(parentQuery.Conditions);
			}

			// Build a statement to get the child items
			var foreignKeyColumnName = this.Configuration.GetForeignKeyColumnName(itemType, parentType);
			var childTableName = this.Configuration.GetTableName(itemType);
			var childQuery = Select.From(childTableName).Where(
				new Condition(foreignKeyColumnName, SqlOperator.IsIn, selectParentItemIDs));

			return childQuery;
		}

		private SelectStatement GetChildItemSubquery(SelectStatement parentQuery, PropertyInfo parentProperty, Type itemType)
		{
			Column foreignKeyColumn;
			if (parentQuery.Source is UserDefinedFunction)
			{
				// Build a statement to use as a subquery to get the IDs of the parent items
				var foreignTable = (UserDefinedFunction)parentQuery.Source;
				foreignKeyColumn = new Column(
					!string.IsNullOrEmpty(foreignTable.Alias) ? foreignTable.Alias : foreignTable.Name,
					this.Configuration.GetForeignKeyColumnName(parentProperty));
			}
			else
			{
				// Build a statement to use as a subquery to get the IDs of the parent items
				var foreignTable = (Table)parentQuery.Source;
				foreignKeyColumn = new Column(
					!string.IsNullOrEmpty(foreignTable.Alias) ? foreignTable.Alias : foreignTable.Name,
					this.Configuration.GetForeignKeyColumnName(parentProperty));
			}

			var selectChildItemIDs = Select.From(parentQuery.Source).Columns(foreignKeyColumn);
			if (parentQuery.SourceJoins.Count > 0)
			{
				selectChildItemIDs.SourceJoins.AddRange(parentQuery.SourceJoins);
			}
			if (parentQuery.Conditions.Count > 0)
			{
				selectChildItemIDs = selectChildItemIDs.Where(parentQuery.Conditions);
			}

			// Build a statement to get the child items
			var primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(itemType);
			var childTableName = this.Configuration.GetTableName(itemType);
			var childQuery = Select.From(childTableName).Where(
				new Condition(primaryKeyColumnName, SqlOperator.IsIn, selectChildItemIDs));

			return childQuery;
		}

		private IEnumerable LoadChildCollection(SelectStatement childQuery, Type propertyParentType, PropertyInfo pathProperty, MethodInfo loadCollectionMethod)
		{
			childQuery.SourceFields.Clear();

			Type itemType;
			if (this.Configuration.IsRelatedCollection(pathProperty))
			{
				itemType = TypeHelper.GetElementType(pathProperty.PropertyType);

				var tableName = this.Configuration.GetTableName(itemType);
				childQuery = childQuery.ColumnsFrom(tableName);
				childQuery = childQuery.Join(
					tableName,
					this.Configuration.GetTableName(propertyParentType),
					this.Configuration.GetPrimaryKeyColumnName(propertyParentType),
					tableName,
					this.Configuration.GetForeignKeyColumnName(itemType, propertyParentType));
			}
			else if (this.Configuration.IsRelatedItem(pathProperty))
			{
				itemType = pathProperty.PropertyType;

				var tableName = this.Configuration.GetTableName(itemType);
				childQuery = childQuery.ColumnsFrom(tableName);
				childQuery = childQuery.Join(
					tableName,
					this.Configuration.GetTableName(propertyParentType),
					this.Configuration.GetForeignKeyColumnName(propertyParentType, itemType),
					tableName,
					this.Configuration.GetPrimaryKeyColumnName(itemType));
			}
			else
			{
				throw new InvalidOperationException();
			}

			var genericLoadCollectionMethod = loadCollectionMethod.MakeGenericMethod(itemType);
			var childCollection = genericLoadCollectionMethod.Invoke(this, new object[] { childQuery });
			return (IEnumerable)childCollection;
		}

		private void SetChildItems(IEnumerable parentCollection, Type parentType, IncludePath[] paths, int pathIndex)
		{
			var pathToLoad = paths[pathIndex].Path;
			var propertyToLoad = paths[pathIndex].Property;
			var childCollection = paths[pathIndex].ChildCollection;
			if (this.Configuration.IsRelatedCollection(propertyToLoad))
			{
				var itemType = TypeHelper.GetElementType(propertyToLoad.PropertyType);
				var foreignKeyColumnName = this.Configuration.GetForeignKeyColumnName(itemType, parentType);
				foreach (IDynamicProxy parent in parentCollection)
				{
					var children = propertyToLoad.PropertyType.IsInterface ?
						(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType)) :
						(IList)Activator.CreateInstance(propertyToLoad.PropertyType);
					foreach (var child in childCollection)
					{
						var parentID = child.GetType().GetProperty(foreignKeyColumnName).GetValue(child);
						if (parent.PrimaryKeyValue.Equals(parentID))
						{
							children.Add(child);
						}
					}
					parent.StateTracker.SetCollection(pathToLoad, children);
					propertyToLoad.SetValue(parent, children);

					if (pathIndex < paths.Length - 1)
					{
						SetChildItems(children, itemType, paths, pathIndex + 1);
					}
				}

			}
			else if (this.Configuration.IsRelatedItem(propertyToLoad))
			{
				var itemType = propertyToLoad.PropertyType;
				var foreignKeyColumnName = this.Configuration.GetForeignKeyColumnName(propertyToLoad);

				var parentProxyType = DynamicProxyFactory.GetDynamicProxyType(parentType, this);
				var parentChildIDProperty = parentProxyType.GetProperty(foreignKeyColumnName);
				foreach (IDynamicProxy parent in parentCollection)
				{
					var parentChildID = parentChildIDProperty.GetValue(parent);
					foreach (var child in childCollection)
					{
						if (((IDynamicProxy)child).PrimaryKeyValue.Equals(parentChildID))
						{
							propertyToLoad.SetValue(parent, child);

							if (pathIndex < paths.Length - 1)
							{
								SetChildItems(new object[] { child }, itemType, paths, pathIndex + 1);
							}

							break;
						}
					}
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Loads a collection of items from the database using the supplied select statement.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="select">The select statement.</param>
		/// <returns></returns>
		public IList<T> LoadCollection<T>(SelectStatement<T> select)
		{
			var select2 = select.CreateStatement(new QueryMapper(this.Configuration));
			return LoadCollection<T>(select2);
		}

		/// <summary>
		/// Loads a collection of items from the database using the supplied query.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query">The query as a string with parameter value placeholders signified by @0, @1 etc.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public IList<T> LoadCollection<T>(string query, params object[] parameters)
		{
			var result = new List<T>();

			var itemType = GetCollectionItemType(typeof(T));

			using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(query, this.Configuration, parameters))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var newItem = LoadItemInCollection<T>(reader, itemType);
						result.Add(newItem);
					}
				}
				OnAfterExecuteCommand(command);
			}

			return result;
		}

		private CollectionItemType GetCollectionItemType(Type itemType)
		{
			if (itemType.IsValueType || itemType == typeof(string) || itemType == typeof(byte[]))
			{
				return CollectionItemType.Value;
			}
			else if (TypeHelper.IsAnonymous(itemType))
			{
				return CollectionItemType.Anonymous;
			}
			else if (typeof(IDynamicProxy).IsAssignableFrom(itemType))
			{
				// TODO: Should this actually be whether the item should be mapped (from Configuration)?
				return CollectionItemType.DynamicProxy;
			}
			else if (this.Configuration.ShouldMapType(itemType))
			{
				return CollectionItemType.MappedObject;
			}
			else
			{
				return CollectionItemType.PlainObject;
			}
		}

		private T LoadItemInCollection<T>(DbDataReader reader, CollectionItemType itemType)
		{
			switch (itemType)
			{
				case CollectionItemType.Value:
				{
					var value = TypeHelper.ChangeType(reader.GetValue(0), typeof(T));
					return value != null ? (T)value : default;
				}
				case CollectionItemType.Anonymous:
				{
					var values = new List<object>();
					foreach (var p in typeof(T).GetProperties())
					{
						var ordinal = reader.GetOrdinal(p.Name);
						var value = TypeHelper.ChangeType(reader.GetValue(ordinal), p.PropertyType);
						values.Add(value);
					}
					return (T)Activator.CreateInstance(typeof(T), values.ToArray());
				}
				case CollectionItemType.DynamicProxy:
				{
					var newItem = (T)typeof(T).GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
					var proxy = (IDynamicProxy)newItem;
					proxy.StateTracker.Database = this;
					proxy.SetValuesFromReader(reader);
					return newItem;
				}
				case CollectionItemType.MappedObject:
				{
					var newItem = DynamicProxyFactory.GetDynamicProxy<T>(this);
					var proxy = (IDynamicProxy)newItem;
					proxy.SetValuesFromReader(reader);
					return newItem;
				}
				case CollectionItemType.PlainObject:
				default:
				{
					return (T)typeof(T).GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
				}
			}
		}

		private void RefreshCollection(SelectStatement select, Type elementType, IList collection)
		{
			var result = new List<IDynamicProxy>();

			// Load items from the database
			using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var proxy = DynamicProxyFactory.GetDynamicProxy(elementType, this);
						proxy.SetValuesFromReader(reader);
						result.Add(proxy);
					}
				}
				OnAfterExecuteCommand(command);
			}

			// Remove items from the collection that are no longer in the database
			for (var i = collection.Count - 1; i >= 0; i--)
			{
				if (!result.Any(p => p.PrimaryKeyValue == ((IDynamicProxy)collection[i]).PrimaryKeyValue))
				{
					collection.RemoveAt(i);
				}
			}

			// Update the existing items in the collection and add new items
			foreach (var newItem in result)
			{
				// TODO: Gross
				IDynamicProxy proxy = null;
				foreach (var item in collection)
				{
					if (((IDynamicProxy)item).PrimaryKeyValue == newItem.PrimaryKeyValue)
					{
						proxy = (IDynamicProxy)item;
						break;
					}
				}
				if (proxy != null)
				{
					LoadValues(newItem, proxy);
				}
				else
				{
					collection.Add(newItem);
				}
			}
		}

		/// <summary>
		/// Loads the first returned value from the database using the supplied query.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns></returns>
		public object LoadValue(SelectStatement select)
		{
			OnBeforeLoadValue(select);

			object value;

			using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				value = command.ExecuteScalar();
				OnAfterExecuteCommand(command);
			}

			OnAfterLoadValue(value);

			return value;
		}

		/// <summary>
		/// Loads the first returned value from the database using the supplied query.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns></returns>
		public object LoadValue<T>(SelectStatement<T> select)
		{
			var select2 = select.CreateStatement(new QueryMapper(this.Configuration));
			return LoadValue(select2);
		}

		/// <summary>
		/// Loads the first returned value from the database using the supplied query.
		/// </summary>
		/// <param name="query">The query as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public object LoadValue(string query, params object[] parameters)
		{
			object value;

			using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(query, this.Configuration, parameters))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				value = command.ExecuteScalar();
				OnAfterExecuteCommand(command);
			}

			return value;
		}

		/// <summary>
		/// Saves the specified item to the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item">The item.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		public void Save<T>(T item, DbConnection connection = null, DbTransaction transaction = null)
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, "item");
			}

			var proxy = (IDynamicProxy)item;

			OnBeforeSave(proxy);

			if (!proxy.IsValid)
			{
				var ex = new ValidationException(
					$"Validation failed for {item.GetType().BaseType.Name}: {item.ToString()}");
				ex.ValidationErrors.AddRange(proxy.ValidationErrors);
				throw ex;
			}

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection(this.Configuration);
			var transactionToUse = transaction ?? connectionToUse.BeginTransaction();
			try
			{
				SaveItem(proxy, connectionToUse, transactionToUse);

				UpdateSavedCollectionIDs(item);

				// If a transaction was not passed in and we created our own, commit it
				if (transaction == null)
				{
					transactionToUse.Commit();
				}
			}
			catch
			{
				// If a transaction was not passed in and we created our own, roll it back
				if (transaction == null)
				{
					transactionToUse.Rollback();
				}
				throw;
			}
			finally
			{
				// If a transaction or connection was not passed in and we created our own, dispose them
				if (transaction == null)
				{
					transactionToUse.Dispose();
				}
				if (connection == null)
				{
					connectionToUse.Dispose();
				}
			}

			OnAfterSave(proxy);
		}

		private void SaveItem(IDynamicProxy proxy, DbConnection connection, DbTransaction transaction)
		{
			var tableType = proxy.GetType().BaseType;
			var tableName = this.Configuration.GetTableName(tableType);
			var primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(tableType);

			// Insert or update all of the related items that should be saved with this item
			var newRelatedItems = new List<IDynamicProxy>();
			foreach (var propertyName in proxy.StateTracker.LoadedItems)
			{
				var property = tableType.GetProperty(propertyName);
				var relatedItem = (IDynamicProxy)property.GetValue(proxy, null);
				if (relatedItem != null)
				{
					if (this.Configuration.ShouldCascadeInternal(property))
					{
						SaveItem(relatedItem, connection, transaction);

						// Update the related item ID property
						var relatedItemIDPropertyName = this.Configuration.GetForeignKeyColumnName(property);
						var relatedItemIDProperty = proxy.GetType().GetProperty(relatedItemIDPropertyName);
						relatedItemIDProperty.SetValue(proxy, relatedItem.PrimaryKeyValue, null);

						// Add the related item to a list so that we can check whether it needs to be
						// re-saved as part of a related collection
						newRelatedItems.Add(relatedItem);
					}
					else if (relatedItem.IsNew)
					{
						var message = $"The related item '{tableType.Name}.{propertyName}' must be saved before the parent '{tableType.Name}'.";
						throw new InvalidOperationException(message);
					}
				}
			}

			if (proxy.IsNew)
			{
				// Insert the item
				InsertItem(proxy, tableName, tableType, primaryKeyColumnName, connection, transaction);
			}
			else
			{
				// Only update the item if its fields have been changed
				if (proxy.HasChanges)
				{
					UpdateItem(proxy, tableName, primaryKeyColumnName, connection, transaction);
				}
			}

			// Add or update it in the cache
			if (this.Configuration.ShouldCacheType(tableType))
			{
				var cacheKey = DynamicProxyFactory.GetDynamicTypeName(tableType, this);
				var cache = Database.Cache.GetOrAdd(
					cacheKey,
					(string s) => new ItemCache(
						this.Configuration.GetCacheExpiryLength(tableType),
						this.Configuration.GetCacheMaxItems(tableType)));
				cache.AddOrUpdate(
					proxy.PrimaryKeyValue,
					proxy.GetBagFromValues(),
					(key, existingValue) => proxy.GetBagFromValues());
			}

			// Clear any changed fields
			proxy.StateTracker.ChangedFields.Clear();

			// Insert, update or delete all of the related collections that should be saved with this item
			foreach (var collectionPropertyName in proxy.StateTracker.LoadedCollections)
			{
				var property = tableType.GetProperty(collectionPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

				// Save items and collect the item IDs while we're at it
				var collectionIDs = new List<object>();
				foreach (IDynamicProxy childItem in ((IEnumerable)property.GetValue(proxy, null)))
				{
					if (this.Configuration.ShouldCascade(property))
					{
						if (childItem.IsNew || newRelatedItems.Contains(childItem))
						{
							// Set the parent ID of the item in the collection
							var parentIDPropertyName = this.Configuration.GetForeignKeyColumnName(childItem.GetType().BaseType, tableType);
							var parentIDProperty = childItem.GetType().GetProperty(parentIDPropertyName);
							parentIDProperty.SetValue(childItem, proxy.PrimaryKeyValue, null);
						}
						SaveItem(childItem, connection, transaction);
					}
					collectionIDs.Add(((IDynamicProxy)childItem).PrimaryKeyValue);
				}

				// Delete items that have been removed from the collection
				if (this.Configuration.ShouldCascadeDelete(property))
				{
					var cascadeIDsToDelete = proxy.StateTracker.SavedCollectionIDs[property.Name].Except(collectionIDs).ToArray();
					if (cascadeIDsToDelete.Length > 0)
					{
						// Do it ten at a time just in case there's a huge amount and we would time out
						var deleteType = TypeHelper.GetElementType(property.PropertyType);
						var deleteTableName = this.Configuration.GetTableName(deleteType);
						var deletePrimaryKeyName = this.Configuration.GetPrimaryKeyColumnName(deleteType);

						var chunkSize = 10;
						for (var i = 0; i < cascadeIDsToDelete.Length; i += chunkSize)
						{
							var chunkedIDsToDelete = cascadeIDsToDelete.Skip(i).Take(chunkSize);
							var select = Select.From(deleteTableName).Where(deletePrimaryKeyName, SqlOperator.IsIn, chunkedIDsToDelete);

							// Load the items and delete them. Not the most efficient but it ensures that things
							// are cascaded correctly and the right events are raised
							foreach (var deleteItem in LoadCollection(select, deleteType))
							{
								Delete(deleteItem, deleteType, connection, transaction);
							}
						}
					}
				}
			}

			proxy.ResetOriginalValues();
			proxy.HasChanges = false;
		}

		private void InsertItem(IDynamicProxy proxy, string tableName, Type tableType, string primaryKeyColumnName, DbConnection connection, DbTransaction transaction)
		{
			var insert = Watsonia.QueryBuilder.Insert.Into(tableName);
			foreach (var property in this.Configuration.PropertiesToLoadAndSave(proxy.GetType()))
			{
				if (property.Name != primaryKeyColumnName)
				{
					if (property.PropertyType == typeof(string))
					{
						insert.Value(property.Name, property.GetValue(proxy, null) ?? "");
					}
					else
					{
						insert.Value(property.Name, property.GetValue(proxy, null));
					}
				}
			}

			Execute(insert, connection, transaction);

			// TODO: This probably isn't going to deal too well with concurrency, should there be a transaction?
			//	Or wack it on the end of the build(insert)?
			using (var getPrimaryKeyValueCommand = this.Configuration.DataAccessProvider.BuildInsertedIDCommand(this.Configuration))
			{
				getPrimaryKeyValueCommand.Connection = connection;
				getPrimaryKeyValueCommand.Transaction = transaction;
				var primaryKeyValue = getPrimaryKeyValueCommand.ExecuteScalar();
				proxy.PrimaryKeyValue = Convert.ChangeType(primaryKeyValue, this.Configuration.GetPrimaryKeyColumnType(tableType));
				proxy.IsNew = false;
			}
		}

		private void UpdateItem(IDynamicProxy proxy, string tableName, string primaryKeyColumnName, DbConnection connection, DbTransaction transaction)
		{
			// TODO: Get rid of this, it's just to stop properties like Database and HasChanges
			var doUpdate = false;

			var update = Update.Table(tableName);
			foreach (var property in this.Configuration.PropertiesToLoadAndSave(proxy.GetType()))
			{
				if (property.Name != primaryKeyColumnName && proxy.StateTracker.ChangedFields.Contains(property.Name))
				{
					if (property.PropertyType == typeof(string))
					{
						update.Set(property.Name, property.GetValue(proxy, null) ?? "");
					}
					else
					{
						update.Set(property.Name, property.GetValue(proxy, null));
					}
					doUpdate = true;
				}
			}

			if (!doUpdate)
			{
				return;
			}

			update = update.Where(primaryKeyColumnName, SqlOperator.Equals, proxy.PrimaryKeyValue);

			Execute(update, connection, transaction);
		}

		private void UpdateSavedCollectionIDs<T>(T item)
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, "item");
			}

			var proxy = (IDynamicProxy)item;

			if (proxy.StateTracker.LoadedCollections.Count > 0)
			{
				foreach (var property in this.Configuration.PropertiesToCascade(typeof(T)))
				{
					if (proxy.StateTracker.LoadedCollections.Contains(property.Name))
					{
						var collectionIDs = new List<object>();
						foreach (var cascadeItem in ((IEnumerable)property.GetValue(proxy, null)))
						{
							UpdateSavedCollectionIDs(cascadeItem);
							collectionIDs.Add(((IDynamicProxy)cascadeItem).PrimaryKeyValue);
						}
						proxy.StateTracker.SavedCollectionIDs[property.Name] = collectionIDs;
					}
				}
			}
		}

		/// <summary>
		/// Inserts the specified item and returns a proxy object.
		/// </summary>
		/// <typeparam name="T">The type of item to insert and create a proxy for.</typeparam>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		public T Insert<T>(T item)
		{
			OnBeforeInsert(item);

			var newItem = Create(item);
			Save(newItem);

			OnAfterInsert((IDynamicProxy)newItem);

			return newItem;
		}

		/// <summary>
		/// Deletes the item with the supplied ID from the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		public void Delete<T>(object id, DbConnection connection = null, DbTransaction transaction = null)
		{
			var item = Load<T>(id);
			Delete(item, connection, transaction);
		}

		/// <summary>
		/// Deletes the specified item from the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item">The item.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		/// <exception cref="ArgumentException">item</exception>
		public void Delete<T>(T item, DbConnection connection = null, DbTransaction transaction = null)
		{
			Delete(item, typeof(T), connection, transaction);
		}

		private void Delete(object item, Type itemType, DbConnection connection = null, DbTransaction transaction = null)
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, "item");
			}

			var proxy = (IDynamicProxy)item;

			OnBeforeDelete(proxy);

			var tableType = proxy.GetType().BaseType;
			var tableName = this.Configuration.GetTableName(tableType);
			var primaryKeyName = this.Configuration.GetPrimaryKeyColumnName(tableType);

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection(this.Configuration);
			var transactionToUse = transaction ?? connectionToUse.BeginTransaction();
			try
			{
				foreach (var property in this.Configuration.PropertiesToCascadeDelete(itemType))
				{
					// Load the items to delete. Not the most efficient but it ensures that
					// things are cascaded correctly and the right events are raised
					IList<IDynamicProxy> itemsToDelete = new List<IDynamicProxy>();

					var deleteType = TypeHelper.GetElementType(property.PropertyType);
					var deleteTableName = this.Configuration.GetTableName(deleteType);

					if (this.Configuration.IsRelatedItem(property))
					{
						var deleteKeyPropertyName = this.Configuration.GetForeignKeyColumnName(property);
						var deleteKeyProperty = proxy.GetType().GetProperty(deleteKeyPropertyName,
							BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
						var deletePrimaryKeyValue = deleteKeyProperty.GetValue(proxy);
						if (deletePrimaryKeyValue != null)
						{
							var deletePrimaryKeyName = this.Configuration.GetPrimaryKeyColumnName(deleteType);
							var select = Select.From(deleteTableName).Where(deletePrimaryKeyName, SqlOperator.Equals, deletePrimaryKeyValue);
							itemsToDelete = LoadCollection(select, deleteType);

							// Update the field to null to avoid a reference exception when deleting the item
							var columnName = this.Configuration.GetColumnName(property);
							var updateQuery = Update.Table(tableName).Set(columnName, null).Where(primaryKeyName, SqlOperator.Equals, proxy.PrimaryKeyValue);
							Execute(updateQuery, connection, transaction);
						}
					}
					else if (this.Configuration.IsRelatedCollection(property))
					{
						var deleteForeignKeyName = this.Configuration.GetForeignKeyColumnName(deleteType, tableType);
						var select = Select.From(deleteTableName).Where(deleteForeignKeyName, SqlOperator.Equals, proxy.PrimaryKeyValue);
						itemsToDelete = LoadCollection(select, deleteType);
					}

					foreach (var deleteItem in itemsToDelete)
					{
						Delete(deleteItem, deleteType, connection, transaction);
					}
				}

				var deleteQuery = Watsonia.QueryBuilder.Delete.From(tableName).Where(primaryKeyName, SqlOperator.Equals, proxy.PrimaryKeyValue);
				Execute(deleteQuery, connectionToUse, transactionToUse);

				// If a transaction was not passed in and we created our own, commit it
				if (transaction == null)
				{
					transactionToUse.Commit();
				}
			}
			catch
			{
				// If a transaction was not passed in and we created our own, roll it back
				if (transaction == null)
				{
					transactionToUse.Rollback();
				}
				throw;
			}
			finally
			{
				// If a transaction or connection was not passed in and we created our own, dispose them
				if (transaction == null)
				{
					transactionToUse.Dispose();
				}
				if (connection == null)
				{
					connectionToUse.Dispose();
				}
			}

			OnAfterDelete(proxy);
		}

		/// <summary>
		/// Executes the specified query against the database.
		/// </summary>
		/// <param name="statement">The statement.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		public void Execute(Statement statement, DbConnection connection = null, DbTransaction transaction = null)
		{
			OnBeforeExecute(statement);

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection(this.Configuration);
			try
			{
				using (var command = this.Configuration.DataAccessProvider.BuildCommand(statement, this.Configuration))
				{
					command.Connection = connectionToUse;
					command.Transaction = transaction;
					OnBeforeExecuteCommand(command);
					command.ExecuteNonQuery();
					OnAfterExecuteCommand(command);
				}
			}
			finally
			{
				// If a connection was not passed in and we created our own, dispose it
				if (connection == null)
				{
					connectionToUse.Dispose();
				}
			}

			OnAfterExecute(statement);
		}

		/// <summary>
		/// Executes the specified query against the database.
		/// </summary>
		/// <param name="statement">The statement as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="parameters">The parameters.</param>
		public void Execute(string statement, params object[] parameters)
		{
			Execute(statement, connection: null, transaction: null, parameters: parameters);
		}

		/// <summary>
		/// Executes the specified query against the database.
		/// </summary>
		/// <param name="query">The statement as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="parameters">The parameters.</param>
		public void Execute(string statement, DbConnection connection, params object[] parameters)
		{
			Execute(statement, connection: connection, transaction: null, parameters: parameters);
		}

		/// <summary>
		/// Executes the specified query against the database.
		/// </summary>
		/// <param name="query">The query as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		/// <param name="parameters">The parameters.</param>
		public void Execute(string statement, DbConnection connection, DbTransaction transaction, params object[] parameters)
		{
			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection(this.Configuration);
			try
			{
				using (var command = this.Configuration.DataAccessProvider.BuildCommand(statement, this.Configuration, parameters))
				{
					command.Connection = connectionToUse;
					command.Transaction = transaction;
					OnBeforeExecuteCommand(command);
					command.ExecuteNonQuery();
					OnAfterExecuteCommand(command);
				}
			}
			finally
			{
				// If a connection was not passed in and we created our own, dispose it
				if (connection == null)
				{
					connectionToUse.Dispose();
				}
			}
		}

		/// <summary>
		/// Executes a stored procedure against the database.
		/// </summary>
		/// <param name="procedureName">The name of the stored procedure.</param>
		/// <param name="parameters">Any parameters that need to be passed to the stored procedure.</param>
		/// <returns>Any value returned by the stored procedure.</returns>
		public object ExecuteProcedure(string procedureName, params Parameter[] parameters)
		{
			object value;

			using (var connection = this.Configuration.DataAccessProvider.OpenConnection(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildProcedureCommand(procedureName, parameters))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				value = command.ExecuteScalar();
				OnAfterExecuteCommand(command);
			}

			return value;
		}

		private void LoadValues(object source, IDynamicProxy destination)
		{
			if (!destination.IsNew)
			{
				destination.StateTracker.IsLoading = true;
			}

			// TODO: Move this into the dynamic proxy
			// TODO: Pipe everything through the same area; this, linq, etc
			foreach (var sourceProperty in this.Configuration.PropertiesToMap(source.GetType()))
			{
				if (this.Configuration.ShouldMapTypeInternal(sourceProperty.PropertyType))
				{
					var destPropertyName = this.Configuration.GetForeignKeyColumnName(sourceProperty);
					var destProperty = destination.GetType().GetProperty(destPropertyName);
					if ((source is IDynamicProxy) == false ||
						((IDynamicProxy)source).StateTracker.LoadedItems.Contains(sourceProperty.Name))
					{
						var sourceItem = (IDynamicProxy)sourceProperty.GetValue(source, null);
						if (destProperty != null && sourceItem != null)
						{
							destProperty.SetValue(destination, sourceItem.PrimaryKeyValue, null);
						}
					}
				}
				else if (this.Configuration.ShouldLoadAndSaveProperty(sourceProperty))
				{
					var destProperty = destination.GetType().GetProperty(sourceProperty.Name);
					if (destProperty != null)
					{
						destProperty.SetValue(destination, sourceProperty.GetValue(source, null), null);
					}
				}
			}

			destination.StateTracker.IsLoading = false;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			// NOTE: This is here just in case it's ever needed, really.  I figure it's better to have code with using blocks
			// up front rather than forcing them to be retro-fitted later on
			// Dispose might one day be needed to clean up transactions or connections or the like
		}

		/// <summary>
		/// Called at the start of UpdateDatabase.
		/// </summary>
		protected virtual void OnBeforeUpdateDatabase()
		{
			BeforeUpdateDatabase?.Invoke(this, System.EventArgs.Empty);
		}

		/// <summary>
		/// Called at the end of UpdateDatabase.
		/// </summary>
		protected virtual void OnAfterUpdateDatabase()
		{
			AfterUpdateDatabase?.Invoke(this, System.EventArgs.Empty);
		}

		/// <summary>
		/// Called at the end of Create.
		/// </summary>
		/// <param name="proxy">The proxy object that was created.</param>
		protected virtual void OnAfterCreate(IDynamicProxy proxy)
		{
			AfterCreate?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the end of Load.
		/// </summary>
		/// <param name="proxy">The proxy object that was created.</param>
		protected virtual void OnAfterLoad(IDynamicProxy proxy)
		{
			AfterLoad?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the start of Refresh.
		/// </summary>
		/// <param name="proxy">The proxy object that will be refreshed.</param>
		protected virtual void OnBeforeRefresh(IDynamicProxy proxy)
		{
			BeforeRefresh?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the end of Refresh.
		/// </summary>
		/// <param name="proxy">The proxy object that was refreshed.</param>
		protected virtual void OnAfterRefresh(IDynamicProxy proxy)
		{
			AfterRefresh?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the start of LoadCollection.
		/// </summary>
		/// <param name="select">The statement that will be used to load the collection.</param>
		protected virtual void OnBeforeLoadCollection(SelectStatement select)
		{
			BeforeLoadCollection?.Invoke(this, new StatementEventArgs(select));
		}

		/// <summary>
		/// Called at the end of LoadCollection.
		/// </summary>
		/// <param name="collection">The collection that was loaded.</param>
		protected virtual void OnAfterLoadCollection(IEnumerable collection)
		{
			AfterLoadCollection?.Invoke(this, new CollectionEventArgs(collection));
		}

		/// <summary>
		/// Called at the start of LoadValue.
		/// </summary>
		/// <param name="select">The statement that will be used to load the value.</param>
		protected virtual void OnBeforeLoadValue(SelectStatement select)
		{
			BeforeLoadValue?.Invoke(this, new StatementEventArgs(select));
		}

		/// <summary>
		/// Called at the end of LoadValue.
		/// </summary>
		/// <param name="value">The value that was loaded.</param>
		protected virtual void OnAfterLoadValue(object value)
		{
			AfterLoadValue?.Invoke(this, new ValueEventArgs(value));
		}

		/// <summary>
		/// Called at the start of Save.
		/// </summary>
		/// <param name="proxy">The proxy object that will be saved.</param>
		protected virtual void OnBeforeSave(IDynamicProxy proxy)
		{
			BeforeSave?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the end of Save.
		/// </summary>
		/// <param name="proxy">The proxy object that was saved.</param>
		protected virtual void OnAfterSave(IDynamicProxy proxy)
		{
			AfterSave?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the start of Insert.
		/// </summary>
		/// <param name="item">The item that will be inserted.</param>
		protected virtual void OnBeforeInsert(object item)
		{
			BeforeInsert?.Invoke(this, new ValueEventArgs(item));
		}

		/// <summary>
		/// Called at the end of Insert.
		/// </summary>
		/// <param name="proxy">The proxy object that was inserted.</param>
		protected virtual void OnAfterInsert(IDynamicProxy proxy)
		{
			AfterInsert?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the start of Delete.
		/// </summary>
		/// <param name="proxy">The proxy object that will be deleted.</param>
		protected virtual void OnBeforeDelete(IDynamicProxy proxy)
		{
			BeforeDelete?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the end of Delete.
		/// </summary>
		/// <param name="proxy">The proxy object that was deleted.</param>
		protected virtual void OnAfterDelete(IDynamicProxy proxy)
		{
			AfterDelete?.Invoke(this, new ItemEventArgs(proxy));
		}

		/// <summary>
		/// Called at the start of Execute.
		/// </summary>
		/// <param name="statement">The statement that will be executed.</param>
		protected virtual void OnBeforeExecute(Statement statement)
		{
			BeforeExecute?.Invoke(this, new StatementEventArgs(statement));
		}

		/// <summary>
		/// Called at the end of Execute.
		/// </summary>
		/// <param name="statement">The statement that was executed.</param>
		protected virtual void OnAfterExecute(Statement statement)
		{
			AfterExecute?.Invoke(this, new StatementEventArgs(statement));
		}

		/// <summary>
		/// Called before a command is executed.
		/// </summary>
		/// <param name="command">The command that will be executed.</param>
		protected virtual void OnBeforeExecuteCommand(DbCommand command)
		{
			BeforeExecuteCommand?.Invoke(this, new CommandEventArgs(command));
		}

		/// <summary>
		/// Called at the end of ExecuteSQL.
		/// </summary>
		/// <param name="statement">The statement that was executed.</param>
		protected virtual void OnAfterExecuteCommand(DbCommand command)
		{
			AfterExecuteCommand?.Invoke(this, new CommandEventArgs(command));
		}

		protected string GetSqlStringFromCommand(DbCommand command)
		{
			var b = new StringBuilder();
			b.Append(Regex.Replace(command.CommandText.Replace(Environment.NewLine, " "), @"\s+", " "));
			if (command.Parameters.Count > 0)
			{
				b.Append(" { ");
				for (var i = 0; i < command.Parameters.Count; i++)
				{
					if (i > 0)
					{
						b.Append(", ");
					}
					b.Append(command.Parameters[i].ParameterName);
					b.Append(" = '");
					b.Append(command.Parameters[i].Value);
					b.Append("'");
				}
				b.Append(" }");
			}
			return b.ToString();
		}
	}
}

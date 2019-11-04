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
using System.Threading.Tasks;
using Watsonia.Data.EventArgs;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	/// <summary>
	/// Provides functionality for accessing and updating a database.
	/// </summary>
	public partial class Database
	{
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
		internal DatabaseItemCache Cache { get; } = new DatabaseItemCache();

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
		public string DatabaseName { get; set; }

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
			proxy.StateTracker.IsNew = true;

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
			proxy.StateTracker.IsNew = true;
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
			// NOTE: If changing to LoadCollectionAsync, you can cast to dynamic to await the collection without knowing its type
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
				new Table(parentTable.Name, parentTable.Alias, parentTable.Schema),
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
			var childTableSchema = this.Configuration.GetSchemaName(itemType);
			var childTableName = this.Configuration.GetTableName(itemType);
			var childQuery = Select.From(childTableName, null, childTableSchema).Where(
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
					new Table(foreignTable.Name, foreignTable.Alias, foreignTable.Schema),
					this.Configuration.GetForeignKeyColumnName(parentProperty));
			}
			else
			{
				// Build a statement to use as a subquery to get the IDs of the parent items
				var foreignTable = (Table)parentQuery.Source;
				foreignKeyColumn = new Column(
					new Table(foreignTable.Name, foreignTable.Alias, foreignTable.Schema),
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
			var childTableSchema = this.Configuration.GetSchemaName(itemType);
			var childTableName = this.Configuration.GetTableName(itemType);
			var childQuery = Select.From(childTableName, null, childTableSchema).Where(
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
			// NOTE: If changing to LoadCollectionAsync, you can cast to dynamic to await the collection without knowing its type
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
					foreach (IDynamicProxy child in childCollection)
					{
						var parentID = child.__GetValue(foreignKeyColumnName);
						if (parent.__PrimaryKeyValue.Equals(parentID))
						{
							children.Add(child);
						}
					}
					parent.StateTracker.SetCollection(pathToLoad, children);
					parent.__SetValue(propertyToLoad.Name, children);

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
						if (((IDynamicProxy)child).__PrimaryKeyValue.Equals(parentChildID))
						{
							parent.__SetValue(propertyToLoad.Name, child);

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

		private T LoadItemInCollection<T>(DbDataReader reader, CollectionItemType itemType, string[] fieldNames)
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
					var proxy = DynamicProxyFactory.GetDynamicProxyConstructor(typeof(T), this).Create();
					proxy.StateTracker.Database = this;
					proxy.__SetValuesFromReader(reader, fieldNames);
					return (T)proxy;
				}
				case CollectionItemType.MappedObject:
				{
					var newItem = DynamicProxyFactory.GetDynamicProxy<T>(this);
					var proxy = (IDynamicProxy)newItem;
					proxy.__SetValuesFromReader(reader, fieldNames);
					return newItem;
				}
				case CollectionItemType.PlainObject:
				default:
				{
					return (T)typeof(T).GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
				}
			}
		}

		private void UpdateSavedCollectionIDs<T>(T item)
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, nameof(item));
			}

			var proxy = (IDynamicProxy)item;

			if (proxy.StateTracker.LoadedCollections.Count > 0)
			{
				foreach (var property in this.Configuration.PropertiesToCascade(typeof(T)))
				{
					if (proxy.StateTracker.LoadedCollections.Contains(property.Name))
					{
						var collectionIDs = new List<object>();
						foreach (var cascadeItem in (IEnumerable)proxy.__GetValue(property.Name))
						{
							UpdateSavedCollectionIDs(cascadeItem);
							collectionIDs.Add(((IDynamicProxy)cascadeItem).__PrimaryKeyValue);
						}
						proxy.StateTracker.SavedCollectionIDs[property.Name] = collectionIDs;
					}
				}
			}
		}

		private void LoadValues(object source, IDynamicProxy destination)
		{
			if (!destination.StateTracker.IsNew)
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
							destProperty.SetValue(destination, sourceItem.__PrimaryKeyValue, null);
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

		private bool TryGetCacheAndProxy<T>(object id, out ItemCache cache, out T item)
		{
			var cacheKey = DynamicProxyFactory.GetDynamicTypeName(typeof(T), this);
			cache = this.Configuration.ShouldCacheType(typeof(T))
				? this.Cache.GetOrAdd(
					cacheKey,
					(string s) => new ItemCache(
						this.Configuration.GetCacheExpiryLength(typeof(T)),
						this.Configuration.GetCacheMaxItems(typeof(T))))
				: null;
			if (cache != null && cache.ContainsKey(id))
			{
				item = Create<T>();
				var proxy = (IDynamicProxy)item;
				proxy.__SetValuesFromBag(cache.GetValues(id));
				proxy.StateTracker.IsNew = false;
				proxy.StateTracker.HasChanges = false;
				return true;
			}

			item = default;
			return false;
		}

		private void AddOrUpdateCache(object id, ItemCache cache, IDynamicProxy proxy)
		{
			if (cache != null)
			{
				cache.AddOrUpdate(
					id,
					proxy.__GetBagFromValues(),
					(key, existingValue) => proxy.__GetBagFromValues());
			}
		}

		private void RefreshCollectionItems(IList collection, List<IDynamicProxy> newCollection)
		{

			// Remove items from the collection that are no longer in the database
			for (var i = collection.Count - 1; i >= 0; i--)
			{
				if (!newCollection.Any(p => p.__PrimaryKeyValue == ((IDynamicProxy)collection[i]).__PrimaryKeyValue))
				{
					collection.RemoveAt(i);
				}
			}

			// Update the existing items in the collection and add new items
			foreach (var newItem in newCollection)
			{
				// TODO: Gross
				IDynamicProxy proxy = null;
				foreach (var item in collection)
				{
					if (((IDynamicProxy)item).__PrimaryKeyValue == newItem.__PrimaryKeyValue)
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

		private string[] GetReaderFieldNames(DbDataReader reader)
		{
			var result = new string[reader.FieldCount];
			for (var i = 0; i < reader.FieldCount; i++)
			{
				result[i] = reader.GetName(i).ToUpperInvariant();
			}
			return result;
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
		/// Called at the start of LoadCollection.
		/// </summary>
		/// <param name="select">The statement that will be used to load the collection.</param>
		protected virtual void OnBeforeLoadCollection(string query)
		{
			BeforeLoadCollection?.Invoke(this, new ValueEventArgs(query));
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
	}
}

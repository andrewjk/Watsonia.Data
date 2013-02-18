//using IQToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Watsonia.Data.Linq;
using Watsonia.Data.Sql;

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
		/// Gets the configuration options used for mapping to and accessing the database.
		/// </summary>
		/// <value>
		/// The configuration.
		/// </value>
		internal DatabaseConfiguration Configuration
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets the name of the database to use when creating proxy objects to avoid the same
		/// proxy class being used for different databases with different table and column names.
		/// </summary>
		/// <value>
		/// The name of the database.
		/// </value>
		internal string DatabaseName
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Database" /> class.
		/// </summary>
		/// <param name="connectionString">The connection string used to access the database.</param>
		/// <param name="entityNamespace">The namespace in which entity classes are located.</param>
		public Database(string connectionString, string entityNamespace)
		{
			this.Configuration = new DatabaseConfiguration(connectionString, entityNamespace);
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
			return this.Configuration.DataAccessProvider.OpenConnection();
		}

		/// <summary>
		/// Updates the database from the mapped entity classes.
		/// </summary>
		public void UpdateDatabase()
		{
			OnBeforeUpdateDatabase();

			DatabaseUpdater updater = new DatabaseUpdater();
			updater.UpdateDatabase(this.Configuration);

			OnAfterUpdateDatabase();
		}

		/// <summary>
		/// Gets the update script for the mapped entity classes.
		/// </summary>
		/// <returns>A string containing the update script.</returns>
		public string GetUpdateScript()
		{
			DatabaseUpdater updater = new DatabaseUpdater();
			return updater.GetUpdateScript(this.Configuration);
		}

		/// <summary>
		/// Creates a proxy object for the type T.
		/// </summary>
		/// <typeparam name="T">The type of item to create a proxy for.</typeparam>
		/// <returns></returns>
		public T Create<T>()
		{
			T newItem = DynamicProxyFactory.GetDynamicProxy<T>(this);
			IDynamicProxy proxy = (IDynamicProxy)newItem;
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
			T newItem = DynamicProxyFactory.GetDynamicProxy<T>(this);
			IDynamicProxy proxy = (IDynamicProxy)newItem;
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
			var provider = new DynamicEntityProvider(this);
			Type proxyTableType = DynamicProxyFactory.GetDynamicProxyType(typeof(T), this);

			EntityMappingEntity entityMappingEntity = (EntityMappingEntity)provider.Mapping.GetEntity(proxyTableType);
			IQueryable<T> entityTable = (IQueryable<T>)provider.GetTable(entityMappingEntity);

			return new DatabaseQuery<T>(entityTable, entityMappingEntity);

			// TODO: Re-evaluate re-linq
			//var queryParser = Remotion.Linq.Parsing.Structure.QueryParser.CreateDefault();
			//return new DatabaseQuery<T>(queryParser, new DatabaseQueryExecutor(this));
		}

		internal Select[] Compile(Expression expression)
		{
			// For testing
			var provider = new DynamicEntityProvider(this);
			Expression plan = provider.GetExecutionPlan(expression);
			Expression[] blocks = QueryBlockGatherer.Gather(plan).Select(c => c.Expression).ToArray();
			return Array.ConvertAll<Expression, Select>(blocks, b => StatementCreator.Compile(b));
		}

		internal object Execute(Expression expression)
		{
			// For testing
			var provider = new DynamicEntityProvider(this);
			return provider.Execute(expression);
		}

		/// <summary>
		/// Loads the item from the database with the supplied ID.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <returns></returns>
		public T Load<T>(object id)
		{
			T item = Create<T>();
			IDynamicProxy proxy = (IDynamicProxy)item;

			string tableName = this.Configuration.GetTableName(typeof(T));
			string primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(typeof(T));

			var select =
				Select.From(tableName)
					  .Where(primaryKeyColumnName, SqlOperator.Equals, id);

			using (DbConnection connection = this.Configuration.DataAccessProvider.OpenConnection())
			using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(select))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (DbDataReader reader = command.ExecuteReader())
				{
					if (reader.Read())
					{
						proxy.SetValuesFromReader(reader);
						proxy.IsNew = false;
					}
				}
				OnAfterExecuteCommand(command);
			}

			OnAfterLoad(proxy);

			return item;
		}

		/// <summary>
		/// Refreshes the supplied item from the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item">The item.</param>
		/// <exception cref="System.ArgumentException">item</exception>
		public void Refresh<T>(T item)
		{
			if ((item as IDynamicProxy) == null)
			{
				string message = string.Format("item must be an IDynamicProxy (not {0})", item.GetType().Name);
				throw new ArgumentException(message, "item");
			}

			IDynamicProxy proxy = (IDynamicProxy)item;

			OnBeforeRefresh(proxy);

			T newItem = Load<T>(proxy.PrimaryKeyValue);
			LoadValues(newItem, (IDynamicProxy)item);

			// Refresh any loaded children
			foreach (string collectionPropertyName in proxy.StateTracker.LoadedCollections)
			{
				PropertyInfo property = proxy.GetType().GetProperty(collectionPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

				Type elementType = TypeHelper.GetElementType(property.PropertyType);
				string tableName = this.Configuration.GetTableName(elementType);
				string foreignKeyColumnName = this.Configuration.GetForeignKeyColumnName(elementType, typeof(T));
				var select = Select.From(tableName).Where(foreignKeyColumnName, SqlOperator.Equals, proxy.PrimaryKeyValue);

				// We know that this is an IList because we created it as an ObservableCollection in the DynamicProxyFactory
				RefreshCollection(select, elementType, (IList)property.GetValue(item, null));
			}

			OnAfterRefresh(proxy);
		}

		/// <summary>
		/// Loads a collection of items from the database using the supplied query.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query">The query.</param>
		/// <returns></returns>
		public IList<T> LoadCollection<T>(Select query)
		{
			OnBeforeLoadCollection(query);

			var result = new ObservableCollection<T>();

			using (DbConnection connection = this.Configuration.DataAccessProvider.OpenConnection())
			using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(query))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (DbDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						T newItem = LoadItemInCollection<T>(reader);
						result.Add(newItem);
					}
				}
				OnAfterExecuteCommand(command);

				//if (result.Count > 0)
				//{
				//	LoadIncludePaths(query, result);
				//}
			}

			OnAfterLoadCollection(result);

			return result;
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
			var result = new ObservableCollection<T>();

			using (DbConnection connection = this.Configuration.DataAccessProvider.OpenConnection())
			using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(query, parameters))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (DbDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						T newItem = LoadItemInCollection<T>(reader);
						result.Add(newItem);
					}
				}
				OnAfterExecuteCommand(command);
			}

			return result;
		}

		private T LoadItemInCollection<T>(DbDataReader reader)
		{
			Type itemType = typeof(T);

			T newItem;

			if (itemType.IsValueType || itemType == typeof(string))
			{
				newItem = (T)TypeHelper.ChangeType(reader.GetValue(0), typeof(T));
			}
			else if (TypeHelper.IsAnonymous(itemType))
			{
				List<object> values = new List<object>();
				foreach (PropertyInfo p in itemType.GetProperties())
				{
					int ordinal = reader.GetOrdinal(p.Name);
					object value = TypeHelper.ChangeType(reader.GetValue(ordinal), p.PropertyType);
					values.Add(value);
				}
				newItem = (T)Activator.CreateInstance(itemType, values.ToArray());
			}
			else if (itemType.IsSealed)
			{
				newItem = (T)itemType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			}
			else if (typeof(IDynamicProxy).IsAssignableFrom(itemType))
			{
				newItem = (T)itemType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
				IDynamicProxy proxy = (IDynamicProxy)newItem;
				proxy.StateTracker.Database = this;
				proxy.SetValuesFromReader(reader);
			}
			else
			{
				newItem = DynamicProxyFactory.GetDynamicProxy<T>(this);
				IDynamicProxy proxy = (IDynamicProxy)newItem;
				proxy.SetValuesFromReader(reader);
			}

			return newItem;
		}

		private void RefreshCollection(Select query, Type elementType, IList collection)
		{
			var result = new ObservableCollection<IDynamicProxy>();

			// Load items from the database
			using (DbConnection connection = this.Configuration.DataAccessProvider.OpenConnection())
			using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(query))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (DbDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						IDynamicProxy newItem = DynamicProxyFactory.GetDynamicProxy(elementType, this);
						newItem.SetValuesFromReader(reader);
						result.Add(newItem);
					}
				}
				OnAfterExecuteCommand(command);
			}

			// Remove items from the collection that are no longer in the database
			for (int i = collection.Count - 1; i >= 0; i--)
			{
				if (!result.Any(p => p.PrimaryKeyValue == ((IDynamicProxy)collection[i]).PrimaryKeyValue))
				{
					collection.RemoveAt(i);
				}
			}

			// Update the existing items in the collection and add new items
			foreach (IDynamicProxy newItem in result)
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
		/// <param name="query">The query.</param>
		/// <returns></returns>
		public object LoadValue(Select query)
		{
			OnBeforeLoadValue(query);

			object value;

			using (DbConnection connection = this.Configuration.DataAccessProvider.OpenConnection())
			using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(query))
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
		/// <param name="query">The query as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public object LoadValue(string query, params object[] parameters)
		{
			object value;

			using (DbConnection connection = this.Configuration.DataAccessProvider.OpenConnection())
			using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(query, parameters))
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
				string message = string.Format("item must be an IDynamicProxy (not {0})", item.GetType().Name);
				throw new ArgumentException(message, "item");
			}

			IDynamicProxy proxy = (IDynamicProxy)item;

			OnBeforeSave(proxy);

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			DbConnection connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection();
			DbTransaction transactionToUse = transaction ?? connectionToUse.BeginTransaction();
			try
			{
				SaveWithOptionalExtraFields(proxy, connectionToUse, transactionToUse);

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

		private void SaveWithOptionalExtraFields(IDynamicProxy proxy, DbConnection connection, DbTransaction transaction, params SetValue[] extraValues)
		{
			// TODO: This method is a quick and dirty way to get the right parent ID into related collections
			// It would be ideal to have the field created as part of the dynamic proxy but might not be feasible

			Type tableType = proxy.GetType().BaseType;
			string tableName = this.Configuration.GetTableName(tableType);
			string primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(tableType);

			// Insert or update all of the related items that should be saved with this item
			foreach (string propertyName in proxy.StateTracker.LoadedItems)
			{
				PropertyInfo property = tableType.GetProperty(propertyName);
				IDynamicProxy relatedItem = (IDynamicProxy)property.GetValue(proxy, null);
				if (relatedItem != null)
				{
					if (this.Configuration.ShouldCascade(property))
					{
						SaveWithOptionalExtraFields(relatedItem, connection, transaction);

						// Update the related item ID property
						string relatedItemIDPropertyName = this.Configuration.GetForeignKeyColumnName(property);
						PropertyInfo relatedItemIDProperty = proxy.GetType().GetProperty(relatedItemIDPropertyName);
						relatedItemIDProperty.SetValue(proxy, relatedItem.PrimaryKeyValue, null);
					}
					else if (relatedItem.IsNew)
					{
						string message = string.Format("The related item '{0}.{1}' must be saved before the parent '{0}'.", tableType.Name, propertyName);
						throw new InvalidOperationException(message);
					}
				}
			}

			// Insert or update the item if its fields have been changed and then clear its changed fields
			if (proxy.StateTracker.ChangedFields.Count > 0 || extraValues.Length > 0)
			{
				if (proxy.IsNew)
				{
					InsertItem(proxy, tableName, tableType, primaryKeyColumnName, connection, transaction, extraValues);
				}
				else
				{
					UpdateItem(proxy, tableName, primaryKeyColumnName, connection, transaction, extraValues);
				}
				proxy.StateTracker.ChangedFields.Clear();
			}

			// Insert, update or delete all of the related collections that should be saved with this item
			foreach (string collectionPropertyName in proxy.StateTracker.LoadedCollections)
			{
				PropertyInfo property = proxy.GetType().GetProperty(collectionPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

				// Insert items and collect the item IDs while we're at it
				var collectionIDs = new List<object>();
				foreach (IDynamicProxy cascadeItem in ((IEnumerable)property.GetValue(proxy, null)))
				{
					// We can't just set a property value here because it won't exist unless each item in the collection
					// has a property pointing back to the parent item.  Luckily the only time we will need to set the parent
					// IDs is when the item is new (as they will never change)
					if (cascadeItem.IsNew)
					{
						string parentIDPropertyName = this.Configuration.GetForeignKeyColumnName(cascadeItem.GetType().BaseType, tableType);
						SetValue setParentID = new SetValue(parentIDPropertyName, proxy.PrimaryKeyValue);
						SaveWithOptionalExtraFields(cascadeItem, connection, transaction, setParentID);
					}
					else
					{
						SaveWithOptionalExtraFields(cascadeItem, connection, transaction);
					}
					collectionIDs.Add(((IDynamicProxy)cascadeItem).PrimaryKeyValue);
				}

				// Delete items that have been removed from the collection
				object[] cascadeIDsToDelete = proxy.StateTracker.SavedCollectionIDs[property.Name].Except(collectionIDs).ToArray();
				if (cascadeIDsToDelete.Length > 0)
				{
					Type cascadeType = TypeHelper.GetElementType(property.PropertyType);
					string cascadeTableName = this.Configuration.GetTableName(cascadeType);
					string cascadePrimaryKeyName = this.Configuration.GetPrimaryKeyColumnName(cascadeType);
					var delete = Watsonia.Data.Delete.From(cascadeTableName).Where(cascadePrimaryKeyName, SqlOperator.IsIn, cascadeIDsToDelete);
					Execute(delete, connection, transaction);
				}
			}
		}

		private void InsertItem(IDynamicProxy proxy, string tableName, Type tableType, string primaryKeyColumnName, DbConnection connection, DbTransaction transaction, params SetValue[] extraValues)
		{
			var insert = Watsonia.Data.Insert.Into(tableName);
			foreach (PropertyInfo property in this.Configuration.PropertiesToLoadAndSave(proxy.GetType()))
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

			foreach (SetValue setValue in extraValues)
			{
				SetValue localSetValue = setValue;
				SetValue existingSetValue = insert.SetValues.FirstOrDefault(sv => sv.Column.Name == localSetValue.Column.Name);
				if (existingSetValue == null)
				{
					insert.SetValues.Add(localSetValue);
				}
				else
				{
					existingSetValue.Value = localSetValue.Value;
				}
			}

			Execute(insert, connection, transaction);

			// TODO: This probably isn't going to deal too well with concurrency, should there be a transaction?
			//	Or wack it on the end of the build(insert)?
			using (DbCommand getPrimaryKeyValueCommand = this.Configuration.DataAccessProvider.BuildInsertedIDCommand())
			{
				getPrimaryKeyValueCommand.Connection = connection;
				getPrimaryKeyValueCommand.Transaction = transaction;
				object primaryKeyValue = getPrimaryKeyValueCommand.ExecuteScalar();
				proxy.PrimaryKeyValue = Convert.ChangeType(primaryKeyValue, this.Configuration.GetPrimaryKeyColumnType(tableType));
				proxy.IsNew = false;
			}
		}

		private void UpdateItem(IDynamicProxy proxy, string tableName, string primaryKeyColumnName, DbConnection connection, DbTransaction transaction, params SetValue[] extraValues)
		{
			// TODO: Get rid of this, it's just to stop propertise like Database and HasChanges
			bool doUpdate = false;

			Update update = Update.Table(tableName);
			foreach (PropertyInfo property in this.Configuration.PropertiesToLoadAndSave(proxy.GetType()))
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

			foreach (SetValue setValue in extraValues)
			{
				SetValue localSetValue = setValue;
				SetValue existingSetValue = update.SetValues.FirstOrDefault(sv => sv.Column.Name == localSetValue.Column.Name);
				if (existingSetValue == null)
				{
					update.SetValues.Add(localSetValue);
				}
				else
				{
					existingSetValue.Value = localSetValue.Value;
				}
				doUpdate = true;
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
				string message = string.Format("item must be an IDynamicProxy (not {0})", item.GetType().Name);
				throw new ArgumentException(message, "item");
			}

			IDynamicProxy proxy = (IDynamicProxy)item;

			if (proxy.StateTracker.LoadedCollections.Count > 0)
			{
				foreach (PropertyInfo property in this.Configuration.PropertiesToCascade(typeof(T)))
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

			T newItem = Create(item);
			Save(newItem);

			OnAfterInsert((IDynamicProxy)newItem);

			return newItem;
		}

		/// <summary>
		/// Deletes the specified item from the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item">The item.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		/// <exception cref="System.ArgumentException">item</exception>
		public void Delete<T>(T item, DbConnection connection = null, DbTransaction transaction = null)
		{
			if ((item as IDynamicProxy) == null)
			{
				string message = string.Format("item must be an IDynamicProxy (not {0})", item.GetType().Name);
				throw new ArgumentException(message, "item");
			}

			IDynamicProxy proxy = (IDynamicProxy)item;

			OnBeforeDelete(proxy);

			Type tableType = proxy.GetType().BaseType;
			string tableName = this.Configuration.GetTableName(tableType);
			string primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(tableType);

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			DbConnection connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection();
			DbTransaction transactionToUse = transaction ?? connectionToUse.BeginTransaction();
			try
			{
				foreach (PropertyInfo property in this.Configuration.PropertiesToCascade(typeof(T)))
				{
					// TODO: This isn't going to work for cascades of cascades, I think we need to build up a database mapping
					//       Or should we just allow cascading for one level?
					Type enumeratedType = TypeHelper.GetElementType(property.PropertyType);
					string relatedTableName = this.Configuration.GetTableName(enumeratedType);
					string foreignKeyColumnName = this.Configuration.GetForeignKeyColumnName(enumeratedType, tableType);
					var relatedTableStatement = Watsonia.Data
														.Delete
														.From(relatedTableName)
														.Where(foreignKeyColumnName, SqlOperator.Equals, proxy.PrimaryKeyValue);
					Execute(relatedTableStatement, connectionToUse, transactionToUse);
				}

				var query = Watsonia.Data
										.Delete
										.From(tableName)
										.Where(primaryKeyColumnName, SqlOperator.Equals, proxy.PrimaryKeyValue);
				Execute(query, connectionToUse, transactionToUse);

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
		/// Deletes the item with the supplied ID from the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		public void Delete<T>(object id, DbConnection connection = null, DbTransaction transaction = null)
		{
			Type tableType = typeof(T);
			string tableName = this.Configuration.GetTableName(tableType);
			string primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(tableType);

			// TODO: Cascade?  And events...

			var query = Watsonia.Data
									.Delete
									.From(tableName)
									.Where(primaryKeyColumnName, SqlOperator.Equals, id);
			Execute(query, connection, transaction);
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
			DbConnection connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection();
			try
			{
				using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(statement))
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
			DbConnection connectionToUse = connection ?? this.Configuration.DataAccessProvider.OpenConnection();
			try
			{
				using (DbCommand command = this.Configuration.DataAccessProvider.BuildCommand(statement, parameters))
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

		private void LoadValues(object source, IDynamicProxy destination)
		{
			if (!destination.IsNew)
			{
				destination.StateTracker.IsLoading = true;
			}

			// TODO: Move this into the dynamic proxy
			// TODO: Pipe everything through the same area; this, linq, etc
			foreach (PropertyInfo sourceProperty in this.Configuration.PropertiesToMap(source.GetType()))
			{
				if (this.Configuration.ShouldMapType(sourceProperty.PropertyType))
				{
					string destPropertyName = this.Configuration.GetForeignKeyColumnName(sourceProperty);
					PropertyInfo destProperty = destination.GetType().GetProperty(destPropertyName);
					if ((source is IDynamicProxy) == false ||
						((IDynamicProxy)source).StateTracker.LoadedItems.Contains(sourceProperty.Name))
					{
						IDynamicProxy sourceItem = (IDynamicProxy)sourceProperty.GetValue(source, null);
						if (destProperty != null && sourceItem != null)
						{
							destProperty.SetValue(destination, sourceItem.PrimaryKeyValue, null);
						}
					}
				}
				else if (this.Configuration.ShouldLoadAndSaveProperty(sourceProperty))
				{
					PropertyInfo destProperty = destination.GetType().GetProperty(sourceProperty.Name);
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
			var ev = BeforeUpdateDatabase;
			if (ev != null)
			{
				ev(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Called at the end of UpdateDatabase.
		/// </summary>
		protected virtual void OnAfterUpdateDatabase()
		{
			var ev = AfterUpdateDatabase;
			if (ev != null)
			{
				ev(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Called at the end of Create.
		/// </summary>
		/// <param name="proxy">The proxy object that was created.</param>
		protected virtual void OnAfterCreate(IDynamicProxy proxy)
		{
			var ev = AfterCreate;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the end of Load.
		/// </summary>
		/// <param name="proxy">The proxy object that was created.</param>
		protected virtual void OnAfterLoad(IDynamicProxy proxy)
		{
			var ev = AfterLoad;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the start of Refresh.
		/// </summary>
		/// <param name="proxy">The proxy object that will be refreshed.</param>
		protected virtual void OnBeforeRefresh(IDynamicProxy proxy)
		{
			var ev = BeforeRefresh;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the end of Refresh.
		/// </summary>
		/// <param name="proxy">The proxy object that was refreshed.</param>
		protected virtual void OnAfterRefresh(IDynamicProxy proxy)
		{
			var ev = AfterRefresh;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the start of LoadCollection.
		/// </summary>
		/// <param name="statement">The statement that will be used to load the collection.</param>
		protected virtual void OnBeforeLoadCollection(Select statement)
		{
			var ev = BeforeLoadCollection;
			if (ev != null)
			{
				ev(this, new EventArgs<Select>(statement));
			}
		}

		/// <summary>
		/// Called at the end of LoadCollection.
		/// </summary>
		/// <param name="collection">The collection that was loaded.</param>
		protected virtual void OnAfterLoadCollection(IEnumerable collection)
		{
			var ev = AfterLoadCollection;
			if (ev != null)
			{
				ev(this, new EventArgs<IEnumerable>(collection));
			}
		}

		/// <summary>
		/// Called at the start of LoadValue.
		/// </summary>
		/// <param name="statement">The statement that will be used to load the value.</param>
		protected virtual void OnBeforeLoadValue(Select statement)
		{
			var ev = BeforeLoadValue;
			if (ev != null)
			{
				ev(this, new EventArgs<Select>(statement));
			}
		}

		/// <summary>
		/// Called at the end of LoadValue.
		/// </summary>
		/// <param name="value">The value that was loaded.</param>
		protected virtual void OnAfterLoadValue(object value)
		{
			var ev = AfterLoadValue;
			if (ev != null)
			{
				ev(this, new EventArgs<object>(value));
			}
		}

		/// <summary>
		/// Called at the start of Save.
		/// </summary>
		/// <param name="proxy">The proxy object that will be saved.</param>
		protected virtual void OnBeforeSave(IDynamicProxy proxy)
		{
			var ev = BeforeSave;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the end of Save.
		/// </summary>
		/// <param name="proxy">The proxy object that was saved.</param>
		protected virtual void OnAfterSave(IDynamicProxy proxy)
		{
			var ev = AfterSave;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the start of Insert.
		/// </summary>
		/// <param name="item">The item that will be inserted.</param>
		protected virtual void OnBeforeInsert(object item)
		{
			var ev = BeforeInsert;
			if (ev != null)
			{
				ev(this, new EventArgs<object>(item));
			}
		}

		/// <summary>
		/// Called at the end of Insert.
		/// </summary>
		/// <param name="proxy">The proxy object that was inserted.</param>
		protected virtual void OnAfterInsert(IDynamicProxy proxy)
		{
			var ev = AfterInsert;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the start of Delete.
		/// </summary>
		/// <param name="proxy">The proxy object that will be deleted.</param>
		protected virtual void OnBeforeDelete(IDynamicProxy proxy)
		{
			var ev = BeforeDelete;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the end of Delete.
		/// </summary>
		/// <param name="proxy">The proxy object that was deleted.</param>
		protected virtual void OnAfterDelete(IDynamicProxy proxy)
		{
			var ev = AfterDelete;
			if (ev != null)
			{
				ev(this, new EventArgs<IDynamicProxy>(proxy));
			}
		}

		/// <summary>
		/// Called at the start of Execute.
		/// </summary>
		/// <param name="statement">The statement that will be executed.</param>
		protected virtual void OnBeforeExecute(Statement statement)
		{
			var ev = BeforeExecute;
			if (ev != null)
			{
				ev(this, new EventArgs<Statement>(statement));
			}
		}

		/// <summary>
		/// Called at the end of Execute.
		/// </summary>
		/// <param name="statement">The statement that was executed.</param>
		protected virtual void OnAfterExecute(Statement statement)
		{
			var ev = AfterExecute;
			if (ev != null)
			{
				ev(this, new EventArgs<Statement>(statement));
			}
		}

		/// <summary>
		/// Called before a command is executed.
		/// </summary>
		/// <param name="command">The command that will be executed.</param>
		protected virtual void OnBeforeExecuteCommand(DbCommand command)
		{
			var ev = BeforeExecuteCommand;
			if (ev != null)
			{
				ev(this, new EventArgs<DbCommand>(command));
			}
		}

		/// <summary>
		/// Called at the end of ExecuteSQL.
		/// </summary>
		/// <param name="statement">The statement that was executed.</param>
		protected virtual void OnAfterExecuteCommand(DbCommand command)
		{
			var ev = AfterExecuteCommand;
			if (ev != null)
			{
				ev(this, new EventArgs<DbCommand>(command));
			}
		}

		protected string GetSqlStringFromCommand(DbCommand command)
		{
			StringBuilder b = new StringBuilder();
			b.Append(command.CommandText.Replace(Environment.NewLine, " "));
			if (command.Parameters.Count > 0)
			{
				b.Append(" { ");
				for (int i = 0; i < command.Parameters.Count; i++)
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
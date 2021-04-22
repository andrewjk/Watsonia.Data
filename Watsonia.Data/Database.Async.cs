using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	/// <summary>
	/// Provides functionality for accessing and updating a database.
	/// </summary>
	public partial class Database
	{
		/// <summary>
		/// Opens a connection to the database.
		/// </summary>
		/// <returns></returns>
		public async Task<DbConnection> OpenConnectionAsync()
		{
			return await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration);
		}

		/// <summary>
		/// Loads the item from the database with the supplied ID.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <returns></returns>
		public async Task<T> LoadAsync<T>(object id) where T : class
		{
			return await LoadOrDefaultAsync<T>(id, true);
		}

		/// <summary>
		/// Loads the item from the database with the supplied ID.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <returns></returns>
		public async Task<T> LoadOrDefaultAsync<T>(object id) where T : class
		{
			return await LoadOrDefaultAsync<T>(id, false);
		}

		/// <summary>
		/// Loads the item from the database with the supplied ID.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The ID.</param>
		/// <returns></returns>
		private async Task<T> LoadOrDefaultAsync<T>(object id, bool throwIfNotFound) where T : class
		{
			IDynamicProxy proxy = null;

			var tableName = this.Configuration.GetTableName(typeof(T));
			var primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(typeof(T));

			// First, check the cache
			if (!TryGetCacheAndProxy<T>(id, out var cache, out var item))
			{
				// It's not in the cache, going to have to load it from the database
				var select = Select.From(tableName).Where(primaryKeyColumnName, SqlOperator.Equals, id);

				using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
				using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
				{
					command.Connection = connection;
					OnBeforeExecuteCommand(command);
					using (var reader = await command.ExecuteReaderAsync())
					{
						var fieldNames = GetReaderFieldNames(reader);
						if (await reader.ReadAsync())
						{
							item = Create<T>();
							proxy = (IDynamicProxy)item;
							proxy.__SetValuesFromReader(reader, fieldNames);
							proxy.StateTracker.IsNew = false;
							proxy.StateTracker.HasChanges = false;

							// Add or update it in the cache
							AddOrUpdateCache(id, cache, proxy);
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
		public async Task RefreshAsync<T>(T item) where T : class
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, nameof(item));
			}

			var proxy = (IDynamicProxy)item;

			OnBeforeRefresh(proxy);

			var dbItem = await LoadAsync<T>(proxy.__PrimaryKeyValue);
			LoadValues(dbItem, proxy);

			// Refresh any loaded children
			foreach (var collectionPropertyName in proxy.StateTracker.LoadedCollections)
			{
				var property = proxy.GetType().GetProperty(collectionPropertyName,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

				var elementType = TypeHelper.GetElementType(property.PropertyType);
				var tableName = this.Configuration.GetTableName(elementType);
				var foreignKeyColumnName = this.Configuration.GetForeignKeyColumnName(elementType, typeof(T));
				var select = Select.From(tableName).Where(foreignKeyColumnName, SqlOperator.Equals, proxy.__PrimaryKeyValue);

				// We know that this is an IList because we created it as an List in the DynamicProxyFactory
				await RefreshCollectionAsync(select, elementType, (IList)proxy.__GetValue(property.Name));
			}

			OnAfterRefresh(proxy);
		}

		/// <summary>
		/// Loads a collection of items from the database using the supplied query.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="select">The select statement.</param>
		/// <returns></returns>
		public async Task<IList<T>> LoadCollectionAsync<T>(SelectStatement select)
		{
			OnBeforeLoadCollection(select);

			var result = new List<T>();

			var itemType = GetCollectionItemType(typeof(T));

			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = await command.ExecuteReaderAsync())
				{
					var fieldNames = GetReaderFieldNames(reader);
					while (await reader.ReadAsync())
					{
						var newItem = LoadItemInCollection<T>(reader, itemType, fieldNames);
						result.Add(newItem);
					}
				}
				OnAfterExecuteCommand(command);

				if (result.Count > 0 && select.IncludePaths.Count > 0)
				{
					// HACK: Include paths are loaded synchronously for now, because I don't want to duplicate all of that code
					LoadIncludePaths(select, result);
				}
			}

			OnAfterLoadCollection(result);

			return result;
		}

		/// <summary>
		/// Loads a collection of items from the database using the supplied select statement.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="select">The select statement.</param>
		/// <returns></returns>
		public async Task<IList<T>> LoadCollectionAsync<T>(SelectStatement<T> select)
		{
			var select2 = (SelectStatement)select.CreateStatement(new QueryMapper(this.Configuration));
			return await LoadCollectionAsync<T>(select2);
		}

		/// <summary>
		/// Loads a collection of items from the database using the supplied query.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query">The query as a string with parameter value placeholders signified by @0, @1 etc.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public async Task<IList<T>> LoadCollectionAsync<T>(string query, params object[] parameters)
		{
			OnBeforeLoadCollection(query);

			var result = new List<T>();

			var itemType = GetCollectionItemType(typeof(T));

			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(query, this.Configuration, parameters))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = await command.ExecuteReaderAsync())
				{
					var fieldNames = GetReaderFieldNames(reader);
					while (await reader.ReadAsync())
					{
						var newItem = LoadItemInCollection<T>(reader, itemType, fieldNames);
						result.Add(newItem);
					}
				}
				OnAfterExecuteCommand(command);
			}

			OnAfterLoadCollection(result);

			return result;
		}

		private async Task<IList<IDynamicProxy>> LoadCollectionAsync(SelectStatement select, Type itemType)
		{
			OnBeforeLoadCollection(select);

			var result = new List<IDynamicProxy>();

			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = await command.ExecuteReaderAsync())
				{
					var fieldNames = GetReaderFieldNames(reader);
					while (await reader.ReadAsync())
					{
						var proxyConstructor = DynamicProxyFactory.GetDynamicProxyConstructor(itemType, this);
						var proxy = proxyConstructor.Create();
						proxy.StateTracker.Database = this;
						proxy.__SetValuesFromReader(reader, fieldNames);
						result.Add(proxy);
					}
				}
				OnAfterExecuteCommand(command);

				if (result.Count > 0 && select.IncludePaths.Count > 0)
				{
					// HACK: Include paths are loaded synchronously for now, because I don't want to duplicate all of that code
					LoadIncludePaths(select, result);
				}
			}

			OnAfterLoadCollection(result);

			return result;
		}

		private async Task RefreshCollectionAsync(SelectStatement select, Type elementType, IList collection)
		{
			var newCollection = new List<IDynamicProxy>();

			// Load items from the database
			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = await command.ExecuteReaderAsync())
				{
					var fieldNames = GetReaderFieldNames(reader);
					while (await reader.ReadAsync())
					{
						var proxy = DynamicProxyFactory.GetDynamicProxy(elementType, this);
						proxy.__SetValuesFromReader(reader, fieldNames);
						newCollection.Add(proxy);
					}
				}
				OnAfterExecuteCommand(command);
			}

			// Refresh the items in the collection from the database collection
			RefreshCollectionItems(collection, newCollection);
		}

		/// <summary>
		/// Loads the first returned value from the database using the supplied query.
		/// </summary>
		/// <param name="select">The select statement.</param>
		/// <returns></returns>
		public async Task<object> LoadValueAsync(SelectStatement select)
		{
			OnBeforeLoadValue(select);

			object value;

			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				value = await command.ExecuteScalarAsync();
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
		public async Task<object> LoadValueAsync<T>(SelectStatement<T> select)
		{
			var select2 = (SelectStatement)select.CreateStatement(new QueryMapper(this.Configuration));
			return await LoadValueAsync(select2);
		}

		/// <summary>
		/// Loads the first returned value from the database using the supplied query.
		/// </summary>
		/// <param name="query">The query as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public async Task<object> LoadValueAsync(string query, params object[] parameters)
		{
			object value;

			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(query, this.Configuration, parameters))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				value = await command.ExecuteScalarAsync();
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
		public async Task SaveAsync<T>(T item, DbConnection connection = null, DbTransaction transaction = null)
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, nameof(item));
			}

			var proxy = (IDynamicProxy)item;

			OnBeforeSave(proxy);

			if (!proxy.StateTracker.IsValid)
			{
				var ex = new ValidationException(
					$"Validation failed for {item.GetType().BaseType.Name}: {item}");
				ex.ValidationErrors.AddRange(proxy.StateTracker.ValidationErrors);
				throw ex;
			}

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration);
			var transactionToUse = transaction ?? connectionToUse.BeginTransaction();
			try
			{
				var parents = new List<string>();
				await SaveItemAsync(proxy, parents, connectionToUse, transactionToUse);

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

		private async Task SaveItemAsync(IDynamicProxy proxy, List<string> parents, DbConnection connection, DbTransaction transaction)
		{
			var tableType = proxy.GetType().BaseType;
			var tableName = this.Configuration.GetTableName(tableType);
			var primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(tableType);

			// Don't save this item if it's already slated to be saved as part of a parent
			var parentKey = tableName + "$" + proxy.__PrimaryKeyValue;
			if (parents.Contains(parentKey))
			{
				return;
			}
			parents.Add(parentKey);

			// Insert or update all of the related items that should be saved with this item
			var newRelatedItems = new List<IDynamicProxy>();
			foreach (var propertyName in proxy.StateTracker.LoadedItems)
			{
				var property = tableType.GetProperty(propertyName);
				var relatedItem = (IDynamicProxy)proxy.__GetValue(propertyName);
				if (relatedItem != null)
				{
					if (this.Configuration.ShouldCascadeInternal(property))
					{
						await SaveItemAsync(relatedItem, parents, connection, transaction);

						// Update the related item ID property
						var relatedItemIDPropertyName = this.Configuration.GetForeignKeyColumnName(property);
						proxy.__SetValue(relatedItemIDPropertyName, relatedItem.__PrimaryKeyValue);

						// Add the related item to a list so that we can check whether it needs to be
						// re-saved as part of a related collection
						newRelatedItems.Add(relatedItem);
					}
					else if (relatedItem.StateTracker.IsNew)
					{
						var message = $"The related item '{tableType.Name}.{propertyName}' must be saved before the parent '{tableType.Name}'.";
						throw new InvalidOperationException(message);
					}
				}
			}

			if (proxy.StateTracker.IsNew)
			{
				// Insert the item
				await InsertItemAsync(proxy, tableName, tableType, primaryKeyColumnName, connection, transaction);

				// Update the parent key with the new primary key value
				parents.Remove(parentKey);
				parentKey = tableName + "$" + proxy.__PrimaryKeyValue;
				parents.Add(parentKey);
			}
			else
			{
				// Only update the item if its fields have been changed
				if (proxy.StateTracker.HasChanges)
				{
					await UpdateItemAsync(proxy, tableName, primaryKeyColumnName, connection, transaction);
				}
			}

			// Add or update it in the cache
			if (this.Configuration.ShouldCacheType(tableType))
			{
				var cacheKey = DynamicProxyFactory.GetDynamicTypeName(tableType, this);
				var cache = this.Cache.GetOrAdd(
					cacheKey,
					(string s) => new ItemCache(
						this.Configuration.GetCacheExpiryLength(tableType),
						this.Configuration.GetCacheMaxItems(tableType)));
				cache.AddOrUpdate(
					proxy.__PrimaryKeyValue,
					proxy.__GetBagFromValues(),
					(key, existingValue) => proxy.__GetBagFromValues());
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
				foreach (IDynamicProxy childItem in (IEnumerable)proxy.__GetValue(collectionPropertyName))
				{
					if (this.Configuration.ShouldCascade(property))
					{
						if (childItem.StateTracker.IsNew || newRelatedItems.Contains(childItem))
						{
							// Set the parent ID of the item in the collection
							var parentIDPropertyName = this.Configuration.GetForeignKeyColumnName(childItem.GetType().BaseType, tableType);
							childItem.__SetValue(parentIDPropertyName, proxy.__PrimaryKeyValue);
						}
						await SaveItemAsync(childItem, parents, connection, transaction);
					}
					collectionIDs.Add(((IDynamicProxy)childItem).__PrimaryKeyValue);
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
							foreach (var deleteItem in await LoadCollectionAsync(select, deleteType))
							{
								await DeleteAsync(deleteItem, deleteType, connection, transaction);
							}
						}
					}
				}
			}

			proxy.__SetOriginalValues();
			proxy.StateTracker.HasChanges = false;
		}

		private async Task InsertItemAsync(IDynamicProxy proxy, string tableName, Type tableType, string primaryKeyColumnName, DbConnection connection, DbTransaction transaction)
		{
			var insert = QueryBuilder.Insert.Into(tableName);
			foreach (var property in this.Configuration.PropertiesToLoadAndSave(proxy.GetType()))
			{
				if (property.Name != primaryKeyColumnName)
				{
					if (property.PropertyType == typeof(string))
					{
						insert.Value(property.Name, proxy.__GetValue(property.Name) ?? "");
					}
					else
					{
						insert.Value(property.Name, proxy.__GetValue(property.Name));
					}
				}
			}

			await ExecuteAsync(insert, connection, transaction);

			// TODO: This probably isn't going to deal too well with concurrency, should there be a transaction?
			//	Or wack it on the end of the build(insert)?
			using (var getPrimaryKeyValueCommand = this.Configuration.DataAccessProvider.BuildInsertedIDCommand(this.Configuration))
			{
				getPrimaryKeyValueCommand.Connection = connection;
				getPrimaryKeyValueCommand.Transaction = transaction;
				var primaryKeyValue = await getPrimaryKeyValueCommand.ExecuteScalarAsync();
				proxy.__PrimaryKeyValue = Convert.ChangeType(primaryKeyValue, this.Configuration.GetPrimaryKeyColumnType(tableType));
				proxy.StateTracker.IsNew = false;
			}
		}

		private async Task UpdateItemAsync(IDynamicProxy proxy, string tableName, string primaryKeyColumnName, DbConnection connection, DbTransaction transaction)
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
						update.Set(property.Name, proxy.__GetValue(property.Name) ?? "");
					}
					else
					{
						update.Set(property.Name, proxy.__GetValue(property.Name));
					}
					doUpdate = true;
				}
			}

			if (!doUpdate)
			{
				return;
			}

			update = update.Where(primaryKeyColumnName, SqlOperator.Equals, proxy.__PrimaryKeyValue);

			await ExecuteAsync(update, connection, transaction);
		}

		/// <summary>
		/// Inserts the specified item and returns a proxy object.
		/// </summary>
		/// <typeparam name="T">The type of item to insert and create a proxy for.</typeparam>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		public async Task<T> InsertAsync<T>(T item) where T : class
		{
			OnBeforeInsert(item);

			var newItem = Create(item);
			await SaveAsync(newItem);

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
		public async Task DeleteAsync<T>(object id, DbConnection connection = null, DbTransaction transaction = null) where T : class
		{
			var item = await LoadAsync<T>(id);
			await DeleteAsync(item, connection, transaction);
		}

		/// <summary>
		/// Deletes the specified item from the database.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item">The item.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		/// <exception cref="ArgumentException">item</exception>
		public async Task DeleteAsync<T>(T item, DbConnection connection = null, DbTransaction transaction = null)
		{
			await DeleteAsync(item, typeof(T), connection, transaction);
		}

		private async Task DeleteAsync(object item, Type itemType, DbConnection connection = null, DbTransaction transaction = null)
		{
			if ((item as IDynamicProxy) == null)
			{
				var message = $"item must be an IDynamicProxy (not {item.GetType().Name})";
				throw new ArgumentException(message, nameof(item));
			}

			var proxy = (IDynamicProxy)item;

			OnBeforeDelete(proxy);

			var tableType = proxy.GetType().BaseType;
			var tableName = this.Configuration.GetTableName(tableType);
			var primaryKeyName = this.Configuration.GetPrimaryKeyColumnName(tableType);

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration);
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
						var deletePrimaryKeyValue = proxy.__GetValue(deleteKeyPropertyName);
						if (deletePrimaryKeyValue != null)
						{
							var deletePrimaryKeyName = this.Configuration.GetPrimaryKeyColumnName(deleteType);
							var select = Select.From(deleteTableName).Where(deletePrimaryKeyName, SqlOperator.Equals, deletePrimaryKeyValue);
							itemsToDelete = await LoadCollectionAsync(select, deleteType);

							// Update the field to null to avoid a reference exception when deleting the item
							var columnName = this.Configuration.GetColumnName(property);
							var updateQuery = Update.Table(tableName).Set(columnName, null).Where(primaryKeyName, SqlOperator.Equals, proxy.__PrimaryKeyValue);
							await ExecuteAsync(updateQuery, connection, transaction);
						}
					}
					else if (this.Configuration.IsRelatedCollection(property))
					{
						var deleteForeignKeyName = this.Configuration.GetForeignKeyColumnName(deleteType, tableType);
						var select = Select.From(deleteTableName).Where(deleteForeignKeyName, SqlOperator.Equals, proxy.__PrimaryKeyValue);
						itemsToDelete = await LoadCollectionAsync(select, deleteType);
					}

					foreach (var deleteItem in itemsToDelete)
					{
						await DeleteAsync(deleteItem, deleteType, connection, transaction);
					}
				}

				var deleteQuery = QueryBuilder.Delete.From(tableName).Where(primaryKeyName, SqlOperator.Equals, proxy.__PrimaryKeyValue);
				await ExecuteAsync(deleteQuery, connectionToUse, transactionToUse);

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
		public async Task ExecuteAsync(Statement statement, DbConnection connection = null, DbTransaction transaction = null)
		{
			OnBeforeExecute(statement);

			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration);
			try
			{
				using (var command = this.Configuration.DataAccessProvider.BuildCommand(statement, this.Configuration))
				{
					command.Connection = connectionToUse;
					command.Transaction = transaction;
					OnBeforeExecuteCommand(command);
					await command.ExecuteNonQueryAsync();
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
		public async Task ExecuteAsync(string statement, params object[] parameters)
		{
			await ExecuteAsync(statement, connection: null, transaction: null, parameters: parameters);
		}

		/// <summary>
		/// Executes the specified query against the database.
		/// </summary>
		/// <param name="query">The statement as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="parameters">The parameters.</param>
		public async Task ExecuteAsync(string statement, DbConnection connection, params object[] parameters)
		{
			await ExecuteAsync(statement, connection: connection, transaction: null, parameters: parameters);
		}

		/// <summary>
		/// Executes the specified query against the database.
		/// </summary>
		/// <param name="query">The query as a composite format string with parameter value placeholders signified by {0}, {1} etc.</param>
		/// <param name="connection">The connection.</param>
		/// <param name="transaction">The transaction.</param>
		/// <param name="parameters">The parameters.</param>
		public async Task ExecuteAsync(string statement, DbConnection connection, DbTransaction transaction, params object[] parameters)
		{
			// Create a connection if one wasn't passed in
			// Store it in a variable so that we know whether to dispose or leave it for the calling function
			var connectionToUse = connection ?? await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration);
			try
			{
				using (var command = this.Configuration.DataAccessProvider.BuildCommand(statement, this.Configuration, parameters))
				{
					command.Connection = connectionToUse;
					command.Transaction = transaction;
					OnBeforeExecuteCommand(command);
					await command.ExecuteNonQueryAsync();
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
		public async Task<object> ExecuteProcedureAsync(string procedureName, params Parameter[] parameters)
		{
			object value;

			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildProcedureCommand(procedureName, parameters))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				value = await command.ExecuteScalarAsync();
				OnAfterExecuteCommand(command);
			}

			return value;
		}

		// NOTE: EnumerateCollectionAsync doesn't have a sync equivalent as it depends on the
		// IAsyncEnumerable interface
#if NET5_0
		/// <summary>
		/// Loads a collection of items from the database using the supplied query.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query">The query as a string with parameter value placeholders signified by @0, @1 etc.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		public async IAsyncEnumerable<T> EnumerateCollectionAsync<T>(SelectStatement select)
		{
			OnBeforeLoadCollection(select);

			var result = new List<T>();

			var itemType = GetCollectionItemType(typeof(T));

			using (var connection = await this.Configuration.DataAccessProvider.OpenConnectionAsync(this.Configuration))
			using (var command = this.Configuration.DataAccessProvider.BuildCommand(select, this.Configuration))
			{
				command.Connection = connection;
				OnBeforeExecuteCommand(command);
				using (var reader = await command.ExecuteReaderAsync())
				{
					var fieldNames = GetReaderFieldNames(reader);
					while (await reader.ReadAsync())
					{
						var newItem = LoadItemInCollection<T>(reader, itemType, fieldNames);
						result.Add(newItem);
						yield return newItem;
					}
				}
				OnAfterExecuteCommand(command);
			}

			OnAfterLoadCollection(result);
		}
#endif
	}
}

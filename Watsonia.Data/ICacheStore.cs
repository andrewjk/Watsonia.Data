using System;

namespace Watsonia.Data
{
	/// <summary>
	/// A store of item caches, one per type being cached.
	/// </summary>
	/// <remarks>
	/// Implement this interface (e.g. over IMemoryCache) to replace the default in-process cache.
	/// </remarks>
	public interface ICacheStore
	{
		/// <summary>
		/// Gets the cache for the supplied key, creating it if it doesn't exist.
		/// </summary>
		/// <param name="cacheKey">The key identifying the cache (usually the entity type).</param>
		/// <param name="expiryLength">The length of time in milliseconds to keep items in the cache.</param>
		/// <param name="maxItems">The maximum number of items to keep in the cache.</param>
		/// <returns>The item cache.</returns>
		IItemCache GetOrAdd(string cacheKey, int expiryLength, int maxItems);

		/// <summary>
		/// Determines whether the cache for the supplied key contains an item with the supplied primary key.
		/// </summary>
		/// <param name="cacheKey">The key identifying the cache.</param>
		/// <param name="primaryKey">The primary key of the item.</param>
		/// <returns></returns>
		bool ContainsItemWithKey(string cacheKey, object primaryKey);

		/// <summary>
		/// Removes the item with the supplied primary key from the cache for the supplied key.
		/// </summary>
		/// <param name="cacheKey">The key identifying the cache.</param>
		/// <param name="primaryKey">The primary key of the item.</param>
		void RemoveItemByKey(string cacheKey, object primaryKey);

		/// <summary>
		/// Clears all items from all caches.
		/// </summary>
		void Clear();
	}

	/// <summary>
	/// A cache of items of a single entity type, keyed by primary key.
	/// </summary>
	public interface IItemCache
	{
		/// <summary>
		/// Determines whether the cache contains the supplied primary key.
		/// </summary>
		/// <param name="primaryKey">The primary key of the item.</param>
		/// <returns></returns>
		bool ContainsKey(object primaryKey);

		/// <summary>
		/// Gets the cached values for the supplied primary key.
		/// </summary>
		/// <param name="primaryKey">The primary key of the item.</param>
		/// <returns>The cached values.</returns>
		IValueBag GetValues(object primaryKey);

		/// <summary>
		/// Adds or updates the cached values for the supplied primary key.
		/// </summary>
		/// <param name="primaryKey">The primary key of the item.</param>
		/// <param name="addValue">The values to add.</param>
		/// <param name="updateValueFactory">Creates the new values from the key and the existing values.</param>
		void AddOrUpdate(object primaryKey, IValueBag addValue, Func<object, IValueBag, IValueBag> updateValueFactory);

		/// <summary>
		/// Removes the item with the supplied primary key.
		/// </summary>
		/// <param name="primaryKey">The primary key of the item.</param>
		void Remove(object primaryKey);
	}
}

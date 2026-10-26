using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Extensions;
using Watsonia.Data.Tests.Database;
using Watsonia.Data.Tests.Database.Entities;

namespace Watsonia.Data.Tests.Cache
{
	[TestClass]
	public class CacheStoreContractTests
	{
		private static readonly EntitiesDatabase _db = new EntitiesDatabase();

		[TestMethod]
		public void LoadPopulatesTheCacheAndSaveRefreshesIt()
		{
			var store = new RecordingCacheStore();
			var database = new EntitiesDatabase { Cache = store };
			var item = database.Insert(new HasChanges { String = "CACHE" });
			var id = ((IDynamicProxy)item).__PrimaryKeyValue;
			var deleted = false;

			try
			{
				database.Load<HasChanges>(id);
				Assert.IsTrue(store.GetOrAddCount > 0);
				Assert.IsTrue(store.LastItemCache.AddOrUpdateCount > 0);

				var loaded = database.Load<HasChanges>(id);
				Assert.AreEqual("CACHE", loaded.String);

				loaded.String = "CACHE2";
				database.Save(loaded);

				// The save must refresh the cached values so later loads see the new value
				var reloaded = database.Load<HasChanges>(id);
				Assert.AreEqual("CACHE2", reloaded.String);
				Assert.IsTrue(store.LastItemCache.AddOrUpdateCount >= 2);

				database.Delete(reloaded);
				deleted = true;
				Assert.IsTrue(store.RemoveItemByKeyCount > 0);
			}
			finally
			{
				if (!deleted)
				{
					database.Delete<HasChanges>(id);
				}
			}
		}

		[TestMethod]
		public void MemoryCacheStoreExpiresAndClearsEntries()
		{
			using var memoryCache = new MemoryCache(new MemoryCacheOptions());
			var store = new MemoryCacheStore(memoryCache);
			var itemCache = store.GetOrAdd("ExpiryKey", 50, 10);
			var bag = new FakeValueBag();

			itemCache.AddOrUpdate(1L, bag, null);
			Assert.IsTrue(itemCache.ContainsKey(1L));
			Assert.IsTrue(store.ContainsItemWithKey("ExpiryKey", 1L));

			System.Threading.Thread.Sleep(150);
			Assert.IsFalse(itemCache.ContainsKey(1L));

			itemCache.AddOrUpdate(2L, bag, null);
			Assert.IsTrue(itemCache.ContainsKey(2L));
			store.Clear();
			Assert.IsFalse(itemCache.ContainsKey(2L));
		}

		private sealed class FakeValueBag : IValueBag
		{
		}

		private sealed class RecordingCacheStore : ICacheStore
		{
			private readonly ConcurrentDictionary<string, RecordingItemCache> _caches =
				new ConcurrentDictionary<string, RecordingItemCache>();

			public int GetOrAddCount { get; private set; }

			public int RemoveItemByKeyCount { get; private set; }

			public RecordingItemCache LastItemCache { get; private set; }

			public IItemCache GetOrAdd(string cacheKey, int expiryLength, int maxItems)
			{
				GetOrAddCount++;
				var cache = _caches.GetOrAdd(cacheKey, k => new RecordingItemCache());
				LastItemCache = cache;
				return cache;
			}

			public bool ContainsItemWithKey(string cacheKey, object primaryKey)
			{
				return _caches.TryGetValue(cacheKey, out var cache) && cache.ContainsKey(primaryKey);
			}

			public void RemoveItemByKey(string cacheKey, object primaryKey)
			{
				RemoveItemByKeyCount++;
				if (_caches.TryGetValue(cacheKey, out var cache))
				{
					cache.Remove(primaryKey);
				}
			}

			public void Clear()
			{
				_caches.Clear();
			}
		}

		private sealed class RecordingItemCache : IItemCache
		{
			private readonly Dictionary<object, IValueBag> _values = new Dictionary<object, IValueBag>();

			public int AddOrUpdateCount { get; private set; }

			public bool ContainsKey(object primaryKey)
			{
				return _values.ContainsKey(primaryKey);
			}

			public IValueBag GetValues(object primaryKey)
			{
				return _values.TryGetValue(primaryKey, out var value) ? value : null;
			}

			public void AddOrUpdate(object primaryKey, IValueBag addValue, Func<object, IValueBag, IValueBag> updateValueFactory)
			{
				AddOrUpdateCount++;
				_values[primaryKey] = addValue;
			}

			public void Remove(object primaryKey)
			{
				_values.Remove(primaryKey);
			}
		}
	}
}

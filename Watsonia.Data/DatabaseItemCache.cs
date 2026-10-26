using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// A cache of items in the database, keyed by table name.
	/// </summary>
	public class DatabaseItemCache : ICacheStore
	{
		private ConcurrentDictionary<string, ItemCache> Cache { get; set; } = new ConcurrentDictionary<string, ItemCache>();

		internal DatabaseItemCache()
		{
		}

		public IItemCache GetOrAdd(string cacheKey, int expiryLength, int maxItems)
		{
			return this.Cache.GetOrAdd(cacheKey, (string s) => new ItemCache(expiryLength, maxItems));
		}

		public bool ContainsItemWithKey(string cacheKey, object primaryKey)
		{
			return this.Cache.ContainsKey(cacheKey) && this.Cache[cacheKey].ContainsKey(primaryKey);
		}

		public void RemoveItemByKey(string cacheKey, object primaryKey)
		{
			if (this.Cache.ContainsKey(cacheKey))
			{
				this.Cache[cacheKey].Remove(primaryKey);
			}
		}

		public void Clear()
		{
			this.Cache.Clear();
		}
	}
}

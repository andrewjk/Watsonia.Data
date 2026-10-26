using System;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace Watsonia.Data.Extensions
{
	/// <summary>
	/// An <see cref="ICacheStore"/> backed by <see cref="IMemoryCache"/>.
	/// </summary>
	/// <remarks>
	/// The "maximum items" setting can't be enforced per entity type through IMemoryCache; use the
	/// cache's global SizeLimit to bound total memory.
	/// </remarks>
	public sealed class MemoryCacheStore : ICacheStore
	{
		private const char KeySeparator = '\u001f';

		private readonly IMemoryCache _cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryCacheStore"/> class.
		/// </summary>
		/// <param name="cache">The memory cache.</param>
		public MemoryCacheStore(IMemoryCache cache)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		/// <inheritdoc />
		public IItemCache GetOrAdd(string cacheKey, int expiryLength, int maxItems)
		{
			return new MemoryCacheItemCache(_cache, cacheKey, expiryLength);
		}

		/// <inheritdoc />
		public bool ContainsItemWithKey(string cacheKey, object primaryKey)
		{
			return _cache.TryGetValue(GetKey(cacheKey, primaryKey), out _);
		}

		/// <inheritdoc />
		public void RemoveItemByKey(string cacheKey, object primaryKey)
		{
			_cache.Remove(GetKey(cacheKey, primaryKey));
		}

		/// <inheritdoc />
		public void Clear()
		{
			// IMemoryCache has no clear operation; clearing is controlled by the cache's owner
			if (_cache is MemoryCache memoryCache)
			{
				memoryCache.Clear();
			}
		}

		internal static string GetKey(string cacheKey, object primaryKey)
		{
			var key = primaryKey is IFormattable formattable
				? formattable.ToString(null, CultureInfo.InvariantCulture)
				: Convert.ToString(primaryKey, CultureInfo.InvariantCulture);
			return cacheKey + KeySeparator + key;
		}

		private sealed class MemoryCacheItemCache : IItemCache
		{
			private readonly IMemoryCache _cache;
			private readonly string _cacheKey;
			private readonly int _expiryLength;

			public MemoryCacheItemCache(IMemoryCache cache, string cacheKey, int expiryLength)
			{
				_cache = cache;
				_cacheKey = cacheKey;
				_expiryLength = expiryLength;
			}

			public bool ContainsKey(object primaryKey)
			{
				return _cache.TryGetValue(GetKey(_cacheKey, primaryKey), out _);
			}

			public IValueBag GetValues(object primaryKey)
			{
				return _cache.TryGetValue(GetKey(_cacheKey, primaryKey), out var value) ? (IValueBag)value : null;
			}

			public void AddOrUpdate(object primaryKey, IValueBag addValue, Func<object, IValueBag, IValueBag> updateValueFactory)
			{
				var key = GetKey(_cacheKey, primaryKey);
				var values = addValue;
				if (updateValueFactory != null && _cache.TryGetValue(key, out var existing))
				{
					values = updateValueFactory(primaryKey, (IValueBag)existing);
				}

				_cache.Set(key, values, new MemoryCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(_expiryLength),
					Size = 1
				});
			}

			public void Remove(object primaryKey)
			{
				_cache.Remove(GetKey(_cacheKey, primaryKey));
			}
		}
	}
}

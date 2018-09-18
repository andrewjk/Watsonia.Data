using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data
{
	/// <summary>
	/// A cache of items in a table, keyed by ID.
	/// </summary>
	public class ItemCache
	{
		private object _lock = new object();

		/// <summary>
		/// Gets or sets the expiry length in milliseconds.
		/// </summary>
		/// <value>
		/// The expiry length in milliseconds.
		/// </value>
		public long ExpiryLength { get; set; }

		/// <summary>
		/// Gets or sets the maximum number of items to store in the cache.
		/// </summary>
		/// <value>
		/// The limit.
		/// </value>
		public int MaxItems { get; set; }

		/// <summary>
		/// Gets the items in the cache for the table.
		/// </summary>
		/// <value>
		/// The items.
		/// </value>
		private ConcurrentDictionary<object, IValueBag> Items { get; } = new ConcurrentDictionary<object, IValueBag>();

		// These properties are internal for testing
		internal int ItemCount
		{
			get
			{
				return this.Items.Count;
			}
		}

		internal List<Tuple<object, DateTime>> ItemsByLoadedTime { get; }  = new List<Tuple<object, DateTime>>();

		internal List<object> ItemsByAccessedTime { get; } = new List<object>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemCache"/> class.
		/// </summary>
		/// <param name="expiryLength">Length of the expiry.</param>
		/// <param name="maxItems">The maximum items.</param>
		public ItemCache(long expiryLength, int maxItems)
		{
			this.ExpiryLength = expiryLength;
			this.MaxItems = maxItems;
		}

		/// <summary>
		/// Determines whether the cache contains the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>true if the cache contains an element with the specified key; otherwise, false.</returns>
		public bool ContainsKey(object key)
		{
			lock (_lock)
			{
				// Remove anything that's older than it should be, before checking whether the item exists
				var expiryDateTime = DateTime.Now.AddMilliseconds(-1 * this.ExpiryLength);
				var keysToRemove = new List<object>();
				for (int i= 0; i < this.ItemsByLoadedTime.Count; i++)
				{
					if (this.ItemsByLoadedTime[i].Item2 < expiryDateTime)
					{
						keysToRemove.Add(this.ItemsByLoadedTime[i].Item1);
					}
				}
				foreach (object removeKey in keysToRemove)
				{
					Remove(removeKey);
				}

				// Return whether this item exists
				return this.Items.ContainsKey(key);
			}
		}

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The value of the key/value pair at the specified index.</returns>
		public IValueBag GetValues(object key)
		{
			lock (_lock)
			{
				UpdateAccessTimes(key);
				return this.Items[key];
			}
		}

		/// <summary>
		/// Adds a key/value pair to the cache if the key does not already exist, or updates a key/value
		/// pair in the cache by using the specified function if the key already exists.
		/// </summary>
		/// <param name="key">The key to be added or whose value should be updated.</param>
		/// <param name="addValue">The value to be added for an absent key.</param>
		/// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value.</param>
		public void AddOrUpdate(object key, IValueBag addValue, Func<object, IValueBag, IValueBag> updateValueFactory)
		{
			lock (_lock)
			{
				UpdateAccessTimes(key);
				this.Items.AddOrUpdate(key, addValue, updateValueFactory);

				// Remove items if we've got too many
				while (this.Items.Count > this.MaxItems)
				{
					Remove(this.ItemsByAccessedTime[0]);
				}
			}
		}

		private void UpdateAccessTimes(object key)
		{
			// TODO: This is pretty naive and might need some tuning
			if (this.ItemsByAccessedTime.Contains(key))
			{
				this.ItemsByAccessedTime.Remove(key);
				this.ItemsByAccessedTime.Add(key);
			}
			else
			{
				this.ItemsByAccessedTime.Add(key);
				this.ItemsByLoadedTime.Add(new Tuple<object, DateTime>(key, DateTime.Now));
			}
		}

		/// <summary>
		/// Removes the item with the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		public void Remove(object key)
		{
			this.Items.TryRemove(key, out IValueBag value);
			for (int i = 0; i < this.ItemsByLoadedTime.Count; i++)
			{
				if (this.ItemsByLoadedTime[i].Item1 == key)
				{
					this.ItemsByLoadedTime.RemoveAt(i);
					break;
				}
			}
			this.ItemsByAccessedTime.Remove(key);
		}
	}
}

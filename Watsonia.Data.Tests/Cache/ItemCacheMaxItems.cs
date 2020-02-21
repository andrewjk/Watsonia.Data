using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Cache
{
	public partial class CacheTests
	{
		[TestMethod]
		public void ItemCacheMaxItems()
		{
			// Test that adding 25 items to a cache that has a limit of 20 has the right number of items
			var cache = new ItemCache(900000, 20);
			for (var i = 0; i < 25; i++)
			{
				cache.AddOrUpdate(
					i,
					new TestValueBag() { ID = i },
					(object o, IValueBag values) => values);
			}
			Assert.AreEqual(20, cache.ItemCount);

			// Test that the values look good
			Assert.AreEqual(20, cache.ItemsByAccessedTime.Count);
			Assert.AreEqual(5, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(6, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(7, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(8, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(9, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(23, cache.ItemsByAccessedTime[18]);
			Assert.AreEqual(24, cache.ItemsByAccessedTime[19]);

			Assert.AreEqual(20, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(5, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(6, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(7, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(8, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(9, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(23, cache.ItemsByLoadedTime[18].Item1);
			Assert.AreEqual(24, cache.ItemsByLoadedTime[19].Item1);
		}
	}
}

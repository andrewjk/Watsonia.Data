﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		public void ItemCacheExpiry()
		{
			// Add test data
			var cache = new ItemCache(900000, 20);
			for (var i = 0; i < 20; i++)
			{
				cache.AddOrUpdate(
					i,
					new TestValueBag() { ID = i },
					(object o, IValueBag values) => values);
			}
			Assert.AreEqual(20, cache.ItemCount);

			// Test that the initial lists look good
			Assert.AreEqual(20, cache.ItemsByAccessedTime.Count);
			Assert.AreEqual(0, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(1, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(2, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(3, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(4, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(18, cache.ItemsByAccessedTime[18]);
			Assert.AreEqual(19, cache.ItemsByAccessedTime[19]);

			Assert.AreEqual(20, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(1, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(2, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(3, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(4, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(18, cache.ItemsByLoadedTime[18].Item1);
			Assert.AreEqual(19, cache.ItemsByLoadedTime[19].Item1);

			// Test that checking for an expired record returns false and updates the associated lists correctly
			// We have to fudge this because we can't update the date or wait for fifteen minutes
			cache.ItemsByLoadedTime[4] = new Tuple<object, DateTime>(
				cache.ItemsByLoadedTime[4].Item1,
				cache.ItemsByLoadedTime[4].Item2.AddMinutes(-16));
			Assert.AreEqual(false, cache.ContainsKey(4));

			Assert.AreEqual(19, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByAccessedTime[0]);
			Assert.AreEqual(1, cache.ItemsByAccessedTime[1]);
			Assert.AreEqual(2, cache.ItemsByAccessedTime[2]);
			Assert.AreEqual(3, cache.ItemsByAccessedTime[3]);
			Assert.AreEqual(5, cache.ItemsByAccessedTime[4]);
			Assert.AreEqual(18, cache.ItemsByAccessedTime[17]);
			Assert.AreEqual(19, cache.ItemsByAccessedTime[18]);

			Assert.AreEqual(19, cache.ItemsByLoadedTime.Count);
			Assert.AreEqual(0, cache.ItemsByLoadedTime[0].Item1);
			Assert.AreEqual(1, cache.ItemsByLoadedTime[1].Item1);
			Assert.AreEqual(2, cache.ItemsByLoadedTime[2].Item1);
			Assert.AreEqual(3, cache.ItemsByLoadedTime[3].Item1);
			Assert.AreEqual(5, cache.ItemsByLoadedTime[4].Item1);
			Assert.AreEqual(18, cache.ItemsByLoadedTime[17].Item1);
			Assert.AreEqual(19, cache.ItemsByLoadedTime[18].Item1);
		}
	}
}

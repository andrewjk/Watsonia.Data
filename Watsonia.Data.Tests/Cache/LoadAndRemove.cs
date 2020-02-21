using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Tests.Cache.Entities;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Tests.Cache
{
	public partial class CacheTests
	{
		[TestMethod]
		public async Task LoadAndRemoveAsync()
		{
			// Delete all existing condiments
			var deleteCondiments = Delete.From<Condiment>().Where(true);
			_db.Execute(deleteCondiments);
			_db.Insert(new Condiment() { CondimentID = 1, CondimentName = "Aniseed Syrup", UnitPrice = 10 });

			var syrup = _db.Query<Condiment>().FirstOrDefault(p => p.CondimentName == "Aniseed Syrup");

			// Check that the product can be loaded
			var syrup1 = await _db.LoadOrDefaultAsync<Condiment>(syrup.CondimentID);
			Assert.IsNotNull(syrup1);
			Assert.AreEqual(10, syrup1.UnitPrice);

			var cacheKey = DynamicProxyFactory.GetDynamicTypeName(typeof(Condiment), _db);

			// Check that the product is in the cache and can be removed
			Assert.IsTrue(_db.Cache.ContainsItemWithKey(cacheKey, syrup.CondimentID));
			_db.Cache.RemoveItemByKey(cacheKey, syrup.CondimentID);
			Assert.IsFalse(_db.Cache.ContainsItemWithKey(cacheKey, syrup.CondimentID));

			// Get the product back in the cache
			var syrup2 = await _db.LoadAsync<Condiment>(syrup.CondimentID);
			Assert.IsTrue(_db.Cache.ContainsItemWithKey(cacheKey, syrup.CondimentID));

			// Update the product's price in the database and test that it doesn't get updated until
			// we remove and re-load the item
			var sql = "UPDATE Condiment SET UnitPrice = @0 WHERE CondimentName = 'Aniseed Syrup'";
			await _db.ExecuteAsync(sql, 11);

			var syrup3 = await _db.LoadAsync<Condiment>(syrup.CondimentID);
			Assert.AreEqual(10, syrup3.UnitPrice);

			_db.Cache.RemoveItemByKey(cacheKey, syrup.CondimentID);
			Assert.IsFalse(_db.Cache.ContainsItemWithKey(cacheKey, syrup.CondimentID));

			var syrup4 = await _db.LoadAsync<Condiment>(syrup.CondimentID);
			Assert.AreEqual(11, syrup4.UnitPrice);

			// Check that clearing works
			Assert.IsTrue(_db.Cache.ContainsItemWithKey(cacheKey, syrup.CondimentID));
			_db.Cache.Clear();
			Assert.IsFalse(_db.Cache.ContainsItemWithKey(cacheKey, syrup.CondimentID));
		}
	}
}

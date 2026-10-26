using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Tests.Queries.Northwind;

namespace Watsonia.Data.Tests.Queries
{
	[TestClass]
	public class QueryPlanCacheTests
	{
		private static readonly NorthwindDatabase _db = new NorthwindDatabase();

		[TestMethod]
		public void CachedPlanIsReusedForTheSameQuery()
		{
			var id = "ALFKI";

			var first = _db.Query<Customer>().Where(c => c.CustomerID == id).ToList();
			var second = _db.Query<Customer>().Where(c => c.CustomerID == id).ToList();

			Assert.AreEqual(1, first.Count);
			Assert.AreEqual(1, second.Count);
			Assert.IsTrue(_db.CompiledQueryCache.Count > 0);
		}

		[TestMethod]
		public void CachedPlanIsNotReusedForDifferentValues()
		{
			var id = "ALFKI";
			var first = _db.Query<Customer>().Where(c => c.CustomerID == id).ToList();

			id = "ANATR";
			var second = _db.Query<Customer>().Where(c => c.CustomerID == id).ToList();

			Assert.AreEqual("ALFKI", first[0].CustomerID);
			Assert.AreEqual("ANATR", second[0].CustomerID);
		}

		[TestMethod]
		public void IncludePathsArePartOfThePlanKey()
		{
			var withoutInclude = _db.Query<Order>().Where(o => o.CustomerID == "ALFKI").ToList();
			Assert.IsFalse(((IDynamicProxy)withoutInclude[0]).StateTracker.LoadedItems.Contains("Customer"));

			var withInclude = _db.Query<Order>()
				.Include(o => o.Customer)
				.Where(o => o.CustomerID == "ALFKI")
				.ToList();
			Assert.IsTrue(((IDynamicProxy)withInclude[0]).StateTracker.LoadedItems.Contains("Customer"));
		}

		[TestMethod]
		public void ResultsReflectSavesAfterThePlanIsCached()
		{
			var existing = _db.Query<Order>().First();
			var orderID = existing.OrderID;
			var original = existing.OrderDate;

			try
			{
				// Warm the plan cache for this query shape and value
				var before = _db.Query<Order>().Where(o => o.OrderID == orderID).Single();
				Assert.AreEqual(original, before.OrderDate);

				before.OrderDate = original.AddDays(1);
				_db.Save(before);

				// The same (cached) plan must return fresh data from the database
				var after = _db.Query<Order>().Where(o => o.OrderID == orderID).Single();
				Assert.AreEqual(original.AddDays(1), after.OrderDate);
			}
			finally
			{
				var restore = _db.Query<Order>().Where(o => o.OrderID == orderID).Single();
				restore.OrderDate = original;
				_db.Save(restore);
			}
		}

		[TestMethod]
		public void NonCacheableQueriesRemainCorrect()
		{
			var cacheCount = _db.CompiledQueryCache.Count;

			// A mutable captured collection must never be cached by value
			var ids = new[] { "ALFKI" };
			var first = _db.Query<Customer>().Where(c => ids.Contains(c.CustomerID)).ToList();
			ids[0] = "ANATR";
			var second = _db.Query<Customer>().Where(c => ids.Contains(c.CustomerID)).ToList();

			Assert.AreEqual("ALFKI", first.Single().CustomerID);
			Assert.AreEqual("ANATR", second.Single().CustomerID);
			Assert.AreEqual(cacheCount, _db.CompiledQueryCache.Count);

			// Likewise for members over non-query types (DateTime.Now)
			var count = _db.Query<Order>().Count(o => o.OrderDate < DateTime.Now);
			Assert.IsTrue(count > 0);
			Assert.AreEqual(cacheCount, _db.CompiledQueryCache.Count);
		}

		[TestMethod]
		public void CompiledQueryCacheIsBounded()
		{
			for (var i = 0; i < 600; i++)
			{
				var value = "BOUND" + i;
				_db.Query<Customer>().Where(c => c.CustomerID == value).ToList();
			}

			Assert.IsTrue(_db.CompiledQueryCache.Count <= Watsonia.Data.Database.MaxCompiledQueries);
		}

		[TestMethod]
		public async Task ConcurrentQueriesAreCorrect()
		{
			var ids = new[] { "ALFKI", "ANATR", "ANTON", "AROUT", "BERGS", "BLAUS", "BLONP", "BOLID" };

			var results = await Task.WhenAll(ids.Select(id => Task.Run(
				() => _db.Query<Customer>().Where(c => c.CustomerID == id).Single().CustomerID)));

			for (var i = 0; i < ids.Length; i++)
			{
				Assert.AreEqual(ids[i], results[i]);
			}
		}
	}
}

using System.Linq;
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
	}
}

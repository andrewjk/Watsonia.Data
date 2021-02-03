using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Watsonia.Data.Tests.Queries.Northwind;

// TODO: Implement all double commented (////) tests

namespace Watsonia.Data.Tests.Queries
{
	[TestClass]
	public class QueryExecutionAsyncTests
	{
		private static readonly NorthwindDatabase _db = new NorthwindDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
		}
		
		////[TestMethod]
		////public async Task ThingAsync()
		////{
		////	// TODO:
		////	//Customer c = db.Customers.FirstOrDefaultAsync();
		////	//TestQuery(
		////	//	db.Orders.Where(o => o.Customer == c));
		////}

		[TestMethod]
		public async Task WhereAsync()
		{
			var list = await _db.Customers.Where(c => c.City == "London").ToListAsync();
			Assert.AreEqual(6, list.Count);
		}

		[TestMethod]
		public async Task WhereTrueAsync()
		{
			var list = await _db.Customers.Where(c => true).ToListAsync();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public async Task CompareEntityEqualAsync()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			var list = await _db.Customers.Where(c => c == alfki).ToListAsync();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("ALFKI", list[0].CustomerID);
		}

		[TestMethod]
		public async Task CompareEntityNotEqualAsync()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			var list = await _db.Customers.Where(c => c != alfki).ToListAsync();
			Assert.AreEqual(90, list.Count);
		}

		////[TestMethod]
		////public async Task CompareConstructedEqualAsync()
		////{
		////	var list = db.Customers.Where(c => new { x = c.City } == new { x = "London" }).ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public async Task CompareConstructedMultiValueEqualAsync()
		////{
		////	var list = db.Customers.Where(c => new { x = c.City, y = c.Country } == new { x = "London", y = "UK" }).ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public async Task CompareConstructedMultiValueNotEqualAsync()
		////{
		////	var list = db.Customers.Where(c => new { x = c.City, y = c.Country } != new { x = "London", y = "UK" }).ToListAsync();
		////	Assert.AreEqual(85, list.Count);
		////}

		[TestMethod]
		public async Task SelectScalarAsync()
		{
			var list = await _db.Customers.Where(c => c.City == "London").Select(c => c.City).ToListAsync();
			Assert.AreEqual(6, list.Count);
			Assert.AreEqual("London", list[0]);
			Assert.IsTrue(list.All(x => x == "London"));
		}

		[TestMethod]
		public async Task SelectAnonymousOneAsync()
		{
			var list = await _db.Customers.Where(c => c.City == "London").Select(c => new { c.City }).ToListAsync();
			Assert.AreEqual(6, list.Count);
			Assert.AreEqual("London", list[0].City);
			Assert.IsTrue(list.All(x => x.City == "London"));
		}

		[TestMethod]
		public async Task SelectAnonymousTwoAsync()
		{
			var list = await _db.Customers.Where(c => c.City == "London").Select(c => new { c.City, c.Phone }).ToListAsync();
			Assert.AreEqual(6, list.Count);
			Assert.AreEqual("London", list[0].City);
			Assert.IsTrue(list.All(x => x.City == "London"));
			Assert.IsTrue(list.All(x => x.Phone != null));
		}

		[TestMethod]
		public async Task SelectCustomerTableAsync()
		{
			var list = await _db.Customers.ToListAsync();
			Assert.AreEqual(91, list.Count);
		}

		////[TestMethod]
		////public async Task SelectAnonymousWithObjectAsync()
		////{
		////	var list = db.Customers.Where(c => c.City == "London").Select(c => new { c.City, c }).ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.AreEqual("London", list[0].City);
		////	Assert.IsTrue(list.All(x => x.City == "London"));
		////	Assert.IsTrue(list.All(x => x.c.City == x.City));
		////}

		////[TestMethod]
		////public async Task SelectAnonymousLiteralAsync()
		////{
		////	var list = db.Customers.Where(c => c.City == "London").Select(c => new { X = 10 }).ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.IsTrue(list.All(x => x.X == 10));
		////}

		[TestMethod]
		public async Task SelectConstantIntAsync()
		{
			var list = await _db.Customers.Select(c => 10).ToListAsync();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(list.All(x => x == 10));
		}

		[TestMethod]
		public async Task SelectConstantNullStringAsync()
		{
			var list = await _db.Customers.Select(c => (string)null).ToListAsync();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(list.All(x => x == null));
		}

		[TestMethod]
		public async Task SelectLocalAsync()
		{
			var x = 10;
			var list = await _db.Customers.Select(c => x).ToListAsync();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(list.All(y => y == 10));
		}

		////[TestMethod]
		////public async Task SelectNestedCollectionAsync()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		select db.Orders.Where(o => o.CustomerID == c.CustomerID).Select(o => o.OrderID)
		////		).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].CountAsync());
		////}

		////[TestMethod]
		////public async Task SelectNestedCollectionInAnonymousTypeAsync()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		select new { Foos = db.Orders.Where(o => o.CustomerID == c.CustomerID).Select(o => o.OrderID).ToListAsync() }
		////		).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Foos.CountAsync);
		////}

		[TestMethod]
		public async Task JoinCustomerOrdersAsync()
		{
			var list = await (
				from c in _db.Customers
				where c.CustomerID == "ALFKI"
				join o in _db.Orders on c.CustomerID equals o.CustomerID
				select new { c.ContactName, o.OrderID }
				).ToListAsync();
			Assert.AreEqual(6, list.Count);
		}

		////[TestMethod]
		////public async Task JoinMultiKeyAsync()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		join o in db.Orders on new { a = c.CustomerID, b = c.CustomerID } equals new { a = o.CustomerID, b = o.CustomerID }
		////		select new { c, o }
		////		).ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public async Task JoinIntoCustomersOrdersCountAsync()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		select new { cust = c, ords = ords.CountAsync() }
		////		).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].ords);
		////}

		////[TestMethod]
		////public async Task JoinIntoDefaultIfEmptyAsync()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "PARIS"
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		from o in ords.DefaultIfEmpty()
		////		select new { c, o }
		////		).ToListAsync();

		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(null, list[0].o);
		////}

		////[TestMethod]
		////public async Task MultipleJoinsWithJoinConditionsInWhereAsync()
		////{
		////	var list = (
		////		from c in db.Customers
		////		from o in db.Orders
		////		from d in db.OrderDetails
		////		where o.CustomerID == c.CustomerID && o.OrderID == d.OrderID
		////		where c.CustomerID == "ALFKI"
		////		select d
		////		).ToListAsync();

		////	Assert.AreEqual(12, list.Count);
		////}

		////[TestMethod]
		////public async Task MultipleJoinsWithMissingJoinConditionAsync()
		////{
		////	var list = (
		////		from c in db.Customers
		////		from o in db.Orders
		////		from d in db.OrderDetails
		////		where o.CustomerID == c.CustomerID /*&& o.OrderID == d.OrderID*/
		////		where c.CustomerID == "ALFKI"
		////		select d
		////		).ToListAsync();

		////	Assert.AreEqual(12930, list.Count);
		////}

		[TestMethod]
		public async Task OrderByAsync()
		{
			var list = await _db.Customers.OrderBy(c => c.CustomerID).Select(c => c.CustomerID).ToListAsync();
			var sorted = list.OrderBy(c => c).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		////[TestMethod]
		////public async Task OrderByOrderByAsync()
		////{
		////	var list = db.Customers.OrderBy(c => c.Phone).OrderBy(c => c.CustomerID).ToListAsync();
		////	var sorted = list.OrderBy(c => c.CustomerID).ToListAsync();
		////	Assert.AreEqual(91, list.Count);
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		[TestMethod]
		public async Task OrderByThenByAsync()
		{
			var list = await _db.Customers.OrderBy(c => c.CustomerID).ThenBy(c => c.Phone).ToListAsync();
			var sorted = list.OrderBy(c => c.CustomerID).ThenBy(c => c.Phone).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		[TestMethod]
		public async Task OrderByDescendingAsync()
		{
			var list = await _db.Customers.OrderByDescending(c => c.CustomerID).ToListAsync();
			var sorted = list.OrderByDescending(c => c.CustomerID).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		[TestMethod]
		public async Task OrderByDescendingThenByAsync()
		{
			var list = await _db.Customers.OrderByDescending(c => c.CustomerID).ThenBy(c => c.Country).ToListAsync();
			var sorted = list.OrderByDescending(c => c.CustomerID).ThenBy(c => c.Country).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		[TestMethod]
		public async Task OrderByDescendingThenByDescendingAsync()
		{
			var list = await _db.Customers.OrderByDescending(c => c.CustomerID).ThenByDescending(c => c.Country).ToListAsync();
			var sorted = list.OrderByDescending(c => c.CustomerID).ThenByDescending(c => c.Country).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		////[TestMethod]
		////public async Task OrderByJoinAsync()
		////{
		////	var list = (
		////		from c in db.Customers.OrderBy(c => c.CustomerID)
		////		join o in db.Orders.OrderBy(o => o.OrderID) on c.CustomerID equals o.CustomerID
		////		select new { CustomerID = c.CustomerID, o.OrderID }
		////		).ToListAsync();

		////	var sorted = list.OrderBy(x => x.CustomerID).ThenBy(x => x.OrderID);
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		////[TestMethod]
		////public async Task OrderBySelectManyAsync()
		////{
		////	var list = (
		////		from c in db.Customers.OrderBy(c => c.CustomerID)
		////		from o in db.Orders.OrderBy(o => o.OrderID)
		////		where c.CustomerID == o.CustomerID
		////		select new { CustomerID = c.CustomerID, o.OrderID }
		////		).ToListAsync();
		////	var sorted = list.OrderBy(x => x.CustomerID).ThenBy(x => x.OrderID).ToListAsync();
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		////[TestMethod]
		////public async Task CountPropertyAsync()
		////{
		////	var list = db.Customers.Where(c => c.Orders.CountAsync > 0).ToListAsync();
		////	Assert.AreEqual(89, list.Count);
		////}

		////[TestMethod]
		////public async Task GroupByAsync()
		////{
		////	var list = db.Customers.GroupBy(c => c.City).ToListAsync();
		////	Assert.AreEqual(69, list.Count);
		////}

		////[TestMethod]
		////public async Task GroupByOneAsync()
		////{
		////	var list = db.Customers.Where(c => c.City == "London").GroupBy(c => c.City).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].CountAsync());
		////}

		////[TestMethod]
		////public async Task GroupBySelectManyAsync()
		////{
		////	var list = db.Customers.GroupBy(c => c.City).SelectMany(g => g).ToListAsync();
		////	Assert.AreEqual(91, list.Count);
		////}

		////[TestMethod]
		////public async Task GroupBySumAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g => g.SumAsync(o => (o.CustomerID == "ALFKI" ? 1 : 1))).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public async Task GroupByCountAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g => g.CountAsync()).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public async Task GroupByLongCountAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g => g.LongCount()).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6L, list[0]);
		////}

		////[TestMethod]
		////public async Task GroupBySumMinMaxAvgAsync()
		////{
		////	var list =
		////		db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g =>
		////			new
		////			{
		////				SumAsync = g.SumAsync(o => (o.CustomerID == "ALFKI" ? 1 : 1)),
		////				Min = g.Min(o => o.OrderID),
		////				MaxAsync = g.MaxAsync(o => o.OrderID),
		////				Avg = g.Average(o => o.OrderID)
		////			}).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].SumAsync);
		////}

		////[TestMethod]
		////public async Task GroupByWithResultSelectorAsync()
		////{
		////	var list =
		////		db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, (k, g) =>
		////			new
		////			{
		////				SumAsync = g.SumAsync(o => (o.CustomerID == "ALFKI" ? 1 : 1)),
		////				Min = g.Min(o => o.OrderID),
		////				MaxAsync = g.MaxAsync(o => o.OrderID),
		////				Avg = g.Average(o => o.OrderID)
		////			}).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].SumAsync);
		////}

		////[TestMethod]
		////public async Task GroupByWithElementSelectorSumAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1)).Select(g => g.SumAsync()).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public async Task GroupByWithElementSelectorAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1)).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].CountAsync());
		////	Assert.AreEqual(6, list[0].SumAsync());
		////}

		////[TestMethod]
		////public async Task GroupByWithElementSelectorSumMaxAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1)).Select(g => new { SumAsync = g.SumAsync(), MaxAsync = g.MaxAsync() }).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].SumAsync);
		////	Assert.AreEqual(1, list[0].MaxAsync);
		////}

		////[TestMethod]
		////public async Task GroupByWithAnonymousElementAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => new { X = (o.CustomerID == "ALFKI" ? 1 : 1) }).Select(g => g.SumAsync(x => x.X)).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public async Task GroupByWithTwoPartKeyAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => new { CustomerID = o.CustomerID, o.OrderDate }).Select(g => g.SumAsync(o => (o.CustomerID == "ALFKI" ? 1 : 1))).ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public async Task GroupByWithCountInWhereAsync()
		////{
		////	var list = db.Customers.Where(a => a.Orders.CountAsync() > 15).GroupBy(a => a.City).ToListAsync();
		////	Assert.AreEqual(9, list.Count);
		////}

		////[TestMethod]
		////public async Task OrderByGroupByAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	var grp = list[0].ToListAsync();
		////	var sorted = grp.OrderBy(o => o.OrderID);
		////	Assert.IsTrue(Enumerable.SequenceEqual(grp, sorted));
		////}

		////[TestMethod]
		////public async Task OrderByGroupBySelectManyAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).SelectMany(g => g).ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////	var sorted = list.OrderBy(o => o.OrderID).ToListAsync();
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		[TestMethod]
		public async Task SumWithNoArgAsync()
		{
			var sum = await _db.Orders.Where(o => o.CustomerID == "ALFKI").Select(o => (o.CustomerID == "ALFKI" ? 1 : 1)).SumAsync();
			Assert.AreEqual(6, sum);
		}

		[TestMethod]
		public async Task SumWithArgAsync()
		{
			var sum = await _db.Orders.Where(o => o.CustomerID == "ALFKI").SumAsync(o => (o.CustomerID == "ALFKI" ? 1 : 1));
			Assert.AreEqual(6, sum);
		}

		[TestMethod]
		public async Task CountWithNoPredicateAsync()
		{
			var cnt = await _db.Orders.CountAsync();
			Assert.AreEqual(830, cnt);
		}

		[TestMethod]
		public async Task CountWithPredicateAsync()
		{
			var cnt = await _db.Orders.CountAsync(o => o.CustomerID == "ALFKI");
			Assert.AreEqual(6, cnt);
		}

		[TestMethod]
		public async Task DistinctNoDupesAsync()
		{
			var list = await _db.Customers.Distinct().ToListAsync();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public async Task DistinctScalarAsync()
		{
			var list = await _db.Customers.Select(c => c.City).Distinct().ToListAsync();
			Assert.AreEqual(69, list.Count);
		}

		[TestMethod]
		public async Task OrderByDistinctAsync()
		{
			var list = await _db.Customers.Where(c => c.City.StartsWith("P")).OrderBy(c => c.City).Select(c => c.City).Distinct().ToListAsync();
			var sorted = list.OrderBy(x => x).ToList();
			Assert.AreEqual(list[0], sorted[0]);
			Assert.AreEqual(list[^1], sorted[list.Count - 1]);
		}

		////[TestMethod]
		////public async Task DistinctOrderByAsync()
		////{
		////	var list = db.Customers.Where(c => c.City.StartsWith("P")).Select(c => c.City).Distinct().OrderBy(c => c).ToListAsync();
		////	var sorted = list.OrderBy(x => x).ToListAsync();
		////	Assert.AreEqual(list[0], sorted[0]);
		////	Assert.AreEqual(list[list.Count - 1], sorted[list.Count - 1]);
		////}

		////[TestMethod]
		////public async Task DistinctGroupByAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").Distinct().GroupBy(o => o.CustomerID).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public async Task GroupByDistinctAsync()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Distinct().ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		[TestMethod]
		public async Task DistinctCountAsync()
		{
			var cnt = await _db.Customers.Distinct().CountAsync();
			Assert.AreEqual(91, cnt);
		}

		////[TestMethod]
		////public async Task SelectDistinctCountAsync()
		////{
		////	var cnt = db.Customers.Select(c => c.City).Distinct().CountAsync();
		////	Assert.AreEqual(69, cnt);
		////}

		////[TestMethod]
		////public async Task SelectSelectDistinctCountAsync()
		////{
		////	var cnt = db.Customers.Select(c => c.City).Select(c => c).Distinct().CountAsync();
		////	Assert.AreEqual(69, cnt);
		////}

		////[TestMethod]
		////public async Task DistinctCountPredicateAsync()
		////{
		////	var cnt = db.Customers.Select(c => new { c.City, c.Country }).Distinct().CountAsync(c => c.City == "London");
		////	Assert.AreEqual(1, cnt);
		////}

		////[TestMethod]
		////public async Task DistinctSumWithArgAsync()
		////{
		////	var sum = db.Orders.Where(o => o.CustomerID == "ALFKI").Distinct().SumAsync(o => (o.CustomerID == "ALFKI" ? 1 : 1));
		////	Assert.AreEqual(6, sum);
		////}

		[TestMethod]
		public async Task SelectDistinctSumAsync()
		{
			var sum = await _db.Orders.Where(o => o.CustomerID == "ALFKI").Select(o => o.OrderID).Distinct().SumAsync();
			Assert.AreEqual(64835, sum);
		}

		[TestMethod]
		public async Task TakeAsync()
		{
			var list = await _db.Orders.Take(5).ToListAsync();
			Assert.AreEqual(5, list.Count);
		}

		////[TestMethod]
		////public async Task TakeDistinctAsync()
		////{
		////	var list = db.Orders.OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Take(5).Distinct().ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		[TestMethod]
		public async Task DistinctTakeAsync()
		{
			var list = await _db.Orders.OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Distinct().Take(5).ToListAsync();
			Assert.AreEqual(5, list.Count);
		}

		////[TestMethod]
		////public async Task DistinctTakeCountAsync()
		////{
		////	var cnt = db.Orders.Distinct().OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Take(5).CountAsync();
		////	Assert.AreEqual(5, cnt);
		////}

		////[TestMethod]
		////public async Task TakeDistinctCountAsync()
		////{
		////	var cnt = db.Orders.OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Take(5).Distinct().CountAsync();
		////	Assert.AreEqual(1, cnt);
		////}

		[TestMethod]
		public async Task FirstAsync()
		{
			var first = await _db.Customers.OrderBy(c => c.ContactName).FirstAsync();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("ROMEY", first.CustomerID);
		}

		[TestMethod]
		public async Task FirstPredicateAsync()
		{
			var first = await _db.Customers.OrderBy(c => c.ContactName).FirstAsync(c => c.City == "London");
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public async Task WhereFirstAsync()
		{
			var first = await _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").FirstAsync();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public async Task FirstOrDefaultAsync()
		{
			var first = await _db.Customers.OrderBy(c => c.ContactName).FirstOrDefaultAsync();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("ROMEY", first.CustomerID);
		}

		[TestMethod]
		public async Task FirstOrDefaultPredicateAsync()
		{
			var first = await _db.Customers.OrderBy(c => c.ContactName).FirstOrDefaultAsync(c => c.City == "London");
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public async Task WhereFirstOrDefaultAsync()
		{
			var first = await _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").FirstOrDefaultAsync();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public async Task FirstOrDefaultPredicateNoMatchAsync()
		{
			var first = await _db.Customers.OrderBy(c => c.ContactName).FirstOrDefaultAsync(c => c.City == "SpongeBob");
			Assert.AreEqual(null, first);
		}

		[TestMethod]
		public async Task ReverseAsync()
		{
			var list = await _db.Customers.OrderBy(c => c.ContactName).Reverse().ToListAsync();
			Assert.AreEqual(91, list.Count);
			Assert.AreEqual("WOLZA", list[0].CustomerID);
			Assert.AreEqual("ROMEY", list[90].CustomerID);
		}

		[TestMethod]
		public async Task ReverseReverseAsync()
		{
			var list = await _db.Customers.OrderBy(c => c.ContactName).Reverse().Reverse().ToListAsync();
			Assert.AreEqual(91, list.Count);
			Assert.AreEqual("ROMEY", list[0].CustomerID);
			Assert.AreEqual("WOLZA", list[90].CustomerID);
		}

		////[TestMethod]
		////public async Task ReverseWhereReverseAsync()
		////{
		////	var list = db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Reverse().ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.AreEqual("EASTC", list[0].CustomerID);
		////	Assert.AreEqual("BSBEV", list[5].CustomerID);
		////}

		////[TestMethod]
		////public async Task ReverseTakeReverseAsync()
		////{
		////	var list = db.Customers.OrderBy(c => c.ContactName).Reverse().Take(5).Reverse().ToListAsync();
		////	Assert.AreEqual(5, list.Count);
		////	Assert.AreEqual("CHOPS", list[0].CustomerID);
		////	Assert.AreEqual("WOLZA", list[4].CustomerID);
		////}

		////[TestMethod]
		////public async Task ReverseWhereTakeReverseAsync()
		////{
		////	var list = db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Take(5).Reverse().ToListAsync();
		////	Assert.AreEqual(5, list.Count);
		////	Assert.AreEqual("CONSH", list[0].CustomerID);
		////	Assert.AreEqual("BSBEV", list[4].CustomerID);
		////}

		[TestMethod]
		public async Task LastAsync()
		{
			var last = await _db.Customers.OrderBy(c => c.ContactName).LastAsync();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("WOLZA", last.CustomerID);
		}

		[TestMethod]
		public async Task LastPredicateAsync()
		{
			var last = await _db.Customers.OrderBy(c => c.ContactName).LastAsync(c => c.City == "London");
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public async Task WhereLastAsync()
		{
			var last = await _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").LastAsync();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public async Task LastOrDefaultAsync()
		{
			var last = await _db.Customers.OrderBy(c => c.ContactName).LastOrDefaultAsync();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("WOLZA", last.CustomerID);
		}

		[TestMethod]
		public async Task LastOrDefaultPredicateAsync()
		{
			var last = await _db.Customers.OrderBy(c => c.ContactName).LastOrDefaultAsync(c => c.City == "London");
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public async Task WhereLastOrDefaultAsync()
		{
			var last = await _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").LastOrDefaultAsync();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public async Task LastOrDefaultNoMatchesAsync()
		{
			var last = await _db.Customers.OrderBy(c => c.ContactName).LastOrDefaultAsync(c => c.City == "SpongeBob");
			Assert.AreEqual(null, last);
		}

		////[TestMethod]
		////public async Task SingleFailsAsync()
		////{
		////	var single = db.Customers.SingleAsync();
		////}

		[TestMethod]
		public async Task SinglePredicateAsync()
		{
			var single = await _db.Customers.SingleAsync(c => c.CustomerID == "ALFKI");
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		[TestMethod]
		public async Task WhereSingleAsync()
		{
			var single = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleAsync();
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		////[TestMethod]
		////public async Task SingleOrDefaultFailsAsync()
		////{
		////	var single = db.Customers.SingleOrDefaultAsync();
		////}

		[TestMethod]
		public async Task SingleOrDefaultPredicateAsync()
		{
			var single = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI");
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		[TestMethod]
		public async Task WhereSingleOrDefaultAsync()
		{
			var single = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync();
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		////[TestMethod]
		////public async Task SingleOrDefaultNoMatchesAsync()
		////{
		////	var single = db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "SpongeBob");
		////	Assert.AreEqual(null, single);
		////}

		[TestMethod]
		public async Task AnyTopLevelAsync()
		{
			var any = await _db.Customers.AnyAsync();
			Assert.IsTrue(any);
		}

		////[TestMethod]
		////public async Task AnyWithSubqueryAsync()
		////{
		////	bool what = db.Customers.AnyAsync(c => c.CustomerID == "ALFKI");
		////	var list = db.Customers.Where(c => c.Orders.AnyAsync(o => o.CustomerID == "ALFKI")).ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public async Task AnyWithSubqueryNoPredicateAsync()
		////{
		////	var list = db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).AnyAsync()).ToListAsync();
		////	Assert.AreEqual(89, list.Count);
		////}

		////[TestMethod]
		////public async Task AnyWithLocalCollectionAsync()
		////{
		////	string[] ids = new[] { "ALFKI", "WOLZA", "NOONE" };
		////	var list = db.Customers.Where(c => ids.AnyAsync(id => c.CustomerID == id)).ToListAsync();
		////	Assert.AreEqual(2, list.Count);
		////}

		////[TestMethod]
		////public async Task AllWithSubqueryAsync()
		////{
		////	var list = db.Customers.Where(c => c.Orders.AllAsync(o => o.CustomerID == "ALFKI")).ToListAsync();
		////	Assert.AreEqual(3, list.Count);
		////}

		////[TestMethod]
		////public async Task AllWithLocalCollectionAsync()
		////{
		////	string[] patterns = new[] { "m", "d" };

		////	var list = db.Customers.Where(c => patterns.AllAsync(p => c.ContactName.Contains(p))).Select(c => c.ContactName).ToListAsync();
		////	var local = db.Customers.AsEnumerable().Where(c => patterns.AllAsync(p => c.ContactName.ToLower().ContainsAsync(p))).Select(c => c.ContactName).ToListAsync();

		////	Assert.AreEqual(local.CountAsync, list.Count);
		////}

		[TestMethod]
		public async Task AllTopLevelAsync()
		{
			var all = await _db.Customers.AllAsync(c => c.ContactName.Length > 0);
			Assert.IsTrue(all);
		}

		[TestMethod]
		public async Task AllTopLevelNoMatchesAsync()
		{
			var all = await _db.Customers.AllAsync(c => c.ContactName.Contains("a"));
			Assert.IsFalse(all);
		}

		[TestMethod]
		public async Task ContainsWithSubqueryAsync()
		{
			var list = await _db.Customers.Where(c => _db.Orders.Select(o => o.CustomerID).Contains(c.CustomerID)).ToListAsync();
			Assert.AreEqual(85, list.Count);
		}

		[TestMethod]
		public async Task ContainsWithLocalCollectionAsync()
		{
			var ids = new[] { "ALFKI", "WOLZA", "NOONE" };
			var list = await _db.Customers.Where(c => ids.Contains(c.CustomerID)).ToListAsync();
			Assert.AreEqual(2, list.Count);
		}

		[TestMethod]
		public async Task ContainsTopLevelAsync()
		{
			var contains = await _db.Customers.Select(c => c.CustomerID).ContainsAsync("ALFKI");
			Assert.IsTrue(contains);
		}

		////[TestMethod]
		////public async Task SkipTakeAsync()
		////{
		////	var list = db.Customers.OrderBy(c => c.CustomerID).Skip(5).Take(10).ToListAsync();
		////	Assert.AreEqual(10, list.Count);
		////	Assert.AreEqual("BLAUS", list[0].CustomerID);
		////	Assert.AreEqual("COMMI", list[9].CustomerID);
		////}

		////[TestMethod]
		////public async Task DistinctSkipTakeAsync()
		////{
		////	var list = db.Customers.Select(c => c.City).Distinct().OrderBy(c => c).Skip(5).Take(10).ToListAsync();
		////	Assert.AreEqual(10, list.Count);
		////	var hs = new HashSet<string>(list);
		////	Assert.AreEqual(10, hs.CountAsync);
		////}

		////[TestMethod]
		////public async Task CoalesceAsync()
		////{
		////	var list = db.Customers.Select(c => new { City = (c.City == "London" ? null : c.City), Country = (c.CustomerID == "EASTC" ? null : c.Country) })
		////				 .Where(x => (x.City ?? "NoCity") == "NoCity").ToListAsync();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.AreEqual(null, list[0].City);
		////}

		////[TestMethod]
		////public async Task Coalesce2Async()
		////{
		////	var list = db.Customers.Select(c => new { City = (c.City == "London" ? null : c.City), Country = (c.CustomerID == "EASTC" ? null : c.Country) })
		////				 .Where(x => (x.City ?? x.Country ?? "NoCityOrCountry") == "NoCityOrCountry").ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(null, list[0].City);
		////	Assert.AreEqual(null, list[0].Country);
		////}

		[TestMethod]
		public async Task StringLengthAsync()
		{
			var list = await _db.Customers.Where(c => c.City.Length == 7).ToListAsync();
			Assert.AreEqual(9, list.Count);
		}

		[TestMethod]
		public async Task StringStartsWithLiteralAsync()
		{
			var list = await _db.Customers.Where(c => c.ContactName.StartsWith("M")).ToListAsync();
			Assert.AreEqual(12, list.Count);
		}

		[TestMethod]
		public async Task StringStartsWithColumnAsync()
		{
			var list = await _db.Customers.Where(c => c.ContactName.StartsWith(c.ContactName)).ToListAsync();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public async Task StringEndsWithLiteralAsync()
		{
			var list = await _db.Customers.Where(c => c.ContactName.EndsWith("s")).ToListAsync();
			Assert.AreEqual(9, list.Count);
		}

		[TestMethod]
		public async Task StringEndsWithColumnAsync()
		{
			var list = await _db.Customers.Where(c => c.ContactName.EndsWith(c.ContactName)).ToListAsync();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public async Task StringContainsLiteralAsync()
		{
			var list = await _db.Customers.Where(c => c.ContactName.Contains("nd")).Select(c => c.ContactName).ToListAsync();
			var local = _db.Customers.AsEnumerable().Where(c => c.ContactName.ToLower().Contains("nd")).Select(c => c.ContactName).ToList();
			Assert.AreEqual(local.Count, list.Count);
		}

		[TestMethod]
		public async Task StringContainsColumnAsync()
		{
			var list = await _db.Customers.Where(c => c.ContactName.Contains(c.ContactName)).ToListAsync();
			Assert.AreEqual(91, list.Count);
		}

		////[TestMethod]
		////public async Task StringConcatImplicit2ArgsAsync()
		////{
		////	var list = db.Customers.Where(c => c.ContactName + "X" == "Maria AndersX").ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public async Task StringConcatExplicit2ArgsAsync()
		////{
		////	var list = db.Customers.Where(c => string.Concat(c.ContactName, "X") == "Maria AndersX").ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public async Task StringConcatExplicit3ArgsAsync()
		////{
		////	var list = db.Customers.Where(c => string.Concat(c.ContactName, "X", c.Country) == "Maria AndersXGermany").ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public async Task StringConcatExplicitNArgsAsync()
		////{
		////	var list = db.Customers.Where(c => string.Concat(new string[] { c.ContactName, "X", c.Country }) == "Maria AndersXGermany").ToListAsync();
		////	Assert.AreEqual(1, list.Count);
		////}

		[TestMethod]
		public async Task StringIsNullOrEmptyAsync()
		{
			var list = await _db.Customers.Select(c => c.City == "London" ? null : c.CustomerID).Where(x => string.IsNullOrEmpty(x)).ToListAsync();
			Assert.AreEqual(6, list.Count);
		}

		////[TestMethod]
		////public async Task StringToUpperAsync()
		////{
		////	var str = db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => (c.CustomerID == "ALFKI" ? "abc" : "abc").ToUpper());
		////	Assert.AreEqual("ABC", str);
		////}

		////[TestMethod]
		////public async Task StringToLowerAsync()
		////{
		////	var str = db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => (c.CustomerID == "ALFKI" ? "ABC" : "ABC").ToLower());
		////	Assert.AreEqual("abc", str);
		////}

		[TestMethod]
		public async Task StringSubstringAsync()
		{
			var list = await _db.Customers.Where(c => c.City.Substring(0, 4) == "Seat").ToListAsync();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("Seattle", list[0].City);
		}

		[TestMethod]
		public async Task StringSubstringNoLengthAsync()
		{
			var list = await _db.Customers.Where(c => c.City.Substring(4) == "tle").ToListAsync();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("Seattle", list[0].City);
		}

		[TestMethod]
		public async Task StringIndexOfAsync()
		{
			var n = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => c.ContactName.IndexOf("ar"));
			Assert.AreEqual(1, n);
		}

		[TestMethod]
		public async Task StringIndexOfCharAsync()
		{
			var n = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => c.ContactName.IndexOf('r'));
			Assert.AreEqual(2, n);
		}

		[TestMethod]
		public async Task StringIndexOfWithStartAsync()
		{
			var n = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => c.ContactName.IndexOf("a", 3));
			Assert.AreEqual(4, n);
		}

		////[TestMethod]
		////public async Task StringTrimAsync()
		////{
		////	var notrim = db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => ("  " + c.City + " "));
		////	var trim = db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => ("  " + c.City + " ").Trim());
		////	Assert.AreNotEqual(notrim, trim);
		////	Assert.AreEqual(notrim.Trim(), trim);
		////}

		[TestMethod]
		public async Task DateTimeConstructYmdAsync()
		{
			var dt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4));
			Assert.AreEqual(1997, dt.Year);
			Assert.AreEqual(7, dt.Month);
			Assert.AreEqual(4, dt.Day);
			Assert.AreEqual(0, dt.Hour);
			Assert.AreEqual(0, dt.Minute);
			Assert.AreEqual(0, dt.Second);
		}

		[TestMethod]
		public async Task DateTimeConstructYmdhmsAsync()
		{
			var dt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6));
			Assert.AreEqual(1997, dt.Year);
			Assert.AreEqual(7, dt.Month);
			Assert.AreEqual(4, dt.Day);
			Assert.AreEqual(3, dt.Hour);
			Assert.AreEqual(5, dt.Minute);
			Assert.AreEqual(6, dt.Second);
		}

		////[TestMethod]
		////public async Task DateTimeDayAsync()
		////{
		////	var v = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).MaxAsync(o => o.OrderDate.Day);
		////	Assert.AreEqual(25, v);
		////}

		////[TestMethod]
		////public async Task DateTimeMonthAsync()
		////{
		////	var v = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).MaxAsync(o => o.OrderDate.Month);
		////	Assert.AreEqual(8, v);
		////}

		////[TestMethod]
		////public async Task DateTimeYearAsync()
		////{
		////	var v = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).MaxAsync(o => o.OrderDate.Year);
		////	Assert.AreEqual(1997, v);
		////}

		[TestMethod]
		public async Task DateTimeHourAsync()
		{
			var hour = await _db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Hour);
			Assert.AreEqual(3, hour);
		}

		[TestMethod]
		public async Task DateTimeMinuteAsync()
		{
			var minute = await _db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Minute);
			Assert.AreEqual(5, minute);
		}

		[TestMethod]
		public async Task DateTimeSecondAsync()
		{
			var second = await _db.Customers.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Second);
			Assert.AreEqual(6, second);
		}

		////[TestMethod]
		////public async Task DateTimeDayOfWeekAsync()
		////{
		////	var dow = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).MaxAsync(o => o.OrderDate.DayOfWeek);
		////	Assert.AreEqual(DayOfWeek.Monday, dow);
		////}

		[TestMethod]
		public async Task DateTimeAddYearsAsync()
		{
			var od = await _db.Orders.FirstOrDefaultAsync(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddYears(2).Year == 2014);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public async Task DateTimeAddMonthsAsync()
		{
			var od = await _db.Orders.FirstOrDefaultAsync(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddMonths(2).Month == 10);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public async Task DateTimeAddDaysAsync()
		{
			var od = await _db.Orders.FirstOrDefaultAsync(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddDays(2).Day == 25);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public async Task DateTimeAddHoursAsync()
		{
			var od = await _db.Orders.FirstOrDefaultAsync(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddHours(3).Hour == 3);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public async Task DateTimeAddMinutesAsync()
		{
			var od = await _db.Orders.FirstOrDefaultAsync(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddMinutes(5).Minute == 5);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public async Task DateTimeAddSecondsAsync()
		{
			var od = await _db.Orders.FirstOrDefaultAsync(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddSeconds(6).Second == 6);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public async Task MathAbsAsync()
		{
			var neg1 = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Abs((c.CustomerID == "ALFKI") ? -1 : 0));
			var pos1 = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Abs((c.CustomerID == "ALFKI") ? 1 : 0));
			Assert.AreEqual(Math.Abs(-1), neg1);
			Assert.AreEqual(Math.Abs(1), pos1);
		}

		[TestMethod]
		public async Task MathAtanAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Atan((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var one = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Atan((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			Assert.AreEqual(Math.Atan(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Atan(1.0), one, 0.0001);
		}

		[TestMethod]
		public async Task MathCosAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Cos((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var pi = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Cos((c.CustomerID == "ALFKI") ? Math.PI : Math.PI));
			Assert.AreEqual(Math.Cos(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Cos(Math.PI), pi, 0.0001);
		}

		[TestMethod]
		public async Task MathSinAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Sin((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var pi = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Sin((c.CustomerID == "ALFKI") ? Math.PI : Math.PI));
			var pi2 = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Sin(((c.CustomerID == "ALFKI") ? Math.PI : Math.PI) / 2.0));
			Assert.AreEqual(Math.Sin(0.0), zero);
			Assert.AreEqual(Math.Sin(Math.PI), pi, 0.0001);
			Assert.AreEqual(Math.Sin(Math.PI / 2.0), pi2, 0.0001);
		}

		[TestMethod]
		public async Task MathTanAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Tan((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var pi = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Tan((c.CustomerID == "ALFKI") ? Math.PI : Math.PI));
			Assert.AreEqual(Math.Tan(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Tan(Math.PI), pi, 0.0001);
		}

		[TestMethod]
		public async Task MathExpAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Exp((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var one = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Exp((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			var two = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Exp((c.CustomerID == "ALFKI") ? 2.0 : 2.0));
			Assert.AreEqual(Math.Exp(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Exp(1.0), one, 0.0001);
			Assert.AreEqual(Math.Exp(2.0), two, 0.0001);
		}

		[TestMethod]
		public async Task MathLogAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var one = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Log((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			var e = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Log((c.CustomerID == "ALFKI") ? Math.E : Math.E));
			Assert.AreEqual(Math.Log(1.0), one, 0.0001);
			Assert.AreEqual(Math.Log(Math.E), e, 0.0001);
		}

		[TestMethod]
		public async Task MathSqrtAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var one = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			var four = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 4.0 : 4.0));
			var nine = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 9.0 : 9.0));
			Assert.AreEqual(1.0, one);
			Assert.AreEqual(2.0, four);
			Assert.AreEqual(3.0, nine);
		}

		[TestMethod]
		public async Task MathPowAsync()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 0.0));
			var one = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 1.0));
			var two = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 2.0));
			var three = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 3.0));
			Assert.AreEqual(1.0, zero);
			Assert.AreEqual(2.0, one);
			Assert.AreEqual(4.0, two);
			Assert.AreEqual(8.0, three);
		}

		[TestMethod]
		public async Task MathRoundDefaultAsync()
		{
			var four = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Round((c.CustomerID == "ALFKI") ? 3.4 : 3.4));
			var six = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Round((c.CustomerID == "ALFKI") ? 3.6 : 3.6));
			Assert.AreEqual(3.0, four);
			Assert.AreEqual(4.0, six);
		}

		[TestMethod]
		public async Task MathFloorAsync()
		{
			var four = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Floor((c.CustomerID == "ALFKI" ? 3.4 : 3.4)));
			var six = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Floor((c.CustomerID == "ALFKI" ? 3.6 : 3.6)));
			var nfour = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Floor((c.CustomerID == "ALFKI" ? -3.4 : -3.4)));
			Assert.AreEqual(Math.Floor(3.4), four);
			Assert.AreEqual(Math.Floor(3.6), six);
			Assert.AreEqual(Math.Floor(-3.4), nfour);
		}

		[TestMethod]
		public async Task DecimalFloorAsync()
		{
			var four = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Floor((c.CustomerID == "ALFKI" ? 3.4m : 3.4m)));
			var six = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Floor((c.CustomerID == "ALFKI" ? 3.6m : 3.6m)));
			var nfour = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Floor((c.CustomerID == "ALFKI" ? -3.4m : -3.4m)));
			Assert.AreEqual(decimal.Floor(3.4m), four);
			Assert.AreEqual(decimal.Floor(3.6m), six);
			Assert.AreEqual(decimal.Floor(-3.4m), nfour);
		}

		[TestMethod]
		public async Task MathTruncateAsync()
		{
			var four = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.4 : 3.4));
			var six = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.6 : 3.6));
			var neg4 = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Truncate((c.CustomerID == "ALFKI") ? -3.4 : -3.4));
			Assert.AreEqual(Math.Truncate(3.4), four);
			Assert.AreEqual(Math.Truncate(3.6), six);
			Assert.AreEqual(Math.Truncate(-3.4), neg4);
		}

		[TestMethod]
		public async Task StringCompareToAsync()
		{
			var lt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => c.City.CompareTo("Seattle"));
			var gt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => c.City.CompareTo("Aaa"));
			var eq = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => c.City.CompareTo("Berlin"));
			Assert.AreEqual(-1, lt);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(0, eq);
		}

		[TestMethod]
		public async Task StringCompareToLessThanAsync()
		{
			var cmpLT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Seattle") < 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Berlin") < 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public async Task StringCompareToLessThanOrEqualToAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Seattle") <= 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Berlin") <= 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Aaa") <= 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task StringCompareToGreaterThanAsync()
		{
			var cmpLT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Aaa") > 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Berlin") > 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public async Task StringCompareToGreaterThanOrEqualToAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Seattle") >= 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Berlin") >= 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Aaa") >= 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task StringCompareToEqualsAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Seattle") == 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Berlin") == 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Aaa") == 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task StringCompareToNotEqualsAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Seattle") != 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Berlin") != 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => c.City.CompareTo("Aaa") != 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task StringCompareAsync()
		{
			var lt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => string.Compare(c.City, "Seattle"));
			var gt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => string.Compare(c.City, "Aaa"));
			var eq = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => string.Compare(c.City, "Berlin"));
			Assert.AreEqual(-1, lt);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(0, eq);
		}

		[TestMethod]
		public async Task StringCompareLessThanAsync()
		{
			var cmpLT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Seattle") < 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Berlin") < 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public async Task StringCompareLessThanOrEqualToAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Seattle") <= 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Berlin") <= 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Aaa") <= 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task StringCompareGreaterThanAsync()
		{
			var cmpLT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Aaa") > 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Berlin") > 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public async Task StringCompareGreaterThanOrEqualToAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Seattle") >= 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Berlin") >= 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Aaa") >= 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task StringCompareEqualsAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Seattle") == 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Berlin") == 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Aaa") == 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task StringCompareNotEqualsAsync()
		{
			var cmpLE = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Seattle") != 0);
			var cmpEQ = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Berlin") != 0);
			var cmpGT = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefaultAsync(c => string.Compare(c.City, "Aaa") != 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public async Task IntCompareToAsync()
		{
			var eq = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(10));
			var gt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(9));
			var lt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(11));
			Assert.AreEqual(0, eq);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(-1, lt);
		}

		[TestMethod]
		public async Task DecimalCompareAsync()
		{
			var eq = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 10m));
			var gt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 9m));
			var lt = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 11m));
			Assert.AreEqual(0, eq);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(-1, lt);
		}

		[TestMethod]
		public async Task DecimalAddAsync()
		{
			var onetwo = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Add((c.CustomerID == "ALFKI" ? 1m : 1m), 2m));
			Assert.AreEqual(3m, onetwo);
		}

		[TestMethod]
		public async Task DecimalSubtractAsync()
		{
			var onetwo = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Subtract((c.CustomerID == "ALFKI" ? 1m : 1m), 2m));
			Assert.AreEqual(-1m, onetwo);
		}

		[TestMethod]
		public async Task DecimalMultiplyAsync()
		{
			var onetwo = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Multiply((c.CustomerID == "ALFKI" ? 1m : 1m), 2m));
			Assert.AreEqual(2m, onetwo);
		}

		[TestMethod]
		public async Task DecimalDivideAsync()
		{
			var onetwo = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Divide((c.CustomerID == "ALFKI" ? 1.0m : 1.0m), 2.0m));
			Assert.AreEqual(0.5m, onetwo);
		}

		[TestMethod]
		public async Task DecimalNegateAsync()
		{
			var one = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Negate((c.CustomerID == "ALFKI" ? 1m : 1m)));
			Assert.AreEqual(-1m, one);
		}

		[TestMethod]
		public async Task DecimalRoundDefaultAsync()
		{
			var four = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Round((c.CustomerID == "ALFKI" ? 3.4m : 3.4m)));
			var six = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Round((c.CustomerID == "ALFKI" ? 3.5m : 3.5m)));
			Assert.AreEqual(3.0m, four);
			Assert.AreEqual(4.0m, six);
		}

		[TestMethod]
		public async Task DecimalTruncateAsync()
		{
			var four = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => decimal.Truncate((c.CustomerID == "ALFKI") ? 3.4m : 3.4m));
			var six = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.6m : 3.6m));
			var neg4 = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => Math.Truncate((c.CustomerID == "ALFKI") ? -3.4m : -3.4m));
			Assert.AreEqual(decimal.Truncate(3.4m), four);
			Assert.AreEqual(decimal.Truncate(3.6m), six);
			Assert.AreEqual(decimal.Truncate(-3.4m), neg4);
		}

		[TestMethod]
		public async Task DecimalLessThanAsync()
		{
			var alfki = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1.0m : 3.0m) < 2.0m);
			Assert.AreNotEqual(null, alfki);
		}

		[TestMethod]
		public async Task IntLessThanAsync()
		{
			var alfki = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) < 2);
			var alfkiN = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) < 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public async Task IntLessThanOrEqualAsync()
		{
			var alfki = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) <= 2);
			var alfki2 = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 2 : 3) <= 2);
			var alfkiN = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) <= 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreNotEqual(null, alfki2);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public async Task IntGreaterThanAsync()
		{
			var alfki = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) > 2);
			var alfkiN = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) > 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public async Task IntGreaterThanOrEqualAsync()
		{
			var alfki = await _db.Customers.SingleAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) >= 2);
			var alfki2 = await _db.Customers.SingleAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 2) >= 2);
			var alfkiN = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) > 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreNotEqual(null, alfki2);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public async Task IntEqualAsync()
		{
			var alfki = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 1) == 1);
			var alfkiN = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 1) == 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public async Task IntNotEqualAsync()
		{
			var alfki = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 2 : 2) != 1);
			var alfkiN = await _db.Customers.SingleOrDefaultAsync(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 2 : 2) != 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public async Task IntAddAsync()
		{
			var three = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 1 : 1) + 2);
			Assert.AreEqual(3, three);
		}

		[TestMethod]
		public async Task IntSubtractAsync()
		{
			var negone = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 1 : 1) - 2);
			Assert.AreEqual(-1, negone);
		}

		[TestMethod]
		public async Task IntMultiplyAsync()
		{
			var six = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 2 : 2) * 3);
			Assert.AreEqual(6, six);
		}

		[TestMethod]
		public async Task IntDivideAsync()
		{
			var one = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 3 : 3) / 2);
			Assert.AreEqual(1, one);
		}

		[TestMethod]
		public async Task IntModuloAsync()
		{
			var three = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 7 : 7) % 4);
			Assert.AreEqual(3, three);
		}

		[TestMethod]
		public async Task IntLeftShiftAsync()
		{
			var eight = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 1 : 1) << 3);
			Assert.AreEqual(8, eight);
		}

		[TestMethod]
		public async Task IntRightShiftAsync()
		{
			var eight = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 32 : 32) >> 2);
			Assert.AreEqual(8, eight);
		}

		[TestMethod]
		public async Task IntBitwiseAndAsync()
		{
			var band = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 6 : 6) & 3);
			Assert.AreEqual(2, band);
		}

		[TestMethod]
		public async Task IntBitwiseOrAsync()
		{
			var eleven = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 10 : 10) | 3);
			Assert.AreEqual(11, eleven);
		}

		[TestMethod]
		public async Task IntBitwiseExclusiveOrAsync()
		{
			var zero = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ((c.CustomerID == "ALFKI") ? 1 : 1) ^ 1);
			Assert.AreEqual(0, zero);
		}

		////[TestMethod]
		////public async Task IntBitwiseNotAsync()
		////{
		////	var bneg = db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => ~((c.CustomerID == "ALFKI") ? -1 : -1));
		////	Assert.AreEqual(~-1, bneg);
		////}

		[TestMethod]
		public async Task IntNegateAsync()
		{
			var neg = await _db.Customers.Where(c => c.CustomerID == "ALFKI").SumAsync(c => -((c.CustomerID == "ALFKI") ? 1 : 1));
			Assert.AreEqual(-1, neg);
		}

		[TestMethod]
		public async Task AndAsync()
		{
			var custs = await _db.Customers.Where(c => c.Country == "USA" && c.City.StartsWith("A")).Select(c => c.City).ToListAsync();
			Assert.AreEqual(2, custs.Count);
			Assert.IsTrue(custs.All(c => c.StartsWith("A")));
		}

		[TestMethod]
		public async Task OrAsync()
		{
			var custs = await _db.Customers.Where(c => c.Country == "USA" || c.City.StartsWith("A")).Select(c => c.City).ToListAsync();
			Assert.AreEqual(14, custs.Count);
		}

		[TestMethod]
		public async Task NotAsync()
		{
			var custs = await _db.Customers.Where(c => !(c.Country == "USA")).Select(c => c.Country).ToListAsync();
			Assert.AreEqual(78, custs.Count);
		}

		////[TestMethod]
		////public async Task EqualLiteralNullAsync()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => x == null);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).ContainsAsync("IS NULL"));
		////	var n = q.CountAsync();
		////	Assert.AreEqual(1, n);
		////}

		////[TestMethod]
		////public async Task EqualLiteralNullReversedAsync()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => null == x);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).ContainsAsync("IS NULL"));
		////	var n = q.CountAsync();
		////	Assert.AreEqual(1, n);
		////}

		////[TestMethod]
		////public async Task NotEqualLiteralNullAsync()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => x != null);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).ContainsAsync("IS NOT NULL"));
		////	var n = q.CountAsync();
		////	Assert.AreEqual(90, n);
		////}

		////[TestMethod]
		////public async Task NotEqualLiteralNullReversedAsync()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => null != x);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).ContainsAsync("IS NOT NULL"));
		////	var n = q.CountAsync();
		////	Assert.AreEqual(90, n);
		////}

		////[TestMethod]
		////public async Task ConditionalResultsArePredicatesAsync()
		////{
		////	bool value = db.Orders.Where(c => c.CustomerID == "ALFKI").MaxAsync(c => (c.CustomerID == "ALFKI" ? string.Compare(c.CustomerID, "POTATO") < 0 : string.Compare(c.CustomerID, "POTATO") > 0));
		////	Assert.IsTrue(value);
		////}

		////[TestMethod]
		////public async Task SelectManyJoinedAsync()
		////{
		////	var cods =
		////		(from c in db.Customers
		////		 from o in db.Orders.Where(o => o.CustomerID == c.CustomerID)
		////		 select new { c.ContactName, o.OrderDate }).ToListAsync();
		////	Assert.AreEqual(830, cods.CountAsync);
		////}

		////[TestMethod]
		////public async Task SelectManyJoinedDefaultIfEmptyAsync()
		////{
		////	var cods = (
		////		from c in db.Customers
		////		from o in db.Orders.Where(o => o.CustomerID == c.CustomerID).DefaultIfEmpty()
		////		select new { c.ContactName, o.OrderDate }
		////		).ToListAsync();
		////	Assert.AreEqual(832, cods.CountAsync);
		////}

		[TestMethod]
		public async Task SelectWhereAssociationAsync()
		{
			var ords = await (
				from o in _db.Orders
				where o.Customer.City == "Seattle"
				select o
				).ToListAsync();
			Assert.AreEqual(14, ords.Count);
		}

		[TestMethod]
		public async Task SelectWhereAssociationTwiceAsync()
		{
			var n = await _db.Orders.Where(c => c.CustomerID == "WHITC").CountAsync();
			var ords = await (
				from o in _db.Orders
				where o.Customer.Country == "USA" && o.Customer.City == "Seattle"
				select o
				).ToListAsync();
			Assert.AreEqual(n, ords.Count);
		}

		[TestMethod]
		public async Task SelectAssociationAsync()
		{
			var custs = await (
				from o in _db.Orders
				where o.CustomerID == "ALFKI"
				select o.Customer
				).ToListAsync();
			Assert.AreEqual(6, custs.Count);
			Assert.IsTrue(custs.All(c => c.CustomerID == "ALFKI"));
		}

		////[TestMethod]
		////public async Task SelectAssociationsAsync()
		////{
		////	var doubleCusts = (
		////		from o in db.Orders
		////		where o.CustomerID == "ALFKI"
		////		select new { A = o.Customer, B = o.Customer }
		////		).ToListAsync();

		////	Assert.AreEqual(6, doubleCusts.CountAsync);
		////	Assert.IsTrue(doubleCusts.AllAsync(c => c.A.CustomerID == "ALFKI" && c.B.CustomerID == "ALFKI"));
		////}

		////[TestMethod]
		////public async Task SelectAssociationsWhereAssociationsAsync()
		////{
		////	var stuff = (
		////		from o in db.Orders
		////		where o.Customer.Country == "USA"
		////		where o.Customer.City != "Seattle"
		////		select new { A = o.Customer, B = o.Customer }
		////		).ToListAsync();
		////	Assert.AreEqual(108, stuff.CountAsync);
		////}
	}
}

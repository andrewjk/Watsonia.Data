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
	public class QueryExecutionTests
	{
		private static readonly NorthwindDatabase _db = new NorthwindDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
		}
		
		////[TestMethod]
		////public void Thing()
		////{
		////	// TODO:
		////	//Customer c = db.Customers.FirstOrDefault();
		////	//TestQuery(
		////	//	db.Orders.Where(o => o.Customer == c));
		////}

		[TestMethod]
		public void Where()
		{
			var list = _db.Customers.Where(c => c.City == "London").ToList();
			Assert.AreEqual(6, list.Count);
		}

		[TestMethod]
		public void WhereTrue()
		{
			var list = _db.Customers.Where(c => true).ToList();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public void CompareEntityEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			var list = _db.Customers.Where(c => c == alfki).ToList();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("ALFKI", list[0].CustomerID);
		}

		[TestMethod]
		public void CompareEntityNotEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			var list = _db.Customers.Where(c => c != alfki).ToList();
			Assert.AreEqual(90, list.Count);
		}

		////[TestMethod]
		////public void CompareConstructedEqual()
		////{
		////	var list = db.Customers.Where(c => new { x = c.City } == new { x = "London" }).ToList();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public void CompareConstructedMultiValueEqual()
		////{
		////	var list = db.Customers.Where(c => new { x = c.City, y = c.Country } == new { x = "London", y = "UK" }).ToList();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public void CompareConstructedMultiValueNotEqual()
		////{
		////	var list = db.Customers.Where(c => new { x = c.City, y = c.Country } != new { x = "London", y = "UK" }).ToList();
		////	Assert.AreEqual(85, list.Count);
		////}

		[TestMethod]
		public void SelectScalar()
		{
			var list = _db.Customers.Where(c => c.City == "London").Select(c => c.City).ToList();
			Assert.AreEqual(6, list.Count);
			Assert.AreEqual("London", list[0]);
			Assert.IsTrue(list.All(x => x == "London"));
		}

		[TestMethod]
		public void SelectAnonymousOne()
		{
			var list = _db.Customers.Where(c => c.City == "London").Select(c => new { c.City }).ToList();
			Assert.AreEqual(6, list.Count);
			Assert.AreEqual("London", list[0].City);
			Assert.IsTrue(list.All(x => x.City == "London"));
		}

		[TestMethod]
		public void SelectAnonymousTwo()
		{
			var list = _db.Customers.Where(c => c.City == "London").Select(c => new { c.City, c.Phone }).ToList();
			Assert.AreEqual(6, list.Count);
			Assert.AreEqual("London", list[0].City);
			Assert.IsTrue(list.All(x => x.City == "London"));
			Assert.IsTrue(list.All(x => x.Phone != null));
		}

		[TestMethod]
		public void SelectCustomerTable()
		{
			var list = _db.Customers.ToList();
			Assert.AreEqual(91, list.Count);
		}

		////[TestMethod]
		////public void SelectAnonymousWithObject()
		////{
		////	var list = db.Customers.Where(c => c.City == "London").Select(c => new { c.City, c }).ToList();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.AreEqual("London", list[0].City);
		////	Assert.IsTrue(list.All(x => x.City == "London"));
		////	Assert.IsTrue(list.All(x => x.c.City == x.City));
		////}

		////[TestMethod]
		////public void SelectAnonymousLiteral()
		////{
		////	var list = db.Customers.Where(c => c.City == "London").Select(c => new { X = 10 }).ToList();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.IsTrue(list.All(x => x.X == 10));
		////}

		[TestMethod]
		public void SelectConstantInt()
		{
			var list = _db.Customers.Select(c => 10).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(list.All(x => x == 10));
		}

		[TestMethod]
		public void SelectConstantNullString()
		{
			var list = _db.Customers.Select(c => (string)null).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(list.All(x => x == null));
		}

		[TestMethod]
		public void SelectLocal()
		{
			var x = 10;
			var list = _db.Customers.Select(c => x).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(list.All(y => y == 10));
		}

		////[TestMethod]
		////public void SelectNestedCollection()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		select db.Orders.Where(o => o.CustomerID == c.CustomerID).Select(o => o.OrderID)
		////		).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Count());
		////}

		////[TestMethod]
		////public void SelectNestedCollectionInAnonymousType()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		select new { Foos = db.Orders.Where(o => o.CustomerID == c.CustomerID).Select(o => o.OrderID).ToList() }
		////		).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Foos.Count);
		////}

		[TestMethod]
		public void JoinCustomerOrders()
		{
			var list = (
				from c in _db.Customers
				where c.CustomerID == "ALFKI"
				join o in _db.Orders on c.CustomerID equals o.CustomerID
				select new { c.ContactName, o.OrderID }
				).ToList();
			Assert.AreEqual(6, list.Count);
		}

		////[TestMethod]
		////public void JoinMultiKey()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		join o in db.Orders on new { a = c.CustomerID, b = c.CustomerID } equals new { a = o.CustomerID, b = o.CustomerID }
		////		select new { c, o }
		////		).ToList();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public void JoinIntoCustomersOrdersCount()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		select new { cust = c, ords = ords.Count() }
		////		).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].ords);
		////}

		////[TestMethod]
		////public void JoinIntoDefaultIfEmpty()
		////{
		////	var list = (
		////		from c in db.Customers
		////		where c.CustomerID == "PARIS"
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		from o in ords.DefaultIfEmpty()
		////		select new { c, o }
		////		).ToList();

		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(null, list[0].o);
		////}

		////[TestMethod]
		////public void MultipleJoinsWithJoinConditionsInWhere()
		////{
		////	var list = (
		////		from c in db.Customers
		////		from o in db.Orders
		////		from d in db.OrderDetails
		////		where o.CustomerID == c.CustomerID && o.OrderID == d.OrderID
		////		where c.CustomerID == "ALFKI"
		////		select d
		////		).ToList();

		////	Assert.AreEqual(12, list.Count);
		////}

		////[TestMethod]
		////public void MultipleJoinsWithMissingJoinCondition()
		////{
		////	var list = (
		////		from c in db.Customers
		////		from o in db.Orders
		////		from d in db.OrderDetails
		////		where o.CustomerID == c.CustomerID /*&& o.OrderID == d.OrderID*/
		////		where c.CustomerID == "ALFKI"
		////		select d
		////		).ToList();

		////	Assert.AreEqual(12930, list.Count);
		////}

		[TestMethod]
		public void OrderBy()
		{
			var list = _db.Customers.OrderBy(c => c.CustomerID).Select(c => c.CustomerID).ToList();
			var sorted = list.OrderBy(c => c).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		////[TestMethod]
		////public void OrderByOrderBy()
		////{
		////	var list = db.Customers.OrderBy(c => c.Phone).OrderBy(c => c.CustomerID).ToList();
		////	var sorted = list.OrderBy(c => c.CustomerID).ToList();
		////	Assert.AreEqual(91, list.Count);
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		[TestMethod]
		public void OrderByThenBy()
		{
			var list = _db.Customers.OrderBy(c => c.CustomerID).ThenBy(c => c.Phone).ToList();
			var sorted = list.OrderBy(c => c.CustomerID).ThenBy(c => c.Phone).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		[TestMethod]
		public void OrderByDescending()
		{
			var list = _db.Customers.OrderByDescending(c => c.CustomerID).ToList();
			var sorted = list.OrderByDescending(c => c.CustomerID).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		[TestMethod]
		public void OrderByDescendingThenBy()
		{
			var list = _db.Customers.OrderByDescending(c => c.CustomerID).ThenBy(c => c.Country).ToList();
			var sorted = list.OrderByDescending(c => c.CustomerID).ThenBy(c => c.Country).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		[TestMethod]
		public void OrderByDescendingThenByDescending()
		{
			var list = _db.Customers.OrderByDescending(c => c.CustomerID).ThenByDescending(c => c.Country).ToList();
			var sorted = list.OrderByDescending(c => c.CustomerID).ThenByDescending(c => c.Country).ToList();
			Assert.AreEqual(91, list.Count);
			Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		}

		////[TestMethod]
		////public void OrderByJoin()
		////{
		////	var list = (
		////		from c in db.Customers.OrderBy(c => c.CustomerID)
		////		join o in db.Orders.OrderBy(o => o.OrderID) on c.CustomerID equals o.CustomerID
		////		select new { CustomerID = c.CustomerID, o.OrderID }
		////		).ToList();

		////	var sorted = list.OrderBy(x => x.CustomerID).ThenBy(x => x.OrderID);
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		////[TestMethod]
		////public void OrderBySelectMany()
		////{
		////	var list = (
		////		from c in db.Customers.OrderBy(c => c.CustomerID)
		////		from o in db.Orders.OrderBy(o => o.OrderID)
		////		where c.CustomerID == o.CustomerID
		////		select new { CustomerID = c.CustomerID, o.OrderID }
		////		).ToList();
		////	var sorted = list.OrderBy(x => x.CustomerID).ThenBy(x => x.OrderID).ToList();
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		////[TestMethod]
		////public void CountProperty()
		////{
		////	var list = db.Customers.Where(c => c.Orders.Count > 0).ToList();
		////	Assert.AreEqual(89, list.Count);
		////}

		////[TestMethod]
		////public void GroupBy()
		////{
		////	var list = db.Customers.GroupBy(c => c.City).ToList();
		////	Assert.AreEqual(69, list.Count);
		////}

		////[TestMethod]
		////public void GroupByOne()
		////{
		////	var list = db.Customers.Where(c => c.City == "London").GroupBy(c => c.City).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Count());
		////}

		////[TestMethod]
		////public void GroupBySelectMany()
		////{
		////	var list = db.Customers.GroupBy(c => c.City).SelectMany(g => g).ToList();
		////	Assert.AreEqual(91, list.Count);
		////}

		////[TestMethod]
		////public void GroupBySum()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g => g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1))).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public void GroupByCount()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g => g.Count()).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public void GroupByLongCount()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g => g.LongCount()).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6L, list[0]);
		////}

		////[TestMethod]
		////public void GroupBySumMinMaxAvg()
		////{
		////	var list =
		////		db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Select(g =>
		////			new
		////			{
		////				Sum = g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1)),
		////				Min = g.Min(o => o.OrderID),
		////				Max = g.Max(o => o.OrderID),
		////				Avg = g.Average(o => o.OrderID)
		////			}).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Sum);
		////}

		////[TestMethod]
		////public void GroupByWithResultSelector()
		////{
		////	var list =
		////		db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, (k, g) =>
		////			new
		////			{
		////				Sum = g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1)),
		////				Min = g.Min(o => o.OrderID),
		////				Max = g.Max(o => o.OrderID),
		////				Avg = g.Average(o => o.OrderID)
		////			}).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Sum);
		////}

		////[TestMethod]
		////public void GroupByWithElementSelectorSum()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1)).Select(g => g.Sum()).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public void GroupByWithElementSelector()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1)).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Count());
		////	Assert.AreEqual(6, list[0].Sum());
		////}

		////[TestMethod]
		////public void GroupByWithElementSelectorSumMax()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => (o.CustomerID == "ALFKI" ? 1 : 1)).Select(g => new { Sum = g.Sum(), Max = g.Max() }).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0].Sum);
		////	Assert.AreEqual(1, list[0].Max);
		////}

		////[TestMethod]
		////public void GroupByWithAnonymousElement()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID, o => new { X = (o.CustomerID == "ALFKI" ? 1 : 1) }).Select(g => g.Sum(x => x.X)).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(6, list[0]);
		////}

		////[TestMethod]
		////public void GroupByWithTwoPartKey()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => new { CustomerID = o.CustomerID, o.OrderDate }).Select(g => g.Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1))).ToList();
		////	Assert.AreEqual(6, list.Count);
		////}

		////[TestMethod]
		////public void GroupByWithCountInWhere()
		////{
		////	var list = db.Customers.Where(a => a.Orders.Count() > 15).GroupBy(a => a.City).ToList();
		////	Assert.AreEqual(9, list.Count);
		////}

		////[TestMethod]
		////public void OrderByGroupBy()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).ToList();
		////	Assert.AreEqual(1, list.Count);
		////	var grp = list[0].ToList();
		////	var sorted = grp.OrderBy(o => o.OrderID);
		////	Assert.IsTrue(Enumerable.SequenceEqual(grp, sorted));
		////}

		////[TestMethod]
		////public void OrderByGroupBySelectMany()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).SelectMany(g => g).ToList();
		////	Assert.AreEqual(6, list.Count);
		////	var sorted = list.OrderBy(o => o.OrderID).ToList();
		////	Assert.IsTrue(Enumerable.SequenceEqual(list, sorted));
		////}

		[TestMethod]
		public void SumWithNoArg()
		{
			var sum = _db.Orders.Where(o => o.CustomerID == "ALFKI").Select(o => (o.CustomerID == "ALFKI" ? 1 : 1)).Sum();
			Assert.AreEqual(6, sum);
		}

		[TestMethod]
		public void SumWithArg()
		{
			var sum = _db.Orders.Where(o => o.CustomerID == "ALFKI").Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1));
			Assert.AreEqual(6, sum);
		}

		[TestMethod]
		public void CountWithNoPredicate()
		{
			var cnt = _db.Orders.Count();
			Assert.AreEqual(830, cnt);
		}

		[TestMethod]
		public void CountWithPredicate()
		{
			var cnt = _db.Orders.Count(o => o.CustomerID == "ALFKI");
			Assert.AreEqual(6, cnt);
		}

		[TestMethod]
		public void DistinctNoDupes()
		{
			var list = _db.Customers.Distinct().ToList();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public void DistinctScalar()
		{
			var list = _db.Customers.Select(c => c.City).Distinct().ToList();
			Assert.AreEqual(69, list.Count);
		}

		[TestMethod]
		public void OrderByDistinct()
		{
			var list = _db.Customers.Where(c => c.City.StartsWith("P")).OrderBy(c => c.City).Select(c => c.City).Distinct().ToList();
			var sorted = list.OrderBy(x => x).ToList();
			Assert.AreEqual(list[0], sorted[0]);
			Assert.AreEqual(list[^1], sorted[list.Count - 1]);
		}

		////[TestMethod]
		////public void DistinctOrderBy()
		////{
		////	var list = db.Customers.Where(c => c.City.StartsWith("P")).Select(c => c.City).Distinct().OrderBy(c => c).ToList();
		////	var sorted = list.OrderBy(x => x).ToList();
		////	Assert.AreEqual(list[0], sorted[0]);
		////	Assert.AreEqual(list[list.Count - 1], sorted[list.Count - 1]);
		////}

		////[TestMethod]
		////public void DistinctGroupBy()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").Distinct().GroupBy(o => o.CustomerID).ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public void GroupByDistinct()
		////{
		////	var list = db.Orders.Where(o => o.CustomerID == "ALFKI").GroupBy(o => o.CustomerID).Distinct().ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		[TestMethod]
		public void DistinctCount()
		{
			var cnt = _db.Customers.Distinct().Count();
			Assert.AreEqual(91, cnt);
		}

		////[TestMethod]
		////public void SelectDistinctCount()
		////{
		////	var cnt = db.Customers.Select(c => c.City).Distinct().Count();
		////	Assert.AreEqual(69, cnt);
		////}

		////[TestMethod]
		////public void SelectSelectDistinctCount()
		////{
		////	var cnt = db.Customers.Select(c => c.City).Select(c => c).Distinct().Count();
		////	Assert.AreEqual(69, cnt);
		////}

		////[TestMethod]
		////public void DistinctCountPredicate()
		////{
		////	var cnt = db.Customers.Select(c => new { c.City, c.Country }).Distinct().Count(c => c.City == "London");
		////	Assert.AreEqual(1, cnt);
		////}

		////[TestMethod]
		////public void DistinctSumWithArg()
		////{
		////	var sum = db.Orders.Where(o => o.CustomerID == "ALFKI").Distinct().Sum(o => (o.CustomerID == "ALFKI" ? 1 : 1));
		////	Assert.AreEqual(6, sum);
		////}

		[TestMethod]
		public void SelectDistinctSum()
		{
			var sum = _db.Orders.Where(o => o.CustomerID == "ALFKI").Select(o => o.OrderID).Distinct().Sum();
			Assert.AreEqual(64835, sum);
		}

		[TestMethod]
		public void Take()
		{
			var list = _db.Orders.Take(5).ToList();
			Assert.AreEqual(5, list.Count);
		}

		////[TestMethod]
		////public void TakeDistinct()
		////{
		////	var list = db.Orders.OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Take(5).Distinct().ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		[TestMethod]
		public void DistinctTake()
		{
			var list = _db.Orders.OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Distinct().Take(5).ToList();
			Assert.AreEqual(5, list.Count);
		}

		////[TestMethod]
		////public void DistinctTakeCount()
		////{
		////	var cnt = db.Orders.Distinct().OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Take(5).Count();
		////	Assert.AreEqual(5, cnt);
		////}

		////[TestMethod]
		////public void TakeDistinctCount()
		////{
		////	var cnt = db.Orders.OrderBy(o => o.CustomerID).Select(o => o.CustomerID).Take(5).Distinct().Count();
		////	Assert.AreEqual(1, cnt);
		////}

		[TestMethod]
		public void First()
		{
			var first = _db.Customers.OrderBy(c => c.ContactName).First();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("ROMEY", first.CustomerID);
		}

		[TestMethod]
		public void FirstPredicate()
		{
			var first = _db.Customers.OrderBy(c => c.ContactName).First(c => c.City == "London");
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public void WhereFirst()
		{
			var first = _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").First();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public void FirstOrDefault()
		{
			var first = _db.Customers.OrderBy(c => c.ContactName).FirstOrDefault();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("ROMEY", first.CustomerID);
		}

		[TestMethod]
		public void FirstOrDefaultPredicate()
		{
			var first = _db.Customers.OrderBy(c => c.ContactName).FirstOrDefault(c => c.City == "London");
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public void WhereFirstOrDefault()
		{
			var first = _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").FirstOrDefault();
			Assert.AreNotEqual(null, first);
			Assert.AreEqual("EASTC", first.CustomerID);
		}

		[TestMethod]
		public void FirstOrDefaultPredicateNoMatch()
		{
			var first = _db.Customers.OrderBy(c => c.ContactName).FirstOrDefault(c => c.City == "SpongeBob");
			Assert.AreEqual(null, first);
		}

		[TestMethod]
		public void Reverse()
		{
			var list = _db.Customers.OrderBy(c => c.ContactName).Reverse().ToList();
			Assert.AreEqual(91, list.Count);
			Assert.AreEqual("WOLZA", list[0].CustomerID);
			Assert.AreEqual("ROMEY", list[90].CustomerID);
		}

		[TestMethod]
		public void ReverseReverse()
		{
			var list = _db.Customers.OrderBy(c => c.ContactName).Reverse().Reverse().ToList();
			Assert.AreEqual(91, list.Count);
			Assert.AreEqual("ROMEY", list[0].CustomerID);
			Assert.AreEqual("WOLZA", list[90].CustomerID);
		}

		////[TestMethod]
		////public void ReverseWhereReverse()
		////{
		////	var list = db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Reverse().ToList();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.AreEqual("EASTC", list[0].CustomerID);
		////	Assert.AreEqual("BSBEV", list[5].CustomerID);
		////}

		////[TestMethod]
		////public void ReverseTakeReverse()
		////{
		////	var list = db.Customers.OrderBy(c => c.ContactName).Reverse().Take(5).Reverse().ToList();
		////	Assert.AreEqual(5, list.Count);
		////	Assert.AreEqual("CHOPS", list[0].CustomerID);
		////	Assert.AreEqual("WOLZA", list[4].CustomerID);
		////}

		////[TestMethod]
		////public void ReverseWhereTakeReverse()
		////{
		////	var list = db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Take(5).Reverse().ToList();
		////	Assert.AreEqual(5, list.Count);
		////	Assert.AreEqual("CONSH", list[0].CustomerID);
		////	Assert.AreEqual("BSBEV", list[4].CustomerID);
		////}

		[TestMethod]
		public void Last()
		{
			var last = _db.Customers.OrderBy(c => c.ContactName).Last();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("WOLZA", last.CustomerID);
		}

		[TestMethod]
		public void LastPredicate()
		{
			var last = _db.Customers.OrderBy(c => c.ContactName).Last(c => c.City == "London");
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public void WhereLast()
		{
			var last = _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").Last();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public void LastOrDefault()
		{
			var last = _db.Customers.OrderBy(c => c.ContactName).LastOrDefault();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("WOLZA", last.CustomerID);
		}

		[TestMethod]
		public void LastOrDefaultPredicate()
		{
			var last = _db.Customers.OrderBy(c => c.ContactName).LastOrDefault(c => c.City == "London");
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public void WhereLastOrDefault()
		{
			var last = _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").LastOrDefault();
			Assert.AreNotEqual(null, last);
			Assert.AreEqual("BSBEV", last.CustomerID);
		}

		[TestMethod]
		public void LastOrDefaultNoMatches()
		{
			var last = _db.Customers.OrderBy(c => c.ContactName).LastOrDefault(c => c.City == "SpongeBob");
			Assert.AreEqual(null, last);
		}

		////[TestMethod]
		////public void SingleFails()
		////{
		////	var single = db.Customers.Single();
		////}

		[TestMethod]
		public void SinglePredicate()
		{
			var single = _db.Customers.Single(c => c.CustomerID == "ALFKI");
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		[TestMethod]
		public void WhereSingle()
		{
			var single = _db.Customers.Where(c => c.CustomerID == "ALFKI").Single();
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		////[TestMethod]
		////public void SingleOrDefaultFails()
		////{
		////	var single = db.Customers.SingleOrDefault();
		////}

		[TestMethod]
		public void SingleOrDefaultPredicate()
		{
			var single = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI");
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		[TestMethod]
		public void WhereSingleOrDefault()
		{
			var single = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault();
			Assert.AreNotEqual(null, single);
			Assert.AreEqual("ALFKI", single.CustomerID);
		}

		////[TestMethod]
		////public void SingleOrDefaultNoMatches()
		////{
		////	var single = db.Customers.SingleOrDefault(c => c.CustomerID == "SpongeBob");
		////	Assert.AreEqual(null, single);
		////}

		[TestMethod]
		public void AnyTopLevel()
		{
			var any = _db.Customers.Any();
			Assert.IsTrue(any);
		}

		////[TestMethod]
		////public void AnyWithSubquery()
		////{
		////	bool what = db.Customers.Any(c => c.CustomerID == "ALFKI");
		////	var list = db.Customers.Where(c => c.Orders.Any(o => o.CustomerID == "ALFKI")).ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public void AnyWithSubqueryNoPredicate()
		////{
		////	var list = db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).Any()).ToList();
		////	Assert.AreEqual(89, list.Count);
		////}

		////[TestMethod]
		////public void AnyWithLocalCollection()
		////{
		////	string[] ids = new[] { "ALFKI", "WOLZA", "NOONE" };
		////	var list = db.Customers.Where(c => ids.Any(id => c.CustomerID == id)).ToList();
		////	Assert.AreEqual(2, list.Count);
		////}

		////[TestMethod]
		////public void AllWithSubquery()
		////{
		////	var list = db.Customers.Where(c => c.Orders.All(o => o.CustomerID == "ALFKI")).ToList();
		////	Assert.AreEqual(3, list.Count);
		////}

		////[TestMethod]
		////public void AllWithLocalCollection()
		////{
		////	string[] patterns = new[] { "m", "d" };

		////	var list = db.Customers.Where(c => patterns.All(p => c.ContactName.Contains(p))).Select(c => c.ContactName).ToList();
		////	var local = db.Customers.AsEnumerable().Where(c => patterns.All(p => c.ContactName.ToLower().Contains(p))).Select(c => c.ContactName).ToList();

		////	Assert.AreEqual(local.Count, list.Count);
		////}

		[TestMethod]
		public void AllTopLevel()
		{
			var all = _db.Customers.All(c => c.ContactName.Length > 0);
			Assert.IsTrue(all);
		}

		[TestMethod]
		public void AllTopLevelNoMatches()
		{
			var all = _db.Customers.All(c => c.ContactName.Contains("a"));
			Assert.IsFalse(all);
		}

		[TestMethod]
		public void ContainsWithSubquery()
		{
			var list = _db.Customers.Where(c => _db.Orders.Select(o => o.CustomerID).Contains(c.CustomerID)).ToList();
			Assert.AreEqual(85, list.Count);
		}

		[TestMethod]
		public void ContainsWithLocalCollection()
		{
			var ids = new[] { "ALFKI", "WOLZA", "NOONE" };
			var list = _db.Customers.Where(c => ids.Contains(c.CustomerID)).ToList();
			Assert.AreEqual(2, list.Count);
		}

		[TestMethod]
		public void ContainsTopLevel()
		{
			var contains = _db.Customers.Select(c => c.CustomerID).Contains("ALFKI");
			Assert.IsTrue(contains);
		}

		////[TestMethod]
		////public void SkipTake()
		////{
		////	var list = db.Customers.OrderBy(c => c.CustomerID).Skip(5).Take(10).ToList();
		////	Assert.AreEqual(10, list.Count);
		////	Assert.AreEqual("BLAUS", list[0].CustomerID);
		////	Assert.AreEqual("COMMI", list[9].CustomerID);
		////}

		////[TestMethod]
		////public void DistinctSkipTake()
		////{
		////	var list = db.Customers.Select(c => c.City).Distinct().OrderBy(c => c).Skip(5).Take(10).ToList();
		////	Assert.AreEqual(10, list.Count);
		////	var hs = new HashSet<string>(list);
		////	Assert.AreEqual(10, hs.Count);
		////}

		////[TestMethod]
		////public void Coalesce()
		////{
		////	var list = db.Customers.Select(c => new { City = (c.City == "London" ? null : c.City), Country = (c.CustomerID == "EASTC" ? null : c.Country) })
		////				 .Where(x => (x.City ?? "NoCity") == "NoCity").ToList();
		////	Assert.AreEqual(6, list.Count);
		////	Assert.AreEqual(null, list[0].City);
		////}

		////[TestMethod]
		////public void Coalesce2()
		////{
		////	var list = db.Customers.Select(c => new { City = (c.City == "London" ? null : c.City), Country = (c.CustomerID == "EASTC" ? null : c.Country) })
		////				 .Where(x => (x.City ?? x.Country ?? "NoCityOrCountry") == "NoCityOrCountry").ToList();
		////	Assert.AreEqual(1, list.Count);
		////	Assert.AreEqual(null, list[0].City);
		////	Assert.AreEqual(null, list[0].Country);
		////}

		[TestMethod]
		public void StringLength()
		{
			var list = _db.Customers.Where(c => c.City.Length == 7).ToList();
			Assert.AreEqual(9, list.Count);
		}

		[TestMethod]
		public void StringStartsWithLiteral()
		{
			var list = _db.Customers.Where(c => c.ContactName.StartsWith("M")).ToList();
			Assert.AreEqual(12, list.Count);
		}

		[TestMethod]
		public void StringStartsWithColumn()
		{
			var list = _db.Customers.Where(c => c.ContactName.StartsWith(c.ContactName)).ToList();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public void StringEndsWithLiteral()
		{
			var list = _db.Customers.Where(c => c.ContactName.EndsWith("s")).ToList();
			Assert.AreEqual(9, list.Count);
		}

		[TestMethod]
		public void StringEndsWithColumn()
		{
			var list = _db.Customers.Where(c => c.ContactName.EndsWith(c.ContactName)).ToList();
			Assert.AreEqual(91, list.Count);
		}

		[TestMethod]
		public void StringContainsLiteral()
		{
			var list = _db.Customers.Where(c => c.ContactName.Contains("nd")).Select(c => c.ContactName).ToList();
			var local = _db.Customers.AsEnumerable().Where(c => c.ContactName.ToLower().Contains("nd")).Select(c => c.ContactName).ToList();
			Assert.AreEqual(local.Count, list.Count);
		}

		[TestMethod]
		public void StringContainsColumn()
		{
			var list = _db.Customers.Where(c => c.ContactName.Contains(c.ContactName)).ToList();
			Assert.AreEqual(91, list.Count);
		}

		////[TestMethod]
		////public void StringConcatImplicit2Args()
		////{
		////	var list = db.Customers.Where(c => c.ContactName + "X" == "Maria AndersX").ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public void StringConcatExplicit2Args()
		////{
		////	var list = db.Customers.Where(c => string.Concat(c.ContactName, "X") == "Maria AndersX").ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public void StringConcatExplicit3Args()
		////{
		////	var list = db.Customers.Where(c => string.Concat(c.ContactName, "X", c.Country) == "Maria AndersXGermany").ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		////[TestMethod]
		////public void StringConcatExplicitNArgs()
		////{
		////	var list = db.Customers.Where(c => string.Concat(new string[] { c.ContactName, "X", c.Country }) == "Maria AndersXGermany").ToList();
		////	Assert.AreEqual(1, list.Count);
		////}

		[TestMethod]
		public void StringIsNullOrEmpty()
		{
			var list = _db.Customers.Select(c => c.City == "London" ? null : c.CustomerID).Where(x => string.IsNullOrEmpty(x)).ToList();
			Assert.AreEqual(6, list.Count);
		}

		////[TestMethod]
		////public void StringToUpper()
		////{
		////	var str = db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => (c.CustomerID == "ALFKI" ? "abc" : "abc").ToUpper());
		////	Assert.AreEqual("ABC", str);
		////}

		////[TestMethod]
		////public void StringToLower()
		////{
		////	var str = db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => (c.CustomerID == "ALFKI" ? "ABC" : "ABC").ToLower());
		////	Assert.AreEqual("abc", str);
		////}

		[TestMethod]
		public void StringSubstring()
		{
			var list = _db.Customers.Where(c => c.City.Substring(0, 4) == "Seat").ToList();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("Seattle", list[0].City);
		}

		[TestMethod]
		public void StringSubstringNoLength()
		{
			var list = _db.Customers.Where(c => c.City.Substring(4) == "tle").ToList();
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("Seattle", list[0].City);
		}

		[TestMethod]
		public void StringIndexOf()
		{
			var n = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => c.ContactName.IndexOf("ar"));
			Assert.AreEqual(1, n);
		}

		[TestMethod]
		public void StringIndexOfChar()
		{
			var n = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => c.ContactName.IndexOf('r'));
			Assert.AreEqual(2, n);
		}

		[TestMethod]
		public void StringIndexOfWithStart()
		{
			var n = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => c.ContactName.IndexOf("a", 3));
			Assert.AreEqual(4, n);
		}

		////[TestMethod]
		////public void StringTrim()
		////{
		////	var notrim = db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => ("  " + c.City + " "));
		////	var trim = db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => ("  " + c.City + " ").Trim());
		////	Assert.AreNotEqual(notrim, trim);
		////	Assert.AreEqual(notrim.Trim(), trim);
		////}

		[TestMethod]
		public void DateTimeConstructYmd()
		{
			var dt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4));
			Assert.AreEqual(1997, dt.Year);
			Assert.AreEqual(7, dt.Month);
			Assert.AreEqual(4, dt.Day);
			Assert.AreEqual(0, dt.Hour);
			Assert.AreEqual(0, dt.Minute);
			Assert.AreEqual(0, dt.Second);
		}

		[TestMethod]
		public void DateTimeConstructYmdhms()
		{
			var dt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6));
			Assert.AreEqual(1997, dt.Year);
			Assert.AreEqual(7, dt.Month);
			Assert.AreEqual(4, dt.Day);
			Assert.AreEqual(3, dt.Hour);
			Assert.AreEqual(5, dt.Minute);
			Assert.AreEqual(6, dt.Second);
		}

		////[TestMethod]
		////public void DateTimeDay()
		////{
		////	var v = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).Max(o => o.OrderDate.Day);
		////	Assert.AreEqual(25, v);
		////}

		////[TestMethod]
		////public void DateTimeMonth()
		////{
		////	var v = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).Max(o => o.OrderDate.Month);
		////	Assert.AreEqual(8, v);
		////}

		////[TestMethod]
		////public void DateTimeYear()
		////{
		////	var v = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).Max(o => o.OrderDate.Year);
		////	Assert.AreEqual(1997, v);
		////}

		[TestMethod]
		public void DateTimeHour()
		{
			var hour = _db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Hour);
			Assert.AreEqual(3, hour);
		}

		[TestMethod]
		public void DateTimeMinute()
		{
			var minute = _db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Minute);
			Assert.AreEqual(5, minute);
		}

		[TestMethod]
		public void DateTimeSecond()
		{
			var second = _db.Customers.Where(c => c.CustomerID == "ALFKI").Max(c => new DateTime((c.CustomerID == "ALFKI") ? 1997 : 1997, 7, 4, 3, 5, 6).Second);
			Assert.AreEqual(6, second);
		}

		////[TestMethod]
		////public void DateTimeDayOfWeek()
		////{
		////	var dow = db.Orders.Where(o => o.OrderDate == new DateTime(2012, 8, 23)).Take(1).Max(o => o.OrderDate.DayOfWeek);
		////	Assert.AreEqual(DayOfWeek.Monday, dow);
		////}

		[TestMethod]
		public void DateTimeAddYears()
		{
			var od = _db.Orders.FirstOrDefault(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddYears(2).Year == 2014);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public void DateTimeAddMonths()
		{
			var od = _db.Orders.FirstOrDefault(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddMonths(2).Month == 10);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public void DateTimeAddDays()
		{
			var od = _db.Orders.FirstOrDefault(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddDays(2).Day == 25);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public void DateTimeAddHours()
		{
			var od = _db.Orders.FirstOrDefault(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddHours(3).Hour == 3);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public void DateTimeAddMinutes()
		{
			var od = _db.Orders.FirstOrDefault(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddMinutes(5).Minute == 5);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public void DateTimeAddSeconds()
		{
			var od = _db.Orders.FirstOrDefault(o => o.OrderDate == new DateTime(2012, 8, 23) && o.OrderDate.AddSeconds(6).Second == 6);
			Assert.AreNotEqual(null, od);
		}

		[TestMethod]
		public void MathAbs()
		{
			var neg1 = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Abs((c.CustomerID == "ALFKI") ? -1 : 0));
			var pos1 = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Abs((c.CustomerID == "ALFKI") ? 1 : 0));
			Assert.AreEqual(Math.Abs(-1), neg1);
			Assert.AreEqual(Math.Abs(1), pos1);
		}

		[TestMethod]
		public void MathAtan()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Atan((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var one = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Atan((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			Assert.AreEqual(Math.Atan(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Atan(1.0), one, 0.0001);
		}

		[TestMethod]
		public void MathCos()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Cos((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var pi = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Cos((c.CustomerID == "ALFKI") ? Math.PI : Math.PI));
			Assert.AreEqual(Math.Cos(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Cos(Math.PI), pi, 0.0001);
		}

		[TestMethod]
		public void MathSin()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Sin((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var pi = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Sin((c.CustomerID == "ALFKI") ? Math.PI : Math.PI));
			var pi2 = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Sin(((c.CustomerID == "ALFKI") ? Math.PI : Math.PI) / 2.0));
			Assert.AreEqual(Math.Sin(0.0), zero);
			Assert.AreEqual(Math.Sin(Math.PI), pi, 0.0001);
			Assert.AreEqual(Math.Sin(Math.PI / 2.0), pi2, 0.0001);
		}

		[TestMethod]
		public void MathTan()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Tan((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var pi = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Tan((c.CustomerID == "ALFKI") ? Math.PI : Math.PI));
			Assert.AreEqual(Math.Tan(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Tan(Math.PI), pi, 0.0001);
		}

		[TestMethod]
		public void MathExp()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Exp((c.CustomerID == "ALFKI") ? 0.0 : 0.0));
			var one = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Exp((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			var two = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Exp((c.CustomerID == "ALFKI") ? 2.0 : 2.0));
			Assert.AreEqual(Math.Exp(0.0), zero, 0.0001);
			Assert.AreEqual(Math.Exp(1.0), one, 0.0001);
			Assert.AreEqual(Math.Exp(2.0), two, 0.0001);
		}

		[TestMethod]
		public void MathLog()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var one = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Log((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			var e = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Log((c.CustomerID == "ALFKI") ? Math.E : Math.E));
			Assert.AreEqual(Math.Log(1.0), one, 0.0001);
			Assert.AreEqual(Math.Log(Math.E), e, 0.0001);
		}

		[TestMethod]
		public void MathSqrt()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var one = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 1.0 : 1.0));
			var four = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 4.0 : 4.0));
			var nine = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Sqrt((c.CustomerID == "ALFKI") ? 9.0 : 9.0));
			Assert.AreEqual(1.0, one);
			Assert.AreEqual(2.0, four);
			Assert.AreEqual(3.0, nine);
		}

		[TestMethod]
		public void MathPow()
		{
			// Math functions are not supported in SQLite
			if (_db.Configuration.DataAccessProvider.GetType().Name.Contains("SQLite"))
			{
				return;
			}

			var zero = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 0.0));
			var one = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 1.0));
			var two = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 2.0));
			var three = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Pow((c.CustomerID == "ALFKI") ? 2.0 : 2.0, 3.0));
			Assert.AreEqual(1.0, zero);
			Assert.AreEqual(2.0, one);
			Assert.AreEqual(4.0, two);
			Assert.AreEqual(8.0, three);
		}

		[TestMethod]
		public void MathRoundDefault()
		{
			var four = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Round((c.CustomerID == "ALFKI") ? 3.4 : 3.4));
			var six = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Round((c.CustomerID == "ALFKI") ? 3.6 : 3.6));
			Assert.AreEqual(3.0, four);
			Assert.AreEqual(4.0, six);
		}

		[TestMethod]
		public void MathFloor()
		{
			var four = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Floor((c.CustomerID == "ALFKI" ? 3.4 : 3.4)));
			var six = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Floor((c.CustomerID == "ALFKI" ? 3.6 : 3.6)));
			var nfour = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Floor((c.CustomerID == "ALFKI" ? -3.4 : -3.4)));
			Assert.AreEqual(Math.Floor(3.4), four);
			Assert.AreEqual(Math.Floor(3.6), six);
			Assert.AreEqual(Math.Floor(-3.4), nfour);
		}

		[TestMethod]
		public void DecimalFloor()
		{
			var four = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Floor((c.CustomerID == "ALFKI" ? 3.4m : 3.4m)));
			var six = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Floor((c.CustomerID == "ALFKI" ? 3.6m : 3.6m)));
			var nfour = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Floor((c.CustomerID == "ALFKI" ? -3.4m : -3.4m)));
			Assert.AreEqual(decimal.Floor(3.4m), four);
			Assert.AreEqual(decimal.Floor(3.6m), six);
			Assert.AreEqual(decimal.Floor(-3.4m), nfour);
		}

		[TestMethod]
		public void MathTruncate()
		{
			var four = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.4 : 3.4));
			var six = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.6 : 3.6));
			var neg4 = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Truncate((c.CustomerID == "ALFKI") ? -3.4 : -3.4));
			Assert.AreEqual(Math.Truncate(3.4), four);
			Assert.AreEqual(Math.Truncate(3.6), six);
			Assert.AreEqual(Math.Truncate(-3.4), neg4);
		}

		[TestMethod]
		public void StringCompareTo()
		{
			var lt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => c.City.CompareTo("Seattle"));
			var gt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => c.City.CompareTo("Aaa"));
			var eq = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => c.City.CompareTo("Berlin"));
			Assert.AreEqual(-1, lt);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(0, eq);
		}

		[TestMethod]
		public void StringCompareToLessThan()
		{
			var cmpLT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") < 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") < 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public void StringCompareToLessThanOrEqualTo()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") <= 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") <= 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") <= 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public void StringCompareToGreaterThan()
		{
			var cmpLT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") > 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") > 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public void StringCompareToGreaterThanOrEqualTo()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") >= 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") >= 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") >= 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public void StringCompareToEquals()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") == 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") == 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") == 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public void StringCompareToNotEquals()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Seattle") != 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Berlin") != 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => c.City.CompareTo("Aaa") != 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public void StringCompare()
		{
			var lt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => string.Compare(c.City, "Seattle"));
			var gt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => string.Compare(c.City, "Aaa"));
			var eq = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => string.Compare(c.City, "Berlin"));
			Assert.AreEqual(-1, lt);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(0, eq);
		}

		[TestMethod]
		public void StringCompareLessThan()
		{
			var cmpLT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") < 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") < 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public void StringCompareLessThanOrEqualTo()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") <= 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") <= 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") <= 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public void StringCompareGreaterThan()
		{
			var cmpLT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") > 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") > 0);
			Assert.AreNotEqual(null, cmpLT);
			Assert.AreEqual(null, cmpEQ);
		}

		[TestMethod]
		public void StringCompareGreaterThanOrEqualTo()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") >= 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") >= 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") >= 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public void StringCompareEquals()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") == 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") == 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") == 0);
			Assert.AreEqual(null, cmpLE);
			Assert.AreNotEqual(null, cmpEQ);
			Assert.AreEqual(null, cmpGT);
		}

		[TestMethod]
		public void StringCompareNotEquals()
		{
			var cmpLE = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Seattle") != 0);
			var cmpEQ = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Berlin") != 0);
			var cmpGT = _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault(c => string.Compare(c.City, "Aaa") != 0);
			Assert.AreNotEqual(null, cmpLE);
			Assert.AreEqual(null, cmpEQ);
			Assert.AreNotEqual(null, cmpGT);
		}

		[TestMethod]
		public void IntCompareTo()
		{
			var eq = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(10));
			var gt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(9));
			var lt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => (c.CustomerID == "ALFKI" ? 10 : 10).CompareTo(11));
			Assert.AreEqual(0, eq);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(-1, lt);
		}

		[TestMethod]
		public void DecimalCompare()
		{
			var eq = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 10m));
			var gt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 9m));
			var lt = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Compare((c.CustomerID == "ALFKI" ? 10m : 10m), 11m));
			Assert.AreEqual(0, eq);
			Assert.AreEqual(1, gt);
			Assert.AreEqual(-1, lt);
		}

		[TestMethod]
		public void DecimalAdd()
		{
			var onetwo = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Add((c.CustomerID == "ALFKI" ? 1m : 1m), 2m));
			Assert.AreEqual(3m, onetwo);
		}

		[TestMethod]
		public void DecimalSubtract()
		{
			var onetwo = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Subtract((c.CustomerID == "ALFKI" ? 1m : 1m), 2m));
			Assert.AreEqual(-1m, onetwo);
		}

		[TestMethod]
		public void DecimalMultiply()
		{
			var onetwo = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Multiply((c.CustomerID == "ALFKI" ? 1m : 1m), 2m));
			Assert.AreEqual(2m, onetwo);
		}

		[TestMethod]
		public void DecimalDivide()
		{
			var onetwo = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Divide((c.CustomerID == "ALFKI" ? 1.0m : 1.0m), 2.0m));
			Assert.AreEqual(0.5m, onetwo);
		}

		[TestMethod]
		public void DecimalNegate()
		{
			var one = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Negate((c.CustomerID == "ALFKI" ? 1m : 1m)));
			Assert.AreEqual(-1m, one);
		}

		[TestMethod]
		public void DecimalRoundDefault()
		{
			var four = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Round((c.CustomerID == "ALFKI" ? 3.4m : 3.4m)));
			var six = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Round((c.CustomerID == "ALFKI" ? 3.5m : 3.5m)));
			Assert.AreEqual(3.0m, four);
			Assert.AreEqual(4.0m, six);
		}

		[TestMethod]
		public void DecimalTruncate()
		{
			var four = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => decimal.Truncate((c.CustomerID == "ALFKI") ? 3.4m : 3.4m));
			var six = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Truncate((c.CustomerID == "ALFKI") ? 3.6m : 3.6m));
			var neg4 = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => Math.Truncate((c.CustomerID == "ALFKI") ? -3.4m : -3.4m));
			Assert.AreEqual(decimal.Truncate(3.4m), four);
			Assert.AreEqual(decimal.Truncate(3.6m), six);
			Assert.AreEqual(decimal.Truncate(-3.4m), neg4);
		}

		[TestMethod]
		public void DecimalLessThan()
		{
			var alfki = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1.0m : 3.0m) < 2.0m);
			Assert.AreNotEqual(null, alfki);
		}

		[TestMethod]
		public void IntLessThan()
		{
			var alfki = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) < 2);
			var alfkiN = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) < 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public void IntLessThanOrEqual()
		{
			var alfki = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) <= 2);
			var alfki2 = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 2 : 3) <= 2);
			var alfkiN = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) <= 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreNotEqual(null, alfki2);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public void IntGreaterThan()
		{
			var alfki = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) > 2);
			var alfkiN = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) > 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public void IntGreaterThanOrEqual()
		{
			var alfki = _db.Customers.Single(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 1) >= 2);
			var alfki2 = _db.Customers.Single(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 3 : 2) >= 2);
			var alfkiN = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 3) > 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreNotEqual(null, alfki2);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public void IntEqual()
		{
			var alfki = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 1) == 1);
			var alfkiN = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 1 : 1) == 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public void IntNotEqual()
		{
			var alfki = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 2 : 2) != 1);
			var alfkiN = _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI" && (c.CustomerID == "ALFKI" ? 2 : 2) != 2);
			Assert.AreNotEqual(null, alfki);
			Assert.AreEqual(null, alfkiN);
		}

		[TestMethod]
		public void IntAdd()
		{
			var three = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 1 : 1) + 2);
			Assert.AreEqual(3, three);
		}

		[TestMethod]
		public void IntSubtract()
		{
			var negone = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 1 : 1) - 2);
			Assert.AreEqual(-1, negone);
		}

		[TestMethod]
		public void IntMultiply()
		{
			var six = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 2 : 2) * 3);
			Assert.AreEqual(6, six);
		}

		[TestMethod]
		public void IntDivide()
		{
			var one = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 3 : 3) / 2);
			Assert.AreEqual(1, one);
		}

		[TestMethod]
		public void IntModulo()
		{
			var three = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 7 : 7) % 4);
			Assert.AreEqual(3, three);
		}

		[TestMethod]
		public void IntLeftShift()
		{
			var eight = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 1 : 1) << 3);
			Assert.AreEqual(8, eight);
		}

		[TestMethod]
		public void IntRightShift()
		{
			var eight = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 32 : 32) >> 2);
			Assert.AreEqual(8, eight);
		}

		[TestMethod]
		public void IntBitwiseAnd()
		{
			var band = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 6 : 6) & 3);
			Assert.AreEqual(2, band);
		}

		[TestMethod]
		public void IntBitwiseOr()
		{
			var eleven = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 10 : 10) | 3);
			Assert.AreEqual(11, eleven);
		}

		[TestMethod]
		public void IntBitwiseExclusiveOr()
		{
			var zero = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ((c.CustomerID == "ALFKI") ? 1 : 1) ^ 1);
			Assert.AreEqual(0, zero);
		}

		////[TestMethod]
		////public void IntBitwiseNot()
		////{
		////	var bneg = db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => ~((c.CustomerID == "ALFKI") ? -1 : -1));
		////	Assert.AreEqual(~-1, bneg);
		////}

		[TestMethod]
		public void IntNegate()
		{
			var neg = _db.Customers.Where(c => c.CustomerID == "ALFKI").Sum(c => -((c.CustomerID == "ALFKI") ? 1 : 1));
			Assert.AreEqual(-1, neg);
		}

		[TestMethod]
		public void And()
		{
			var custs = _db.Customers.Where(c => c.Country == "USA" && c.City.StartsWith("A")).Select(c => c.City).ToList();
			Assert.AreEqual(2, custs.Count);
			Assert.IsTrue(custs.All(c => c.StartsWith("A")));
		}

		[TestMethod]
		public void Or()
		{
			var custs = _db.Customers.Where(c => c.Country == "USA" || c.City.StartsWith("A")).Select(c => c.City).ToList();
			Assert.AreEqual(14, custs.Count);
		}

		[TestMethod]
		public void Not()
		{
			var custs = _db.Customers.Where(c => !(c.Country == "USA")).Select(c => c.Country).ToList();
			Assert.AreEqual(78, custs.Count);
		}

		////[TestMethod]
		////public void EqualLiteralNull()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => x == null);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).Contains("IS NULL"));
		////	var n = q.Count();
		////	Assert.AreEqual(1, n);
		////}

		////[TestMethod]
		////public void EqualLiteralNullReversed()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => null == x);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).Contains("IS NULL"));
		////	var n = q.Count();
		////	Assert.AreEqual(1, n);
		////}

		////[TestMethod]
		////public void NotEqualLiteralNull()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => x != null);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).Contains("IS NOT NULL"));
		////	var n = q.Count();
		////	Assert.AreEqual(90, n);
		////}

		////[TestMethod]
		////public void NotEqualLiteralNullReversed()
		////{
		////	var q = db.Customers.Select(c => c.CustomerID == "ALFKI" ? null : c.CustomerID).Where(x => null != x);
		////	Assert.IsTrue(this.provider.GetQueryText(q.Expression).Contains("IS NOT NULL"));
		////	var n = q.Count();
		////	Assert.AreEqual(90, n);
		////}

		////[TestMethod]
		////public void ConditionalResultsArePredicates()
		////{
		////	bool value = db.Orders.Where(c => c.CustomerID == "ALFKI").Max(c => (c.CustomerID == "ALFKI" ? string.Compare(c.CustomerID, "POTATO") < 0 : string.Compare(c.CustomerID, "POTATO") > 0));
		////	Assert.IsTrue(value);
		////}

		////[TestMethod]
		////public void SelectManyJoined()
		////{
		////	var cods =
		////		(from c in db.Customers
		////		 from o in db.Orders.Where(o => o.CustomerID == c.CustomerID)
		////		 select new { c.ContactName, o.OrderDate }).ToList();
		////	Assert.AreEqual(830, cods.Count);
		////}

		////[TestMethod]
		////public void SelectManyJoinedDefaultIfEmpty()
		////{
		////	var cods = (
		////		from c in db.Customers
		////		from o in db.Orders.Where(o => o.CustomerID == c.CustomerID).DefaultIfEmpty()
		////		select new { c.ContactName, o.OrderDate }
		////		).ToList();
		////	Assert.AreEqual(832, cods.Count);
		////}

		[TestMethod]
		public void SelectWhereAssociation()
		{
			var ords = (
				from o in _db.Orders
				where o.Customer.City == "Seattle"
				select o
				).ToList();
			Assert.AreEqual(14, ords.Count);
		}

		[TestMethod]
		public void SelectWhereAssociationTwice()
		{
			var n = _db.Orders.Where(c => c.CustomerID == "WHITC").Count();
			var ords = (
				from o in _db.Orders
				where o.Customer.Country == "USA" && o.Customer.City == "Seattle"
				select o
				).ToList();
			Assert.AreEqual(n, ords.Count);
		}

		[TestMethod]
		public void SelectAssociation()
		{
			var custs = (
				from o in _db.Orders
				where o.CustomerID == "ALFKI"
				select o.Customer
				).ToList();
			Assert.AreEqual(6, custs.Count);
			Assert.IsTrue(custs.All(c => c.CustomerID == "ALFKI"));
		}

		////[TestMethod]
		////public void SelectAssociations()
		////{
		////	var doubleCusts = (
		////		from o in db.Orders
		////		where o.CustomerID == "ALFKI"
		////		select new { A = o.Customer, B = o.Customer }
		////		).ToList();

		////	Assert.AreEqual(6, doubleCusts.Count);
		////	Assert.IsTrue(doubleCusts.All(c => c.A.CustomerID == "ALFKI" && c.B.CustomerID == "ALFKI"));
		////}

		////[TestMethod]
		////public void SelectAssociationsWhereAssociations()
		////{
		////	var stuff = (
		////		from o in db.Orders
		////		where o.Customer.Country == "USA"
		////		where o.Customer.City != "Seattle"
		////		select new { A = o.Customer, B = o.Customer }
		////		).ToList();
		////	Assert.AreEqual(108, stuff.Count);
		////}
	}
}

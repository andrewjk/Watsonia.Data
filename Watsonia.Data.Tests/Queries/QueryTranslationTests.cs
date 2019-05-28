using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Tests.Northwind;

// TODO: Implement all double commented (////) tests

namespace Watsonia.Data.Tests.Queries
{
	[TestClass]
	public class QueryTranslationTests
	{
		private static readonly NorthwindDatabase _db = new NorthwindDatabase();
		private static Dictionary<string, string> _baselines = new Dictionary<string, string>();

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			var fileName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Queries\QueryTranslationBaselines.xml";
			if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
			{
				var doc = XDocument.Load(fileName);
				_baselines = doc.Root.Elements("baseline").ToDictionary(e => (string)e.Attribute("key"), e => e.Value);
			}
		}

		[TestMethod]
		public void TestWhere()
		{
			TestQuery(
				"TestWhere",
				_db.Customers.Where(c => c.City == "London"));
		}

		[TestMethod]
		public void TestCompareEntityEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			TestQuery(
				"TestCompareEntityEqual",
				_db.Customers.Where(c => c == alfki));
		}

		[TestMethod]
		public void TestCompareEntityNotEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			TestQuery(
				"TestCompareEntityNotEqual",
				_db.Customers.Where(c => c != alfki));
		}

		[TestMethod]
		public void TestCompareConstructedEqual()
		{
			TestQuery(
				"TestCompareConstructedEqual",
				_db.Customers.Where(c => new { x = c.City } == new { x = "London" }));
		}

		[TestMethod]
		public void TestCompareConstructedMultiValueEqual()
		{
			TestQuery(
				"TestCompareConstructedMultiValueEqual",
				_db.Customers.Where(c => new { x = c.City, y = c.Country } == new { x = "London", y = "UK" }));
		}

		[TestMethod]
		public void TestCompareConstructedMultiValueNotEqual()
		{
			TestQuery(
				"TestCompareConstructedMultiValueNotEqual",
				_db.Customers.Where(c => new { x = c.City, y = c.Country } != new { x = "London", y = "UK" }));
		}

		[TestMethod]
		public void TestCompareConstructed()
		{
			TestQuery(
				"TestCompareConstructed",
				_db.Customers.Where(c => new { x = c.City } == new { x = "London" }));
		}

		[TestMethod]
		public void TestSelectScalar()
		{
			TestQuery(
				"TestSelectScalar",
				_db.Customers.Select(c => c.City));
		}

		[TestMethod]
		public void TestSelectAnonymousOne()
		{
			TestQuery(
				"TestSelectAnonymousOne",
				_db.Customers.Select(c => new { c.City }));
		}

		////[TestMethod]
		////public void TestSelectAnonymousTwo()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousTwo",
		////		db.Customers.Select(c => new { c.City, c.Phone }));
		////}

		////[TestMethod]
		////public void TestSelectAnonymousThree()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousThree",
		////		db.Customers.Select(c => new { c.City, c.Phone, c.Country }));
		////}

		[TestMethod]
		public void TestSelectCustomerTable()
		{
			TestQuery(
				"TestSelectCustomerTable",
				_db.Customers);
		}

		[TestMethod]
		public void TestSelectCustomerIdentity()
		{
			TestQuery(
				"TestSelectCustomerIdentity",
				_db.Customers.Select(c => c));
		}

		////[TestMethod]
		////public void TestSelectAnonymousWithObject()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousWithObject",
		////		db.Customers.Select(c => new { c.City, c }));
		////}

		////[TestMethod]
		////public void TestSelectAnonymousNested()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousNested",
		////		db.Customers.Select(c => new { c.City, Country = new { c.Country } }));
		////}

		////[TestMethod]
		////public void TestSelectAnonymousEmpty()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousEmpty",
		////		db.Customers.Select(c => new { }));
		////}

		////[TestMethod]
		////public void TestSelectAnonymousLiteral()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousLiteral",
		////		db.Customers.Select(c => new { X = 10 }));
		////}

		[TestMethod]
		public void TestSelectConstantInt()
		{
			TestQuery(
				"TestSelectConstantInt",
				_db.Customers.Select(c => 0));
		}

		[TestMethod]
		public void TestSelectConstantNullString()
		{
			TestQuery(
				"TestSelectConstantNullString",
				_db.Customers.Select(c => (string)null));
		}

		[TestMethod]
		public void TestSelectLocal()
		{
			var x = 10;
			TestQuery(
				"TestSelectLocal",
				_db.Customers.Select(c => x));
		}

		////[TestMethod]
		////public void TestSelectNestedCollection()
		////{
		////	TestQuery(
		////		"TestSelectNestedCollection",
		////		from c in db.Customers
		////		where c.City == "London"
		////		select db.Orders.Where(o => o.CustomerID == c.CustomerID && o.OrderDate.Year == 1997).Select(o => o.OrderID));
		////}

		////[TestMethod]
		////public void TestSelectNestedCollectionInAnonymousType()
		////{
		////	TestQuery(
		////		"TestSelectNestedCollectionInAnonymousType",
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		select new { Foos = db.Orders.Where(o => o.CustomerID == c.CustomerID && o.OrderDate.Year == 1997).Select(o => o.OrderID) });
		////}

		////[TestMethod]
		////public void TestJoinCustomerOrders()
		////{
		////	TestQuery(
		////		"TestJoinCustomerOrders",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID
		////		select new { c.ContactName, o.OrderID });
		////}

		////[TestMethod]
		////public void TestJoinMultiKey()
		////{
		////	TestQuery(
		////		"TestJoinMultiKey",
		////		from c in db.Customers
		////		join o in db.Orders on new { a = c.CustomerID, b = c.CustomerID } equals new { a = o.CustomerID, b = o.CustomerID }
		////		select new { c, o });
		////}

		////[TestMethod]
		////public void TestJoinIntoCustomersOrders()
		////{
		////	TestQuery(
		////		"TestJoinIntoCustomersOrders",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		select new { cust = c, ords = ords.ToList() });
		////}

		////[TestMethod]
		////public void TestJoinIntoCustomersOrdersCount()
		////{
		////	TestQuery(
		////		"TestJoinIntoCustomersOrdersCount",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		select new { cust = c, ords = ords.Count() });
		////}

		////[TestMethod]
		////public void TestJoinIntoDefaultIfEmpty()
		////{
		////	TestQuery(
		////		"TestJoinIntoDefaultIfEmpty",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		from o in ords.DefaultIfEmpty()
		////		select new { c, o });
		////}

		////[TestMethod]
		////public void TestSelectManyCustomerOrders()
		////{
		////	TestQuery(
		////		"TestSelectManyCustomerOrders",
		////		from c in db.Customers
		////		from o in db.Orders
		////		where c.CustomerID == o.CustomerID
		////		select new { c.ContactName, o.OrderID }
		////		);
		////}

		////[TestMethod]
		////public void TestMultipleJoinsWithJoinConditionsInWhere()
		////{
		////	TestQuery(
		////		"TestMultipleJoinsWithJoinConditionsInWhere",
		////		from c in db.Customers
		////		from o in db.Orders
		////		from d in db.OrderDetails
		////		where o.CustomerID == c.CustomerID && o.OrderID == d.OrderID
		////		where c.CustomerID == "ALFKI"
		////		select d.ProductID
		////		);
		////}

		////[TestMethod]
		////public void TestMultipleJoinsWithMissingJoinCondition()
		////{
		////	TestQuery(
		////		"TestMultipleJoinsWithMissingJoinCondition",
		////		from c in db.Customers
		////		from o in db.Orders
		////		from d in db.OrderDetails
		////		where o.CustomerID == c.CustomerID /*&& o.OrderID == d.OrderID*/
		////		where c.CustomerID == "ALFKI"
		////		select d.ProductID
		////		);
		////}

		[TestMethod]
		public void TestOrderBy()
		{
			TestQuery(
				"TestOrderBy",
				_db.Customers.OrderBy(c => c.CustomerID)
				);
		}

		[TestMethod]
		public void TestOrderBySelect()
		{
			TestQuery(
				"TestOrderBySelect",
				_db.Customers.OrderBy(c => c.CustomerID).Select(c => c.ContactName)
				);
		}

		[TestMethod]
		public void TestOrderByOrderBy()
		{
			TestQuery(
				"TestOrderByOrderBy",
				_db.Customers.OrderBy(c => c.CustomerID).OrderBy(c => c.Country).Select(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByThenBy()
		{
			TestQuery(
				"TestOrderByThenBy",
				_db.Customers.OrderBy(c => c.CustomerID).ThenBy(c => c.Country).Select(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByDescending()
		{
			TestQuery(
				"TestOrderByDescending",
				_db.Customers.OrderByDescending(c => c.CustomerID).Select(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByDescendingThenBy()
		{
			TestQuery(
				"TestOrderByDescendingThenBy",
				_db.Customers.OrderByDescending(c => c.CustomerID).ThenBy(c => c.Country).Select(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByDescendingThenByDescending()
		{
			TestQuery(
				"TestOrderByDescendingThenByDescending",
				_db.Customers.OrderByDescending(c => c.CustomerID).ThenByDescending(c => c.Country).Select(c => c.City)
				);
		}

		////[TestMethod]
		////public void TestOrderByJoin()
		////{
		////	TestQuery(
		////		"TestOrderByJoin",
		////		from c in db.Customers.OrderBy(c => c.CustomerID)
		////		join o in db.Orders.OrderBy(o => o.OrderID) on c.CustomerID equals o.CustomerID
		////		select new { CustomerID = c.CustomerID, o.OrderID }
		////		);
		////}

		////[TestMethod]
		////public void TestOrderBySelectMany()
		////{
		////	TestQuery(
		////		"TestOrderBySelectMany",
		////		from c in db.Customers.OrderBy(c => c.CustomerID)
		////		from o in db.Orders.OrderBy(o => o.OrderID)
		////		where c.CustomerID == o.CustomerID
		////		select new { c.ContactName, o.OrderID }
		////		);
		////}

		////[TestMethod]
		////public void TestGroupBy()
		////{
		////	TestQuery(
		////		"TestGroupBy",
		////		db.Customers.GroupBy(c => c.City)
		////		);
		////}

		////[TestMethod]
		////public void TestGroupBySelectMany()
		////{
		////	TestQuery(
		////		"TestGroupBySelectMany",
		////		db.Customers.GroupBy(c => c.City).SelectMany(g => g)
		////		);
		////}

		////[TestMethod]
		////public void TestGroupBySum()
		////{
		////	TestQuery(
		////		"TestGroupBySum",
		////		db.Orders.GroupBy(o => o.CustomerID).Select(g => g.Sum(o => o.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByCount()
		////{
		////	TestQuery(
		////		"TestGroupByCount",
		////		db.Orders.GroupBy(o => o.CustomerID).Select(g => g.Count())
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByLongCount()
		////{
		////	TestQuery(
		////		"TestGroupByLongCount",
		////		db.Orders.GroupBy(o => o.CustomerID).Select(g => g.LongCount()));
		////}

		////[TestMethod]
		////public void TestGroupBySumMinMaxAvg()
		////{
		////	TestQuery(
		////		"TestGroupBySumMinMaxAvg",
		////		db.Orders.GroupBy(o => o.CustomerID).Select(g =>
		////			new
		////			{
		////				Sum = g.Sum(o => o.OrderID),
		////				Min = g.Min(o => o.OrderID),
		////				Max = g.Max(o => o.OrderID),
		////				Avg = g.Average(o => o.OrderID)
		////			})
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByWithResultSelector()
		////{
		////	TestQuery(
		////		"TestGroupByWithResultSelector",
		////		db.Orders.GroupBy(o => o.CustomerID, (k, g) =>
		////			new
		////			{
		////				Sum = g.Sum(o => o.OrderID),
		////				Min = g.Min(o => o.OrderID),
		////				Max = g.Max(o => o.OrderID),
		////				Avg = g.Average(o => o.OrderID)
		////			})
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByWithElementSelectorSum()
		////{
		////	TestQuery(
		////		"TestGroupByWithElementSelectorSum",
		////		db.Orders.GroupBy(o => o.CustomerID, o => o.OrderID).Select(g => g.Sum())
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByWithElementSelector()
		////{
		////	TestQuery(
		////		"TestGroupByWithElementSelector",
		////		db.Orders.GroupBy(o => o.CustomerID, o => o.OrderID)
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByWithElementSelectorSumMax()
		////{
		////	TestQuery(
		////		"TestGroupByWithElementSelectorSumMax",
		////		db.Orders.GroupBy(o => o.CustomerID, o => o.OrderID).Select(g => new { Sum = g.Sum(), Max = g.Max() })
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByWithAnonymousElement()
		////{
		////	TestQuery(
		////		"TestGroupByWithAnonymousElement",
		////		db.Orders.GroupBy(o => o.CustomerID, o => new { o.OrderID }).Select(g => g.Sum(x => x.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByWithTwoPartKey()
		////{
		////	TestQuery(
		////		"TestGroupByWithTwoPartKey",
		////		db.Orders.GroupBy(o => new { CustomerID = o.CustomerID, o.OrderDate }).Select(g => g.Sum(o => o.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void TestOrderByGroupBy()
		////{
		////	TestQuery(
		////		"TestOrderByGroupBy",
		////		db.Orders.OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).Select(g => g.Sum(o => o.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void TestOrderByGroupBySelectMany()
		////{
		////	TestQuery(
		////		"TestOrderByGroupBySelectMany",
		////		db.Orders.OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).SelectMany(g => g)
		////		);
		////}

		[TestMethod]
		public void TestSumWithNoArg()
		{
			TestQuery(
				"TestSumWithNoArg",
				() => _db.Orders.Select(o => o.OrderID).Sum()
				);
		}

		[TestMethod]
		public void TestSumWithArg()
		{
			TestQuery(
				"TestSumWithArg",
				() => _db.Orders.Sum(o => o.OrderID)
				);
		}

		[TestMethod]
		public void TestCountWithNoPredicate()
		{
			TestQuery(
				"TestCountWithNoPredicate",
				() => _db.Orders.Count()
				);
		}

		[TestMethod]
		public void TestCountWithPredicate()
		{
			TestQuery(
				"TestCountWithPredicate",
				() => _db.Orders.Count(o => o.CustomerID == "ALFKI")
				);
		}

		[TestMethod]
		public void TestDistinct()
		{
			TestQuery(
				"TestDistinct",
				_db.Customers.Distinct()
				);
		}

		[TestMethod]
		public void TestDistinctScalar()
		{
			TestQuery(
				"TestDistinctScalar",
				_db.Customers.Select(c => c.City).Distinct()
				);
		}

		[TestMethod]
		public void TestOrderByDistinct()
		{
			TestQuery(
				"TestOrderByDistinct",
				_db.Customers.OrderBy(c => c.CustomerID).Select(c => c.City).Distinct()
				);
		}

		////[TestMethod]
		////public void TestDistinctOrderBy()
		////{
		////	TestQuery(
		////		"TestDistinctOrderBy",
		////		db.Customers.Select(c => c.City).Distinct().OrderBy(c => c)
		////		);
		////}

		////[TestMethod]
		////public void TestDistinctGroupBy()
		////{
		////	TestQuery(
		////		"TestDistinctGroupBy",
		////		db.Orders.Distinct().GroupBy(o => o.CustomerID)
		////		);
		////}

		////[TestMethod]
		////public void TestGroupByDistinct()
		////{
		////	TestQuery(
		////		"TestGroupByDistinct",
		////		db.Orders.GroupBy(o => o.CustomerID).Distinct()
		////		);

		////}

		[TestMethod]
		public void TestDistinctCount()
		{
			TestQuery(
				"TestDistinctCount",
				() => _db.Customers.Distinct().Count()
				);
		}

		////[TestMethod]
		////public void TestSelectDistinctCount()
		////{
		////	TestQuery(
		////		"TestSelectDistinctCount",
		////		() => db.Customers.Select(c => c.City).Distinct().Count()
		////		);
		////}

		////[TestMethod]
		////public void TestSelectSelectDistinctCount()
		////{
		////	TestQuery(
		////		"TestSelectSelectDistinctCount",
		////		() => db.Customers.Select(c => c.City).Select(c => c).Distinct().Count()
		////		);
		////}

		////[TestMethod]
		////public void TestDistinctCountPredicate()
		////{
		////	TestQuery(
		////		"TestDistinctCountPredicate",
		////		() => db.Customers.Distinct().Count(c => c.CustomerID == "ALFKI")
		////		);
		////}

		////[TestMethod]
		////public void TestDistinctSumWithArg()
		////{
		////	TestQuery(
		////		"TestDistinctSumWithArg",
		////		() => db.Orders.Distinct().Sum(o => o.OrderID)
		////		);
		////}

		////[TestMethod]
		////public void TestSelectDistinctSum()
		////{
		////	TestQuery(
		////		"TestSelectDistinctSum",
		////		() => db.Orders.Select(o => o.OrderID).Distinct().Sum()
		////		);
		////}

		////[TestMethod]
		////public void TestTake()
		////{
		////	TestQuery(
		////		"TestTake",
		////		db.Orders.Take(5)
		////		);
		////}

		////[TestMethod]
		////public void TestTakeDistinct()
		////{
		////	TestQuery(
		////		"TestTakeDistinct",
		////		db.Orders.Take(5).Distinct()
		////		);
		////}

		[TestMethod]
		public void TestDistinctTake()
		{
			TestQuery(
				"TestDistinctTake",
				_db.Orders.Distinct().Take(5)
				);
		}

		////[TestMethod]
		////public void TestDistinctTakeCount()
		////{
		////	TestQuery(
		////		"TestDistinctTakeCount",
		////		() => db.Orders.Distinct().Take(5).Count()
		////		);
		////}

		////[TestMethod]
		////public void TestTakeDistinctCount()
		////{
		////	TestQuery(
		////		"TestTakeDistinctCount",
		////		() => db.Orders.Take(5).Distinct().Count()
		////		);
		////}

		[TestMethod]
		public void TestSkip()
		{
			TestQuery(
				"TestSkip",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5)
				);
		}

		[TestMethod]
		public void TestTakeSkip()
		{
			TestQuery(
				"TestTakeSkip",
				_db.Customers.OrderBy(c => c.ContactName).Take(10).Skip(5)
				);
		}

		////[TestMethod]
		////public void TestDistinctSkip()
		////{
		////	TestQuery(
		////		"TestDistinctSkip",
		////		db.Customers.Distinct().OrderBy(c => c.ContactName).Skip(5)
		////		);
		////}

		[TestMethod]
		public void TestSkipTake()
		{
			TestQuery(
				"TestSkipTake",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5).Take(10)
				);
		}

		////[TestMethod]
		////public void TestDistinctSkipTake()
		////{
		////	TestQuery(
		////		"TestDistinctSkipTake",
		////		db.Customers.Distinct().OrderBy(c => c.ContactName).Skip(5).Take(10)
		////		);
		////}

		[TestMethod]
		public void TestSkipDistinct()
		{
			TestQuery(
				"TestSkipDistinct",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5).Distinct()
				);
		}

		[TestMethod]
		public void TestSkipTakeDistinct()
		{
			TestQuery(
				"TestSkipTakeDistinct",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5).Take(10).Distinct()
				);
		}

		////[TestMethod]
		////public void TestTakeSkipDistinct()
		////{
		////	TestQuery(
		////		"TestTakeSkipDistinct",
		////		db.Customers.OrderBy(c => c.ContactName).Take(10).Skip(5).Distinct()
		////		);
		////}

		[TestMethod]
		public void TestFirst()
		{
			TestQuery(
				"TestFirst",
				() => _db.Customers.OrderBy(c => c.ContactName).First()
				);
		}

		[TestMethod]
		public void TestFirstPredicate()
		{
			TestQuery(
				"TestFirstPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).First(c => c.City == "London")
				);
		}

		[TestMethod]
		public void TestWhereFirst()
		{
			TestQuery(
				"TestWhereFirst",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").First()
				);
		}

		[TestMethod]
		public void TestFirstOrDefault()
		{
			TestQuery(
				"TestFirstOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).FirstOrDefault()
				);
		}

		[TestMethod]
		public void TestFirstOrDefaultPredicate()
		{
			TestQuery(
				"TestFirstOrDefaultPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).FirstOrDefault(c => c.City == "London")
				);
		}

		[TestMethod]
		public void TestWhereFirstOrDefault()
		{
			TestQuery(
				"TestWhereFirstOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").FirstOrDefault()
				);
		}

		[TestMethod]
		public void TestReverse()
		{
			TestQuery(
				"TestReverse",
				_db.Customers.OrderBy(c => c.ContactName).Reverse()
				);
		}

		[TestMethod]
		public void TestReverseReverse()
		{
			TestQuery(
				"TestReverseReverse",
				_db.Customers.OrderBy(c => c.ContactName).Reverse().Reverse()
				);
		}

		////[TestMethod]
		////public void TestReverseWhereReverse()
		////{
		////	TestQuery(
		////		"TestReverseWhereReverse",
		////		db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Reverse()
		////		);
		////}

		////[TestMethod]
		////public void TestReverseTakeReverse()
		////{
		////	TestQuery(
		////		"TestReverseTakeReverse",
		////		db.Customers.OrderBy(c => c.ContactName).Reverse().Take(5).Reverse()
		////		);
		////}

		////[TestMethod]
		////public void TestReverseWhereTakeReverse()
		////{
		////	TestQuery(
		////		"TestReverseWhereTakeReverse",
		////		db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Take(5).Reverse()
		////		);
		////}

		[TestMethod]
		public void TestLast()
		{
			TestQuery(
				"TestLast",
				() => _db.Customers.OrderBy(c => c.ContactName).Last()
				);
		}

		[TestMethod]
		public void TestLastPredicate()
		{
			TestQuery(
				"TestLastPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).Last(c => c.City == "London")
				);
		}

		[TestMethod]
		public void TestWhereLast()
		{
			TestQuery(
				"TestWhereLast",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").Last()
				);
		}

		[TestMethod]
		public void TestLastOrDefault()
		{
			TestQuery(
				"TestLastOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).LastOrDefault()
				);
		}

		[TestMethod]
		public void TestLastOrDefaultPredicate()
		{
			TestQuery(
				"TestLastOrDefaultPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).LastOrDefault(c => c.City == "London")
				);
		}

		[TestMethod]
		public void TestWhereLastOrDefault()
		{
			TestQuery(
				"TestWhereLastOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").LastOrDefault()
				);
		}

		[TestMethod]
		public void TestSingle()
		{
			TestQuery(
				"TestSingle",
				() => _db.Customers.Single());
		}

		[TestMethod]
		public void TestSinglePredicate()
		{
			TestQuery(
				"TestSinglePredicate",
				() => _db.Customers.Single(c => c.CustomerID == "ALFKI")
				);
		}

		[TestMethod]
		public void TestWhereSingle()
		{
			TestQuery(
				"TestWhereSingle",
				() => _db.Customers.Where(c => c.CustomerID == "ALFKI").Single()
				);
		}

		////[TestMethod]
		////public void TestSingleOrDefault()
		////{
		////	TestQuery(
		////		"TestSingleOrDefault",
		////		() => db.Customers.SingleOrDefault());
		////}

		[TestMethod]
		public void TestSingleOrDefaultPredicate()
		{
			TestQuery(
				"TestSingleOrDefaultPredicate",
				() => _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI")
				);
		}

		[TestMethod]
		public void TestWhereSingleOrDefault()
		{
			TestQuery(
				"TestWhereSingleOrDefault",
				() => _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault()
				);
		}

		////[TestMethod]
		////public void TestAnyWithSubquery()
		////{
		////	TestQuery(
		////		"TestAnyWithSubquery",
		////		db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).Any(o => o.OrderDate.Year == 1997))
		////		);
		////}

		////[TestMethod]
		////public void TestAnyWithSubqueryNoPredicate()
		////{
		////	TestQuery(
		////		"TestAnyWithSubqueryNoPredicate",
		////		db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).Any())
		////		);
		////}

		////[TestMethod]
		////public void TestAnyWithLocalCollection()
		////{
		////	string[] ids = new[] { "ABCDE", "ALFKI" };
		////	TestQuery(
		////		"TestAnyWithLocalCollection",
		////		db.Customers.Where(c => ids.Any(id => c.CustomerID == id))
		////		);
		////}

		[TestMethod]
		public void TestAnyTopLevel()
		{
			TestQuery(
				"TestAnyTopLevel",
				() => _db.Customers.Any()
				);
		}

		////[TestMethod]
		////public void TestAllWithSubquery()
		////{
		////	TestQuery(
		////		"TestAllWithSubquery",
		////		db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).All(o => o.OrderDate.Year == 1997))
		////		);
		////}

		////[TestMethod]
		////public void TestAllWithLocalCollection()
		////{
		////	string[] patterns = new[] { "a", "e" };

		////	TestQuery(
		////		"TestAllWithLocalCollection",
		////		db.Customers.Where(c => patterns.All(p => c.ContactName.Contains(p)))
		////		);
		////}

		[TestMethod]
		public void TestAllTopLevel()
		{
			TestQuery(
				"TestAllTopLevel",
				() => _db.Customers.All(c => c.ContactName.StartsWith("a"))
				);
		}

		[TestMethod]
		public void TestContainsWithSubquery()
		{
			TestQuery(
				"TestContainsWithSubquery",
				_db.Customers.Where(c => _db.Orders.Select(o => o.CustomerID).Contains(c.CustomerID))
				);
		}

		[TestMethod]
		public void TestContainsWithLocalCollection()
		{
			var ids = new[] { "ABCDE", "ALFKI" };
			TestQuery(
				"TestContainsWithLocalCollection",
				_db.Customers.Where(c => ids.Contains(c.CustomerID))
				);
		}

		[TestMethod]
		public void TestContainsTopLevel()
		{
			TestQuery(
				"TestContainsTopLevel",
				() => _db.Customers.Select(c => c.CustomerID).Contains("ALFKI")
				);
		}

		////[TestMethod]
		////public void TestCoalesce()
		////{
		////	TestQuery(
		////		"TestCoalesce",
		////		db.Customers.Where(c => (c.City ?? "Seattle") == "Seattle"));
		////}

		////[TestMethod]
		////public void TestCoalesce2()
		////{
		////	TestQuery(
		////		"TestCoalesce2",
		////		db.Customers.Where(c => (c.City ?? c.Country ?? "Seattle") == "Seattle"));
		////}

		[TestMethod]
		public void TestStringLength()
		{
			TestQuery(
				"TestStringLength",
				_db.Customers.Where(c => c.City.Length == 7));
		}

		[TestMethod]
		public void TestStringStartsWithLiteral()
		{
			TestQuery(
				"TestStringStartsWithLiteral",
				_db.Customers.Where(c => c.ContactName.StartsWith("M")));
		}

		[TestMethod]
		public void TestStringStartsWithColumn()
		{
			TestQuery(
				"TestStringStartsWithColumn",
				_db.Customers.Where(c => c.ContactName.StartsWith(c.ContactName)));
		}

		[TestMethod]
		public void TestStringEndsWithLiteral()
		{
			TestQuery(
				"TestStringEndsWithLiteral",
				_db.Customers.Where(c => c.ContactName.EndsWith("s")));
		}

		[TestMethod]
		public void TestStringEndsWithColumn()
		{
			TestQuery(
				"TestStringEndsWithColumn",
				_db.Customers.Where(c => c.ContactName.EndsWith(c.ContactName)));
		}

		[TestMethod]
		public void TestStringContainsLiteral()
		{
			TestQuery(
				"TestStringContainsLiteral",
				_db.Customers.Where(c => c.ContactName.Contains("and")));
		}

		[TestMethod]
		public void TestStringContainsColumn()
		{
			TestQuery(
				"TestStringContainsColumn",
				_db.Customers.Where(c => c.ContactName.Contains(c.ContactName)));
		}

		[TestMethod]
		public void TestStringConcatImplicit2Args()
		{
			TestQuery(
				"TestStringConcatImplicit2Args",
				_db.Customers.Where(c => c.ContactName + "X" == "X"));
		}

		[TestMethod]
		public void TestStringConcatExplicit2Args()
		{
			TestQuery(
				"TestStringConcatExplicit2Args",
				_db.Customers.Where(c => string.Concat(c.ContactName, "X") == "X"));
		}

		[TestMethod]
		public void TestStringConcatExplicit3Args()
		{
			TestQuery(
				"TestStringConcatExplicit3Args",
				_db.Customers.Where(c => string.Concat(c.ContactName, "X", c.Country) == "X"));
		}

		[TestMethod]
		public void TestStringConcatExplicitNArgs()
		{
			TestQuery(
				"TestStringConcatExplicitNArgs",
				_db.Customers.Where(c => string.Concat(new string[] { c.ContactName, "X", c.Country }) == "X"));
		}

		[TestMethod]
		public void TestStringIsNullOrEmpty()
		{
			TestQuery(
				"TestStringIsNullOrEmpty",
				_db.Customers.Where(c => string.IsNullOrEmpty(c.City)));
		}

		[TestMethod]
		public void TestStringToUpper()
		{
			TestQuery(
				"TestStringToUpper",
				_db.Customers.Where(c => c.City.ToUpper() == "SEATTLE"));
		}

		[TestMethod]
		public void TestStringToLower()
		{
			TestQuery(
				"TestStringToLower",
				_db.Customers.Where(c => c.City.ToLower() == "seattle"));
		}

		[TestMethod]
		public void TestStringSubstring()
		{
			TestQuery(
				"TestStringSubstring",
				_db.Customers.Where(c => c.City.Substring(0, 4) == "Seat"));
		}

		[TestMethod]
		public void TestStringSubstringNoLength()
		{
			TestQuery(
				"TestStringSubstringNoLength",
				_db.Customers.Where(c => c.City.Substring(4) == "tle"));
		}

		[TestMethod]
		public void TestStringIndexOf()
		{
			TestQuery(
				"TestStringIndexOf",
				_db.Customers.Where(c => c.City.IndexOf("tt") == 4));
		}

		[TestMethod]
		public void TestStringIndexOfChar()
		{
			TestQuery(
				"TestStringIndexOfChar",
				_db.Customers.Where(c => c.City.IndexOf('t') == 4));
		}

		[TestMethod]
		public void TestStringReplace()
		{
			TestQuery(
				"TestStringReplace",
				_db.Customers.Where(c => c.City.Replace("ea", "ae") == "Saettle"));
		}

		[TestMethod]
		public void TestStringReplaceChars()
		{
			TestQuery(
				"TestStringReplaceChars",
				_db.Customers.Where(c => c.City.Replace("e", "y") == "Syattly"));
		}

		[TestMethod]
		public void TestStringTrim()
		{
			TestQuery(
				"TestStringTrim",
				_db.Customers.Where(c => c.City.Trim() == "Seattle"));
		}

		[TestMethod]
		public void TestStringToString()
		{
			TestQuery(
				"TestStringToString",
				_db.Customers.Where(c => c.City.ToString() == "Seattle"));
		}

		[TestMethod]
		public void TestStringRemove()
		{
			TestQuery(
				"TestStringRemove",
				_db.Customers.Where(c => c.City.Remove(1, 2) == "Sttle"));
		}

		[TestMethod]
		public void TestStringRemoveNoCount()
		{
			TestQuery(
				"TestStringRemoveNoCount",
				_db.Customers.Where(c => c.City.Remove(4) == "Seat"));
		}
		
		[TestMethod]
		public void TestDateTimeConstructYmd()
		{
			TestQuery(
				"TestDateTimeConstructYmd",
				_db.Orders.Where(o => o.OrderDate == new DateTime(o.OrderDate.Year, 1, 1)));
		}

		[TestMethod]
		public void TestDateTimeConstructYmdhms()
		{
			TestQuery(
				"TestDateTimeConstructYmdhms",
				_db.Orders.Where(o => o.OrderDate == new DateTime(o.OrderDate.Year, 1, 1, 10, 25, 55)));
		}

		[TestMethod]
		public void TestDateTimeDay()
		{
			TestQuery(
				"TestDateTimeDay",
				_db.Orders.Where(o => o.OrderDate.Day == 5));
		}

		[TestMethod]
		public void TestDateTimeMonth()
		{
			TestQuery(
				"TestDateTimeMonth",
				_db.Orders.Where(o => o.OrderDate.Month == 12));
		}

		[TestMethod]
		public void TestDateTimeYear()
		{
			TestQuery(
				"TestDateTimeYear",
				_db.Orders.Where(o => o.OrderDate.Year == 1997));
		}

		[TestMethod]
		public void TestDateTimeHour()
		{
			TestQuery(
				"TestDateTimeHour",
				_db.Orders.Where(o => o.OrderDate.Hour == 6));
		}

		[TestMethod]
		public void TestDateTimeMinute()
		{
			TestQuery(
				"TestDateTimeMinute",
				_db.Orders.Where(o => o.OrderDate.Minute == 32));
		}

		[TestMethod]
		public void TestDateTimeSecond()
		{
			TestQuery(
				"TestDateTimeSecond",
				_db.Orders.Where(o => o.OrderDate.Second == 47));
		}

		[TestMethod]
		public void TestDateTimeMillisecond()
		{
			TestQuery(
				"TestDateTimeMillisecond",
				_db.Orders.Where(o => o.OrderDate.Millisecond == 200));
		}

		[TestMethod]
		public void TestDateTimeDayOfWeek()
		{
			TestQuery(
				"TestDateTimeDayOfWeek",
				_db.Orders.Where(o => o.OrderDate.DayOfWeek == DayOfWeek.Friday));
		}

		[TestMethod]
		public void TestDateTimeDayOfYear()
		{
			TestQuery(
				"TestDateTimeDayOfYear",
				_db.Orders.Where(o => o.OrderDate.DayOfYear == 360));
		}

		[TestMethod]
		public void TestMathAbs()
		{
			TestQuery(
				"TestMathAbs",
				_db.Orders.Where(o => Math.Abs(o.OrderID) == 10));
		}

		[TestMethod]
		public void TestMathAcos()
		{
			TestQuery(
				"TestMathAcos",
				_db.Orders.Where(o => Math.Acos(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathAsin()
		{
			TestQuery(
				"TestMathAsin",
				_db.Orders.Where(o => Math.Asin(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathAtan()
		{
			TestQuery(
				"TestMathAtan",
				_db.Orders.Where(o => Math.Atan(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathAtan2()
		{
			TestQuery(
				"TestMathAtan2",
				_db.Orders.Where(o => Math.Atan2(o.OrderID, 3) == 0));
		}

		[TestMethod]
		public void TestMathCos()
		{
			TestQuery(
				"TestMathCos",
				_db.Orders.Where(o => Math.Cos(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathSin()
		{
			TestQuery(
				"TestMathSin",
				_db.Orders.Where(o => Math.Sin(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathTan()
		{
			TestQuery(
				"TestMathTan",
				_db.Orders.Where(o => Math.Tan(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathExp()
		{
			TestQuery(
				"TestMathExp",
				_db.Orders.Where(o => Math.Exp(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathLog()
		{
			TestQuery(
				"TestMathLog",
				_db.Orders.Where(o => Math.Log(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathLog10()
		{
			TestQuery(
				"TestMathLog10",
				_db.Orders.Where(o => Math.Log10(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathSqrt()
		{
			TestQuery(
				"TestMathSqrt",
				_db.Orders.Where(o => Math.Sqrt(o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathCeiling()
		{
			TestQuery(
				"TestMathCeiling",
				_db.Orders.Where(o => Math.Ceiling((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathFloor()
		{
			TestQuery(
				"TestMathFloor",
				_db.Orders.Where(o => Math.Floor((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathPow()
		{
			TestQuery(
				"TestMathPow",
				_db.Orders.Where(o => Math.Pow(o.OrderID < 1000 ? 1 : 2, 3) == 0));
		}

		[TestMethod]
		public void TestMathRoundDefault()
		{
			TestQuery(
				"TestMathRoundDefault",
				_db.Orders.Where(o => Math.Round((decimal)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathRoundToPlace()
		{
			TestQuery(
				"TestMathRoundToPlace",
				_db.Orders.Where(o => Math.Round((decimal)o.OrderID, 2) == 0));
		}

		[TestMethod]
		public void TestMathTruncate()
		{
			TestQuery(
				"TestMathTruncate",
				_db.Orders.Where(o => Math.Truncate((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestStringCompareToLessThan()
		{
			TestQuery(
				"TestStringCompareToLessThan",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") < 0));
		}

		[TestMethod]
		public void TestStringCompareToLessThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareToLessThanOrEqualTo",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") <= 0));
		}

		[TestMethod]
		public void TestStringCompareToGreaterThan()
		{
			TestQuery(
				"TestStringCompareToGreaterThan",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") > 0));
		}

		[TestMethod]
		public void TestStringCompareToGreaterThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareToGreaterThanOrEqualTo",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") >= 0));
		}

		[TestMethod]
		public void TestStringCompareToEquals()
		{
			TestQuery(
				"TestStringCompareToEquals",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") == 0));
		}

		[TestMethod]
		public void TestStringCompareToNotEquals()
		{
			TestQuery(
				"TestStringCompareToNotEquals",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") != 0));
		}

		[TestMethod]
		public void TestStringCompareLessThan()
		{
			TestQuery(
				"TestStringCompareLessThan",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") < 0));
		}

		[TestMethod]
		public void TestStringCompareLessThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareLessThanOrEqualTo",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") <= 0));
		}

		[TestMethod]
		public void TestStringCompareGreaterThan()
		{
			TestQuery(
				"TestStringCompareGreaterThan",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") > 0));
		}

		[TestMethod]
		public void TestStringCompareGreaterThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareGreaterThanOrEqualTo",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") >= 0));
		}

		[TestMethod]
		public void TestStringCompareEquals()
		{
			TestQuery(
				"TestStringCompareEquals",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") == 0));
		}

		[TestMethod]
		public void TestStringCompareNotEquals()
		{
			TestQuery(
				"TestStringCompareNotEquals",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") != 0));
		}

		[TestMethod]
		public void TestIntCompareTo()
		{
			TestQuery(
				"TestIntCompareTo",
				_db.Orders.Where(o => o.OrderID.CompareTo(1000) == 0));
		}

		[TestMethod]
		public void TestDecimalCompare()
		{
			TestQuery(
				"TestDecimalCompare",
				_db.Orders.Where(o => decimal.Compare((decimal)o.OrderID, 0.0m) == 0));
		}

		[TestMethod]
		public void TestDecimalAdd()
		{
			TestQuery(
				"TestDecimalAdd",
				_db.Orders.Where(o => decimal.Add(o.OrderID, 0.0m) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalSubtract()
		{
			TestQuery(
				"TestDecimalSubtract",
				_db.Orders.Where(o => decimal.Subtract(o.OrderID, 0.0m) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalMultiply()
		{
			TestQuery(
				"TestDecimalMultiply",
				_db.Orders.Where(o => decimal.Multiply(o.OrderID, 1.0m) == 1.0m));
		}

		[TestMethod]
		public void TestDecimalDivide()
		{
			TestQuery(
				"TestDecimalDivide",
				_db.Orders.Where(o => decimal.Divide(o.OrderID, 1.0m) == 1.0m));
		}

		[TestMethod]
		public void TestDecimalRemainder()
		{
			TestQuery(
				"TestDecimalRemainder",
				_db.Orders.Where(o => decimal.Remainder(o.OrderID, 1.0m) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalNegate()
		{
			TestQuery(
				"TestDecimalNegate",
				_db.Orders.Where(o => decimal.Negate(o.OrderID) == 1.0m));
		}

		[TestMethod]
		public void TestDecimalCeiling()
		{
			TestQuery(
				"TestDecimalCeiling",
				_db.Orders.Where(o => decimal.Ceiling(o.OrderID) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalFloor()
		{
			TestQuery(
				"TestDecimalFloor",
				_db.Orders.Where(o => decimal.Floor(o.OrderID) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalRoundDefault()
		{
			TestQuery(
				"TestDecimalRoundDefault",
				_db.Orders.Where(o => decimal.Round(o.OrderID) == 0m));
		}

		[TestMethod]
		public void TestDecimalRoundPlaces()
		{
			TestQuery(
				"TestDecimalRoundPlaces",
				_db.Orders.Where(o => decimal.Round(o.OrderID, 2) == 0.00m));
		}

		[TestMethod]
		public void TestDecimalTruncate()
		{
			TestQuery(
				"TestDecimalTruncate",
				_db.Orders.Where(o => decimal.Truncate(o.OrderID) == 0m));
		}

		[TestMethod]
		public void TestDecimalLessThan()
		{
			TestQuery(
				"TestDecimalLessThan",
				_db.Orders.Where(o => ((decimal)o.OrderID) < 0.0m));
		}

		[TestMethod]
		public void TestIntLessThan()
		{
			TestQuery(
				"TestIntLessThan",
				_db.Orders.Where(o => o.OrderID < 0));
		}

		[TestMethod]
		public void TestIntLessThanOrEqual()
		{
			TestQuery(
				"TestIntLessThanOrEqual",
				_db.Orders.Where(o => o.OrderID <= 0));
		}

		[TestMethod]
		public void TestIntGreaterThan()
		{
			TestQuery(
				"TestIntGreaterThan",
				_db.Orders.Where(o => o.OrderID > 0));
		}

		[TestMethod]
		public void TestIntGreaterThanOrEqual()
		{
			TestQuery(
				"TestIntGreaterThanOrEqual",
				_db.Orders.Where(o => o.OrderID >= 0));
		}

		[TestMethod]
		public void TestIntEqual()
		{
			TestQuery(
				"TestIntEqual",
				_db.Orders.Where(o => o.OrderID == 0));
		}

		[TestMethod]
		public void TestIntNotEqual()
		{
			TestQuery(
				"TestIntNotEqual",
				_db.Orders.Where(o => o.OrderID != 0));
		}

		[TestMethod]
		public void TestIntAdd()
		{
			TestQuery(
				"TestIntAdd",
				_db.Orders.Where(o => o.OrderID + 0 == 0));
		}

		[TestMethod]
		public void TestIntSubtract()
		{
			TestQuery(
				"TestIntSubtract",
				_db.Orders.Where(o => o.OrderID - 0 == 0));
		}

		[TestMethod]
		public void TestIntMultiply()
		{
			TestQuery(
				"TestIntMultiply",
				_db.Orders.Where(o => o.OrderID * 1 == 1));
		}

		[TestMethod]
		public void TestIntDivide()
		{
			TestQuery(
				"TestIntDivide",
				_db.Orders.Where(o => o.OrderID / 1 == 1));
		}

		[TestMethod]
		public void TestIntModulo()
		{
			TestQuery(
				"TestIntModulo",
				_db.Orders.Where(o => o.OrderID % 1 == 0));
		}

		[TestMethod]
		public void TestIntLeftShift()
		{
			TestQuery(
				"TestIntLeftShift",
				_db.Orders.Where(o => o.OrderID << 1 == 0));
		}

		[TestMethod]
		public void TestIntRightShift()
		{
			TestQuery(
				"TestIntRightShift",
				_db.Orders.Where(o => o.OrderID >> 1 == 0));
		}

		[TestMethod]
		public void TestIntBitwiseAnd()
		{
			TestQuery(
				"TestIntBitwiseAnd",
				_db.Orders.Where(o => (o.OrderID & 1) == 0));
		}

		[TestMethod]
		public void TestIntBitwiseOr()
		{
			TestQuery(
				"TestIntBitwiseOr",
				_db.Orders.Where(o => (o.OrderID | 1) == 1));
		}

		[TestMethod]
		public void TestIntBitwiseExclusiveOr()
		{
			TestQuery(
				"TestIntBitwiseExclusiveOr",
				_db.Orders.Where(o => (o.OrderID ^ 1) == 1));
		}

		////[TestMethod]
		////public void TestIntBitwiseNot()
		////{
		////	TestQuery(
		////		"TestIntBitwiseNot",
		////		db.Orders.Where(o => ~o.OrderID == 0));
		////}

		[TestMethod]
		public void TestIntNegate()
		{
			TestQuery(
				"TestIntNegate",
				_db.Orders.Where(o => -o.OrderID == -1));
		}

		[TestMethod]
		public void TestAnd()
		{
			TestQuery(
				"TestAnd",
				_db.Orders.Where(o => o.OrderID > 0 && o.OrderID < 2000));
		}

		[TestMethod]
		public void TestOr()
		{
			TestQuery(
				"TestOr",
				_db.Orders.Where(o => o.OrderID < 5 || o.OrderID > 10));
		}

		[TestMethod]
		public void TestNot()
		{
			TestQuery(
				"TestNot",
				_db.Orders.Where(o => !(o.OrderID == 0)));
		}

		[TestMethod]
		public void TestEqualNull()
		{
			TestQuery(
				"TestEqualNull",
				_db.Customers.Where(c => c.City == null));
		}

		[TestMethod]
		public void TestEqualNullReverse()
		{
			TestQuery(
				"TestEqualNullReverse",
				_db.Customers.Where(c => null == c.City));
		}

		[TestMethod]
		public void TestConditional()
		{
			TestQuery(
				"TestConditional",
				_db.Orders.Where(o => (o.CustomerID == "ALFKI" ? 1000 : 0) == 1000));
		}

		[TestMethod]
		public void TestConditional2()
		{
			TestQuery(
				"TestConditional2",
				_db.Orders.Where(o => (o.CustomerID == "ALFKI" ? 1000 : o.CustomerID == "ABCDE" ? 2000 : 0) == 1000));
		}

		[TestMethod]
		public void TestConditionalTestIsValue()
		{
			TestQuery(
				"TestConditionalTestIsValue",
				_db.Orders.Where(o => (((bool)(object)o.OrderID) ? 100 : 200) == 100));
		}

		////[TestMethod]
		////public void TestConditionalResultsArePredicates()
		////{
		////	TestQuery(
		////		"TestConditionalResultsArePredicates",
		////		db.Orders.Where(o => (o.CustomerID == "ALFKI" ? o.OrderID < 10 : o.OrderID > 10)));
		////}

		////[TestMethod]
		////public void TestSelectManyJoined()
		////{
		////	TestQuery(
		////		"TestSelectManyJoined",
		////		from c in db.Customers
		////		from o in db.Orders.Where(o => o.CustomerID == c.CustomerID)
		////		select new { c.ContactName, o.OrderDate });
		////}

		////[TestMethod]
		////public void TestSelectManyJoinedDefaultIfEmpty()
		////{
		////	TestQuery(
		////		"TestSelectManyJoinedDefaultIfEmpty",
		////		from c in db.Customers
		////		from o in db.Orders.Where(o => o.CustomerID == c.CustomerID).DefaultIfEmpty()
		////		select new { c.ContactName, o.OrderDate });
		////}

		////[TestMethod]
		////public void TestSelectWhereAssociation()
		////{
		////	TestQuery(
		////		"TestSelectWhereAssociation",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle"
		////		select o);
		////}

		////[TestMethod]
		////public void TestSelectWhereAssociations()
		////{
		////	TestQuery(
		////		"TestSelectWhereAssociations",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle" && o.Customer.Phone != "555 555 5555"
		////		select o);
		////}

		////[TestMethod]
		////public void TestSelectWhereAssociationTwice()
		////{
		////	TestQuery(
		////		"TestSelectWhereAssociationTwice",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle" && o.Customer.Phone != "555 555 5555"
		////		select o);
		////}

		////[TestMethod]
		////public void TestSelectAssociation()
		////{
		////	TestQuery(
		////		"TestSelectAssociation",
		////		from o in db.Orders
		////		select o.Customer);
		////}

		////[TestMethod]
		////public void TestSelectAssociations()
		////{
		////	TestQuery(
		////		"TestSelectAssociations",
		////		from o in db.Orders
		////		select new { A = o.Customer, B = o.Customer });
		////}

		////[TestMethod]
		////public void TestSelectAssociationsWhereAssociations()
		////{
		////	TestQuery(
		////		"TestSelectAssociationsWhereAssociations",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle"
		////		where o.Customer.Phone != "555 555 5555"
		////		select new { A = o.Customer, B = o.Customer });
		////}

		////[TestMethod]
		////public void TestSingletonAssociationWithMemberAccess()
		////{
		////	TestQuery(
		////		"TestSingletonAssociationWithMemberAccess",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle"
		////		where o.Customer.Phone != "555 555 5555"
		////		select new { A = o.Customer, B = o.Customer.City }
		////		);
		////}

		[TestMethod]
		public void TestCompareDateTimesWithDifferentNullability()
		{
			var today = new DateTime(2013, 1, 1);
			TestQuery(
				"TestCompareDateTimesWithDifferentNullability",
				from o in _db.Orders
				where o.OrderDate < today && ((DateTime?)o.OrderDate) < today
				select o
				);
		}

		[TestMethod]
		public void TestContainsWithEmptyLocalList()
		{
			var ids = new string[0];
			TestQuery(
				"TestContainsWithEmptyLocalList",
				from c in _db.Customers
				where ids.Contains(c.CustomerID)
				select c
				);
		}

		[TestMethod]
		public void TestContainsWithSubquery2()
		{
			var custsInLondon = _db.Customers.Where(c => c.City == "London").Select(c => c.CustomerID);

			TestQuery(
				"TestContainsWithSubquery2",
				from c in _db.Customers
				where custsInLondon.Contains(c.CustomerID)
				select c
				);
		}

		////[TestMethod]
		////public void TestCombineQueriesDeepNesting()
		////{
		////	var custs = db.Customers.Where(c => c.ContactName.StartsWith("xxx"));
		////	var ords = db.Orders.Where(o => custs.Any(c => c.CustomerID == o.CustomerID));
		////	TestQuery(
		////		"TestCombineQueriesDeepNesting",
		////		db.OrderDetails.Where(d => ords.Any(o => o.OrderID == d.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void TestLetWithSubquery()
		////{
		////	TestQuery(
		////		"TestLetWithSubquery",
		////		from customer in db.Customers
		////		let orders =
		////			from order in db.Orders
		////			where order.CustomerID == customer.CustomerID
		////			select order
		////		select new
		////		{
		////			Customer = customer,
		////			OrdersCount = orders.Count(),
		////		}
		////		);
		////}

		protected void TestQuery(string baseline, IQueryable query)
		{
			TestQuery(baseline, query.Expression);
		}

		protected void TestQuery(string baseline, Expression<Func<object>> query)
		{
			TestQuery(baseline, query.Body);
		}

		private void TestQuery(string baseline, Expression query)
		{
			if (query.NodeType == ExpressionType.Convert && query.Type == typeof(object))
			{
				// Remove boxing
				query = ((UnaryExpression)query).Operand;
			}

			var expected = "(" + TrimExtraWhiteSpace(_baselines[baseline].Replace("\n\n", ") (")) + ")";
			var select = _db.BuildSelectStatement(query);
			var actual = TrimExtraWhiteSpace(select.ToString());

			Assert.AreEqual(expected, actual);
		}

		private string TrimExtraWhiteSpace(string s)
		{
			var result = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
			while (result.Contains("  "))
			{
				result = result.Replace("  ", " ");
			}
			result = result.Replace("( ", "(").Replace(" )", ")");
			return result;
		}
	}
}

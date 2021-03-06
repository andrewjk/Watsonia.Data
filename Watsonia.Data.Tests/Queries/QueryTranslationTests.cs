﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Tests.Queries.Northwind;

// TODO: Implement all double commented (////) tests

namespace Watsonia.Data.Tests.Queries
{
	[TestClass]
	public class QueryTranslationTests
	{
		private static readonly NorthwindDatabase _db = new NorthwindDatabase();
		private static Dictionary<string, string> _baselines = new Dictionary<string, string>();

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			var fileName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Queries\QueryTranslationBaselines.xml";
			if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
			{
				var doc = XDocument.Load(fileName);
				_baselines = doc.Root.Elements("baseline").ToDictionary(e => (string)e.Attribute("key"), e => e.Value);
			}
		}

		[TestMethod]
		public void Where()
		{
			TestQuery(
				"TestWhere",
				_db.Customers.Where(c => c.City == "London"));
		}

		[TestMethod]
		public void CompareEntityEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			TestQuery(
				"TestCompareEntityEqual",
				_db.Customers.Where(c => c == alfki));
		}

		[TestMethod]
		public void CompareEntityNotEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			TestQuery(
				"TestCompareEntityNotEqual",
				_db.Customers.Where(c => c != alfki));
		}

		[TestMethod]
		public void CompareConstructedEqual()
		{
			TestQuery(
				"TestCompareConstructedEqual",
				_db.Customers.Where(c => new { x = c.City } == new { x = "London" }));
		}

		[TestMethod]
		public void CompareConstructedMultiValueEqual()
		{
			TestQuery(
				"TestCompareConstructedMultiValueEqual",
				_db.Customers.Where(c => new { x = c.City, y = c.Country } == new { x = "London", y = "UK" }));
		}

		[TestMethod]
		public void CompareConstructedMultiValueNotEqual()
		{
			TestQuery(
				"TestCompareConstructedMultiValueNotEqual",
				_db.Customers.Where(c => new { x = c.City, y = c.Country } != new { x = "London", y = "UK" }));
		}

		[TestMethod]
		public void CompareConstructed()
		{
			TestQuery(
				"TestCompareConstructed",
				_db.Customers.Where(c => new { x = c.City } == new { x = "London" }));
		}

		[TestMethod]
		public void SelectScalar()
		{
			TestQuery(
				"TestSelectScalar",
				_db.Customers.Select(c => c.City));
		}

		[TestMethod]
		public void SelectAnonymousOne()
		{
			TestQuery(
				"TestSelectAnonymousOne",
				_db.Customers.Select(c => new { c.City }));
		}

		////[TestMethod]
		////public void SelectAnonymousTwo()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousTwo",
		////		db.Customers.Select(c => new { c.City, c.Phone }));
		////}

		////[TestMethod]
		////public void SelectAnonymousThree()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousThree",
		////		db.Customers.Select(c => new { c.City, c.Phone, c.Country }));
		////}

		[TestMethod]
		public void SelectCustomerTable()
		{
			TestQuery(
				"TestSelectCustomerTable",
				_db.Customers);
		}

		[TestMethod]
		public void SelectCustomerIdentity()
		{
			TestQuery(
				"TestSelectCustomerIdentity",
				_db.Customers.Select(c => c));
		}

		////[TestMethod]
		////public void SelectAnonymousWithObject()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousWithObject",
		////		db.Customers.Select(c => new { c.City, c }));
		////}

		////[TestMethod]
		////public void SelectAnonymousNested()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousNested",
		////		db.Customers.Select(c => new { c.City, Country = new { c.Country } }));
		////}

		////[TestMethod]
		////public void SelectAnonymousEmpty()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousEmpty",
		////		db.Customers.Select(c => new { }));
		////}

		////[TestMethod]
		////public void SelectAnonymousLiteral()
		////{
		////	TestQuery(
		////		"TestSelectAnonymousLiteral",
		////		db.Customers.Select(c => new { X = 10 }));
		////}

		[TestMethod]
		public void SelectConstantInt()
		{
			TestQuery(
				"TestSelectConstantInt",
				_db.Customers.Select(c => 0));
		}

		[TestMethod]
		public void SelectConstantNullString()
		{
			TestQuery(
				"TestSelectConstantNullString",
				_db.Customers.Select(c => (string)null));
		}

		[TestMethod]
		public void SelectLocal()
		{
			var x = 10;
			TestQuery(
				"TestSelectLocal",
				_db.Customers.Select(c => x));
		}

		////[TestMethod]
		////public void SelectNestedCollection()
		////{
		////	TestQuery(
		////		"TestSelectNestedCollection",
		////		from c in db.Customers
		////		where c.City == "London"
		////		select db.Orders.Where(o => o.CustomerID == c.CustomerID && o.OrderDate.Year == 1997).Select(o => o.OrderID));
		////}

		////[TestMethod]
		////public void SelectNestedCollectionInAnonymousType()
		////{
		////	TestQuery(
		////		"TestSelectNestedCollectionInAnonymousType",
		////		from c in db.Customers
		////		where c.CustomerID == "ALFKI"
		////		select new { Foos = db.Orders.Where(o => o.CustomerID == c.CustomerID && o.OrderDate.Year == 1997).Select(o => o.OrderID) });
		////}

		////[TestMethod]
		////public void JoinCustomerOrders()
		////{
		////	TestQuery(
		////		"TestJoinCustomerOrders",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID
		////		select new { c.ContactName, o.OrderID });
		////}

		////[TestMethod]
		////public void JoinMultiKey()
		////{
		////	TestQuery(
		////		"TestJoinMultiKey",
		////		from c in db.Customers
		////		join o in db.Orders on new { a = c.CustomerID, b = c.CustomerID } equals new { a = o.CustomerID, b = o.CustomerID }
		////		select new { c, o });
		////}

		////[TestMethod]
		////public void JoinIntoCustomersOrders()
		////{
		////	TestQuery(
		////		"TestJoinIntoCustomersOrders",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		select new { cust = c, ords = ords.ToList() });
		////}

		////[TestMethod]
		////public void JoinIntoCustomersOrdersCount()
		////{
		////	TestQuery(
		////		"TestJoinIntoCustomersOrdersCount",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		select new { cust = c, ords = ords.Count() });
		////}

		////[TestMethod]
		////public void JoinIntoDefaultIfEmpty()
		////{
		////	TestQuery(
		////		"TestJoinIntoDefaultIfEmpty",
		////		from c in db.Customers
		////		join o in db.Orders on c.CustomerID equals o.CustomerID into ords
		////		from o in ords.DefaultIfEmpty()
		////		select new { c, o });
		////}

		////[TestMethod]
		////public void SelectManyCustomerOrders()
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
		////public void MultipleJoinsWithJoinConditionsInWhere()
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
		////public void MultipleJoinsWithMissingJoinCondition()
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
		public void OrderBy()
		{
			TestQuery(
				"TestOrderBy",
				_db.Customers.OrderBy(c => c.CustomerID)
				);
		}

		[TestMethod]
		public void OrderBySelect()
		{
			TestQuery(
				"TestOrderBySelect",
				_db.Customers.OrderBy(c => c.CustomerID).Select(c => c.ContactName)
				);
		}

		[TestMethod]
		public void OrderByOrderBy()
		{
			TestQuery(
				"TestOrderByOrderBy",
				_db.Customers.OrderBy(c => c.CustomerID).OrderBy(c => c.Country).Select(c => c.City)
				);
		}

		[TestMethod]
		public void OrderByThenBy()
		{
			TestQuery(
				"TestOrderByThenBy",
				_db.Customers.OrderBy(c => c.CustomerID).ThenBy(c => c.Country).Select(c => c.City)
				);
		}

		[TestMethod]
		public void OrderByDescending()
		{
			TestQuery(
				"TestOrderByDescending",
				_db.Customers.OrderByDescending(c => c.CustomerID).Select(c => c.City)
				);
		}

		[TestMethod]
		public void OrderByDescendingThenBy()
		{
			TestQuery(
				"TestOrderByDescendingThenBy",
				_db.Customers.OrderByDescending(c => c.CustomerID).ThenBy(c => c.Country).Select(c => c.City)
				);
		}

		[TestMethod]
		public void OrderByDescendingThenByDescending()
		{
			TestQuery(
				"TestOrderByDescendingThenByDescending",
				_db.Customers.OrderByDescending(c => c.CustomerID).ThenByDescending(c => c.Country).Select(c => c.City)
				);
		}

		////[TestMethod]
		////public void OrderByJoin()
		////{
		////	TestQuery(
		////		"TestOrderByJoin",
		////		from c in db.Customers.OrderBy(c => c.CustomerID)
		////		join o in db.Orders.OrderBy(o => o.OrderID) on c.CustomerID equals o.CustomerID
		////		select new { CustomerID = c.CustomerID, o.OrderID }
		////		);
		////}

		////[TestMethod]
		////public void OrderBySelectMany()
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
		////public void GroupBy()
		////{
		////	TestQuery(
		////		"TestGroupBy",
		////		db.Customers.GroupBy(c => c.City)
		////		);
		////}

		////[TestMethod]
		////public void GroupBySelectMany()
		////{
		////	TestQuery(
		////		"TestGroupBySelectMany",
		////		db.Customers.GroupBy(c => c.City).SelectMany(g => g)
		////		);
		////}

		////[TestMethod]
		////public void GroupBySum()
		////{
		////	TestQuery(
		////		"TestGroupBySum",
		////		db.Orders.GroupBy(o => o.CustomerID).Select(g => g.Sum(o => o.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void GroupByCount()
		////{
		////	TestQuery(
		////		"TestGroupByCount",
		////		db.Orders.GroupBy(o => o.CustomerID).Select(g => g.Count())
		////		);
		////}

		////[TestMethod]
		////public void GroupByLongCount()
		////{
		////	TestQuery(
		////		"TestGroupByLongCount",
		////		db.Orders.GroupBy(o => o.CustomerID).Select(g => g.LongCount()));
		////}

		////[TestMethod]
		////public void GroupBySumMinMaxAvg()
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
		////public void GroupByWithResultSelector()
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
		////public void GroupByWithElementSelectorSum()
		////{
		////	TestQuery(
		////		"TestGroupByWithElementSelectorSum",
		////		db.Orders.GroupBy(o => o.CustomerID, o => o.OrderID).Select(g => g.Sum())
		////		);
		////}

		////[TestMethod]
		////public void GroupByWithElementSelector()
		////{
		////	TestQuery(
		////		"TestGroupByWithElementSelector",
		////		db.Orders.GroupBy(o => o.CustomerID, o => o.OrderID)
		////		);
		////}

		////[TestMethod]
		////public void GroupByWithElementSelectorSumMax()
		////{
		////	TestQuery(
		////		"TestGroupByWithElementSelectorSumMax",
		////		db.Orders.GroupBy(o => o.CustomerID, o => o.OrderID).Select(g => new { Sum = g.Sum(), Max = g.Max() })
		////		);
		////}

		////[TestMethod]
		////public void GroupByWithAnonymousElement()
		////{
		////	TestQuery(
		////		"TestGroupByWithAnonymousElement",
		////		db.Orders.GroupBy(o => o.CustomerID, o => new { o.OrderID }).Select(g => g.Sum(x => x.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void GroupByWithTwoPartKey()
		////{
		////	TestQuery(
		////		"TestGroupByWithTwoPartKey",
		////		db.Orders.GroupBy(o => new { CustomerID = o.CustomerID, o.OrderDate }).Select(g => g.Sum(o => o.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void OrderByGroupBy()
		////{
		////	TestQuery(
		////		"TestOrderByGroupBy",
		////		db.Orders.OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).Select(g => g.Sum(o => o.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void OrderByGroupBySelectMany()
		////{
		////	TestQuery(
		////		"TestOrderByGroupBySelectMany",
		////		db.Orders.OrderBy(o => o.OrderID).GroupBy(o => o.CustomerID).SelectMany(g => g)
		////		);
		////}

		[TestMethod]
		public void SumWithNoArg()
		{
			TestQuery(
				"TestSumWithNoArg",
				() => _db.Orders.Select(o => o.OrderID).Sum()
				);
		}

		[TestMethod]
		public void SumWithArg()
		{
			TestQuery(
				"TestSumWithArg",
				() => _db.Orders.Sum(o => o.OrderID)
				);
		}

		[TestMethod]
		public void CountWithNoPredicate()
		{
			TestQuery(
				"TestCountWithNoPredicate",
				() => _db.Orders.Count()
				);
		}

		[TestMethod]
		public void CountWithPredicate()
		{
			TestQuery(
				"TestCountWithPredicate",
				() => _db.Orders.Count(o => o.CustomerID == "ALFKI")
				);
		}

		[TestMethod]
		public void Distinct()
		{
			TestQuery(
				"TestDistinct",
				_db.Customers.Distinct()
				);
		}

		[TestMethod]
		public void DistinctScalar()
		{
			TestQuery(
				"TestDistinctScalar",
				_db.Customers.Select(c => c.City).Distinct()
				);
		}

		[TestMethod]
		public void OrderByDistinct()
		{
			TestQuery(
				"TestOrderByDistinct",
				_db.Customers.OrderBy(c => c.CustomerID).Select(c => c.City).Distinct()
				);
		}

		////[TestMethod]
		////public void DistinctOrderBy()
		////{
		////	TestQuery(
		////		"TestDistinctOrderBy",
		////		db.Customers.Select(c => c.City).Distinct().OrderBy(c => c)
		////		);
		////}

		////[TestMethod]
		////public void DistinctGroupBy()
		////{
		////	TestQuery(
		////		"TestDistinctGroupBy",
		////		db.Orders.Distinct().GroupBy(o => o.CustomerID)
		////		);
		////}

		////[TestMethod]
		////public void GroupByDistinct()
		////{
		////	TestQuery(
		////		"TestGroupByDistinct",
		////		db.Orders.GroupBy(o => o.CustomerID).Distinct()
		////		);

		////}

		[TestMethod]
		public void DistinctCount()
		{
			TestQuery(
				"TestDistinctCount",
				() => _db.Customers.Distinct().Count()
				);
		}

		////[TestMethod]
		////public void SelectDistinctCount()
		////{
		////	TestQuery(
		////		"TestSelectDistinctCount",
		////		() => db.Customers.Select(c => c.City).Distinct().Count()
		////		);
		////}

		////[TestMethod]
		////public void SelectSelectDistinctCount()
		////{
		////	TestQuery(
		////		"TestSelectSelectDistinctCount",
		////		() => db.Customers.Select(c => c.City).Select(c => c).Distinct().Count()
		////		);
		////}

		////[TestMethod]
		////public void DistinctCountPredicate()
		////{
		////	TestQuery(
		////		"TestDistinctCountPredicate",
		////		() => db.Customers.Distinct().Count(c => c.CustomerID == "ALFKI")
		////		);
		////}

		////[TestMethod]
		////public void DistinctSumWithArg()
		////{
		////	TestQuery(
		////		"TestDistinctSumWithArg",
		////		() => db.Orders.Distinct().Sum(o => o.OrderID)
		////		);
		////}

		////[TestMethod]
		////public void SelectDistinctSum()
		////{
		////	TestQuery(
		////		"TestSelectDistinctSum",
		////		() => db.Orders.Select(o => o.OrderID).Distinct().Sum()
		////		);
		////}

		////[TestMethod]
		////public void Take()
		////{
		////	TestQuery(
		////		"TestTake",
		////		db.Orders.Take(5)
		////		);
		////}

		////[TestMethod]
		////public void TakeDistinct()
		////{
		////	TestQuery(
		////		"TestTakeDistinct",
		////		db.Orders.Take(5).Distinct()
		////		);
		////}

		[TestMethod]
		public void DistinctTake()
		{
			TestQuery(
				"TestDistinctTake",
				_db.Orders.Distinct().Take(5)
				);
		}

		////[TestMethod]
		////public void DistinctTakeCount()
		////{
		////	TestQuery(
		////		"TestDistinctTakeCount",
		////		() => db.Orders.Distinct().Take(5).Count()
		////		);
		////}

		////[TestMethod]
		////public void TakeDistinctCount()
		////{
		////	TestQuery(
		////		"TestTakeDistinctCount",
		////		() => db.Orders.Take(5).Distinct().Count()
		////		);
		////}

		[TestMethod]
		public void Skip()
		{
			TestQuery(
				"TestSkip",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5)
				);
		}

		[TestMethod]
		public void TakeSkip()
		{
			TestQuery(
				"TestTakeSkip",
				_db.Customers.OrderBy(c => c.ContactName).Take(10).Skip(5)
				);
		}

		////[TestMethod]
		////public void DistinctSkip()
		////{
		////	TestQuery(
		////		"TestDistinctSkip",
		////		db.Customers.Distinct().OrderBy(c => c.ContactName).Skip(5)
		////		);
		////}

		[TestMethod]
		public void SkipTake()
		{
			TestQuery(
				"TestSkipTake",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5).Take(10)
				);
		}

		////[TestMethod]
		////public void DistinctSkipTake()
		////{
		////	TestQuery(
		////		"TestDistinctSkipTake",
		////		db.Customers.Distinct().OrderBy(c => c.ContactName).Skip(5).Take(10)
		////		);
		////}

		[TestMethod]
		public void SkipDistinct()
		{
			TestQuery(
				"TestSkipDistinct",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5).Distinct()
				);
		}

		[TestMethod]
		public void SkipTakeDistinct()
		{
			TestQuery(
				"TestSkipTakeDistinct",
				_db.Customers.OrderBy(c => c.ContactName).Skip(5).Take(10).Distinct()
				);
		}

		////[TestMethod]
		////public void TakeSkipDistinct()
		////{
		////	TestQuery(
		////		"TestTakeSkipDistinct",
		////		db.Customers.OrderBy(c => c.ContactName).Take(10).Skip(5).Distinct()
		////		);
		////}

		[TestMethod]
		public void First()
		{
			TestQuery(
				"TestFirst",
				() => _db.Customers.OrderBy(c => c.ContactName).First()
				);
		}

		[TestMethod]
		public void FirstPredicate()
		{
			TestQuery(
				"TestFirstPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).First(c => c.City == "London")
				);
		}

		[TestMethod]
		public void WhereFirst()
		{
			TestQuery(
				"TestWhereFirst",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").First()
				);
		}

		[TestMethod]
		public void FirstOrDefault()
		{
			TestQuery(
				"TestFirstOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).FirstOrDefault()
				);
		}

		[TestMethod]
		public void FirstOrDefaultPredicate()
		{
			TestQuery(
				"TestFirstOrDefaultPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).FirstOrDefault(c => c.City == "London")
				);
		}

		[TestMethod]
		public void WhereFirstOrDefault()
		{
			TestQuery(
				"TestWhereFirstOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").FirstOrDefault()
				);
		}

		[TestMethod]
		public void Reverse()
		{
			TestQuery(
				"TestReverse",
				_db.Customers.OrderBy(c => c.ContactName).Reverse()
				);
		}

		[TestMethod]
		public void ReverseReverse()
		{
			TestQuery(
				"TestReverseReverse",
				_db.Customers.OrderBy(c => c.ContactName).Reverse().Reverse()
				);
		}

		////[TestMethod]
		////public void ReverseWhereReverse()
		////{
		////	TestQuery(
		////		"TestReverseWhereReverse",
		////		db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Reverse()
		////		);
		////}

		////[TestMethod]
		////public void ReverseTakeReverse()
		////{
		////	TestQuery(
		////		"TestReverseTakeReverse",
		////		db.Customers.OrderBy(c => c.ContactName).Reverse().Take(5).Reverse()
		////		);
		////}

		////[TestMethod]
		////public void ReverseWhereTakeReverse()
		////{
		////	TestQuery(
		////		"TestReverseWhereTakeReverse",
		////		db.Customers.OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Take(5).Reverse()
		////		);
		////}

		[TestMethod]
		public void Last()
		{
			TestQuery(
				"TestLast",
				() => _db.Customers.OrderBy(c => c.ContactName).Last()
				);
		}

		[TestMethod]
		public void LastPredicate()
		{
			TestQuery(
				"TestLastPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).Last(c => c.City == "London")
				);
		}

		[TestMethod]
		public void WhereLast()
		{
			TestQuery(
				"TestWhereLast",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").Last()
				);
		}

		[TestMethod]
		public void LastOrDefault()
		{
			TestQuery(
				"TestLastOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).LastOrDefault()
				);
		}

		[TestMethod]
		public void LastOrDefaultPredicate()
		{
			TestQuery(
				"TestLastOrDefaultPredicate",
				() => _db.Customers.OrderBy(c => c.ContactName).LastOrDefault(c => c.City == "London")
				);
		}

		[TestMethod]
		public void WhereLastOrDefault()
		{
			TestQuery(
				"TestWhereLastOrDefault",
				() => _db.Customers.OrderBy(c => c.ContactName).Where(c => c.City == "London").LastOrDefault()
				);
		}

		[TestMethod]
		public void Single()
		{
			TestQuery(
				"TestSingle",
				() => _db.Customers.Single());
		}

		[TestMethod]
		public void SinglePredicate()
		{
			TestQuery(
				"TestSinglePredicate",
				() => _db.Customers.Single(c => c.CustomerID == "ALFKI")
				);
		}

		[TestMethod]
		public void WhereSingle()
		{
			TestQuery(
				"TestWhereSingle",
				() => _db.Customers.Where(c => c.CustomerID == "ALFKI").Single()
				);
		}

		////[TestMethod]
		////public void SingleOrDefault()
		////{
		////	TestQuery(
		////		"TestSingleOrDefault",
		////		() => db.Customers.SingleOrDefault());
		////}

		[TestMethod]
		public void SingleOrDefaultPredicate()
		{
			TestQuery(
				"TestSingleOrDefaultPredicate",
				() => _db.Customers.SingleOrDefault(c => c.CustomerID == "ALFKI")
				);
		}

		[TestMethod]
		public void WhereSingleOrDefault()
		{
			TestQuery(
				"TestWhereSingleOrDefault",
				() => _db.Customers.Where(c => c.CustomerID == "ALFKI").SingleOrDefault()
				);
		}

		////[TestMethod]
		////public void AnyWithSubquery()
		////{
		////	TestQuery(
		////		"TestAnyWithSubquery",
		////		db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).Any(o => o.OrderDate.Year == 1997))
		////		);
		////}

		////[TestMethod]
		////public void AnyWithSubqueryNoPredicate()
		////{
		////	TestQuery(
		////		"TestAnyWithSubqueryNoPredicate",
		////		db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).Any())
		////		);
		////}

		////[TestMethod]
		////public void AnyWithLocalCollection()
		////{
		////	string[] ids = new[] { "ABCDE", "ALFKI" };
		////	TestQuery(
		////		"TestAnyWithLocalCollection",
		////		db.Customers.Where(c => ids.Any(id => c.CustomerID == id))
		////		);
		////}

		[TestMethod]
		public void AnyTopLevel()
		{
			TestQuery(
				"TestAnyTopLevel",
				() => _db.Customers.Any()
				);
		}

		////[TestMethod]
		////public void AllWithSubquery()
		////{
		////	TestQuery(
		////		"TestAllWithSubquery",
		////		db.Customers.Where(c => db.Orders.Where(o => o.CustomerID == c.CustomerID).All(o => o.OrderDate.Year == 1997))
		////		);
		////}

		////[TestMethod]
		////public void AllWithLocalCollection()
		////{
		////	string[] patterns = new[] { "a", "e" };

		////	TestQuery(
		////		"TestAllWithLocalCollection",
		////		db.Customers.Where(c => patterns.All(p => c.ContactName.Contains(p)))
		////		);
		////}

		[TestMethod]
		public void AllTopLevel()
		{
			TestQuery(
				"TestAllTopLevel",
				() => _db.Customers.All(c => c.ContactName.StartsWith("a"))
				);
		}

		[TestMethod]
		public void ContainsWithSubquery()
		{
			TestQuery(
				"TestContainsWithSubquery",
				_db.Customers.Where(c => _db.Orders.Select(o => o.CustomerID).Contains(c.CustomerID))
				);
		}

		[TestMethod]
		public void ContainsWithLocalCollection()
		{
			var ids = new[] { "ABCDE", "ALFKI" };
			TestQuery(
				"TestContainsWithLocalCollection",
				_db.Customers.Where(c => ids.Contains(c.CustomerID))
				);
		}

		[TestMethod]
		public void ContainsTopLevel()
		{
			TestQuery(
				"TestContainsTopLevel",
				() => _db.Customers.Select(c => c.CustomerID).Contains("ALFKI")
				);
		}

		////[TestMethod]
		////public void Coalesce()
		////{
		////	TestQuery(
		////		"TestCoalesce",
		////		db.Customers.Where(c => (c.City ?? "Seattle") == "Seattle"));
		////}

		////[TestMethod]
		////public void Coalesce2()
		////{
		////	TestQuery(
		////		"TestCoalesce2",
		////		db.Customers.Where(c => (c.City ?? c.Country ?? "Seattle") == "Seattle"));
		////}

		[TestMethod]
		public void StringLength()
		{
			TestQuery(
				"TestStringLength",
				_db.Customers.Where(c => c.City.Length == 7));
		}

		[TestMethod]
		public void StringStartsWithLiteral()
		{
			TestQuery(
				"TestStringStartsWithLiteral",
				_db.Customers.Where(c => c.ContactName.StartsWith("M")));
		}

		[TestMethod]
		public void StringStartsWithColumn()
		{
			TestQuery(
				"TestStringStartsWithColumn",
				_db.Customers.Where(c => c.ContactName.StartsWith(c.ContactName)));
		}

		[TestMethod]
		public void StringEndsWithLiteral()
		{
			TestQuery(
				"TestStringEndsWithLiteral",
				_db.Customers.Where(c => c.ContactName.EndsWith("s")));
		}

		[TestMethod]
		public void StringEndsWithColumn()
		{
			TestQuery(
				"TestStringEndsWithColumn",
				_db.Customers.Where(c => c.ContactName.EndsWith(c.ContactName)));
		}

		[TestMethod]
		public void StringContainsLiteral()
		{
			TestQuery(
				"TestStringContainsLiteral",
				_db.Customers.Where(c => c.ContactName.Contains("and")));
		}

		[TestMethod]
		public void StringContainsColumn()
		{
			TestQuery(
				"TestStringContainsColumn",
				_db.Customers.Where(c => c.ContactName.Contains(c.ContactName)));
		}

		[TestMethod]
		public void StringConcatImplicit2Args()
		{
			TestQuery(
				"TestStringConcatImplicit2Args",
				_db.Customers.Where(c => c.ContactName + "X" == "X"));
		}

		[TestMethod]
		public void StringConcatExplicit2Args()
		{
			TestQuery(
				"TestStringConcatExplicit2Args",
				_db.Customers.Where(c => string.Concat(c.ContactName, "X") == "X"));
		}

		[TestMethod]
		public void StringConcatExplicit3Args()
		{
			TestQuery(
				"TestStringConcatExplicit3Args",
				_db.Customers.Where(c => string.Concat(c.ContactName, "X", c.Country) == "X"));
		}

		[TestMethod]
		public void StringConcatExplicitNArgs()
		{
			TestQuery(
				"TestStringConcatExplicitNArgs",
				_db.Customers.Where(c => string.Concat(new string[] { c.ContactName, "X", c.Country }) == "X"));
		}

		[TestMethod]
		public void StringIsNullOrEmpty()
		{
			TestQuery(
				"TestStringIsNullOrEmpty",
				_db.Customers.Where(c => string.IsNullOrEmpty(c.City)));
		}

		[TestMethod]
		public void StringToUpper()
		{
			TestQuery(
				"TestStringToUpper",
				_db.Customers.Where(c => c.City.ToUpper() == "SEATTLE"));
		}

		[TestMethod]
		public void StringToLower()
		{
			TestQuery(
				"TestStringToLower",
				_db.Customers.Where(c => c.City.ToLower() == "seattle"));
		}

		[TestMethod]
		public void StringSubstring()
		{
			TestQuery(
				"TestStringSubstring",
				_db.Customers.Where(c => c.City.Substring(0, 4) == "Seat"));
		}

		[TestMethod]
		public void StringSubstringNoLength()
		{
			TestQuery(
				"TestStringSubstringNoLength",
				_db.Customers.Where(c => c.City.Substring(4) == "tle"));
		}

		[TestMethod]
		public void StringIndexOf()
		{
			TestQuery(
				"TestStringIndexOf",
				_db.Customers.Where(c => c.City.IndexOf("tt") == 4));
		}

		[TestMethod]
		public void StringIndexOfChar()
		{
			TestQuery(
				"TestStringIndexOfChar",
				_db.Customers.Where(c => c.City.IndexOf('t') == 4));
		}

		[TestMethod]
		public void StringReplace()
		{
			TestQuery(
				"TestStringReplace",
				_db.Customers.Where(c => c.City.Replace("ea", "ae") == "Saettle"));
		}

		[TestMethod]
		public void StringReplaceChars()
		{
			TestQuery(
				"TestStringReplaceChars",
				_db.Customers.Where(c => c.City.Replace("e", "y") == "Syattly"));
		}

		[TestMethod]
		public void StringTrim()
		{
			TestQuery(
				"TestStringTrim",
				_db.Customers.Where(c => c.City.Trim() == "Seattle"));
		}

		[TestMethod]
		public void StringToString()
		{
			TestQuery(
				"TestStringToString",
				_db.Customers.Where(c => c.City.ToString() == "Seattle"));
		}

		[TestMethod]
		public void StringRemove()
		{
			TestQuery(
				"TestStringRemove",
				_db.Customers.Where(c => c.City.Remove(1, 2) == "Sttle"));
		}

		[TestMethod]
		public void StringRemoveNoCount()
		{
			TestQuery(
				"TestStringRemoveNoCount",
				_db.Customers.Where(c => c.City.Remove(4) == "Seat"));
		}
		
		[TestMethod]
		public void DateTimeConstructYmd()
		{
			TestQuery(
				"TestDateTimeConstructYmd",
				_db.Orders.Where(o => o.OrderDate == new DateTime(o.OrderDate.Year, 1, 1)));
		}

		[TestMethod]
		public void DateTimeConstructYmdhms()
		{
			TestQuery(
				"TestDateTimeConstructYmdhms",
				_db.Orders.Where(o => o.OrderDate == new DateTime(o.OrderDate.Year, 1, 1, 10, 25, 55)));
		}

		[TestMethod]
		public void DateTimeDay()
		{
			TestQuery(
				"TestDateTimeDay",
				_db.Orders.Where(o => o.OrderDate.Day == 5));
		}

		[TestMethod]
		public void DateTimeMonth()
		{
			TestQuery(
				"TestDateTimeMonth",
				_db.Orders.Where(o => o.OrderDate.Month == 12));
		}

		[TestMethod]
		public void DateTimeYear()
		{
			TestQuery(
				"TestDateTimeYear",
				_db.Orders.Where(o => o.OrderDate.Year == 1997));
		}

		[TestMethod]
		public void DateTimeHour()
		{
			TestQuery(
				"TestDateTimeHour",
				_db.Orders.Where(o => o.OrderDate.Hour == 6));
		}

		[TestMethod]
		public void DateTimeMinute()
		{
			TestQuery(
				"TestDateTimeMinute",
				_db.Orders.Where(o => o.OrderDate.Minute == 32));
		}

		[TestMethod]
		public void DateTimeSecond()
		{
			TestQuery(
				"TestDateTimeSecond",
				_db.Orders.Where(o => o.OrderDate.Second == 47));
		}

		[TestMethod]
		public void DateTimeMillisecond()
		{
			TestQuery(
				"TestDateTimeMillisecond",
				_db.Orders.Where(o => o.OrderDate.Millisecond == 200));
		}

		[TestMethod]
		public void DateTimeDayOfWeek()
		{
			TestQuery(
				"TestDateTimeDayOfWeek",
				_db.Orders.Where(o => o.OrderDate.DayOfWeek == DayOfWeek.Friday));
		}

		[TestMethod]
		public void DateTimeDayOfYear()
		{
			TestQuery(
				"TestDateTimeDayOfYear",
				_db.Orders.Where(o => o.OrderDate.DayOfYear == 360));
		}

		[TestMethod]
		public void MathAbs()
		{
			TestQuery(
				"TestMathAbs",
				_db.Orders.Where(o => Math.Abs(o.OrderID) == 10));
		}

		[TestMethod]
		public void MathAcos()
		{
			TestQuery(
				"TestMathAcos",
				_db.Orders.Where(o => Math.Acos(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathAsin()
		{
			TestQuery(
				"TestMathAsin",
				_db.Orders.Where(o => Math.Asin(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathAtan()
		{
			TestQuery(
				"TestMathAtan",
				_db.Orders.Where(o => Math.Atan(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathAtan2()
		{
			TestQuery(
				"TestMathAtan2",
				_db.Orders.Where(o => Math.Atan2(o.OrderID, 3) == 0));
		}

		[TestMethod]
		public void MathCos()
		{
			TestQuery(
				"TestMathCos",
				_db.Orders.Where(o => Math.Cos(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathSin()
		{
			TestQuery(
				"TestMathSin",
				_db.Orders.Where(o => Math.Sin(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathTan()
		{
			TestQuery(
				"TestMathTan",
				_db.Orders.Where(o => Math.Tan(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathExp()
		{
			TestQuery(
				"TestMathExp",
				_db.Orders.Where(o => Math.Exp(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathLog()
		{
			TestQuery(
				"TestMathLog",
				_db.Orders.Where(o => Math.Log(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathLog10()
		{
			TestQuery(
				"TestMathLog10",
				_db.Orders.Where(o => Math.Log10(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathSqrt()
		{
			TestQuery(
				"TestMathSqrt",
				_db.Orders.Where(o => Math.Sqrt(o.OrderID) == 0));
		}

		[TestMethod]
		public void MathCeiling()
		{
			TestQuery(
				"TestMathCeiling",
				_db.Orders.Where(o => Math.Ceiling((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void MathFloor()
		{
			TestQuery(
				"TestMathFloor",
				_db.Orders.Where(o => Math.Floor((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void MathPow()
		{
			TestQuery(
				"TestMathPow",
				_db.Orders.Where(o => Math.Pow(o.OrderID < 1000 ? 1 : 2, 3) == 0));
		}

		[TestMethod]
		public void MathRoundDefault()
		{
			TestQuery(
				"TestMathRoundDefault",
				_db.Orders.Where(o => Math.Round((decimal)o.OrderID) == 0));
		}

		[TestMethod]
		public void MathRoundToPlace()
		{
			TestQuery(
				"TestMathRoundToPlace",
				_db.Orders.Where(o => Math.Round((decimal)o.OrderID, 2) == 0));
		}

		[TestMethod]
		public void MathTruncate()
		{
			TestQuery(
				"TestMathTruncate",
				_db.Orders.Where(o => Math.Truncate((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void StringCompareToLessThan()
		{
			TestQuery(
				"TestStringCompareToLessThan",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") < 0));
		}

		[TestMethod]
		public void StringCompareToLessThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareToLessThanOrEqualTo",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") <= 0));
		}

		[TestMethod]
		public void StringCompareToGreaterThan()
		{
			TestQuery(
				"TestStringCompareToGreaterThan",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") > 0));
		}

		[TestMethod]
		public void StringCompareToGreaterThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareToGreaterThanOrEqualTo",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") >= 0));
		}

		[TestMethod]
		public void StringCompareToEquals()
		{
			TestQuery(
				"TestStringCompareToEquals",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") == 0));
		}

		[TestMethod]
		public void StringCompareToNotEquals()
		{
			TestQuery(
				"TestStringCompareToNotEquals",
				_db.Customers.Where(c => c.City.CompareTo("Seattle") != 0));
		}

		[TestMethod]
		public void StringCompareLessThan()
		{
			TestQuery(
				"TestStringCompareLessThan",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") < 0));
		}

		[TestMethod]
		public void StringCompareLessThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareLessThanOrEqualTo",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") <= 0));
		}

		[TestMethod]
		public void StringCompareGreaterThan()
		{
			TestQuery(
				"TestStringCompareGreaterThan",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") > 0));
		}

		[TestMethod]
		public void StringCompareGreaterThanOrEqualTo()
		{
			TestQuery(
				"TestStringCompareGreaterThanOrEqualTo",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") >= 0));
		}

		[TestMethod]
		public void StringCompareEquals()
		{
			TestQuery(
				"TestStringCompareEquals",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") == 0));
		}

		[TestMethod]
		public void StringCompareNotEquals()
		{
			TestQuery(
				"TestStringCompareNotEquals",
				_db.Customers.Where(c => string.Compare(c.City, "Seattle") != 0));
		}

		[TestMethod]
		public void IntCompareTo()
		{
			TestQuery(
				"TestIntCompareTo",
				_db.Orders.Where(o => o.OrderID.CompareTo(1000) == 0));
		}

		[TestMethod]
		public void DecimalCompare()
		{
			TestQuery(
				"TestDecimalCompare",
				_db.Orders.Where(o => decimal.Compare((decimal)o.OrderID, 0.0m) == 0));
		}

		[TestMethod]
		public void DecimalAdd()
		{
			TestQuery(
				"TestDecimalAdd",
				_db.Orders.Where(o => decimal.Add(o.OrderID, 0.0m) == 0.0m));
		}

		[TestMethod]
		public void DecimalSubtract()
		{
			TestQuery(
				"TestDecimalSubtract",
				_db.Orders.Where(o => decimal.Subtract(o.OrderID, 0.0m) == 0.0m));
		}

		[TestMethod]
		public void DecimalMultiply()
		{
			TestQuery(
				"TestDecimalMultiply",
				_db.Orders.Where(o => decimal.Multiply(o.OrderID, 1.0m) == 1.0m));
		}

		[TestMethod]
		public void DecimalDivide()
		{
			TestQuery(
				"TestDecimalDivide",
				_db.Orders.Where(o => decimal.Divide(o.OrderID, 1.0m) == 1.0m));
		}

		[TestMethod]
		public void DecimalRemainder()
		{
			TestQuery(
				"TestDecimalRemainder",
				_db.Orders.Where(o => decimal.Remainder(o.OrderID, 1.0m) == 0.0m));
		}

		[TestMethod]
		public void DecimalNegate()
		{
			TestQuery(
				"TestDecimalNegate",
				_db.Orders.Where(o => decimal.Negate(o.OrderID) == 1.0m));
		}

		[TestMethod]
		public void DecimalCeiling()
		{
			TestQuery(
				"TestDecimalCeiling",
				_db.Orders.Where(o => decimal.Ceiling(o.OrderID) == 0.0m));
		}

		[TestMethod]
		public void DecimalFloor()
		{
			TestQuery(
				"TestDecimalFloor",
				_db.Orders.Where(o => decimal.Floor(o.OrderID) == 0.0m));
		}

		[TestMethod]
		public void DecimalRoundDefault()
		{
			TestQuery(
				"TestDecimalRoundDefault",
				_db.Orders.Where(o => decimal.Round(o.OrderID) == 0m));
		}

		[TestMethod]
		public void DecimalRoundPlaces()
		{
			TestQuery(
				"TestDecimalRoundPlaces",
				_db.Orders.Where(o => decimal.Round(o.OrderID, 2) == 0.00m));
		}

		[TestMethod]
		public void DecimalTruncate()
		{
			TestQuery(
				"TestDecimalTruncate",
				_db.Orders.Where(o => decimal.Truncate(o.OrderID) == 0m));
		}

		[TestMethod]
		public void DecimalLessThan()
		{
			TestQuery(
				"TestDecimalLessThan",
				_db.Orders.Where(o => ((decimal)o.OrderID) < 0.0m));
		}

		[TestMethod]
		public void IntLessThan()
		{
			TestQuery(
				"TestIntLessThan",
				_db.Orders.Where(o => o.OrderID < 0));
		}

		[TestMethod]
		public void IntLessThanOrEqual()
		{
			TestQuery(
				"TestIntLessThanOrEqual",
				_db.Orders.Where(o => o.OrderID <= 0));
		}

		[TestMethod]
		public void IntGreaterThan()
		{
			TestQuery(
				"TestIntGreaterThan",
				_db.Orders.Where(o => o.OrderID > 0));
		}

		[TestMethod]
		public void IntGreaterThanOrEqual()
		{
			TestQuery(
				"TestIntGreaterThanOrEqual",
				_db.Orders.Where(o => o.OrderID >= 0));
		}

		[TestMethod]
		public void IntEqual()
		{
			TestQuery(
				"TestIntEqual",
				_db.Orders.Where(o => o.OrderID == 0));
		}

		[TestMethod]
		public void IntNotEqual()
		{
			TestQuery(
				"TestIntNotEqual",
				_db.Orders.Where(o => o.OrderID != 0));
		}

		[TestMethod]
		public void IntAdd()
		{
			TestQuery(
				"TestIntAdd",
				_db.Orders.Where(o => o.OrderID + 0 == 0));
		}

		[TestMethod]
		public void IntSubtract()
		{
			TestQuery(
				"TestIntSubtract",
				_db.Orders.Where(o => o.OrderID - 0 == 0));
		}

		[TestMethod]
		public void IntMultiply()
		{
			TestQuery(
				"TestIntMultiply",
				_db.Orders.Where(o => o.OrderID * 1 == 1));
		}

		[TestMethod]
		public void IntDivide()
		{
			TestQuery(
				"TestIntDivide",
				_db.Orders.Where(o => o.OrderID / 1 == 1));
		}

		[TestMethod]
		public void IntModulo()
		{
			TestQuery(
				"TestIntModulo",
				_db.Orders.Where(o => o.OrderID % 1 == 0));
		}

		[TestMethod]
		public void IntLeftShift()
		{
			TestQuery(
				"TestIntLeftShift",
				_db.Orders.Where(o => o.OrderID << 1 == 0));
		}

		[TestMethod]
		public void IntRightShift()
		{
			TestQuery(
				"TestIntRightShift",
				_db.Orders.Where(o => o.OrderID >> 1 == 0));
		}

		[TestMethod]
		public void IntBitwiseAnd()
		{
			TestQuery(
				"TestIntBitwiseAnd",
				_db.Orders.Where(o => (o.OrderID & 1) == 0));
		}

		[TestMethod]
		public void IntBitwiseOr()
		{
			TestQuery(
				"TestIntBitwiseOr",
				_db.Orders.Where(o => (o.OrderID | 1) == 1));
		}

		[TestMethod]
		public void IntBitwiseExclusiveOr()
		{
			TestQuery(
				"TestIntBitwiseExclusiveOr",
				_db.Orders.Where(o => (o.OrderID ^ 1) == 1));
		}

		////[TestMethod]
		////public void IntBitwiseNot()
		////{
		////	TestQuery(
		////		"TestIntBitwiseNot",
		////		db.Orders.Where(o => ~o.OrderID == 0));
		////}

		[TestMethod]
		public void IntNegate()
		{
			TestQuery(
				"TestIntNegate",
				_db.Orders.Where(o => -o.OrderID == -1));
		}

		[TestMethod]
		public void And()
		{
			TestQuery(
				"TestAnd",
				_db.Orders.Where(o => o.OrderID > 0 && o.OrderID < 2000));
		}

		[TestMethod]
		public void Or()
		{
			TestQuery(
				"TestOr",
				_db.Orders.Where(o => o.OrderID < 5 || o.OrderID > 10));
		}

		[TestMethod]
		public void Not()
		{
			TestQuery(
				"TestNot",
				_db.Orders.Where(o => !(o.OrderID == 0)));
		}

		[TestMethod]
		public void EqualNull()
		{
			TestQuery(
				"TestEqualNull",
				_db.Customers.Where(c => c.City == null));
		}

		[TestMethod]
		public void EqualNullReverse()
		{
			TestQuery(
				"TestEqualNullReverse",
				_db.Customers.Where(c => null == c.City));
		}

		[TestMethod]
		public void Conditional()
		{
			TestQuery(
				"TestConditional",
				_db.Orders.Where(o => (o.CustomerID == "ALFKI" ? 1000 : 0) == 1000));
		}

		[TestMethod]
		public void Conditional2()
		{
			TestQuery(
				"TestConditional2",
				_db.Orders.Where(o => (o.CustomerID == "ALFKI" ? 1000 : o.CustomerID == "ABCDE" ? 2000 : 0) == 1000));
		}

		[TestMethod]
		public void ConditionalTestIsValue()
		{
			TestQuery(
				"TestConditionalTestIsValue",
				_db.Orders.Where(o => (((bool)(object)o.OrderID) ? 100 : 200) == 100));
		}

		////[TestMethod]
		////public void ConditionalResultsArePredicates()
		////{
		////	TestQuery(
		////		"TestConditionalResultsArePredicates",
		////		db.Orders.Where(o => (o.CustomerID == "ALFKI" ? o.OrderID < 10 : o.OrderID > 10)));
		////}

		////[TestMethod]
		////public void SelectManyJoined()
		////{
		////	TestQuery(
		////		"TestSelectManyJoined",
		////		from c in db.Customers
		////		from o in db.Orders.Where(o => o.CustomerID == c.CustomerID)
		////		select new { c.ContactName, o.OrderDate });
		////}

		////[TestMethod]
		////public void SelectManyJoinedDefaultIfEmpty()
		////{
		////	TestQuery(
		////		"TestSelectManyJoinedDefaultIfEmpty",
		////		from c in db.Customers
		////		from o in db.Orders.Where(o => o.CustomerID == c.CustomerID).DefaultIfEmpty()
		////		select new { c.ContactName, o.OrderDate });
		////}

		////[TestMethod]
		////public void SelectWhereAssociation()
		////{
		////	TestQuery(
		////		"TestSelectWhereAssociation",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle"
		////		select o);
		////}

		////[TestMethod]
		////public void SelectWhereAssociations()
		////{
		////	TestQuery(
		////		"TestSelectWhereAssociations",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle" && o.Customer.Phone != "555 555 5555"
		////		select o);
		////}

		////[TestMethod]
		////public void SelectWhereAssociationTwice()
		////{
		////	TestQuery(
		////		"TestSelectWhereAssociationTwice",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle" && o.Customer.Phone != "555 555 5555"
		////		select o);
		////}

		////[TestMethod]
		////public void SelectAssociation()
		////{
		////	TestQuery(
		////		"TestSelectAssociation",
		////		from o in db.Orders
		////		select o.Customer);
		////}

		////[TestMethod]
		////public void SelectAssociations()
		////{
		////	TestQuery(
		////		"TestSelectAssociations",
		////		from o in db.Orders
		////		select new { A = o.Customer, B = o.Customer });
		////}

		////[TestMethod]
		////public void SelectAssociationsWhereAssociations()
		////{
		////	TestQuery(
		////		"TestSelectAssociationsWhereAssociations",
		////		from o in db.Orders
		////		where o.Customer.City == "Seattle"
		////		where o.Customer.Phone != "555 555 5555"
		////		select new { A = o.Customer, B = o.Customer });
		////}

		////[TestMethod]
		////public void SingletonAssociationWithMemberAccess()
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
		public void CompareDateTimesWithDifferentNullability()
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
		public void ContainsWithEmptyLocalList()
		{
			var ids = Array.Empty<string>();
			TestQuery(
				"TestContainsWithEmptyLocalList",
				from c in _db.Customers
				where ids.Contains(c.CustomerID)
				select c
				);
		}

		[TestMethod]
		public void ContainsWithSubquery2()
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
		////public void CombineQueriesDeepNesting()
		////{
		////	var custs = db.Customers.Where(c => c.ContactName.StartsWith("xxx"));
		////	var ords = db.Orders.Where(o => custs.Any(c => c.CustomerID == o.CustomerID));
		////	TestQuery(
		////		"TestCombineQueriesDeepNesting",
		////		db.OrderDetails.Where(d => ords.Any(o => o.OrderID == d.OrderID))
		////		);
		////}

		////[TestMethod]
		////public void LetWithSubquery()
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

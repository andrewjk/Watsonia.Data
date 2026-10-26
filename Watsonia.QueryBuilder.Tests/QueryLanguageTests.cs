using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.QueryBuilder;
using Watsonia.QueryBuilder.SQLite;
using Watsonia.QueryBuilder.SqlServer;
using Watsonia.QueryBuilder.Tests.Northwind;

// NOTE: the remaining //////// blocks are execution-only operators (First/Last/Single/Reverse, top-level Any/All); they are not SQL shape tests.

namespace Watsonia.Data.Tests.Queries
{
	[TestClass]
	public class QueryLanguageTests
	{
		private static readonly DatabaseMapper _mapper = new NorthwindMapper();
		private static Dictionary<string, string> _sqlServerBaselines = new Dictionary<string, string>();
		private static Dictionary<string, string> _sqliteBaselines = new Dictionary<string, string>();
		private static string _sqlServerBaselineFileName;
		private static string _sqliteBaselineFileName;

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			_sqlServerBaselineFileName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"/Baselines/SqlServer.xml";
			if (!string.IsNullOrEmpty(_sqlServerBaselineFileName) && File.Exists(_sqlServerBaselineFileName))
			{
				var doc = XDocument.Load(_sqlServerBaselineFileName);
				_sqlServerBaselines = doc.Root.Elements("baseline").ToDictionary(e => (string)e.Attribute("key"), e => e.Value);
			}

			_sqliteBaselineFileName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"/Baselines/SQLite.xml";
			if (!string.IsNullOrEmpty(_sqliteBaselineFileName) && File.Exists(_sqliteBaselineFileName))
			{
				var doc = XDocument.Load(_sqliteBaselineFileName);
				_sqliteBaselines = doc.Root.Elements("baseline").ToDictionary(e => (string)e.Attribute("key"), e => e.Value);
			}
		}

		[TestMethod]
		public void TestWhere()
		{
			TestQuery2(
				"TestWhere",
				Select.From<Customer>("c").Where(c => c.City == "London"));
		}

		[TestMethod]
		public void TestWhereEquals()
		{
			TestQuery2(
				"TestWhereEquals",
				Select.From<Customer>("c").Where(c => c.City.Equals("London")));
		}

		[TestMethod]
		public void TestCompareEntityEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			TestQuery2(
				"TestCompareEntityEqual",
				Select.From<Customer>("c").Where(c => c == alfki));
		}

		[TestMethod]
		public void TestCompareEntityNotEqual()
		{
			var alfki = new Customer { CustomerID = "ALFKI" };
			TestQuery2(
				"TestCompareEntityNotEqual",
				Select.From<Customer>("c").Where(c => c != alfki));
		}

		[TestMethod]
		public void TestCompareConstructedEqual()
		{
			TestQuery2(
				"TestCompareConstructedEqual",
				Select.From<Customer>("c").Where(c => new { x = c.City } == new { x = "London" }));
		}

		[TestMethod]
		public void TestCompareConstructedMultiValueEqual()
		{
			TestQuery2(
				"TestCompareConstructedMultiValueEqual",
				Select.From<Customer>("c").Where(c => new { x = c.City, y = c.Country } == new { x = "London", y = "UK" }));
		}

		[TestMethod]
		public void TestCompareConstructedMultiValueNotEqual()
		{
			TestQuery2(
				"TestCompareConstructedMultiValueNotEqual",
				Select.From<Customer>("c").Where(c => new { x = c.City, y = c.Country } != new { x = "London", y = "UK" }));
		}

		[TestMethod]
		public void TestSelectScalar()
		{
			TestQuery2(
				"TestSelectScalar",
				Select.From<Customer>("c").Columns(c => c.City));
		}

		[TestMethod]
		public void TestSelectAnonymousOne()
		{
			TestQuery2(
				"TestSelectAnonymousOne",
				Select.From<Customer>("c").Columns(c => new { c.City }));
		}

		[TestMethod]
		public void TestSelectAnonymousTwo()
		{
			TestQuery2(
				"TestSelectAnonymousTwo",
				Select.From<Customer>("c").Columns(c => new { c.City, c.Phone }));
		}

		[TestMethod]
		public void TestSelectAnonymousThree()
		{
			TestQuery2(
				"TestSelectAnonymousThree",
				Select.From<Customer>("c").Columns(c => new { c.City, c.Phone, c.Country }));
		}

		[TestMethod]
		public void TestSelectCustomerTable()
		{
			TestQuery2(
				"TestSelectCustomerTable",
				Select.From<Customer>("c"));
		}

		[TestMethod]
		public void TestSelectCustomerIdentity()
		{
			TestQuery2(
				"TestSelectCustomerIdentity",
				Select.From<Customer>("c"));
		}

		// NOTE: I don't really see the point of selecting locals:

		[TestMethod]
		public void TestSelectAnonymousWithObject()
		{
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectAnonymousWithObject",
				Select.From(c).Columns(Col("c", "City")).ColumnsFrom(c));
		}

		[TestMethod]
		public void TestSelectAnonymousNested()
		{
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectAnonymousNested",
				Select.From(c).Columns(Col("c", "City"), Col("c", "Country")));
		}

		[TestMethod]
		public void TestSelectAnonymousEmpty()
		{
			TestQuery2(
				"TestSelectAnonymousEmpty",
				Select.From<Customer>("c").Columns(c => new { }));
		}

		[TestMethod]
		public void TestSelectAnonymousLiteral()
		{
			TestQuery2(
				"TestSelectAnonymousLiteral",
				Select.From(new Table("Customers", "c")).Columns(new ConstantPart(10)));
		}

		[TestMethod]
		public void TestSelectConstantInt()
		{
			TestQuery2(
				"TestSelectConstantInt",
				Select.From(new Table("Customers", "c")).Columns(new ConstantPart(0)));
		}

		[TestMethod]
		public void TestSelectConstantNullString()
		{
			TestQuery2(
				"TestSelectConstantNullString",
				Select.From(new Table("Customers", "c")).Columns(new ConstantPart(null)));
		}

		[TestMethod]
		public void TestSelectLocal()
		{
			int x = 10;
			TestQuery2(
				"TestSelectLocal",
				Select.From(new Table("Customers", "c")).Columns(new ConstantPart(x)));
		}

		[TestMethod]
		public void TestSelectNestedCollection()
		{
			// Nested collections are materialised client-side, so SQL only selects the outer columns
			TestQuery2(
				"TestSelectNestedCollection",
				Select.From<Customer>("c").Where(c => c.City == "London").Columns(c => c.CustomerID));
		}

		[TestMethod]
		public void TestSelectNestedCollectionInAnonymousType()
		{
			// Nested collections are materialised client-side, so SQL only selects the outer columns
			TestQuery2(
				"TestSelectNestedCollectionInAnonymousType",
				Select.From<Customer>("c").Where(c => c.CustomerID == "ALFKI").Columns(c => c.CustomerID));
		}

		[TestMethod]
		public void TestJoinCustomerOrders()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestJoinCustomerOrders",
				Select.From(c)
					.Join(o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.Columns(Col("c", "ContactName"), Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestJoinMultiKey()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			var join = new Join(o, Col("c", "CustomerID"), Col("o", "CustomerID"));
			join.Conditions.Add(new Condition(Col("c", "CustomerID"), SqlOperator.Equals, Col("o", "CustomerID")) { Relationship = ConditionRelationship.And });
			TestQuery2(
				"TestJoinMultiKey",
				Select.From(c).Join(join).ColumnsFrom(c, o));
		}

		[TestMethod]
		public void TestJoinIntoCustomersOrders()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestJoinIntoCustomersOrders",
				Select.From(c)
					.Join(JoinType.Left, o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.ColumnsFrom(c, o));
		}

		[TestMethod]
		public void TestJoinIntoCustomersOrdersCount()
		{
			var c = new Table("Customers", "c");
			var orders = Select.From(new Table("Orders", "o"))
				.Where(new Condition(Col("o", "CustomerID"), SqlOperator.Equals, Col("c", "CustomerID")))
				.Count();
			TestQuery2(
				"TestJoinIntoCustomersOrdersCount",
				Select.From(c).ColumnsFrom(c).ColumnSubquery(orders, "OrdersCount"));
		}

		[TestMethod]
		public void TestJoinIntoDefaultIfEmpty()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestJoinIntoDefaultIfEmpty",
				Select.From(c)
					.Join(JoinType.Left, o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.ColumnsFrom(c, o));
		}

		[TestMethod]
		public void TestSelectManyCustomerOrders()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestSelectManyCustomerOrders",
				Select.From(c)
					.Join(o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.Columns(Col("c", "ContactName"), Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestMultipleJoinsWithJoinConditionsInWhere()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			var d = new Table("Order Details", "d");
			TestQuery2(
				"TestMultipleJoinsWithJoinConditionsInWhere",
				Select.From(c)
					.Join(o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.Join(d, Col("o", "OrderID"), Col("d", "OrderID"))
					.Where(new Condition(Col("c", "CustomerID"), SqlOperator.Equals, "ALFKI"))
					.Columns(Col("d", "ProductID")));
		}

		[TestMethod]
		public void TestMultipleJoinsWithMissingJoinCondition()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			var d = new Table("Order Details", "d");
			var detailsJoin = new Join(d, new ConditionCollection(
				new Condition(new ConstantPart(1), SqlOperator.Equals, new ConstantPart(1))));
			TestQuery2(
				"TestMultipleJoinsWithMissingJoinCondition",
				Select.From(c)
					.Join(o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.Join(detailsJoin)
					.Where(new Condition(Col("c", "CustomerID"), SqlOperator.Equals, "ALFKI"))
					.Columns(Col("d", "ProductID")));
		}

		[TestMethod]
		public void TestOrderBy()
		{
			TestQuery2(
				"TestOrderBy",
				Select.From<Customer>("c").OrderBy(c => c.CustomerID)
				);
		}

		[TestMethod]
		public void TestOrderBySelect()
		{
			TestQuery2(
				"TestOrderBySelect",
				Select.From<Customer>("c").OrderBy(c => c.CustomerID).Columns(c => c.ContactName)
				);
		}

		[TestMethod]
		public void TestOrderByOrderBy()
		{
			TestQuery2(
				"TestOrderByOrderBy",
				Select.From<Customer>("c").OrderBy(c => c.CustomerID).OrderBy(c => c.Country).Columns(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByThenBy()
		{
			TestQuery2(
				"TestOrderByThenBy",
				Select.From<Customer>("c").OrderBy(c => c.CustomerID).ThenBy(c => c.Country).Columns(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByDescending()
		{
			TestQuery2(
				"TestOrderByDescending",
				Select.From<Customer>("c").OrderByDescending(c => c.CustomerID).Columns(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByDescendingThenBy()
		{
			TestQuery2(
				"TestOrderByDescendingThenBy",
				Select.From<Customer>("c").OrderByDescending(c => c.CustomerID).ThenBy(c => c.Country).Columns(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByDescendingThenByDescending()
		{
			TestQuery2(
				"TestOrderByDescendingThenByDescending",
				Select.From<Customer>("c").OrderByDescending(c => c.CustomerID).ThenByDescending(c => c.Country).Columns(c => c.City)
				);
		}

		[TestMethod]
		public void TestOrderByJoin()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestOrderByJoin",
				Select.From(c)
					.Join(o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.Columns(Col("c", "CustomerID"), Col("o", "OrderID"))
					.OrderBy(Col("c", "CustomerID"), Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestOrderBySelectMany()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestOrderBySelectMany",
				Select.From(c)
					.Join(o, Col("c", "CustomerID"), Col("o", "CustomerID"))
					.Columns(Col("c", "ContactName"), Col("o", "OrderID"))
					.OrderBy(Col("c", "CustomerID"), Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestGroupBy()
		{
			TestQuery2(
				"TestGroupBy",
				Select.From<Customer>("c").GroupBy(c => c.City)
				);
		}

		[TestMethod]
		public void TestGroupBySelectMany()
		{
			TestQuery2(
				"TestGroupBySelectMany",
				Select.From<Customer>("c"));
		}

		[TestMethod]
		public void TestGroupBySum()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupBySum",
				Select.From(o).GroupBy(Col("o", "CustomerID")).Sum(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestGroupByCount()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByCount",
				Select.From(o).GroupBy(Col("o", "CustomerID")).Count());
		}

		[TestMethod]
		public void TestGroupByLongCount()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByLongCount",
				Select.From(o).GroupBy(Col("o", "CustomerID")).LongCount());
		}

		[TestMethod]
		public void TestGroupBySumMinMaxAvg()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupBySumMinMaxAvg",
				Select.From(o)
					.GroupBy(Col("o", "CustomerID"))
					.Sum(Col("o", "OrderID"))
					.Min(Col("o", "OrderID"))
					.Max(Col("o", "OrderID"))
					.Average(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestGroupByWithResultSelector()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByWithResultSelector",
				Select.From(o)
					.GroupBy(Col("o", "CustomerID"))
					.Sum(Col("o", "OrderID"))
					.Min(Col("o", "OrderID"))
					.Max(Col("o", "OrderID"))
					.Average(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestGroupByWithElementSelectorSum()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByWithElementSelectorSum",
				Select.From(o).GroupBy(Col("o", "CustomerID")).Sum(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestGroupByWithElementSelector()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByWithElementSelector",
				Select.From(o).GroupBy(Col("o", "CustomerID")));
		}

		[TestMethod]
		public void TestGroupByWithElementSelectorSumMax()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByWithElementSelectorSumMax",
				Select.From(o).GroupBy(Col("o", "CustomerID")).Sum(Col("o", "OrderID")).Max(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestGroupByWithAnonymousElement()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByWithAnonymousElement",
				Select.From(o).GroupBy(Col("o", "CustomerID")).Sum(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestGroupByWithTwoPartKey()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByWithTwoPartKey",
				Select.From(o).GroupBy(Col("o", "CustomerID"), Col("o", "OrderDate")).Sum(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestOrderByGroupBy()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestOrderByGroupBy",
				Select.From(o).OrderBy(Col("o", "OrderID")).GroupBy(Col("o", "CustomerID")).Sum(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestOrderByGroupBySelectMany()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestOrderByGroupBySelectMany",
				Select.From(o).OrderBy(Col("o", "OrderID")));
		}

		// NOTE: Don't think I like this syntax, better to force the user to pass in the sum column
		//[TestMethod]
		//public void TestSumWithNoArg()
		//{
		//	TestQuery2(
		//		"TestSumWithNoArg",
		//		Select.From<Order>("o").Columns(o => o.OrderID).Sum()
		//		);
		//}

		[TestMethod]
		public void TestSumWithArg()
		{
			TestQuery2(
				"TestSumWithArg",
				Select.From<Order>("o").Sum(o => o.OrderID)
				);
		}

		[TestMethod]
		public void TestCountWithNoPredicate()
		{
			TestQuery2(
				"TestCountWithNoPredicate",
				Select.From<Order>("o").Count()
				);
		}

		[TestMethod]
		public void TestCountWithPredicate()
		{
			TestQuery2(
				"TestCountWithPredicate",
				Select.From<Order>("o").Count(o => o.CustomerID == "ALFKI")
				);
		}

		[TestMethod]
		public void TestDistinct()
		{
			TestQuery2(
				"TestDistinct",
				Select.From<Customer>("c").Distinct()
				);
		}

		[TestMethod]
		public void TestDistinctScalar()
		{
			TestQuery2(
				"TestDistinctScalar",
				Select.From<Customer>("c").Columns(c => c.City).Distinct()
				);
		}

		[TestMethod]
		public void TestOrderByDistinct()
		{
			TestQuery2(
				"TestOrderByDistinct",
				Select.From<Customer>("c").OrderBy(c => c.CustomerID).Columns(c => c.City).Distinct()
				);
		}

		[TestMethod]
		public void TestDistinctOrderBy()
		{
			TestQuery2(
				"TestDistinctOrderBy",
				Select.From<Customer>("c").Columns(c => c.City).Distinct().OrderBy(c => c.CustomerID)
				);
		}

		[TestMethod]
		public void TestDistinctGroupBy()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestDistinctGroupBy",
				Select.From(o).Distinct().GroupBy(Col("o", "CustomerID")));
		}

		[TestMethod]
		public void TestGroupByDistinct()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestGroupByDistinct",
				Select.From(o).GroupBy(Col("o", "CustomerID")).Distinct());
		}

		[TestMethod]
		public void TestDistinctCount()
		{
			TestQuery2(
				"TestDistinctCount",
				Select.From<Customer>("c").Distinct().Count()
				);
		}

		[TestMethod]
		public void TestSelectDistinctCount()
		{
			var c = new Table("Customers", "c");
			var distinctCities = Select.From(c).Columns(Col("c", "City")).Distinct();
			TestQuery2(
				"TestSelectDistinctCount",
				Select.From(distinctCities).Count());
		}

		[TestMethod]
		public void TestSelectSelectDistinctCount()
		{
			var c = new Table("Customers", "c");
			var distinctCities = Select.From(c).Columns(Col("c", "City")).Distinct();
			TestQuery2(
				"TestSelectSelectDistinctCount",
				Select.From(distinctCities).Count());
		}

		[TestMethod]
		public void TestDistinctCountPredicate()
		{
			TestQuery2(
				"TestDistinctCountPredicate",
				Select.From<Customer>("c").Distinct().Count(c => c.CustomerID == "ALFKI"));
		}

		[TestMethod]
		public void TestDistinctSumWithArg()
		{
			TestQuery2(
				"TestDistinctSumWithArg",
				Select.From<Order>("o").Distinct().Sum(o => o.OrderID));
		}

		[TestMethod]
		public void TestSelectDistinctSum()
		{
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestSelectDistinctSum",
				Select.From(o).SumDistinct(Col("o", "OrderID")));
		}

		[TestMethod]
		public void TestTake()
		{
			TestQuery2(
				"TestTake",
				Select.From<Order>("o").Take(5));
		}

		[TestMethod]
		public void TestTakeDistinct()
		{
			TestQuery2(
				"TestTakeDistinct",
				Select.From<Order>("o").Take(5).Distinct());
		}

		[TestMethod]
		public void TestDistinctTake()
		{
			TestQuery2(
				"TestDistinctTake",
				Select.From<Order>("o").Distinct().Take(5));
		}

		[TestMethod]
		public void TestDistinctTakeCount()
		{
			var o = new Table("Orders", "o");
			var inner = Select.From(o).ColumnsFrom(o).Distinct().Take(5);
			TestQuery2(
				"TestDistinctTakeCount",
				Select.From(inner).Count());
		}

		[TestMethod]
		public void TestTakeDistinctCount()
		{
			var o = new Table("Orders", "o");
			var inner = Select.From(o).ColumnsFrom(o).Take(5).Distinct();
			TestQuery2(
				"TestTakeDistinctCount",
				Select.From(inner).Count());
		}

		[TestMethod]
		public void TestSkip()
		{
			TestQuery2(
				"TestSkip",
				Select.From<Customer>("c").OrderBy(c => c.ContactName).Skip(5));
		}

		[TestMethod]
		public void TestTakeSkip()
		{
			TestQuery2(
				"TestTakeSkip",
				Select.From<Customer>("c").OrderBy(c => c.ContactName).Take(10).Skip(5));
		}

		[TestMethod]
		public void TestDistinctSkip()
		{
			TestQuery2(
				"TestDistinctSkip",
				Select.From<Customer>("c").Distinct().OrderBy(c => c.ContactName).Skip(5));
		}

		[TestMethod]
		public void TestSkipTake()
		{
			TestQuery2(
				"TestSkipTake",
				Select.From<Customer>("c").OrderBy(c => c.ContactName).Skip(5).Take(10));
		}

		[TestMethod]
		public void TestDistinctSkipTake()
		{
			TestQuery2(
				"TestDistinctSkipTake",
				Select.From<Customer>("c").Distinct().OrderBy(c => c.ContactName).Skip(5).Take(10));
		}

		[TestMethod]
		public void TestSkipDistinct()
		{
			TestQuery2(
				"TestSkipDistinct",
				Select.From<Customer>("c").OrderBy(c => c.ContactName).Skip(5).Distinct());
		}

		[TestMethod]
		public void TestSkipTakeDistinct()
		{
			TestQuery2(
				"TestSkipTakeDistinct",
				Select.From<Customer>("c").OrderBy(c => c.ContactName).Skip(5).Take(10).Distinct());
		}

		[TestMethod]
		public void TestTakeSkipDistinct()
		{
			TestQuery2(
				"TestTakeSkipDistinct",
				Select.From<Customer>("c").OrderBy(c => c.ContactName).Take(10).Skip(5).Distinct());
		}

		////////[TestMethod]
		////////public void TestFirst()
		////////{
		////////	TestQuery2(
		////////		"TestFirst",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).First()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestFirstPredicate()
		////////{
		////////	TestQuery2(
		////////		"TestFirstPredicate",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).First(c => c.City == "London")
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestWhereFirst()
		////////{
		////////	TestQuery2(
		////////		"TestWhereFirst",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Where(c => c.City == "London").First()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestFirstOrDefault()
		////////{
		////////	TestQuery2(
		////////		"TestFirstOrDefault",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).FirstOrDefault()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestFirstOrDefaultPredicate()
		////////{
		////////	TestQuery2(
		////////		"TestFirstOrDefaultPredicate",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).FirstOrDefault(c => c.City == "London")
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestWhereFirstOrDefault()
		////////{
		////////	TestQuery2(
		////////		"TestWhereFirstOrDefault",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Where(c => c.City == "London").FirstOrDefault()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestReverse()
		////////{
		////////	TestQuery2(
		////////		"TestReverse",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Reverse()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestReverseReverse()
		////////{
		////////	TestQuery2(
		////////		"TestReverseReverse",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Reverse().Reverse()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestReverseWhereReverse()
		////////{
		////////	TestQuery2(
		////////		"TestReverseWhereReverse",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Reverse()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestReverseTakeReverse()
		////////{
		////////	TestQuery2(
		////////		"TestReverseTakeReverse",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Reverse().Take(5).Reverse()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestReverseWhereTakeReverse()
		////////{
		////////	TestQuery2(
		////////		"TestReverseWhereTakeReverse",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Reverse().Where(c => c.City == "London").Take(5).Reverse()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestLast()
		////////{
		////////	TestQuery2(
		////////		"TestLast",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Last()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestLastPredicate()
		////////{
		////////	TestQuery2(
		////////		"TestLastPredicate",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Last(c => c.City == "London")
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestWhereLast()
		////////{
		////////	TestQuery2(
		////////		"TestWhereLast",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Where(c => c.City == "London").Last()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestLastOrDefault()
		////////{
		////////	TestQuery2(
		////////		"TestLastOrDefault",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).LastOrDefault()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestLastOrDefaultPredicate()
		////////{
		////////	TestQuery2(
		////////		"TestLastOrDefaultPredicate",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).LastOrDefault(c => c.City == "London")
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestWhereLastOrDefault()
		////////{
		////////	TestQuery2(
		////////		"TestWhereLastOrDefault",
		////////		Select.From<Customer>("c").OrderBy(c => c.ContactName).Where(c => c.City == "London").LastOrDefault()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestSingle()
		////////{
		////////	TestQuery2(
		////////		"TestSingle",
		////////		Select.From<Customer>("c").Single());
		////////}

		////////[TestMethod]
		////////public void TestSinglePredicate()
		////////{
		////////	TestQuery2(
		////////		"TestSinglePredicate",
		////////		Select.From<Customer>("c").Single(c => c.CustomerID == "ALFKI")
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestWhereSingle()
		////////{
		////////	TestQuery2(
		////////		"TestWhereSingle",
		////////		Select.From<Customer>("c").Where(c => c.CustomerID == "ALFKI").Single()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestSingleOrDefault()
		////////{
		////////	TestQuery2(
		////////		"TestSingleOrDefault",
		////////		Select.From<Customer>("c").SingleOrDefault());
		////////}

		////////[TestMethod]
		////////public void TestSingleOrDefaultPredicate()
		////////{
		////////	TestQuery2(
		////////		"TestSingleOrDefaultPredicate",
		////////		Select.From<Customer>("c").SingleOrDefault(c => c.CustomerID == "ALFKI")
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestWhereSingleOrDefault()
		////////{
		////////	TestQuery2(
		////////		"TestWhereSingleOrDefault",
		////////		Select.From<Customer>("c").Where(c => c.CustomerID == "ALFKI").SingleOrDefault()
		////////		);
		////////}

		[TestMethod]
		public void TestAnyWithSubquery()
		{
			var c = new Table("Customers", "c");
			var o = Select.From(new Table("Orders", "o"))
				.Where(new Condition(Col("o", "CustomerID"), SqlOperator.Equals, Col("c", "CustomerID")))
				.Where(new Condition(Year(Col("o", "OrderDate")), SqlOperator.Equals, 1997));
			TestQuery2(
				"TestAnyWithSubquery",
				Select.From(c).WhereExists(o));
		}

		[TestMethod]
		public void TestAnyWithSubqueryNoPredicate()
		{
			var c = new Table("Customers", "c");
			var o = Select.From(new Table("Orders", "o"))
				.Where(new Condition(Col("o", "CustomerID"), SqlOperator.Equals, Col("c", "CustomerID")));
			TestQuery2(
				"TestAnyWithSubqueryNoPredicate",
				Select.From(c).WhereExists(o));
		}

		[TestMethod]
		public void TestAnyWithLocalCollection()
		{
			string[] ids = new[] { "ABCDE", "ALFKI" };
			TestQuery2(
				"TestAnyWithLocalCollection",
				Select.From<Customer>("c").Where(c => ids.Contains(c.CustomerID)));
		}

		////////[TestMethod]
		////////public void TestAnyTopLevel()
		////////{
		////////	TestQuery2(
		////////		"TestAnyTopLevel",
		////////		Select.From<Customer>("c").Any()
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestAllWithSubquery()
		////////{
		////////	TestQuery2(
		////////		"TestAllWithSubquery",
		////////		Select.From<Customer>("c").Where(c => Select.From<Order>("o").Where(o => o.CustomerID == c.CustomerID).All(o => o.OrderDate.Year == 1997))
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestAllWithLocalCollection()
		////////{
		////////	string[] patterns = new[] { "a", "e" };

		////////	TestQuery2(
		////////		"TestAllWithLocalCollection",
		////////		Select.From<Customer>("c").Where(c => patterns.All(p => c.ContactName.Contains(p)))
		////////		);
		////////}

		////////[TestMethod]
		////////public void TestAllTopLevel()
		////////{
		////////	TestQuery2(
		////////		"TestAllTopLevel",
		////////		Select.From<Customer>("c").All(c => c.ContactName.StartsWith("a"))
		////////		);
		////////}

		[TestMethod]
		public void TestContainsWithSubquery()
		{
			var o = Select.From(new Table("Orders", "o")).Columns(Col("o", "CustomerID"));
			TestQuery2(
				"TestContainsWithSubquery",
				Select.From(new Table("Customers", "c")).WhereIn(Col("c", "CustomerID"), o));
		}

		[TestMethod]
		public void TestContainsWithLocalCollection()
		{
			var ids = new[] { "ABCDE", "ALFKI" };
			TestQuery2(
				"TestContainsWithLocalCollection",
				Select.From<Customer>("c").Where(c => ids.Contains(c.CustomerID))
				);
		}

		////////[TestMethod]
		////////public void TestContainsTopLevel()
		////////{
		////////	TestQuery2(
		////////		"TestContainsTopLevel",
		////////		Select.From<Customer>("c").Columns(c => c.CustomerID).Contains("ALFKI")
		////////		);
		////////}

		[TestMethod]
		public void TestCoalesce()
		{
			TestQuery2(
				"TestCoalesce",
				Select.From<Customer>("c").Where(c => (c.City ?? "Seattle") == "Seattle"));
		}

		[TestMethod]
		public void TestCoalesce2()
		{
			TestQuery2(
				"TestCoalesce2",
				Select.From<Customer>("c").Where(c => (c.City ?? c.Country ?? "Seattle") == "Seattle"));
		}

		[TestMethod]
		public void TestStringLength()
		{
			TestQuery2(
				"TestStringLength",
				Select.From<Customer>("c").Where(c => c.City.Length == 7));
		}

		[TestMethod]
		public void TestStringStartsWithLiteral()
		{
			TestQuery2(
				"TestStringStartsWithLiteral",
				Select.From<Customer>("c").Where(c => c.ContactName.StartsWith("M")));
		}

		[TestMethod]
		public void TestStringStartsWithColumn()
		{
			TestQuery2(
				"TestStringStartsWithColumn",
				Select.From<Customer>("c").Where(c => c.ContactName.StartsWith(c.ContactName)));
		}

		[TestMethod]
		public void TestStringEndsWithLiteral()
		{
			TestQuery2(
				"TestStringEndsWithLiteral",
				Select.From<Customer>("c").Where(c => c.ContactName.EndsWith("s")));
		}

		[TestMethod]
		public void TestStringEndsWithColumn()
		{
			TestQuery2(
				"TestStringEndsWithColumn",
				Select.From<Customer>("c").Where(c => c.ContactName.EndsWith(c.ContactName)));
		}

		[TestMethod]
		public void TestStringContainsLiteral()
		{
			TestQuery2(
				"TestStringContainsLiteral",
				Select.From<Customer>("c").Where(c => c.ContactName.Contains("and")));
		}

		[TestMethod]
		public void TestStringContainsColumn()
		{
			TestQuery2(
				"TestStringContainsColumn",
				Select.From<Customer>("c").Where(c => c.ContactName.Contains(c.ContactName)));
		}

		[TestMethod]
		public void TestStringConcatImplicit2Args()
		{
			TestQuery2(
				"TestStringConcatImplicit2Args",
				Select.From<Customer>("c").Where(c => c.ContactName + "X" == "X"));
		}

		[TestMethod]
		public void TestStringConcatExplicit2Args()
		{
			TestQuery2(
				"TestStringConcatExplicit2Args",
				Select.From<Customer>("c").Where(c => string.Concat(c.ContactName, "X") == "X"));
		}

		[TestMethod]
		public void TestStringConcatExplicit3Args()
		{
			TestQuery2(
				"TestStringConcatExplicit3Args",
				Select.From<Customer>("c").Where(c => string.Concat(c.ContactName, "X", c.Country) == "X"));
		}

		[TestMethod]
		public void TestStringConcatExplicitNArgs()
		{
			TestQuery2(
				"TestStringConcatExplicitNArgs",
				Select.From<Customer>("c").Where(c => string.Concat(new string[] { c.ContactName, "X", c.Country }) == "X"));
		}

		[TestMethod]
		public void TestStringIsNullOrEmpty()
		{
			TestQuery2(
				"TestStringIsNullOrEmpty",
				Select.From<Customer>("c").Where(c => string.IsNullOrEmpty(c.City)));
		}

		[TestMethod]
		public void TestStringToUpper()
		{
			TestQuery2(
				"TestStringToUpper",
				Select.From<Customer>("c").Where(c => c.City.ToUpper() == "SEATTLE"));
		}

		[TestMethod]
		public void TestStringToLower()
		{
			TestQuery2(
				"TestStringToLower",
				Select.From<Customer>("c").Where(c => c.City.ToLower() == "seattle"));
		}

		[TestMethod]
		public void TestStringSubstring()
		{
			TestQuery2(
				"TestStringSubstring",
				Select.From<Customer>("c").Where(c => c.City.Substring(0, 4) == "Seat"));
		}

		[TestMethod]
		public void TestStringSubstringNoLength()
		{
			TestQuery2(
				"TestStringSubstringNoLength",
				Select.From<Customer>("c").Where(c => c.City.Substring(4) == "tle"));
		}

		[TestMethod]
		public void TestStringIndexOf()
		{
			TestQuery2(
				"TestStringIndexOf",
				Select.From<Customer>("c").Where(c => c.City.IndexOf("tt") == 4));
		}

		[TestMethod]
		public void TestStringIndexOfChar()
		{
			TestQuery2(
				"TestStringIndexOfChar",
				Select.From<Customer>("c").Where(c => c.City.IndexOf('t') == 4));
		}

		[TestMethod]
		public void TestStringReplace()
		{
			TestQuery2(
				"TestStringReplace",
				Select.From<Customer>("c").Where(c => c.City.Replace("ea", "ae") == "Saettle"));
		}

		[TestMethod]
		public void TestStringReplaceChars()
		{
			TestQuery2(
				"TestStringReplaceChars",
				Select.From<Customer>("c").Where(c => c.City.Replace("e", "y") == "Syattly"));
		}

		[TestMethod]
		public void TestStringTrim()
		{
			TestQuery2(
				"TestStringTrim",
				Select.From<Customer>("c").Where(c => c.City.Trim() == "Seattle"));
		}

		[TestMethod]
		public void TestStringToString()
		{
			TestQuery2(
				"TestStringToString",
				Select.From<Customer>("c").Where(c => c.City.ToString() == "Seattle"));
		}

		[TestMethod]
		public void TestStringRemove()
		{
			TestQuery2(
				"TestStringRemove",
				Select.From<Customer>("c").Where(c => c.City.Remove(1, 2) == "Sttle"));
		}

		[TestMethod]
		public void TestStringRemoveNoCount()
		{
			TestQuery2(
				"TestStringRemoveNoCount",
				Select.From<Customer>("c").Where(c => c.City.Remove(4) == "Seat"));
		}

		[TestMethod]
		public void TestDateTimeConstructYmd()
		{
			TestQuery2(
				"TestDateTimeConstructYmd",
				Select.From<Order>("o").Where(o => o.OrderDate == new DateTime(o.OrderDate.Year, 1, 1)));
		}

		[TestMethod]
		public void TestDateTimeConstructYmdhms()
		{
			TestQuery2(
				"TestDateTimeConstructYmdhms",
				Select.From<Order>("o").Where(o => o.OrderDate == new DateTime(o.OrderDate.Year, 1, 1, 10, 25, 55)));
		}

		[TestMethod]
		public void TestDateTimeDay()
		{
			TestQuery2(
				"TestDateTimeDay",
				Select.From<Order>("o").Where(o => o.OrderDate.Day == 5));
		}

		[TestMethod]
		public void TestDateTimeMonth()
		{
			TestQuery2(
				"TestDateTimeMonth",
				Select.From<Order>("o").Where(o => o.OrderDate.Month == 12));
		}

		[TestMethod]
		public void TestDateTimeYear()
		{
			TestQuery2(
				"TestDateTimeYear",
				Select.From<Order>("o").Where(o => o.OrderDate.Year == 1997));
		}

		[TestMethod]
		public void TestDateTimeHour()
		{
			TestQuery2(
				"TestDateTimeHour",
				Select.From<Order>("o").Where(o => o.OrderDate.Hour == 6));
		}

		[TestMethod]
		public void TestDateTimeMinute()
		{
			TestQuery2(
				"TestDateTimeMinute",
				Select.From<Order>("o").Where(o => o.OrderDate.Minute == 32));
		}

		[TestMethod]
		public void TestDateTimeSecond()
		{
			TestQuery2(
				"TestDateTimeSecond",
				Select.From<Order>("o").Where(o => o.OrderDate.Second == 47));
		}

		[TestMethod]
		public void TestDateTimeMillisecond()
		{
			TestQuery2(
				"TestDateTimeMillisecond",
				Select.From<Order>("o").Where(o => o.OrderDate.Millisecond == 200));
		}

		[TestMethod]
		public void TestDateTimeDayOfWeek()
		{
			TestQuery2(
				"TestDateTimeDayOfWeek",
				Select.From<Order>("o").Where(o => o.OrderDate.DayOfWeek == DayOfWeek.Friday));
		}

		[TestMethod]
		public void TestDateTimeDayOfYear()
		{
			TestQuery2(
				"TestDateTimeDayOfYear",
				Select.From<Order>("o").Where(o => o.OrderDate.DayOfYear == 360));
		}

		[TestMethod]
		public void TestMathAbs()
		{
			TestQuery2(
				"TestMathAbs",
				Select.From<Order>("o").Where(o => Math.Abs(o.OrderID) == 10));
		}

		[TestMethod]
		public void TestMathAcos()
		{
			TestQuery2(
				"TestMathAcos",
				Select.From<Order>("o").Where(o => Math.Acos(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathAsin()
		{
			TestQuery2(
				"TestMathAsin",
				Select.From<Order>("o").Where(o => Math.Asin(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathAtan()
		{
			TestQuery2(
				"TestMathAtan",
				Select.From<Order>("o").Where(o => Math.Atan(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathAtan2()
		{
			TestQuery2(
				"TestMathAtan2",
				Select.From<Order>("o").Where(o => Math.Atan2(o.OrderID, 3) == 0),
				false);
		}

		[TestMethod]
		public void TestMathCos()
		{
			TestQuery2(
				"TestMathCos",
				Select.From<Order>("o").Where(o => Math.Cos(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathSin()
		{
			TestQuery2(
				"TestMathSin",
				Select.From<Order>("o").Where(o => Math.Sin(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathTan()
		{
			TestQuery2(
				"TestMathTan",
				Select.From<Order>("o").Where(o => Math.Tan(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathExp()
		{
			TestQuery2(
				"TestMathExp",
				Select.From<Order>("o").Where(o => Math.Exp(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathLog()
		{
			TestQuery2(
				"TestMathLog",
				Select.From<Order>("o").Where(o => Math.Log(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathLog10()
		{
			TestQuery2(
				"TestMathLog10",
				Select.From<Order>("o").Where(o => Math.Log10(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathSqrt()
		{
			TestQuery2(
				"TestMathSqrt",
				Select.From<Order>("o").Where(o => Math.Sqrt(o.OrderID) == 0),
				false);
		}

		[TestMethod]
		public void TestMathCeiling()
		{
			TestQuery2(
				"TestMathCeiling",
				Select.From<Order>("o").Where(o => Math.Ceiling((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathFloor()
		{
			TestQuery2(
				"TestMathFloor",
				Select.From<Order>("o").Where(o => Math.Floor((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathPow()
		{
			TestQuery2(
				"TestMathPow",
				Select.From<Order>("o").Where(o => Math.Pow(o.OrderID < 1000 ? 1 : 2, 3) == 0),
				false);
		}

		[TestMethod]
		public void TestMathRoundDefault()
		{
			TestQuery2(
				"TestMathRoundDefault",
				Select.From<Order>("o").Where(o => Math.Round((decimal)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestMathRoundToPlace()
		{
			TestQuery2(
				"TestMathRoundToPlace",
				Select.From<Order>("o").Where(o => Math.Round((decimal)o.OrderID, 2) == 0));
		}

		[TestMethod]
		public void TestMathTruncate()
		{
			TestQuery2(
				"TestMathTruncate",
				Select.From<Order>("o").Where(o => Math.Truncate((double)o.OrderID) == 0));
		}

		[TestMethod]
		public void TestStringCompareToLessThan()
		{
			TestQuery2(
				"TestStringCompareToLessThan",
				Select.From<Customer>("c").Where(c => c.City.CompareTo("Seattle") < 0));
		}

		[TestMethod]
		public void TestStringCompareToLessThanOrEqualTo()
		{
			TestQuery2(
				"TestStringCompareToLessThanOrEqualTo",
				Select.From<Customer>("c").Where(c => c.City.CompareTo("Seattle") <= 0));
		}

		[TestMethod]
		public void TestStringCompareToGreaterThan()
		{
			TestQuery2(
				"TestStringCompareToGreaterThan",
				Select.From<Customer>("c").Where(c => c.City.CompareTo("Seattle") > 0));
		}

		[TestMethod]
		public void TestStringCompareToGreaterThanOrEqualTo()
		{
			TestQuery2(
				"TestStringCompareToGreaterThanOrEqualTo",
				Select.From<Customer>("c").Where(c => c.City.CompareTo("Seattle") >= 0));
		}

		[TestMethod]
		public void TestStringCompareToEquals()
		{
			TestQuery2(
				"TestStringCompareToEquals",
				Select.From<Customer>("c").Where(c => c.City.CompareTo("Seattle") == 0));
		}

		[TestMethod]
		public void TestStringCompareToNotEquals()
		{
			TestQuery2(
				"TestStringCompareToNotEquals",
				Select.From<Customer>("c").Where(c => c.City.CompareTo("Seattle") != 0));
		}

		[TestMethod]
		public void TestStringCompareLessThan()
		{
			TestQuery2(
				"TestStringCompareLessThan",
				Select.From<Customer>("c").Where(c => string.Compare(c.City, "Seattle") < 0));
		}

		[TestMethod]
		public void TestStringCompareLessThanOrEqualTo()
		{
			TestQuery2(
				"TestStringCompareLessThanOrEqualTo",
				Select.From<Customer>("c").Where(c => string.Compare(c.City, "Seattle") <= 0));
		}

		[TestMethod]
		public void TestStringCompareGreaterThan()
		{
			TestQuery2(
				"TestStringCompareGreaterThan",
				Select.From<Customer>("c").Where(c => string.Compare(c.City, "Seattle") > 0));
		}

		[TestMethod]
		public void TestStringCompareGreaterThanOrEqualTo()
		{
			TestQuery2(
				"TestStringCompareGreaterThanOrEqualTo",
				Select.From<Customer>("c").Where(c => string.Compare(c.City, "Seattle") >= 0));
		}

		[TestMethod]
		public void TestStringCompareEquals()
		{
			TestQuery2(
				"TestStringCompareEquals",
				Select.From<Customer>("c").Where(c => string.Compare(c.City, "Seattle") == 0));
		}

		[TestMethod]
		public void TestStringCompareNotEquals()
		{
			TestQuery2(
				"TestStringCompareNotEquals",
				Select.From<Customer>("c").Where(c => string.Compare(c.City, "Seattle") != 0));
		}

		[TestMethod]
		public void TestIntCompareTo()
		{
			TestQuery2(
				"TestIntCompareTo",
				Select.From<Order>("o").Where(o => o.OrderID.CompareTo(1000) == 0));
		}

		[TestMethod]
		public void TestDecimalCompare()
		{
			TestQuery2(
				"TestDecimalCompare",
				Select.From<Order>("o").Where(o => decimal.Compare((decimal)o.OrderID, 0.0m) == 0));
		}

		[TestMethod]
		public void TestDecimalAdd()
		{
			TestQuery2(
				"TestDecimalAdd",
				Select.From<Order>("o").Where(o => decimal.Add(o.OrderID, 0.0m) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalSubtract()
		{
			TestQuery2(
				"TestDecimalSubtract",
				Select.From<Order>("o").Where(o => decimal.Subtract(o.OrderID, 0.0m) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalMultiply()
		{
			TestQuery2(
				"TestDecimalMultiply",
				Select.From<Order>("o").Where(o => decimal.Multiply(o.OrderID, 1.0m) == 1.0m));
		}

		[TestMethod]
		public void TestDecimalDivide()
		{
			TestQuery2(
				"TestDecimalDivide",
				Select.From<Order>("o").Where(o => decimal.Divide(o.OrderID, 1.0m) == 1.0m));
		}

		[TestMethod]
		public void TestDecimalRemainder()
		{
			TestQuery2(
				"TestDecimalRemainder",
				Select.From<Order>("o").Where(o => decimal.Remainder(o.OrderID, 1.0m) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalNegate()
		{
			TestQuery2(
				"TestDecimalNegate",
				Select.From<Order>("o").Where(o => decimal.Negate(o.OrderID) == 1.0m));
		}

		[TestMethod]
		public void TestDecimalCeiling()
		{
			TestQuery2(
				"TestDecimalCeiling",
				Select.From<Order>("o").Where(o => decimal.Ceiling(o.OrderID) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalFloor()
		{
			TestQuery2(
				"TestDecimalFloor",
				Select.From<Order>("o").Where(o => decimal.Floor(o.OrderID) == 0.0m));
		}

		[TestMethod]
		public void TestDecimalRoundDefault()
		{
			TestQuery2(
				"TestDecimalRoundDefault",
				Select.From<Order>("o").Where(o => decimal.Round(o.OrderID) == 0m));
		}

		[TestMethod]
		public void TestDecimalRoundPlaces()
		{
			TestQuery2(
				"TestDecimalRoundPlaces",
				Select.From<Order>("o").Where(o => decimal.Round(o.OrderID, 2) == 0.00m));
		}

		[TestMethod]
		public void TestDecimalTruncate()
		{
			TestQuery2(
				"TestDecimalTruncate",
				Select.From<Order>("o").Where(o => decimal.Truncate(o.OrderID) == 0m));
		}

		[TestMethod]
		public void TestDecimalLessThan()
		{
			TestQuery2(
				"TestDecimalLessThan",
				Select.From<Order>("o").Where(o => ((decimal)o.OrderID) < 0.0m));
		}

		[TestMethod]
		public void TestIntLessThan()
		{
			TestQuery2(
				"TestIntLessThan",
				Select.From<Order>("o").Where(o => o.OrderID < 0));
		}

		[TestMethod]
		public void TestIntLessThanOrEqual()
		{
			TestQuery2(
				"TestIntLessThanOrEqual",
				Select.From<Order>("o").Where(o => o.OrderID <= 0));
		}

		[TestMethod]
		public void TestIntGreaterThan()
		{
			TestQuery2(
				"TestIntGreaterThan",
				Select.From<Order>("o").Where(o => o.OrderID > 0));
		}

		[TestMethod]
		public void TestIntGreaterThanOrEqual()
		{
			TestQuery2(
				"TestIntGreaterThanOrEqual",
				Select.From<Order>("o").Where(o => o.OrderID >= 0));
		}

		[TestMethod]
		public void TestIntEqual()
		{
			TestQuery2(
				"TestIntEqual",
				Select.From<Order>("o").Where(o => o.OrderID == 0));
		}

		[TestMethod]
		public void TestIntNotEqual()
		{
			TestQuery2(
				"TestIntNotEqual",
				Select.From<Order>("o").Where(o => o.OrderID != 0));
		}

		[TestMethod]
		public void TestIntAdd()
		{
			TestQuery2(
				"TestIntAdd",
				Select.From<Order>("o").Where(o => o.OrderID + 0 == 0));
		}

		[TestMethod]
		public void TestIntSubtract()
		{
			TestQuery2(
				"TestIntSubtract",
				Select.From<Order>("o").Where(o => o.OrderID - 0 == 0));
		}

		[TestMethod]
		public void TestIntMultiply()
		{
			TestQuery2(
				"TestIntMultiply",
				Select.From<Order>("o").Where(o => o.OrderID * 1 == 1));
		}

		[TestMethod]
		public void TestIntDivide()
		{
			TestQuery2(
				"TestIntDivide",
				Select.From<Order>("o").Where(o => o.OrderID / 1 == 1));
		}

		[TestMethod]
		public void TestIntModulo()
		{
			TestQuery2(
				"TestIntModulo",
				Select.From<Order>("o").Where(o => o.OrderID % 1 == 0));
		}

		[TestMethod]
		public void TestIntLeftShift()
		{
			TestQuery2(
				"TestIntLeftShift",
				Select.From<Order>("o").Where(o => o.OrderID << 1 == 0));
		}

		[TestMethod]
		public void TestIntRightShift()
		{
			TestQuery2(
				"TestIntRightShift",
				Select.From<Order>("o").Where(o => o.OrderID >> 1 == 0));
		}

		[TestMethod]
		public void TestIntBitwiseAnd()
		{
			TestQuery2(
				"TestIntBitwiseAnd",
				Select.From<Order>("o").Where(o => (o.OrderID & 1) == 0));
		}

		[TestMethod]
		public void TestIntBitwiseOr()
		{
			TestQuery2(
				"TestIntBitwiseOr",
				Select.From<Order>("o").Where(o => (o.OrderID | 1) == 1));
		}

		[TestMethod]
		public void TestIntBitwiseExclusiveOr()
		{
			TestQuery2(
				"TestIntBitwiseExclusiveOr",
				Select.From<Order>("o").Where(o => (o.OrderID ^ 1) == 1));
		}

		[TestMethod]
		public void TestIntBitwiseNot()
		{
			TestQuery2(
				"TestIntBitwiseNot",
				Select.From<Order>("o").Where(o => ~o.OrderID == 0));
		}

		[TestMethod]
		public void TestIntNegate()
		{
			TestQuery2(
				"TestIntNegate",
				Select.From<Order>("o").Where(o => -o.OrderID == -1));
		}

		[TestMethod]
		public void TestAnd()
		{
			TestQuery2(
				"TestAnd",
				Select.From<Order>("o").Where(o => o.OrderID > 0 && o.OrderID < 2000));
		}

		[TestMethod]
		public void TestOr()
		{
			TestQuery2(
				"TestOr",
				Select.From<Order>("o").Where(o => o.OrderID < 5 || o.OrderID > 10));
		}

		[TestMethod]
		public void TestNot()
		{
			TestQuery2(
				"TestNot",
				Select.From<Order>("o").Where(o => !(o.OrderID == 0)));
		}

		[TestMethod]
		public void TestEqualNull()
		{
			TestQuery2(
				"TestEqualNull",
				Select.From<Customer>("c").Where(c => c.City == null));
		}

		[TestMethod]
		public void TestEqualNullReverse()
		{
			TestQuery2(
				"TestEqualNullReverse",
				Select.From<Customer>("c").Where(c => null == c.City));
		}

		[TestMethod]
		public void TestConditional()
		{
			TestQuery2(
				"TestConditional",
				Select.From<Order>("o").Where(o => (o.CustomerID == "ALFKI" ? 1000 : 0) == 1000));
		}

		[TestMethod]
		public void TestConditional2()
		{
			TestQuery2(
				"TestConditional2",
				Select.From<Order>("o").Where(o => (o.CustomerID == "ALFKI" ? 1000 : o.CustomerID == "ABCDE" ? 2000 : 0) == 1000));
		}

		[TestMethod]
		public void TestConditionalTestIsValue()
		{
			TestQuery2(
				"TestConditionalTestIsValue",
				Select.From<Order>("o").Where(o => (((bool)(object)o.OrderID) ? 100 : 200) == 100));
		}

		[TestMethod]
		public void TestConditionalResultsArePredicates()
		{
			TestQuery2(
				"TestConditionalResultsArePredicates",
				Select.From<Order>("o").Where(o => (o.CustomerID == "ALFKI" ? o.OrderID < 10 : o.OrderID > 10)));
		}

		[TestMethod]
		public void TestSelectManyJoined()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestSelectManyJoined",
				Select.From(c)
					.Join(o, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.Columns(Col("c", "ContactName"), Col("o", "OrderDate")));
		}

		[TestMethod]
		public void TestSelectManyJoinedDefaultIfEmpty()
		{
			var c = new Table("Customers", "c");
			var o = new Table("Orders", "o");
			TestQuery2(
				"TestSelectManyJoinedDefaultIfEmpty",
				Select.From(c)
					.Join(JoinType.Left, o, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.Columns(Col("c", "ContactName"), Col("o", "OrderDate")));
		}

		[TestMethod]
		public void TestSelectWhereAssociation()
		{
			var o = new Table("Orders", "o");
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectWhereAssociation",
				Select.From(o)
					.Join(c, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.Where(new Condition(Col("c", "City"), SqlOperator.Equals, "Seattle"))
					.ColumnsFrom(o));
		}

		[TestMethod]
		public void TestSelectWhereAssociations()
		{
			var o = new Table("Orders", "o");
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectWhereAssociations",
				Select.From(o)
					.Join(c, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.Where(new Condition(Col("c", "City"), SqlOperator.Equals, "Seattle"))
					.Where(new Condition(Col("c", "Phone"), SqlOperator.NotEquals, "555 555 5555"))
					.ColumnsFrom(o));
		}

		[TestMethod]
		public void TestSelectWhereAssociationTwice()
		{
			var o = new Table("Orders", "o");
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectWhereAssociationTwice",
				Select.From(o)
					.Join(c, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.Where(new Condition(Col("c", "City"), SqlOperator.Equals, "Seattle"))
					.Where(new Condition(Col("c", "Phone"), SqlOperator.NotEquals, "555 555 5555"))
					.ColumnsFrom(o));
		}

		[TestMethod]
		public void TestSelectAssociation()
		{
			var o = new Table("Orders", "o");
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectAssociation",
				Select.From(o)
					.Join(c, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.ColumnsFrom(c));
		}

		[TestMethod]
		public void TestSelectAssociations()
		{
			var o = new Table("Orders", "o");
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectAssociations",
				Select.From(o)
					.Join(c, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.ColumnsFrom(c));
		}

		[TestMethod]
		public void TestSelectAssociationsWhereAssociations()
		{
			var o = new Table("Orders", "o");
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSelectAssociationsWhereAssociations",
				Select.From(o)
					.Join(c, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.Where(new Condition(Col("c", "City"), SqlOperator.Equals, "Seattle"))
					.Where(new Condition(Col("c", "Phone"), SqlOperator.NotEquals, "555 555 5555"))
					.ColumnsFrom(c));
		}

		[TestMethod]
		public void TestSingletonAssociationWithMemberAccess()
		{
			var o = new Table("Orders", "o");
			var c = new Table("Customers", "c");
			TestQuery2(
				"TestSingletonAssociationWithMemberAccess",
				Select.From(o)
					.Join(c, Col("o", "CustomerID"), Col("c", "CustomerID"))
					.Where(new Condition(Col("c", "City"), SqlOperator.Equals, "Seattle"))
					.Where(new Condition(Col("c", "Phone"), SqlOperator.NotEquals, "555 555 5555"))
					.ColumnsFrom(c));
		}

		[TestMethod]
		public void TestCompareDateTimesWithDifferentNullability()
		{
			DateTime today = new DateTime(2013, 1, 1);
			TestQuery2(
				"TestCompareDateTimesWithDifferentNullability",
				Select.From<Order>("o").Where(o => o.OrderDate < today && ((DateTime?)o.OrderDate) < today));
		}

		[TestMethod]
		public void TestContainsWithEmptyLocalList()
		{
			var ids = Array.Empty<string>();
			TestQuery2(
				"TestContainsWithEmptyLocalList",
				from c in Select.From<Customer>("c")
				where ids.Contains(c.CustomerID)
				select c
				);
		}

		[TestMethod]
		public void TestContainsWithSubquery2()
		{
			var custsInLondon = Select.From(new Table("Customers", "c2"))
				.Where(new Condition(Col("c2", "City"), SqlOperator.Equals, "London"))
				.Columns(Col("c2", "CustomerID"));
			TestQuery2(
				"TestContainsWithSubquery2",
				Select.From(new Table("Customers", "c")).WhereIn(Col("c", "CustomerID"), custsInLondon));
		}

		[TestMethod]
		public void TestCombineQueriesDeepNesting()
		{
			var custs = Select.From(new Table("Customers", "c2"))
				.Where(new Condition(Col("c2", "ContactName"), SqlOperator.StartsWith, "xxx"))
				.Where(new Condition(Col("c2", "CustomerID"), SqlOperator.Equals, Col("o", "CustomerID")));
			var ords = Select.From(new Table("Orders", "o"))
				.Where(new Condition(Col("o", "OrderID"), SqlOperator.Equals, Col("d", "OrderID")))
				.WhereExists(custs);
			TestQuery2(
				"TestCombineQueriesDeepNesting",
				Select.From(new Table("Order Details", "d")).WhereExists(ords));
		}

		[TestMethod]
		public void TestLetWithSubquery()
		{
			var c = new Table("Customers", "c");
			var orders = Select.From(new Table("Orders", "o"))
				.Where(new Condition(Col("o", "CustomerID"), SqlOperator.Equals, Col("c", "CustomerID")))
				.Count();
			TestQuery2(
				"TestLetWithSubquery",
				Select.From(c).ColumnsFrom(c).ColumnSubquery(orders, "OrdersCount"));
		}

		private static Column Col(string table, string name)
		{
			return new Column(table, name);
		}

		private static DatePartFunction Year(Column column)
		{
			return new DatePartFunction(DatePart.Year) { Argument = column };
		}

		private void TestQuery2(string baseline, Statement query, bool testSQLite = true, bool testSqlServer = true)
		{
			var updateBaselines = Environment.GetEnvironmentVariable("WATSONIA_UPDATE_BASELINES") == "1";

			if (testSQLite)
			{
				// Test the SQLite command builder
				var sqliteBuilder = new SQLiteCommandBuilder();
				var sqliteCommand = query.Build(_mapper, sqliteBuilder);
				if (updateBaselines)
				{
					_sqliteBaselines[baseline] = GetSubstitutedCommandText(sqliteCommand);
				}
				else
				{
					TestQuery2(_sqliteBaselines[baseline], sqliteCommand, "*** SQLITE ***");
				}
			}

			if (testSqlServer)
			{
				// Test the SQL Server command builder
				var sqlServerBuilder = new SqlServerCommandBuilder();
				var sqlServerCommand = query.Build(_mapper, sqlServerBuilder);
				if (updateBaselines)
				{
					_sqlServerBaselines[baseline] = GetSubstitutedCommandText(sqlServerCommand);
				}
				else
				{
					TestQuery2(_sqlServerBaselines[baseline], sqlServerCommand, "*** SQL SERVER ***");
				}
			}

			if (updateBaselines)
			{
				SaveBaselines(_sqliteBaselineFileName, _sqliteBaselines);
				SaveBaselines(_sqlServerBaselineFileName, _sqlServerBaselines);
			}
		}

		private static string GetSubstitutedCommandText(Command command)
		{
			var text = command.CommandText.ToString();
			for (var i = 0; i < command.Parameters.Count; i++)
			{
				var value = Convert.ToString(command.Parameters[i], CultureInfo.InvariantCulture);
				if (command.Parameters[i] is string || command.Parameters[i] is char)
				{
					text = text.Replace("@" + i, "'" + value + "'");
				}
				else
				{
					text = text.Replace("@" + i, value);
				}
			}
			return text;
		}

		private static void SaveBaselines(string fileName, Dictionary<string, string> baselines)
		{
			var doc = new XDocument(new XElement("baselines",
				baselines.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv =>
					new XElement("baseline", new XAttribute("key", kv.Key), kv.Value))));
			doc.Save(fileName);
		}

		private void TestQuery2(string baseline, Command command, string provider)
		{
			var expected = TrimExtraWhiteSpace(baseline.Replace("\n\n", ") ("));
			var actual = TrimExtraWhiteSpace(command.CommandText.ToString());

			// Replace parameter references with their values so that we can check they have the correct value
			for (var i = 0; i < command.Parameters.Count; i++)
			{
				Assert.IsTrue(actual.Contains("@" + i));
				if (command.Parameters[i] is string ||
					command.Parameters[i] is char)
				{
					actual = actual.Replace("@" + i, "'" + Convert.ToString(command.Parameters[i], CultureInfo.InvariantCulture) + "'");
				}
				else
				{
					actual = actual.Replace("@" + i, Convert.ToString(command.Parameters[i], CultureInfo.InvariantCulture));
				}
			}

			Assert.AreEqual(expected, actual, provider);
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

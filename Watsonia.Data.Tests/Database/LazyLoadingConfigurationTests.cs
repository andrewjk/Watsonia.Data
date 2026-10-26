using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.SQLite;
using Watsonia.Data.Tests.Database.Entities;
using Watsonia.Data.Tests.Queries.Northwind;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Tests.Database
{
	/// <summary>
	/// Tests for lazy loading configuration and explicit asynchronous loading.
	/// </summary>
	[TestClass]
	public class LazyLoadingConfigurationTests
	{
		private const string EntitiesConnectionString = @"Data Source=Data/EntitiesTests.sqlite";
		private const string NorthwindConnectionString = @"Data Source=Data/Northwind.sqlite";
		private const string EntitiesNamespace = "Watsonia.Data.Tests.Database.Entities";
		private const string NorthwindNamespace = "Watsonia.Data.Tests.Queries.Northwind";

		private static readonly Watsonia.Data.Database _db = new EntitiesDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			// Seed parent/child rows for these tests (named uniquely so they don't clash with other tests)
			var deleteSubChildren = Delete.From<LoadingSubChild>().Where(true);
			_db.Execute(deleteSubChildren);
			var deleteChildren = Delete.From<LoadingChild>().Where(true);
			_db.Execute(deleteChildren);
			var deleteParents = Delete.From<LoadingParent>().Where(p => p.Name.StartsWith("LA"));
			_db.Execute(deleteParents);

			var parent = _db.Create<LoadingParent>();
			parent.Name = "LA1";
			parent.Children.Add(_db.Create(new LoadingChild() { Value = 11, Description = "Eleven" }));
			parent.Children.Add(_db.Create(new LoadingChild() { Value = 12, Description = "Twelve" }));
			parent.Children[0].SubChildren.Add(_db.Create(new LoadingSubChild() { SubName = "LA-SC1" }));
			_db.Save(parent);
		}

		private static Watsonia.Data.Database CreateDatabase(bool lazyLoadingEnabled)
		{
			var configuration = new DatabaseConfiguration(
				new SQLiteDataAccessProvider(),
				EntitiesConnectionString,
				EntitiesNamespace)
			{
				LazyLoadingEnabled = lazyLoadingEnabled
			};
			return new Watsonia.Data.Database(configuration);
		}

		[TestMethod]
		public void LazyLoadingDisabledThrowsForCollection()
		{
			var db = CreateDatabase(false);
			var parent = db.LoadCollection<LoadingParent>(
				Select.From("LoadingParent").Where("Name", SqlOperator.Equals, "LA1"))[0];

			var exception = Assert.ThrowsException<InvalidOperationException>(() => parent.Children.Count);
			StringAssert.Contains(exception.Message, "Lazy loading is disabled");
			StringAssert.Contains(exception.Message, "Children");
		}

		[TestMethod]
		public void LazyLoadingDisabledThrowsForItem()
		{
			var configuration = new NorthwindConfiguration(NorthwindConnectionString, NorthwindNamespace)
			{
				LazyLoadingEnabled = false
			};
			var db = new Watsonia.Data.Database(configuration);
			var order = db.LoadCollection<Order>(
				Select.From("Orders").Where("CustomerID", SqlOperator.Equals, "ALFKI"))[0];

			var exception = Assert.ThrowsException<InvalidOperationException>(() => order.Customer);
			StringAssert.Contains(exception.Message, "Lazy loading is disabled");
			StringAssert.Contains(exception.Message, "Customer");
		}

		[TestMethod]
		public async Task ExplicitLoadCollectionAsync()
		{
			var db = CreateDatabase(false);
			var parent = db.LoadCollection<LoadingParent>(
				Select.From("LoadingParent").Where("Name", SqlOperator.Equals, "LA1"))[0];

			await db.LoadCollectionAsync(parent, p => p.Children);

			Assert.AreEqual(2, parent.Children.Count);
			Assert.IsTrue(((IDynamicProxy)parent).StateTracker.LoadedCollections.Contains("Children"));
		}

		[TestMethod]
		public async Task ExplicitLoadItemAsync()
		{
			var configuration = new NorthwindConfiguration(NorthwindConnectionString, NorthwindNamespace)
			{
				LazyLoadingEnabled = false
			};
			var db = new Watsonia.Data.Database(configuration);
			var order = db.LoadCollection<Order>(
				Select.From("Orders").Where("CustomerID", SqlOperator.Equals, "ALFKI"))[0];

			await db.LoadItemAsync(order, o => o.Customer);

			Assert.IsNotNull(order.Customer);
			Assert.AreEqual("ALFKI", order.Customer.CustomerID);
			Assert.IsTrue(((IDynamicProxy)order).StateTracker.LoadedItems.Contains("Customer"));
		}

		[TestMethod]
		public async Task AsyncEagerLoadingWithIncludes()
		{
			var db = CreateDatabase(true);
			var select = Select.From("LoadingParent")
				.Include("Children")
				.Include("Children.SubChildren")
				.Where("Name", SqlOperator.Equals, "LA1");

			var parents = await db.LoadCollectionAsync<LoadingParent>(select);

			Assert.AreEqual(1, parents.Count);
			Assert.IsTrue(((IDynamicProxy)parents[0]).StateTracker.LoadedCollections.Contains("Children"));
			Assert.AreEqual(2, parents[0].Children.Count);
			Assert.IsTrue(((IDynamicProxy)parents[0].Children[0]).StateTracker.LoadedCollections.Contains("SubChildren"));
		}
	}
}

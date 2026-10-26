using System;
using System.Linq;
using System.Threading;
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

			var emptyParent = _db.Create<LoadingParent>();
			emptyParent.Name = "LA2";
			_db.Save(emptyParent);
		}

		private static async Task AssertCanceled(Func<Task> action)
		{
			try
			{
				await action();
			}
			catch (OperationCanceledException)
			{
				return;
			}

			Assert.Fail("Expected the operation to be canceled");
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

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => parent.Children.Count);
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

			var exception = Assert.ThrowsExactly<InvalidOperationException>(() => order.Customer);
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

		[TestMethod]
		public async Task CancellationIsHonored()
		{
			var db = CreateDatabase(true);
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await Assert.ThrowsExactlyAsync<OperationCanceledException>(
				() => db.LoadCollectionAsync<LoadingParent>(
					Select.From("LoadingParent").Where("Name", SqlOperator.Equals, "LA1"),
					cancellation.Token));
		}

		[TestMethod]
		public async Task StreamingYieldsRowsIncrementally()
		{
			var db = CreateDatabase(true);
			var rows = new System.Collections.Generic.List<LoadingParent>();

			await foreach (var parent in db.EnumerateCollectionAsync<LoadingParent>(
				Select.From("LoadingParent").Where("Name", SqlOperator.Equals, "LA1")))
			{
				rows.Add(parent);
				break; // early exit should close the reader
			}

			Assert.AreEqual(1, rows.Count);
			Assert.AreEqual("LA1", rows[0].Name);
		}

		[TestMethod]
		public void LazyLoadingWithNullForeignKeyReturnsNull()
		{
			var db = CreateDatabase(false);
			var item = db.Insert(new HasChanges { String = "LAZY" });
			var id = ((IDynamicProxy)item).__PrimaryKeyValue;
			try
			{
				var loaded = db.Load<HasChanges>(id);

				Assert.IsNull(loaded.Related);
				Assert.IsFalse(((IDynamicProxy)loaded).StateTracker.LoadedItems.Contains("Related"));
			}
			finally
			{
				db.Delete<HasChanges>(id);
			}
		}

		[TestMethod]
		public void LazyLoadingWithDefaultForeignKeyReturnsEmptyItem()
		{
			var db = CreateDatabase(false);
			var item = db.Insert(new HasChanges { String = "LAZY" });
			var id = ((IDynamicProxy)item).__PrimaryKeyValue;
			try
			{
				var loaded = db.Load<HasChanges>(id);
				((IDynamicProxy)loaded).__SetValue("RelatedID", -1L);

				// A default foreign key value produces an empty item without touching the database
				var related = loaded.Related;
				Assert.IsNotNull(related);
				Assert.IsTrue(((IDynamicProxy)related).StateTracker.IsNew);
				Assert.IsTrue(((IDynamicProxy)loaded).StateTracker.LoadedItems.Contains("Related"));
			}
			finally
			{
				db.Delete<HasChanges>(id);
			}
		}

		[TestMethod]
		public async Task ExplicitLoadersValidateArguments()
		{
			var db = CreateDatabase(false);
			var parent = db.LoadCollection<LoadingParent>(
				Select.From("LoadingParent").Where("Name", SqlOperator.Equals, "LA1"))[0];

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(
				() => db.LoadCollectionAsync<LoadingParent>(null, p => p.Children));
			await Assert.ThrowsExactlyAsync<ArgumentNullException>(
				() => db.LoadCollectionAsync(parent, null));
			await Assert.ThrowsExactlyAsync<ArgumentException>(
				() => db.LoadCollectionAsync(parent, p => p.Name));
			await Assert.ThrowsExactlyAsync<ArgumentException>(
				() => db.LoadItemAsync(parent, p => p.Children));
		}

		[TestMethod]
		public async Task ExplicitLoadItemWithNullForeignKey()
		{
			var db = CreateDatabase(false);
			var item = db.Insert(new HasChanges { String = "LAZY" });
			var id = ((IDynamicProxy)item).__PrimaryKeyValue;
			try
			{
				var loaded = db.Load<HasChanges>(id);

				await db.LoadItemAsync(loaded, h => h.Related);

				Assert.IsNull(loaded.Related);
				Assert.IsTrue(((IDynamicProxy)loaded).StateTracker.LoadedItems.Contains("Related"));
			}
			finally
			{
				db.Delete<HasChanges>(id);
			}
		}

		[TestMethod]
		public async Task AsyncMutationsHonorCancellation()
		{
			var db = CreateDatabase(true);
			var parent = db.LoadCollection<LoadingParent>(
				Select.From("LoadingParent").Where("Name", SqlOperator.Equals, "LA2"))[0];
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await AssertCanceled(() => db.SaveAsync(parent, cancellationToken: cancellation.Token));
			await AssertCanceled(() => db.InsertAsync(new LoadingParent { Name = "CANCELLED" }, cancellation.Token));
			await AssertCanceled(() => db.DeleteAsync<LoadingParent>(parent, cancellationToken: cancellation.Token));
		}

		[TestMethod]
		public async Task CancellationMidStreamStopsEnumeration()
		{
			var db = CreateDatabase(true);
			using var cancellation = new CancellationTokenSource();
			var seen = 0;
			OperationCanceledException caught = null;

			try
			{
				await foreach (var parent in db.EnumerateCollectionAsync<LoadingParent>(
					Select.From("LoadingParent").Where("Name", SqlOperator.StartsWith, "LA"),
					cancellation.Token))
				{
					seen++;
					cancellation.Cancel();
				}
			}
			catch (OperationCanceledException exception)
			{
				caught = exception;
			}

			Assert.AreEqual(1, seen);
			Assert.IsNotNull(caught);
		}
	}
}

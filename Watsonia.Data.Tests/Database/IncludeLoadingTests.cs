using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.SQLite;
using Watsonia.Data.Tests.Database.Entities;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.Tests.Database
{
	[TestClass]
	public class IncludeLoadingTests
	{
		private const string ConnectionString = @"Data Source=Data/EntitiesTests.sqlite";
		private const string EntityNamespace = "Watsonia.Data.Tests.Database.Entities";

		private static readonly Watsonia.Data.Database _db = new EntitiesDatabase();
		private static long _parentWithChildrenID;
		private static long _parentWithoutChildrenID;

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			var deleteSubChildren = Delete.From<LoadingSubChild>().Where(true);
			_db.Execute(deleteSubChildren);
			var deleteChildren = Delete.From<LoadingChild>().Where(true);
			_db.Execute(deleteChildren);
			var deleteParents = Delete.From<LoadingParent>().Where(p => p.Name.StartsWith("INC"));
			_db.Execute(deleteParents);

			var parent = _db.Create<LoadingParent>();
			parent.Name = "INC1";
			parent.Children.Add(_db.Create(new LoadingChild() { Value = 21, Description = "T21" }));
			parent.Children.Add(_db.Create(new LoadingChild() { Value = 22, Description = "T22" }));
			parent.Children[0].SubChildren.Add(_db.Create(new LoadingSubChild() { SubName = "INC-SC1" }));
			_db.Save(parent);
			_parentWithChildrenID = (long)((IDynamicProxy)parent).__PrimaryKeyValue;

			var emptyParent = _db.Create<LoadingParent>();
			emptyParent.Name = "INC2";
			_db.Save(emptyParent);
			_parentWithoutChildrenID = (long)((IDynamicProxy)emptyParent).__PrimaryKeyValue;
		}

		private static SelectStatement ParentsQuery()
		{
			return Select.From("LoadingParent").Where("Name", SqlOperator.StartsWith, "INC");
		}

		private static Watsonia.Data.Database CreateDatabase(bool lazyLoadingEnabled)
		{
			var configuration = new DatabaseConfiguration(
				new SQLiteDataAccessProvider(),
				ConnectionString,
				EntityNamespace)
			{
				LazyLoadingEnabled = lazyLoadingEnabled
			};
			return new Watsonia.Data.Database(configuration);
		}

		[TestMethod]
		public void IncludeLoadsCollectionsAndHandlesParentsWithoutChildren()
		{
			var query = ParentsQuery().Include("Children");
			var parents = _db.LoadCollection<LoadingParent>(query);

			Assert.AreEqual(2, parents.Count);

			var withChildren = parents.Single(p => p.Name == "INC1");
			var withoutChildren = parents.Single(p => p.Name == "INC2");

			Assert.AreEqual(2, withChildren.Children.Count);
			Assert.AreEqual(0, withoutChildren.Children.Count);
			Assert.IsTrue(((IDynamicProxy)withChildren).StateTracker.LoadedCollections.Contains("Children"));
			Assert.IsTrue(((IDynamicProxy)withoutChildren).StateTracker.LoadedCollections.Contains("Children"));

			// Each parent must get its own collection instance
			Assert.AreNotSame(withChildren.Children, withoutChildren.Children);
		}

		[TestMethod]
		public void IncludeWorksWhenLazyLoadingIsDisabled()
		{
			var db = CreateDatabase(false);
			var parents = db.LoadCollection<LoadingParent>(ParentsQuery().Include("Children"));
			var withChildren = parents.Single(p => p.Name == "INC1");

			Assert.AreEqual(2, withChildren.Children.Count);

			// Lazy loading is still disabled for non-included relationships
			var exception = Assert.ThrowsException<InvalidOperationException>(
				() => withChildren.Children[0].SubChildren.Count);
			StringAssert.Contains(exception.Message, "Lazy loading is disabled");
		}

		[TestMethod]
		public async Task AsyncIncludeHandlesParentsWithoutChildren()
		{
			var parents = await _db.LoadCollectionAsync<LoadingParent>(ParentsQuery().Include("Children"));

			Assert.AreEqual(2, parents.Count);
			Assert.AreEqual(2, parents.Single(p => p.Name == "INC1").Children.Count);
			Assert.AreEqual(0, parents.Single(p => p.Name == "INC2").Children.Count);
			Assert.IsTrue(((IDynamicProxy)parents.Single(p => p.Name == "INC2")).StateTracker.LoadedCollections.Contains("Children"));
		}

		[TestMethod]
		public void CompoundIncludePathsHaveDistinctCacheKeys()
		{
			// Same query shape and values, different include paths: the plan cache must not confuse them
			var flat = _db.LoadCollection<LoadingParent>(
				Select.From("LoadingParent").Include("Children").Where("Name", SqlOperator.Equals, "INC1"));
			Assert.IsFalse(((IDynamicProxy)flat[0].Children[0]).StateTracker.LoadedCollections.Contains("SubChildren"));

			var compound = _db.LoadCollection<LoadingParent>(
				Select.From("LoadingParent").Include("Children.SubChildren").Where("Name", SqlOperator.Equals, "INC1"));
			Assert.IsTrue(((IDynamicProxy)compound[0].Children[0]).StateTracker.LoadedCollections.Contains("SubChildren"));
		}

		[TestMethod]
		public async Task StreamingStopsCleanlyAndDatabaseRemainsUsable()
		{
			var rows = new List<LoadingParent>();
			await foreach (var parent in _db.EnumerateCollectionAsync<LoadingParent>(ParentsQuery()))
			{
				rows.Add(parent);
				break;
			}

			Assert.AreEqual(1, rows.Count);

			// The connection/reader must have been released, so another query works
			var again = await _db.LoadCollectionAsync<LoadingParent>(
				Select.From("LoadingParent").Where("Name", SqlOperator.Equals, "INC2"));
			Assert.AreEqual(1, again.Count);
			Assert.AreEqual(_parentWithoutChildrenID, ((IDynamicProxy)again[0]).__PrimaryKeyValue);
		}
	}
}

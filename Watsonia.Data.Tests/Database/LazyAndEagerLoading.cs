using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Watsonia.Data.SqlServer;
using System.IO;
using System;
using Watsonia.QueryBuilder;
using System.Threading.Tasks;
using Watsonia.Data.Tests.Database.Entities;

namespace Watsonia.Data.Tests.Database
{
	/// <summary>
	/// Contains general tests for the database.
	/// </summary>
	public partial class EntitiesTests
	{
		[TestMethod]
		public void LazyAndEagerLoading()
		{
			// Delete all existing subchilds, childs and parents
			var deleteSubChildren = Delete.From<LoadingSubChild>().Where(true);
			_db.Execute(deleteSubChildren);
			var deleteChildren = Delete.From<LoadingChild>().Where(true);
			_db.Execute(deleteChildren);
			var deleteParents = Delete.From<LoadingParent>().Where(true);
			_db.Execute(deleteParents);

			// Add a couple of test parents
			var newParent = _db.Create<LoadingParent>();
			newParent.Name = "P1";
			newParent.Children.Add(_db.Create(new LoadingChild() { Value = 1, Description = "One" }));
			newParent.Children[0].SubChildren.Add(_db.Create(new LoadingSubChild() { SubName = "SC1" }));
			newParent.Children[0].SubChildren.Add(_db.Create(new LoadingSubChild() { SubName = "SC2" }));
			newParent.Children.Add(_db.Create(new LoadingChild() { Value = 2, Description = "Two" }));
			_db.Save(newParent);

			var newParent2 = _db.Create<LoadingParent>();
			newParent2.Name = "P2";
			newParent2.Children.Add(_db.Create(new LoadingChild() { Value = 3, Description = "Three" }));
			newParent2.Children.Add(_db.Create(new LoadingChild() { Value = 4, Description = "Four" }));
			newParent2.Children.Add(_db.Create(new LoadingChild() { Value = 5, Description = "Five" }));
			_db.Save(newParent2);

			// Test lazy loading
			var select = Select.From("LoadingParent").Where("Name", SqlOperator.StartsWith, "P");
			var collection = _db.LoadCollection<LoadingParent>(select);
			Assert.AreEqual(2, collection.Count);
			Assert.IsFalse(((IDynamicProxy)collection[0]).StateTracker.LoadedCollections.Contains("Children"));
			Assert.AreEqual(2, collection[0].Children.Count);
			Assert.IsTrue(((IDynamicProxy)collection[0]).StateTracker.LoadedCollections.Contains("Children"));

			// Test eager loading
			var select2 = Select.From("LoadingParent").Include("Children").Where("Name", SqlOperator.StartsWith, "P");
			var collection2 = _db.LoadCollection<LoadingParent>(select2);
			Assert.AreEqual(2, collection2.Count);
			Assert.IsTrue(((IDynamicProxy)collection2[0]).StateTracker.LoadedCollections.Contains("Children"));

			// Test eager loading with dots
			var select3 = Select.From("LoadingParent").Include("Children").Include("Children.SubChildren").Where("Name", SqlOperator.StartsWith, "P");
			var collection3 = _db.LoadCollection<LoadingParent>(select3);
			Assert.AreEqual(2, collection3.Count);
			Assert.IsTrue(((IDynamicProxy)collection3[0]).StateTracker.LoadedCollections.Contains("Children"));
			Assert.IsTrue(((IDynamicProxy)((LoadingParent)collection3[0]).Children[0]).StateTracker.LoadedCollections.Contains("SubChildren"));
		}
	}
}

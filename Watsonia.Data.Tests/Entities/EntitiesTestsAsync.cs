﻿using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Watsonia.Data.SqlServer;
using System.IO;
using System;
using Watsonia.QueryBuilder;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Entities
{
	/// <summary>
	/// Contains general tests for the database.
	/// </summary>
	[TestClass]
	public class EntitiesTestsAsync
	{
		private readonly static EntitiesDatabase _db = new EntitiesDatabase();

		[ClassInitialize]
		public static void InitializeAsync(TestContext _)
		{
			if (!File.Exists(@"Data\EntitiesTestsAsync.sqlite"))
			{
				var file = File.Create(@"Data\EntitiesTestsAsync.sqlite");
				file.Dispose();
			}

			_db.UpdateDatabase();
		}

		[ClassCleanup]
		public static void Cleanup()
		{
		}

		[TestMethod]
		public async Task TestCrudOperationsAsync()
		{
			// Delete all existing cruds
			var deleteCruds = Delete.From("Crud").Where(true);
			await _db.ExecuteAsync(deleteCruds);

			// Check that the delete worked
			var countCruds = Select.From("Crud").Count("*");
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countCruds)));

			// Insert a crud and check that the insert worked and the new ID is correctly set
			var newCrud = _db.Create<Crud>();
			newCrud.Name = "ABC";
			await _db.SaveAsync(newCrud);
			Assert.AreEqual(1, Convert.ToInt32(await _db.LoadValueAsync(countCruds)));
			Assert.IsTrue(newCrud.ID > 0);

			// Load the inserted crud
			var crud = await _db.LoadAsync<Crud>(newCrud.ID);
			Assert.AreEqual("ABC", crud.Name);

			// Update the crud
			crud.Name = "DEF";
			await _db.SaveAsync(crud);

			// Load the updated crud
			var updatedCrud = await _db.LoadAsync<Crud>(newCrud.ID);
			Assert.AreEqual("DEF", crud.Name);

			// And delete it
			await _db.DeleteAsync(updatedCrud);
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countCruds)));
		}

		[TestMethod]
		public async Task TestHasChangesAsync()
		{
			// Delete all existing has changes items
			var deleteHasChanges = Delete.From("HasChanges").Where(true);
			await _db.ExecuteAsync(deleteHasChanges);
			var deleteHasChangesRelated = Delete.From("HasChangesRelated").Where(true);
			await _db.ExecuteAsync(deleteHasChangesRelated);

			// Check that the delete worked
			var countHasChanges = Select.From("HasChanges").Count("*");
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countHasChanges)));
			var countHasChangesRelated = Select.From("HasChangesRelated").Count("*");
			Assert.AreEqual(0, Convert.ToInt32(await _db.LoadValueAsync(countHasChangesRelated)));

			// Create a HasChanges and check that it has no changes
			var newHasChanges = _db.Create<HasChanges>();
			Assert.IsTrue(((IDynamicProxy)newHasChanges).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.HasChanges);

			// Create two related items and check that they have no changes
			var hasChangesRelated1 = _db.Create<HasChangesRelated>();
			Assert.IsTrue(((IDynamicProxy)hasChangesRelated1).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)hasChangesRelated1).StateTracker.HasChanges);
			await _db.SaveAsync(hasChangesRelated1);

			var hasChangesRelated2 = _db.Create<HasChangesRelated>();
			Assert.IsTrue(((IDynamicProxy)hasChangesRelated2).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)hasChangesRelated2).StateTracker.HasChanges);
			await _db.SaveAsync(hasChangesRelated2);

			// Even if we set things to their default values
			newHasChanges.Bool = false;
			newHasChanges.Int = 0;
			newHasChanges.DateTimeNullable = null;
			newHasChanges.Direction = CardinalDirection.Unknown;
			newHasChanges.DecimalWithDefault = 5.5m;
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)newHasChanges).StateTracker.ChangedFields.Count);

			// Insert it and check that the insert worked
			newHasChanges.String = "ABC";
			await _db.SaveAsync(newHasChanges);
			Assert.AreEqual(1, Convert.ToInt32(await _db.LoadValueAsync(countHasChanges)));
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)newHasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)newHasChanges).StateTracker.ChangedFields.Count);

			// Load the inserted HasChanges
			var hasChanges = await _db.LoadAsync<HasChanges>(((IDynamicProxy)newHasChanges).__PrimaryKeyValue);
			Assert.AreEqual("ABC", hasChanges.String);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.IsNew);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Make sure that setting things to their existing values doesn't change HasChanges
			hasChanges.String = "ABC";
			hasChanges.DecimalWithDefault = 5.5m;
			hasChanges.Int = 0;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Start setting things and checking that it's all good
			hasChanges.Bool = true;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Bool", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Bool = false;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.BoolNullable = true;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("BoolNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.BoolNullable = false;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("BoolNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.BoolNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Int = 10;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Int", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Int = 0;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.IntNullable = 11;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.IntNullable = 0;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.IntNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Decimal = 12.2m;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Decimal", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Decimal = 0;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.DecimalNullable = 13.3m;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DecimalNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DecimalNullable = 0;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DecimalNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DecimalNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.DateTime = DateTime.Now;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DateTime", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DateTime = new DateTime(1900, 1, 1);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.DateTimeNullable = DateTime.Now.AddDays(1);
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DateTimeNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DateTimeNullable = DateTime.MinValue;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("DateTimeNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.DateTimeNullable = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Direction = CardinalDirection.East;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Direction", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Direction = CardinalDirection.Unknown;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.String = "DEFGH";
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("String", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.String = "ABC";
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Test setting a related item
			hasChanges.Related = hasChangesRelated1;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			hasChanges.Related = null;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Related = hasChangesRelated2;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			await _db.SaveAsync(hasChanges);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Related = hasChangesRelated1;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);

			hasChanges.Related = null;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(1, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);

			hasChanges.Related = hasChangesRelated2;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			// Test setting multiple things
			hasChanges.IntNullable = 400;
			hasChanges.Int = 500;
			hasChanges.Bool = true;
			hasChanges.String = "GHI";
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(4, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			Assert.AreEqual("Int", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[1]);
			Assert.AreEqual("Bool", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[2]);
			Assert.AreEqual("String", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[3]);

			// Test saving and resetting original fields
			hasChanges.Bool = true;
			hasChanges.BoolNullable = false;
			hasChanges.Int = 6000;
			hasChanges.IntNullable = 7000;
			hasChanges.Decimal = 100.1m;
			hasChanges.DecimalNullable = 0;
			hasChanges.DateTime = DateTime.Today;
			hasChanges.DateTimeNullable = DateTime.Today.AddDays(-1);
			hasChanges.Direction = CardinalDirection.North;
			hasChanges.String = "JKL";
			hasChanges.DecimalWithDefault = 200.2m;
			hasChanges.Related = hasChangesRelated1;
			await _db.SaveAsync(hasChanges);
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);

			hasChanges.Bool = false;
			hasChanges.BoolNullable = true;
			hasChanges.Int = 8000;
			hasChanges.IntNullable = 9000;
			hasChanges.Decimal = 300.3m;
			hasChanges.DecimalNullable = 400.4m;
			hasChanges.DateTime = DateTime.Today.AddMonths(1);
			hasChanges.DateTimeNullable = DateTime.Today.AddDays(-1).AddMonths(1);
			hasChanges.Direction = CardinalDirection.South;
			hasChanges.String = "MNO";
			hasChanges.DecimalWithDefault = 500.5m;
			hasChanges.Related = hasChangesRelated2;
			Assert.IsTrue(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(12, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
			Assert.AreEqual("Bool", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[0]);
			Assert.AreEqual("BoolNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[1]);
			Assert.AreEqual("Int", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[2]);
			Assert.AreEqual("IntNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[3]);
			Assert.AreEqual("Decimal", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[4]);
			Assert.AreEqual("DecimalNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[5]);
			Assert.AreEqual("DateTime", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[6]);
			Assert.AreEqual("DateTimeNullable", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[7]);
			Assert.AreEqual("Direction", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[8]);
			Assert.AreEqual("String", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[9]);
			Assert.AreEqual("DecimalWithDefault", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[10]);
			Assert.AreEqual("RelatedID", ((IDynamicProxy)hasChanges).StateTracker.ChangedFields[11]);

			hasChanges.Bool = true;
			hasChanges.BoolNullable = false;
			hasChanges.Int = 6000;
			hasChanges.IntNullable = 7000;
			hasChanges.Decimal = 100.1m;
			hasChanges.DecimalNullable = 0;
			hasChanges.DateTime = DateTime.Today;
			hasChanges.DateTimeNullable = DateTime.Today.AddDays(-1);
			hasChanges.Direction = CardinalDirection.North;
			hasChanges.String = "JKL";
			hasChanges.DecimalWithDefault = 200.2m;
			hasChanges.Related = hasChangesRelated1;
			Assert.IsFalse(((IDynamicProxy)hasChanges).StateTracker.HasChanges);
			Assert.AreEqual(0, ((IDynamicProxy)hasChanges).StateTracker.ChangedFields.Count);
		}

		[TestMethod]
		public async Task TestCollectionsAsync()
		{
			// Delete all existing colls
			var deleteCollections = Delete.From("Collection").Where(true);
			await _db.ExecuteAsync(deleteCollections);

			await _db.InsertAsync(new Collection() { Value = 1, Description = "One" });
			await _db.InsertAsync(new Collection() { Value = 2, Description = "Two" });
			await _db.InsertAsync(new Collection() { Value = 3, Description = "Three" });
			await _db.InsertAsync(new Collection() { Value = 4, Description = "Four" });
			await _db.InsertAsync(new Collection() { Value = 5, Description = "Five" });

			// Load all of the colls except for one
			var select = Select.From("Collection").Where("Description", SqlOperator.NotEquals, "Four").OrderBy("Description");
			var collection = await _db.LoadCollectionAsync<Collection>(select);
			Assert.AreEqual(4, collection.Count);
			Assert.AreEqual("Two", collection[3].Description);

			// Load all of the colls except for one with an IN statement
			var select2 = Select.From("Collection").Where("Value", SqlOperator.IsIn, new int[] { 1, 2, 3, 5, 6}).OrderBy("Description");
			var collection2 = await _db.LoadCollectionAsync<Collection>(select2);
			Assert.AreEqual(4, collection2.Count);
			Assert.AreEqual("Two", collection2[3].Description);
		}

		[TestMethod]
		public async Task TestLazyAndEagerLoadingAsync()
		{
			// Delete all existing subchils, chils and pars
			var deleteSubChildren = Delete.From("SubChild").Where(true);
			await _db.ExecuteAsync(deleteSubChildren);
			var deleteChildren = Delete.From("Child").Where(true);
			await _db.ExecuteAsync(deleteChildren);
			var deleteParents = Delete.From("Parent").Where(true);
			await _db.ExecuteAsync(deleteParents);

			// Add a couple of test pars
			var newParent = _db.Create<Parent>();
			newParent.Name = "P1";
			newParent.Children.Add(_db.Create(new Child() { Value = 1, Description = "One" }));
			newParent.Children[0].SubChildren.Add(_db.Create(new SubChild() { SubName = "SC1" }));
			newParent.Children[0].SubChildren.Add(_db.Create(new SubChild() { SubName = "SC2" }));
			newParent.Children.Add(_db.Create(new Child() { Value = 2, Description = "Two" }));
			await _db.SaveAsync(newParent);

			var newParent2 = _db.Create<Parent>();
			newParent2.Name = "P2";
			newParent2.Children.Add(_db.Create(new Child() { Value = 3, Description = "Three" }));
			newParent2.Children.Add(_db.Create(new Child() { Value = 4, Description = "Four" }));
			newParent2.Children.Add(_db.Create(new Child() { Value = 5, Description = "Five" }));
			await _db.SaveAsync(newParent2);

			// Test lazy loading
			var select = Select.From("Parent").Where("Name", SqlOperator.StartsWith, "P");
			var collection = await _db.LoadCollectionAsync<Parent>(select);
			Assert.AreEqual(2, collection.Count);
			Assert.IsFalse(((IDynamicProxy)collection[0]).StateTracker.LoadedCollections.Contains("Children"));
			Assert.AreEqual(2, collection[0].Children.Count);
			Assert.IsTrue(((IDynamicProxy)collection[0]).StateTracker.LoadedCollections.Contains("Children"));

			// Test eager loading
			var select2 = Select.From("Parent").Include("Children").Where("Name", SqlOperator.StartsWith, "P");
			var collection2 = await _db.LoadCollectionAsync<Parent>(select2);
			Assert.AreEqual(2, collection2.Count);
			Assert.IsTrue(((IDynamicProxy)collection2[0]).StateTracker.LoadedCollections.Contains("Children"));

			// Test eager loading with dots
			var select3 = Select.From("Parent").Include("Children").Include("Children.SubChildren").Where("Name", SqlOperator.StartsWith, "P");
			var collection3 = await _db.LoadCollectionAsync<Parent>(select3);
			Assert.AreEqual(2, collection3.Count);
			Assert.IsTrue(((IDynamicProxy)collection3[0]).StateTracker.LoadedCollections.Contains("Children"));
			Assert.IsTrue(((IDynamicProxy)((Parent)collection3[0]).Children[0]).StateTracker.LoadedCollections.Contains("SubChildren"));
		}

		[TestMethod]
		public async Task TestAggregateFunctionsAsync()
		{
			// Delete all existing aggs
			var deleteAggregates = Delete.From("Aggregate").Where(true);
			await _db.ExecuteAsync(deleteAggregates);

			// Add some test aggs
			await _db.InsertAsync(new Aggregate() { Value = 1 });
			await _db.InsertAsync(new Aggregate() { Value = 3 });
			await _db.InsertAsync(new Aggregate() { Value = 5 });
			await _db.InsertAsync(new Aggregate() { Value = 7 });
			await _db.InsertAsync(new Aggregate() { Value = 11 });

			// Test count
			var selectCount = Select.From("Aggregate").Count("*");
			Assert.AreEqual(5, Convert.ToInt32(await _db.LoadValueAsync(selectCount)));

			// Test sum
			var selectSum = Select.From("Aggregate").Sum("Value");
			Assert.AreEqual(27d, Convert.ToDouble(await _db.LoadValueAsync(selectSum)));

			// Test average
			var selectAverage = Select.From("Aggregate").Average("Value");
			Assert.AreEqual(5.4, Convert.ToDouble(await _db.LoadValueAsync(selectAverage)));

			// Test minimum
			var selectMin = Select.From("Aggregate").Min("Value");
			Assert.AreEqual(1d, Convert.ToDouble(await _db.LoadValueAsync(selectMin)));

			// Test maximum
			var selectMax = Select.From("Aggregate").Max("Value");
			Assert.AreEqual(11d, Convert.ToDouble(await _db.LoadValueAsync(selectMax)));
		}

		[TestMethod]
		public void TestInsertStatement()
		{
			var insert =
				Insert.Into("Customer")
					.Value("Code", "HI123")
					.Value("Description", "Hi I'm a test value")
					.Value("ABN", "123 456 789")
					.Value("LicenseCount", 5);

			var dummyConfig = new DatabaseConfiguration(null, "", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			var command = provider.BuildCommand(insert, dummyConfig);

			// Make sure the SQL is correct
			Assert.AreEqual(
				"INSERT INTO [Customer] ([Code], [Description], [ABN], [LicenseCount]) VALUES (@0, @1, @2, @3)",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(4, command.Parameters.Count);
			Assert.AreEqual("HI123", command.Parameters["@0"].Value);
		}

		[TestMethod]
		public void TestSelectStatement()
		{
			var select =
				Select.From("Customer")
				.Columns("Name", "Code", "LicenseCount")
				.Where("Code", SqlOperator.Equals, "HI123")
				.And("ABN", SqlOperator.Equals, "123 456 789")
				.OrderBy("Name");

			var dummyConfig = new DatabaseConfiguration(null, "", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			var command = provider.BuildCommand(select, dummyConfig);

			// Make sure the SQL is correct
			Assert.AreEqual(
				"SELECT [Name], [Code], [LicenseCount] FROM [Customer] WHERE [Code] = @0 AND [ABN] = @1 ORDER BY [Name]",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(2, command.Parameters.Count);
			Assert.AreEqual("HI123", command.Parameters["@0"].Value);
		}

		[TestMethod]
		public void TestUpdateStatement()
		{
			var update =
				Update.Table("Customer")
					.Set("Code", "HI456")
					.Set("Description", "Hi I'm a test value")
					.Set("LicenseCount", 10)
					.Where("Code", SqlOperator.Equals, "HI123")
					.And("ABN", SqlOperator.Equals, "123 456 789");

			var dummyConfig = new DatabaseConfiguration(null, "", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			var command = provider.BuildCommand(update, dummyConfig);

			// Make sure the SQL is correct
			Assert.AreEqual(
				"UPDATE [Customer] SET [Code] = @0, [Description] = @1, [LicenseCount] = @2 WHERE [Code] = @3 AND [ABN] = @4",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(5, command.Parameters.Count);
			Assert.AreEqual("HI456", command.Parameters["@0"].Value);
		}

		[TestMethod]
		public void TestDeleteStatement()
		{
			var delete =
				Delete.From("Customer")
					.Where("Code", SqlOperator.Equals, "HI123")
					.And("LicenseCount", SqlOperator.Equals, 10);

			var dummyConfig = new DatabaseConfiguration(null, "", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			var command = provider.BuildCommand(delete, dummyConfig);

			// Make sure the SQL is correct
			Assert.AreEqual(
				"DELETE FROM [Customer] WHERE [Code] = @0 AND [LicenseCount] = @1",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(2, command.Parameters.Count);
			Assert.AreEqual("HI123", command.Parameters["@0"].Value);
			Assert.AreEqual(10, command.Parameters["@1"].Value);
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

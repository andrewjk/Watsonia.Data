using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.Tests.Models;
using System.Data.Common;
using Watsonia.Data.SqlServer;
using System.IO;

namespace Watsonia.Data.Tests
{
	[TestClass]
	public class DatabaseTests
	{
		public const string ConnectionString = @"Data Source=Data\DatabaseTests.sdf;Persist Security Info=False";

		private readonly static Database db = new Database(DatabaseTests.ConnectionString, "Watsonia.Data.Tests.Models");

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			if (!File.Exists(@"Data\DatabaseTests.sdf"))
			{
				File.Create(@"Data\DatabaseTests.sdf");
			}

			db.Configuration.ProviderName = "Watsonia.Data.SqlServerCe";
			db.UpdateDatabase();
		}

		[ClassCleanup]
		public static void Cleanup()
		{
		}

		[TestMethod]
		public void TestCrudOperations()
		{
			// Delete all existing cruds
			var deleteCruds = Delete.From("Crud").Where(true);
			db.Execute(deleteCruds);

			// Check that the delete worked
			var countCruds = Select.From("Crud").Count("*");
			Assert.AreEqual(0, db.LoadValue(countCruds));

			// Insert a crud and check that the insert worked and the new ID is correctly set
			Crud newCrud = db.Create<Crud>();
			newCrud.Name = "ABC";
			db.Save(newCrud);
			Assert.AreEqual(1, db.LoadValue(countCruds));
			Assert.IsTrue(newCrud.ID > 0);

			// Load the inserted crud
			Crud crud = db.Load<Crud>(newCrud.ID);
			Assert.AreEqual("ABC", crud.Name);

			// Update the crud
			crud.Name = "DEF";
			db.Save(crud);

			// Load the updated crud
			Crud updatedCrud = db.Load<Crud>(newCrud.ID);
			Assert.AreEqual("DEF", crud.Name);

			// And delete it
			db.Delete(updatedCrud);
			Assert.AreEqual(0, db.LoadValue(countCruds));
		}

		[TestMethod]
		public void TestCollections()
		{
			// Delete all existing colls
			var deleteColls = Delete.From("Coll").Where(true);
			db.Execute(deleteColls);

			db.Insert(new Coll() { Value = 1, Description = "One" });
			db.Insert(new Coll() { Value = 2, Description = "Two" });
			db.Insert(new Coll() { Value = 3, Description = "Three" });
			db.Insert(new Coll() { Value = 4, Description = "Four" });
			db.Insert(new Coll() { Value = 5, Description = "Five" });

			// Load all of the colls except for one
			var select = Select.From("Coll").Where("Description", SqlOperator.NotEquals, "Four").OrderBy("Description");
			var collection = db.LoadCollection<Coll>(select);
			Assert.AreEqual(4, collection.Count);
			Assert.AreEqual("Two", collection[3].Description);

			// Load all of the colls except for one with an IN statement
			var select2 = Select.From("Coll").Where("Value", SqlOperator.IsIn, new int[] { 1, 2, 3, 5, 6}).OrderBy("Description");
			var collection2 = db.LoadCollection<Coll>(select2);
			Assert.AreEqual(4, collection2.Count);
			Assert.AreEqual("Two", collection2[3].Description);
		}

		[TestMethod]
		public void TestLazyAndEagerLoading()
		{
			// Delete all existing subchils, chils and pars
			var deleteSubChils = Delete.From("SubChil").Where(true);
			db.Execute(deleteSubChils);
			var deleteChils = Delete.From("Chil").Where(true);
			db.Execute(deleteChils);
			var deletePars = Delete.From("Par").Where(true);
			db.Execute(deletePars);

			// Add a couple of test pars
			Par newPar = db.Create<Par>();
			newPar.Name = "P1";
			newPar.Chils.Add(db.Create(new Chil() { Value = 1, Description = "One" }));
			newPar.Chils[0].SubChils.Add(db.Create(new SubChil() { SubName = "SC1" }));
			newPar.Chils[0].SubChils.Add(db.Create(new SubChil() { SubName = "SC2" }));
			newPar.Chils.Add(db.Create(new Chil() { Value = 2, Description = "Two" }));
			db.Save(newPar);

			Par newPar2 = db.Create<Par>();
			newPar2.Name = "P2";
			newPar2.Chils.Add(db.Create(new Chil() { Value = 3, Description = "Three" }));
			newPar2.Chils.Add(db.Create(new Chil() { Value = 4, Description = "Four" }));
			newPar2.Chils.Add(db.Create(new Chil() { Value = 5, Description = "Five" }));
			db.Save(newPar2);

			// Test lazy loading
			var select = Select.From("Par").Where("Name", SqlOperator.StartsWith, "P");
			var collection = db.LoadCollection<Par>(select);
			Assert.AreEqual(2, collection.Count);
			Assert.IsFalse(((IDynamicProxy)collection[0]).StateTracker.LoadedCollections.Contains("Chils"));
			Assert.AreEqual(2, collection[0].Chils.Count);
			Assert.IsTrue(((IDynamicProxy)collection[0]).StateTracker.LoadedCollections.Contains("Chils"));

			// Test eager loading
			var select2 = Select.From("Par").Include("Chils").Where("Name", SqlOperator.StartsWith, "P");
			var collection2 = db.LoadCollection<Par>(select2);
			Assert.AreEqual(2, collection2.Count);
			Assert.IsTrue(((IDynamicProxy)collection2[0]).StateTracker.LoadedCollections.Contains("Chils"));

			// Test eager loading with dots
			var select3 = Select.From("Par").Include("Chils").Include("Chils.SubChils").Where("Name", SqlOperator.StartsWith, "P");
			var collection3 = db.LoadCollection<Par>(select3);
			Assert.AreEqual(2, collection3.Count);
			Assert.IsTrue(((IDynamicProxy)collection3[0]).StateTracker.LoadedCollections.Contains("Chils"));
			Assert.IsTrue(((IDynamicProxy)((Par)collection3[0]).Chils[0]).StateTracker.LoadedCollections.Contains("SubChils"));
		}

		[TestMethod]
		public void TestAggregateFunctions()
		{
			// Delete all existing aggs
			var deleteAggs = Delete.From("Agg").Where(true);
			db.Execute(deleteAggs);

			// Add some test aggs
			db.Insert(new Agg() { Value = 1 });
			db.Insert(new Agg() { Value = 3 });
			db.Insert(new Agg() { Value = 5 });
			db.Insert(new Agg() { Value = 7 });
			db.Insert(new Agg() { Value = 11 });

			// Test count
			var selectCount = Select.From("Agg").Count("*");
			Assert.AreEqual(5, db.LoadValue(selectCount));

			// Test sum
			var selectSum = Select.From("Agg").Sum("Value");
			Assert.AreEqual(27d, db.LoadValue(selectSum));

			// Test average
			var selectAverage = Select.From("Agg").Average("Value");
			Assert.AreEqual(5.4, db.LoadValue(selectAverage));

			// Test minimum
			var selectMin = Select.From("Agg").Min("Value");
			Assert.AreEqual(1d, db.LoadValue(selectMin));

			// Test maximum
			var selectMax = Select.From("Agg").Max("Value");
			Assert.AreEqual(11d, db.LoadValue(selectMax));
		}

		private void ExecuteNonQuery(string sql)
		{
			using (SqlConnection connection = new SqlConnection(DatabaseTests.ConnectionString))
			{
				connection.Open();
				using (SqlCommand command = new SqlCommand(sql, connection))
				{
					command.ExecuteNonQuery();
				}
			}
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

			DatabaseConfiguration dummyConfig = new DatabaseConfiguration("", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			DbCommand command = provider.BuildCommand(insert, dummyConfig);

			// Make sure the SQL is correct
			Assert.AreEqual(
				"INSERT INTO [Customer] ([Code], [Description], [ABN], [LicenseCount]) VALUES (@0, @1, @2, 5)",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(3, command.Parameters.Count);
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

			DatabaseConfiguration dummyConfig = new DatabaseConfiguration("", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			DbCommand command = provider.BuildCommand(select, dummyConfig);

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

			DatabaseConfiguration dummyConfig = new DatabaseConfiguration("", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			DbCommand command = provider.BuildCommand(update, dummyConfig);

			// Make sure the SQL is correct
			Assert.AreEqual(
				"UPDATE [Customer] SET [Code] = @0, [Description] = @1, [LicenseCount] = 10 WHERE [Code] = @2 AND [ABN] = @3",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(4, command.Parameters.Count);
			Assert.AreEqual("HI456", command.Parameters["@0"].Value);
		}

		[TestMethod]
		public void TestDeleteStatement()
		{
			var delete =
				Delete.From("Customer")
					.Where("Code", SqlOperator.Equals, "HI123")
					.And("LicenseCount", SqlOperator.Equals, 10);

			DatabaseConfiguration dummyConfig = new DatabaseConfiguration("", "");
			IDataAccessProvider provider = new SqlServerDataAccessProvider();
			DbCommand command = provider.BuildCommand(delete, dummyConfig);

			// Make sure the SQL is correct
			Assert.AreEqual(
				"DELETE FROM [Customer] WHERE [Code] = @0 AND [LicenseCount] = 10",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(1, command.Parameters.Count);
			Assert.AreEqual("HI123", command.Parameters["@0"].Value);
		}

		private string TrimExtraWhiteSpace(string s)
		{
			string result = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
			while (result.Contains("  "))
			{
				result = result.Replace("  ", " ");
			}
			result = result.Replace("( ", "(").Replace(" )", ")");
			return result;
		}
	}
}

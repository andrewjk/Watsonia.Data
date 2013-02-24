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
			// Delete all existing pars and colls
			//var deletePars = Delete.From("Par");
			//_database.ExecuteNonQuery(deletePars);
			var deleteColls = Delete.From("Coll").Where(true);
			db.Execute(deleteColls);

			db.Insert(new Coll() { Description = "One" });
			db.Insert(new Coll() { Description = "Two" });
			db.Insert(new Coll() { Description = "Three" });
			db.Insert(new Coll() { Description = "Four" });
			db.Insert(new Coll() { Description = "Five" });

			// TODO:
			// Add a test par
			//Par newPar = _database.Create<Par>();
			//newPar.Colls.Add(_database.Create(new Coll() { Description = "One" }));
			//newPar.Colls.Add(_database.Create(new Coll() { Description = "Two" }));
			//newPar.Colls.Add(_database.Create(new Coll() { Description = "Three" }));
			//newPar.Colls.Add(_database.Create(new Coll() { Description = "Four" }));
			//newPar.Colls.Add(_database.Create(new Coll() { Description = "Five" }));
			//_database.Save(newPar);

			// Select the colls
			var select = Select.From("Coll").Where("Description", SqlOperator.NotEquals, "Four").OrderBy("Description");
			var collection = db.LoadCollection<Coll>(select);
			Assert.AreEqual(4, collection.Count);
			Assert.AreEqual("Two", collection[3].Description);

			// Test lazy loading
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

		[TestMethod]
		public void TestLoad()
		{
			//Customer c = _database.Load<Customer>(1);
			//Assert.AreEqual(c.Name, "ABC Supplies");
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
				"INSERT INTO [Customer] ([Code], [Description], [ABN], [LicenseCount]) VALUES (@p0, @p1, @p2, 5)",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(3, command.Parameters.Count);
			Assert.AreEqual("HI123", command.Parameters["@p0"].Value);
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
				"SELECT [Name], [Code], [LicenseCount] FROM [Customer] WHERE [Code] = @p0 AND [ABN] = @p1 ORDER BY [Name]",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(2, command.Parameters.Count);
			Assert.AreEqual("HI123", command.Parameters["@p0"].Value);
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
				"UPDATE [Customer] SET [Code] = @p0, [Description] = @p1, [LicenseCount] = 10 WHERE [Code] = @p2 AND [ABN] = @p3",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(4, command.Parameters.Count);
			Assert.AreEqual("HI456", command.Parameters["@p0"].Value);
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
				"DELETE FROM [Customer] WHERE [Code] = @p0 AND [LicenseCount] = 10",
				TrimExtraWhiteSpace(command.CommandText));

			// Make sure the parameters are correct
			Assert.AreEqual(1, command.Parameters.Count);
			Assert.AreEqual("HI123", command.Parameters["@p0"].Value);
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

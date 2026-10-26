using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.SqlServer;
using Watsonia.Data.Tests.Database.Entities;

namespace Watsonia.Data.Tests.Database
{
	/// <summary>
	/// SQL Server integration tests, including the batched schema update path (DbBatch).
	/// Set WATSONIA_SQLSERVER_CONNECTION to a database you can modify to run them.
	/// </summary>
	[TestClass]
	[TestCategory("SqlServer")]
	public class SqlServerUpdateTests
	{
		private const string EntityNamespace = "Watsonia.Data.Tests.Database.Entities";

		private static Watsonia.Data.Database CreateDatabase()
		{
			var connectionString = Environment.GetEnvironmentVariable("WATSONIA_SQLSERVER_CONNECTION");
			if (string.IsNullOrEmpty(connectionString))
			{
				Assert.Inconclusive("Set WATSONIA_SQLSERVER_CONNECTION to run SQL Server integration tests");
			}

			return new Watsonia.Data.Database(new DatabaseConfiguration(
				new SqlServerDataAccessProvider(),
				connectionString,
				EntityNamespace));
		}

		[TestMethod]
		public void SchemaUpdatesRunTwiceAndRoundTripAnItem()
		{
			var database = CreateDatabase();

			// The first run creates the schema; the second exercises the batched (DbBatch) DDL path
			database.UpdateDatabase();
			database.UpdateDatabase();

			var item = database.Insert(new HasChanges { String = "SQLBATCH" });
			var id = ((IDynamicProxy)item).__PrimaryKeyValue;
			try
			{
				var loaded = database.Load<HasChanges>(id);
				Assert.AreEqual("SQLBATCH", loaded.String);
			}
			finally
			{
				database.Delete<HasChanges>(id);
			}
		}
	}
}

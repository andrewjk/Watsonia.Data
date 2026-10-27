using System;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace Watsonia.Data.Tests
{
	[TestClass]
	public static class TestSetup
	{
		[AssemblyInitialize]
		public static void AssemblyInitialize(TestContext context)
		{
			// Run from the test assembly's directory so that relative paths
			// to the Data folder and baseline files resolve correctly
			var assemblyDirectory = Path.GetDirectoryName(typeof(TestSetup).Assembly.Location);
			if (!string.IsNullOrEmpty(assemblyDirectory))
			{
				Directory.SetCurrentDirectory(assemblyDirectory);

				// Generate the Northwind fixture from the checked-in script so that
				// no binary database needs to be committed
				var dataDirectory = Path.Combine(assemblyDirectory, "Data");
				Directory.CreateDirectory(dataDirectory);
				var scriptPath = Path.Combine(dataDirectory, "Northwind.sql");
				var databasePath = Path.Combine(dataDirectory, "Northwind.sqlite");
				if (File.Exists(scriptPath))
				{
					if (File.Exists(databasePath))
					{
						File.Delete(databasePath);
					}

					using (var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + databasePath))
					{
						connection.Open();
						using (var command = connection.CreateCommand())
						{
							command.CommandText = File.ReadAllText(scriptPath);
							command.ExecuteNonQuery();
						}
					}
				}

				// Several test classes share the entity test database. Create its schema here
				// so that they do not depend on the order MSTest runs the test classes in.
				var entitiesPath = Path.Combine(dataDirectory, "EntitiesTests.sqlite");
				var entitiesDatabase = new Watsonia.Data.Database(new Watsonia.Data.DatabaseConfiguration(
					new Watsonia.Data.SQLite.SQLiteDataAccessProvider(),
					"Data Source=" + entitiesPath,
					"Watsonia.Data.Tests.Database.Entities"));
				entitiesDatabase.UpdateDatabase();
			}

			// Used by CI to run the suite under a non-English culture
			var culture = Environment.GetEnvironmentVariable("WATSONIA_TEST_CULTURE");
			if (!string.IsNullOrEmpty(culture))
			{
				var cultureInfo = new CultureInfo(culture);
				CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
				CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
				CultureInfo.CurrentCulture = cultureInfo;
				CultureInfo.CurrentUICulture = cultureInfo;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Watsonia.Data.SQLite;
using Watsonia.Data.Tests.Database.Entities;

namespace Watsonia.Data.Tests.Database
{
	[TestClass]
	public class SchemaUpdateTests
	{
		private const string EntityNamespace = "Watsonia.Data.Tests.Database.Entities";
		private static readonly List<string> TempDirectories = new List<string>();

		private static string CreateDatabaseFile()
		{
			var directory = Path.Combine(Path.GetTempPath(), "watsonia-schema-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			TempDirectories.Add(directory);
			return "Data Source=" + Path.Combine(directory, "Schema.sqlite");
		}

		[ClassCleanup]
		public static void Cleanup()
		{
			foreach (var directory in TempDirectories)
			{
				if (Directory.Exists(directory))
				{
					Directory.Delete(directory, true);
				}
			}
		}

		[TestMethod]
		public void UpdateScriptCreatesTablesWithoutDroppingAnything()
		{
			// A restricted type set (no enum seed data) so the script can be generated before the tables exist
			var database = new Watsonia.Data.Database(new ParentAndChildConfiguration(CreateDatabaseFile()));

			var script = database.GetUpdateScript();
			StringAssert.Contains(script, "CREATE TABLE");
			StringAssert.Contains(script, "LoadingParent");
			StringAssert.Contains(script, "LoadingChild");
			Assert.IsFalse(script.Contains("DROP TABLE"));

			database.UpdateDatabase();
			Assert.IsFalse(database.GetUpdateScript().Contains("CREATE TABLE"));
		}

		[TestMethod]
		public void UpdateScriptWorksOnAnEmptyDatabaseWithEnums()
		{
			// The full entity namespace includes enums (HasChangesDirection), whose seed data used to
			// be read from a table that didn't exist yet when generating a script
			var database = new Watsonia.Data.Database(new DatabaseConfiguration(
				new SQLiteDataAccessProvider(),
				CreateDatabaseFile(),
				EntityNamespace));

			var script = database.GetUpdateScript();

			StringAssert.Contains(script, "CREATE TABLE");
			StringAssert.Contains(script, "HasChangesDirection");
			StringAssert.Contains(script, "INSERT");

			database.UpdateDatabase();
			Assert.IsFalse(database.GetUpdateScript().Contains("CREATE TABLE"));
		}

		[TestMethod]
		public void SchemaUpgradeCreatesOnlyTheNewTables()
		{
			var connectionString = CreateDatabaseFile();

			var parentOnly = new Watsonia.Data.Database(new ParentOnlyConfiguration(connectionString));
			parentOnly.UpdateDatabase();
			Assert.IsFalse(parentOnly.GetUpdateScript().Contains("LoadingChild"));

			// Adding a type to the mapped set creates just its table
			var parentAndChild = new Watsonia.Data.Database(new ParentAndChildConfiguration(connectionString));
			var upgradeScript = parentAndChild.GetUpdateScript();
			StringAssert.Contains(upgradeScript, "CREATE TABLE");
			StringAssert.Contains(upgradeScript, "LoadingChild");
			Assert.IsFalse(upgradeScript.Contains("DROP TABLE"));

			parentAndChild.UpdateDatabase();
			Assert.IsFalse(parentAndChild.GetUpdateScript().Contains("CREATE TABLE"));
		}

		private sealed class ParentOnlyConfiguration : DatabaseConfiguration
		{
			public ParentOnlyConfiguration(string connectionString)
				: base(new SQLiteDataAccessProvider(), connectionString, SchemaUpdateTests.EntityNamespace)
			{
			}

			public override bool ShouldMapType(Type type)
			{
				return type == typeof(LoadingParent);
			}

			public override bool ShouldMapProperty(System.Reflection.PropertyInfo property)
			{
				return property.Name != nameof(LoadingParent.Children);
			}
		}

		private sealed class ParentAndChildConfiguration : DatabaseConfiguration
		{
			public ParentAndChildConfiguration(string connectionString)
				: base(new SQLiteDataAccessProvider(), connectionString, SchemaUpdateTests.EntityNamespace)
			{
			}

			public override bool ShouldMapType(Type type)
			{
				return type == typeof(LoadingParent) || type == typeof(LoadingChild);
			}

			public override bool ShouldMapProperty(System.Reflection.PropertyInfo property)
			{
				return property.Name != nameof(LoadingParent.Children)
					&& property.Name != nameof(LoadingChild.SubChildren);
			}
		}
	}
}

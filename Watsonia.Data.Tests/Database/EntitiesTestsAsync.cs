﻿using System.Data.SqlClient;
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
	[TestClass]
	public partial class EntitiesTestsAsync
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
	}
}

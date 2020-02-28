using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using Watsonia.Data.SqlServer;
using System.IO;
using System;
using Watsonia.QueryBuilder;
using System.Threading.Tasks;

namespace Watsonia.Data.Tests.Saving
{
	/// <summary>
	/// Contains general tests for the database.
	/// </summary>
	[TestClass]
	public partial class SavingTests
	{
		private readonly static SavingDatabase _db = new SavingDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			if (!File.Exists(@"Data\SavingTests.sqlite"))
			{
				var file = File.Create(@"Data\SavingTests.sqlite");
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Tests.Cache.Entities;

namespace Watsonia.Data.Tests.Cache
{
	[TestClass]
	public partial class CacheTests
	{
		private static readonly CacheDatabase _db = new CacheDatabase();

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			if (!File.Exists(@"Data\CacheTests.sqlite"))
			{
				var file = File.Create(@"Data\CacheTests.sqlite");
				file.Dispose();
			}

			_db.UpdateDatabase();

			// Create the proxies first so that their bags also get created
#pragma warning disable IDE0059 // Unnecessary assignment of a value
			var author = _db.Create<Teacher>();
			var book = _db.Create<Class>();
			var chapter = _db.Create<Student>();
#pragma warning restore IDE0059 // Unnecessary assignment of a value
		}

		private class TestValueBag : IValueBag
		{
			public int ID { get; set; }
		}
	}
}

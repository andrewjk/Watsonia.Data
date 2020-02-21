using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SQLite;

namespace Watsonia.Data.Tests.Cache
{
	public class CacheDatabase : Watsonia.Data.Database
	{
		private const string ConnectionString = @"Data Source=Data\CacheTests.sqlite";

		public CacheDatabase()
			: base(new SQLiteDataAccessProvider(), CacheDatabase.ConnectionString, "Watsonia.Data.Tests.Cache.Entities")
		{
		}
	}
}

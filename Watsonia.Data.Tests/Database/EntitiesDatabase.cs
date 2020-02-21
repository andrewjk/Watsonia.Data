using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SQLite;

namespace Watsonia.Data.Tests.Database
{
	public class EntitiesDatabase : Watsonia.Data.Database
	{
		private const string ConnectionString = @"Data Source=Data\EntitiesTests.sqlite";

		public EntitiesDatabase()
			: base(new SQLiteDataAccessProvider(), EntitiesDatabase.ConnectionString, "Watsonia.Data.Tests.Database.Entities")
		{
		}
	}
}

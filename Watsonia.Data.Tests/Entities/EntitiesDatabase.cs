using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SQLite;

namespace Watsonia.Data.Tests.Entities
{
	public class EntitiesDatabase : Database
	{
		private const string ConnectionString = @"Data Source=Data\EntitiesTests.sqlite";

		public EntitiesDatabase()
			: base(new SQLiteDataAccessProvider(), EntitiesDatabase.ConnectionString, "Watsonia.Data.Tests.Entities")
		{
		}
	}
}

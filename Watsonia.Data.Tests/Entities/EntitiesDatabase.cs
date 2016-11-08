using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SqlServerCe;

namespace Watsonia.Data.Tests.Entities
{
	public class EntitiesDatabase : Database
	{
		public const string ConnectionString = @"Data Source=Data\EntitiesTests.sdf;Persist Security Info=False";

		public EntitiesDatabase()
			: base(new SqlServerCeDataAccessProvider(), EntitiesDatabase.ConnectionString, "Watsonia.Data.Tests.Entities")
		{
		}
	}
}

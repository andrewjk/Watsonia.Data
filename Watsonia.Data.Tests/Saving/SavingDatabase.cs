using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SQLite;

namespace Watsonia.Data.Tests.Saving
{
	public class SavingDatabase : Watsonia.Data.Database
	{
		private const string ConnectionString = @"Data Source=Data\Saving.sqlite";

		public SavingDatabase()
			: base(new SQLiteDataAccessProvider(), SavingDatabase.ConnectionString, "Watsonia.Data.Tests.Saving.Entities")
		{
		}
	}
}

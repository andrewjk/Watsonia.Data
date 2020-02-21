using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SQLite;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public class DynamicProxyDatabase : Watsonia.Data.Database
	{
		private const string ConnectionString = @"Data Source=Data\DynamicProxyTests.sqlite";

		public DynamicProxyDatabase()
			: base(new SQLiteDataAccessProvider(), DynamicProxyDatabase.ConnectionString, "Watsonia.Data.Tests.DynamicProxy.Entities")
		{
		}
	}
}

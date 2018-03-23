using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.SqlServer;

namespace Watsonia.Data.Tests.DynamicProxy
{
	public class DynamicProxyDatabase : Database
	{
		public DynamicProxyDatabase()
			: base(
				  new SqlServerDataAccessProvider(),
				  AppConfiguration.ConnectionString.Replace("Northwind", "WatsoniaDataTests"),
				  "Watsonia.Data.Tests.DynamicProxy")
		{
		}
	}
}

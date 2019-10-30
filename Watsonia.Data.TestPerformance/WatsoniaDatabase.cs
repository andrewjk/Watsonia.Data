using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.TestPerformance
{
	internal sealed class WatsoniaDatabase : Database
	{
		public const string ConnectionString = @"Data Source=Data\Performance.sqlite";
		private const string EntityNamespace = "Watsonia.Data.TestPerformance.Entities";

		public WatsoniaDatabase(string dbname = null)
			: base(new WatsoniaConfiguration(ConnectionString, EntityNamespace))
		{
			if (!string.IsNullOrEmpty(dbname))
			{
				this.DatabaseName = dbname;
			}
		}

		protected override void OnBeforeExecuteCommand(DbCommand command)
		{
#if DEBUG
			var sqlString = GetSqlStringFromCommand(command);
			System.Diagnostics.Trace.WriteLine(sqlString, "Executed SQL");
#endif
		}
	}
}

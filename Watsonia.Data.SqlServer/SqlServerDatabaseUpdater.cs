using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.SqlServer
{
	internal sealed class SqlServerDatabaseUpdater : TSqlDatabaseUpdater
	{
		public SqlServerDatabaseUpdater(IDataAccessProvider dataAccessProvider, DatabaseConfiguration configuration)
			: base(dataAccessProvider, configuration)
		{
		}
	}
}

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.Linq;
using System.Reflection;
using System.Text;
using Watsonia.Data.SqlServer;

namespace Watsonia.Data.SqlServerCe
{
	internal sealed class SqlServerCeDatabaseUpdater : TSqlDatabaseUpdater
	{
		public SqlServerCeDatabaseUpdater(IDataAccessProvider dataAccessProvider, DatabaseConfiguration configuration)
			: base(dataAccessProvider, configuration)
		{
			// Can't create clustered keys among other things
			this.CompactEdition = true;
		}

		protected override DbCommand CreateCommand(DbConnection connection)
		{
			var command = new SqlCeCommand();
			command.Connection = (SqlCeConnection)connection;
			return command;
		}
	}
}

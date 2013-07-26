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
			// Can't create clustered keys
			this.ClusterKeys = false;
		}

		protected override Type FrameworkTypeFromDatabase(string databaseTypeName, bool allowNulls)
		{
			// Decimal is called numeric, apparently
			if (databaseTypeName.ToUpperInvariant() == "NUMERIC")
			{
				return allowNulls ? typeof(decimal?) : typeof(decimal);
			}
			else
			{
				return base.FrameworkTypeFromDatabase(databaseTypeName, allowNulls);
			}
		}

		protected override List<string> GetDefaultValueConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			// Can't get default value constraints
			return new List<string>();
		}

		protected override DbCommand CreateCommand(DbConnection connection)
		{
			var command = new SqlCeCommand();
			command.Connection = (SqlCeConnection)connection;
			return command;
		}
	}
}

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

		protected override void CreateTable(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			// Can't create clustered keys
			using (var command = CreateCommand(connection))
			{
				StringBuilder b = new StringBuilder();
				b.AppendFormat("CREATE TABLE [{0}] (", table.Name);
				b.AppendLine();
				b.Append(string.Join(", ", Array.ConvertAll(table.Columns.ToArray(), c => ColumnText(table, c, true, true))));
				b.AppendLine(",");
				b.AppendFormat("CONSTRAINT [{0}] PRIMARY KEY ([{1}])", table.PrimaryKeyConstraintName, table.PrimaryKeyColumnName);
				b.AppendLine();
				b.Append(")");
				command.CommandText = b.ToString();
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		protected override void CreateForeignKey(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			// Can't create clustered keys
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format(
					"ALTER TABLE [{0}] ADD CONSTRAINT [{1}] FOREIGN KEY ([{2}]) REFERENCES [{3}] ({4})",
					table.Name, column.Relationship.ConstraintName, column.Name, column.Relationship.ForeignTableName, column.Relationship.ForeignTableColumnName);
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
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

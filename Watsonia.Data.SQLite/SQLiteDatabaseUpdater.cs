using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Mapping;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.SQLite
{
	public class SQLiteDatabaseUpdater
	{
		protected IDataAccessProvider _dataAccessProvider;
		protected DatabaseConfiguration _configuration;

		public SQLiteDatabaseUpdater(IDataAccessProvider dataAccessProvider, DatabaseConfiguration configuration)
		{
			_dataAccessProvider = dataAccessProvider;
			_configuration = configuration;
		}

		public void UpdateDatabase(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions)
		{
			UpdateDatabase(tables, views, procedures, functions, true);
		}

		public string GetUpdateScript(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions)
		{
			var script = new StringBuilder();
			UpdateDatabase(tables, views, procedures, functions, false, script);
			return script.ToString();
		}

		protected virtual void UpdateDatabase(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions, bool doUpdate, StringBuilder script = null)
		{
			using (var connection = _dataAccessProvider.OpenConnection(_configuration))
			{
				// Load the existing tables and columns
				var existingTables = LoadExistingTables(connection);
				var existingColumns = LoadExistingColumns(existingTables, connection);
				var existingViews = LoadExistingViews(connection);

				// First pass - create or update tables and columns
				foreach (var table in tables)
				{
					if (existingTables.ContainsKey(table.Name.ToUpperInvariant()))
					{
						// The table exists so we need to check whether it should be updated
						foreach (var column in table.Columns)
						{
							var key = table.Name.ToUpperInvariant() + "." + column.Name.ToUpperInvariant();
							if (existingColumns.ContainsKey(key))
							{
								// The column exists so we need to check whether it should be updated
								UpdateColumn(table, existingColumns[key], column, connection, doUpdate, script);
							}
							else
							{
								// The column doesn't exist so it needs to be created
								CreateColumn(table, column, connection, doUpdate, script);
							}
						}
					}
					else
					{
						// The table doesn't exist so it needs to be created
						CreateTable(table, connection, doUpdate, script);
					}
				}

				// Second pass - fill table data
				foreach (var table in tables.Where(t => t.Values.Count > 0))
				{
					UpdateTableData(table, connection, doUpdate, script);
				}

				// Third pass - create relationship constraints
				var existingForeignKeys = LoadExistingForeignKeys(connection);
				foreach (var table in tables)
				{
					foreach (var column in table.Columns.Where(c => c.Relationship != null))
					{
						if (!existingForeignKeys.Contains(column.Relationship.ConstraintName))
						{
							CreateForeignKey(table, column, connection, doUpdate, script);
							existingForeignKeys.Add(column.Relationship.ConstraintName);
						}
					}
				}

				// Fourth pass - create views
				foreach (var view in views)
				{
					var key = view.Name.ToUpperInvariant();
					if (existingViews.ContainsKey(key))
					{
						// The view exists so we need to check whether it should be updated
						UpdateView(view, existingViews[key], connection, doUpdate, script);
					}
					else
					{
						// The view doesn't exist so it needs to be created
						CreateView(view, connection, doUpdate, script);
					}
				}
			}
		}

		protected virtual Dictionary<string, MappedTable> LoadExistingTables(DbConnection connection)
		{
			var existingTables = new Dictionary<string, MappedTable>();
			using (var existingTablesCommand = CreateCommand(connection))
			{
				existingTablesCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
				existingTablesCommand.Connection = connection;
				using (var reader = existingTablesCommand.ExecuteReader())
				{
					while (reader.Read())
					{
						var tableName = reader.GetString(reader.GetOrdinal("name"));
						var table = new MappedTable(tableName);
						existingTables.Add(tableName.ToUpperInvariant(), table);
					}
				}
			}
			return existingTables;
		}

		protected virtual Dictionary<string, MappedColumn> LoadExistingColumns(Dictionary<string, MappedTable> existingTables, DbConnection connection)
		{
			var existingColumns = new Dictionary<string, MappedColumn>();
			foreach (var table in existingTables.Values)
			{
				using (var existingColumnsCommand = CreateCommand(connection))
				{
					existingColumnsCommand.CommandText = $"PRAGMA table_info('{table.Name}')";
					existingColumnsCommand.Connection = connection;
					using (var reader = existingColumnsCommand.ExecuteReader())
					{
						while (reader.Read())
						{
							var tableName = table.Name;
							var columnName = reader.GetString(reader.GetOrdinal("name"));
							var allowNulls = (reader.GetString(reader.GetOrdinal("notnull")) == "0");
							var defaultValue = reader.GetValue(reader.GetOrdinal("dflt_value"));
							if (defaultValue == DBNull.Value)
							{
								defaultValue = null;
							}
							var dataTypeName = reader.GetString(reader.GetOrdinal("type"));
							var columnType = FrameworkTypeFromDatabase(dataTypeName, allowNulls);
							var maxLength = 0;
							if (columnType == typeof(string) && dataTypeName.Contains("("))
							{
								var start = dataTypeName.IndexOf("(") + 1;
								var end = dataTypeName.IndexOf(")", start);
								maxLength = Convert.ToInt32(dataTypeName.Substring(start, end - start));
							}
							var column = new MappedColumn(columnName, columnType, "");
							column.MaxLength = maxLength;
							column.AllowNulls = allowNulls;
							column.DefaultValue = defaultValue;
							var key = tableName.ToUpperInvariant() + "." + columnName.ToUpperInvariant();
							existingColumns.Add(key, column);
						}
					}
				}
			}
			return existingColumns;
		}

		protected virtual Dictionary<string, MappedView> LoadExistingViews(DbConnection connection)
		{
			var existingViews = new Dictionary<string, MappedView>();
			using (var existingViewsCommand = CreateCommand(connection))
			{
				existingViewsCommand.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'view'";
				existingViewsCommand.Connection = connection;
				using (var reader = existingViewsCommand.ExecuteReader())
				{
					while (reader.Read())
					{
						var viewName = reader.GetString(reader.GetOrdinal("name"));
						var selectStatementText = reader.GetString(reader.GetOrdinal("sql"));
						var view = new MappedView(viewName);
						view.SelectStatementText = selectStatementText;
						var key = viewName.ToUpperInvariant();
						existingViews.Add(key, view);
					}
				}
			}
			return existingViews;
		}

		protected virtual Type FrameworkTypeFromDatabase(string databaseTypeName, bool allowNulls)
		{
			var typeName = databaseTypeName.Contains("(") ? databaseTypeName.Substring(0, databaseTypeName.IndexOf("(")) : databaseTypeName;

			switch (typeName.ToUpperInvariant())
			{
				case "BIT":
				{
					return allowNulls ? typeof(bool?) : typeof(bool);
				}
				case "DATETIME":
				{
					return allowNulls ? typeof(DateTime?) : typeof(DateTime);
				}
				case "DECIMAL":
				{
					return allowNulls ? typeof(decimal?) : typeof(decimal);
				}
				case "FLOAT":
				{
					return allowNulls ? typeof(double?) : typeof(double);
				}
				case "INT":
				{
					return allowNulls ? typeof(int?) : typeof(int);
				}
				case "BIGINT":
				case "INTEGER":
				{
					return allowNulls ? typeof(long?) : typeof(long);
				}
				case "TINYINT":
				{
					return allowNulls ? typeof(byte?) : typeof(byte);
				}
				case "VARCHAR":
				case "NVARCHAR":
				{
					return typeof(string);
				}
				case "UNIQUEIDENTIFIER":
				{
					return typeof(Guid);
				}
				default:
				{
					throw new InvalidOperationException("Invalid data type: " + databaseTypeName);
				}
			}
		}

		protected virtual List<string> LoadExistingForeignKeys(DbConnection connection)
		{
			var existingForeignKeys = new List<string>();

			// TODO:
			//using (var existingForeignKeysCommand = CreateCommand(connection))
			//{
			//	existingForeignKeysCommand.CommandText = "SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS";
			//	existingForeignKeysCommand.Connection = connection;
			//	using (var reader = existingForeignKeysCommand.ExecuteReader())
			//	{
			//		while (reader.Read())
			//		{
			//			existingForeignKeys.Add(reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME")));
			//		}
			//	}
			//}

			return existingForeignKeys;
		}

		protected virtual void CreateTable(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				var b = new StringBuilder();
				b.AppendLine($"CREATE TABLE [{table.Name}] (");
				b.Append(string.Join(", ", Array.ConvertAll(table.Columns.ToArray(), c => ColumnText(table, c, true, true))));
				b.AppendLine(",");
				b.AppendLine($"CONSTRAINT [{table.PrimaryKeyConstraintName}] PRIMARY KEY ([{table.PrimaryKeyColumnName}] ASC)");
				b.Append(")");
				command.CommandText = b.ToString();
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		protected virtual void CreateColumn(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				command.CommandText = $"ALTER TABLE [{table.Name}] ADD {ColumnText(table, column, true, true)}";
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		protected virtual string ColumnText(MappedTable table, MappedColumn column, bool includeDefault, bool includeIdentity)
		{
			var b = new StringBuilder();
			b.Append($"[{column.Name}] {ColumnTypeText(column)}");
			if (!column.IsPrimaryKey)
			{
				if (column.AllowNulls)
				{
					b.Append(" NULL");
					if (includeDefault && column.DefaultValue != null)
					{
						b.Append(" CONSTRAINT [");
						b.Append(column.DefaultValueConstraintName);
						b.Append("] DEFAULT ");
						b.Append(ColumnDefaultText(column));
					}
				}
				else
				{
					b.Append(" NOT NULL");
					if (includeDefault)
					{
						b.Append(" CONSTRAINT [");
						b.Append(column.DefaultValueConstraintName);
						b.Append("] DEFAULT ");
						b.Append(ColumnDefaultText(column));
					}
				}
			}
			return b.ToString();
		}

		protected virtual string ColumnTypeText(MappedColumn column)
		{
			if (column.IsPrimaryKey)
			{
				// The primary key has to be of type INTEGER in order to be aliased to ROWID
				return "INTEGER";
			}
			else if (column.ColumnType == typeof(bool) || column.ColumnType == typeof(bool?))
			{
				return "BIT";
			}
			else if (column.ColumnType == typeof(DateTime) || column.ColumnType == typeof(DateTime?))
			{
				return "DATETIME";
			}
			else if (column.ColumnType == typeof(decimal) || column.ColumnType == typeof(decimal?))
			{
				return "DECIMAL(19,5)";
			}
			else if (column.ColumnType == typeof(double) || column.ColumnType == typeof(double?))
			{
				return "FLOAT";
			}
			else if (column.ColumnType == typeof(int) || column.ColumnType == typeof(int?))
			{
				return "INT";
			}
			else if (column.ColumnType == typeof(long) || column.ColumnType == typeof(long?))
			{
				return "BIGINT";
			}
			else if (column.ColumnType == typeof(byte) || column.ColumnType == typeof(byte?))
			{
				return "TINYINT";
			}
			else if (column.ColumnType == typeof(string))
			{
				if (column.MaxLength >= 4000)
				{
					return "TEXT";
				}
				else
				{
					return $"NVARCHAR({column.MaxLength})";
				}
			}
			else if (column.ColumnType == typeof(Guid) || column.ColumnType == typeof(Guid?))
			{
				return "UNIQUEIDENTIFIER";
			}
			else
			{
				throw new InvalidOperationException("Invalid column type: " + column.ColumnType);
			}
		}

		protected virtual string ColumnDefaultText(MappedColumn column)
		{
			// TODO: This isn't really the best spot for this
			if (column.ColumnType == typeof(bool) || column.ColumnType == typeof(bool?))
			{
				return column.DefaultValue != null && (bool)column.DefaultValue ? "1" : "0";
			}
			else if (column.ColumnType == typeof(DateTime) || column.ColumnType == typeof(DateTime?))
			{
				return column.DefaultValue != null ? $"'{(DateTime)column.DefaultValue:d-MMM-yyyy}'" : "'1-JAN-1753'";
			}
			else if (column.ColumnType == typeof(decimal) || column.ColumnType == typeof(decimal?))
			{
				return column.DefaultValue != null ? column.DefaultValue.ToString() : "0";
			}
			else if (column.ColumnType == typeof(double) || column.ColumnType == typeof(double?))
			{
				return column.DefaultValue != null ? column.DefaultValue.ToString() : "0";
			}
			else if (column.ColumnType == typeof(int) || column.ColumnType == typeof(int?))
			{
				return column.DefaultValue != null ? column.DefaultValue.ToString() : "0";
			}
			else if (column.ColumnType == typeof(long) || column.ColumnType == typeof(long?))
			{
				return column.DefaultValue != null ? column.DefaultValue.ToString() : "0";
			}
			else if (column.ColumnType == typeof(byte) || column.ColumnType == typeof(byte?))
			{
				return column.DefaultValue != null ? column.DefaultValue.ToString() : "0";
			}
			else if (column.ColumnType == typeof(string))
			{
				return "''";
			}
			else if (column.ColumnType == typeof(Guid))
			{
				return column.DefaultValue != null ? $"'{column.DefaultValue:D}'" : "'00000000-0000-0000-0000-000000000000'";
			}
			else
			{
				throw new InvalidOperationException("Invalid column type: " + column.ColumnType);
			}
		}

		protected virtual void CreateForeignKey(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			// TODO: Uh, can't ALTER TABLE to add foreign keys in SQLite
			//using (var command = CreateCommand(connection))
			//{
			//	string constraintName = column.Relationship.ConstraintName;
			//	string foreignTableName = column.Relationship.ForeignTableName;
			//	string foreignTableColumnName = column.Relationship.ForeignTableColumnName;
			//	command.CommandText = $"ALTER TABLE [{table.Name}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{column.Name}]) REFERENCES [{foreignTableName}] ({foreignTableColumnName})";
			//	command.Connection = connection;
			//	ExecuteSql(command, doUpdate, script);
			//}
		}

		protected virtual void UpdateColumn(MappedTable table, MappedColumn oldColumn, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			if (!column.IsPrimaryKey)
			{
				if (oldColumn.AllowNulls && !column.AllowNulls)
				{
					// We're changing the column from NULL to NOT NULL so we need to first replace all null
					// values with the default value
					using (var command = CreateCommand(connection))
					{
						command.CommandText = $"UPDATE [{table.Name}] SET {column.Name} = {ColumnDefaultText(column)} WHERE {column.Name} IS NULL";
						command.Connection = connection;
						ExecuteSql(command, doUpdate, script);
					}
				}
			}

			// HACK: Need to convert the nullable type that was retrieved from the database to the non-nullable
			// type of primary keys, as we can't create primary keys with NOT NULL in SQLite
			if (column.IsPrimaryKey)
			{
				// HACK: This isn't working for enums which have their ID as ints...
				//oldColumn.ColumnType = Nullable.GetUnderlyingType(oldColumn.ColumnType);
				oldColumn.ColumnType = column.ColumnType;
				oldColumn.AllowNulls = false;
			}

			var columnTypeChanged = (oldColumn.ColumnType != column.ColumnType);
			// TODO: Figure out how to compare database default values to CLR default values.  For the time being we can
			// only go from no default to default
			var defaultValueChanged = (oldColumn.DefaultValue == null && oldColumn.DefaultValue != column.DefaultValue);
			var allowNullsChanged = (oldColumn.AllowNulls != column.AllowNulls);
			var maxLengthChanged = (oldColumn.MaxLength != column.MaxLength && !(oldColumn.MaxLength == -1 && column.MaxLength >= 4000));

			if (columnTypeChanged || defaultValueChanged || allowNullsChanged || maxLengthChanged)
			{
				using (var command = CreateCommand(connection))
				{
					// Drop all constraints before updating the column.  They will be re-created later
					var constraints = GetColumnConstraintsToDrop(table, column, connection);
					if (constraints.Count > 0)
					{
						command.CommandText = string.Join(Environment.NewLine, constraints.ToArray());
						command.Connection = connection;
						ExecuteSql(command, doUpdate, script);
					}

					// Update the column
					command.CommandText = $"ALTER TABLE [{table.Name}] ALTER COLUMN {ColumnText(table, column, false, false)}";
					ExecuteSql(command, doUpdate, script);

					// If the column is the primary key, add that constraint back now
					if (column.IsPrimaryKey)
					{
						command.CommandText = $"ALTER TABLE [{table.Name}] ADD CONSTRAINT [{table.PrimaryKeyConstraintName}] PRIMARY KEY ([{table.PrimaryKeyColumnName}] ASC)";
						ExecuteSql(command, doUpdate, script);
					}

					// If the column has a default value, add that constraint back now
					if (column.DefaultValue != null)
					{
						command.CommandText = $"ALTER TABLE [{table.Name}] ADD CONSTRAINT [{column.DefaultValueConstraintName}] DEFAULT {ColumnDefaultText(column)} FOR [{column.Name}]";
						ExecuteSql(command, doUpdate, script);
					}
				}
			}
		}

		protected virtual List<string> GetColumnConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			constraints.AddRange(GetForeignKeyConstraintsToDrop(table, column, connection));
			constraints.AddRange(GetPrimaryKeyConstraintsToDrop(table, column, connection));
			constraints.AddRange(GetDefaultValueConstraintsToDrop(table, column, connection));

			return constraints;
		}

		protected virtual List<string> GetForeignKeyConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			// TODO:
			//// Get foreign key constraints on this column or that reference this column
			//using (var command = CreateCommand(connection))
			//{
			//	command.CommandText = "" +
			//		"SELECT FK.TABLE_NAME, C.CONSTRAINT_NAME " +
			//		"FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C " +
			//		"INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME " +
			//		"INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME " +
			//		"INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME " +
			//		"INNER JOIN ( " +
			//		"            SELECT i1.TABLE_NAME, i2.COLUMN_NAME " +
			//		"            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1 " +
			//		"				INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME " +
			//		"            WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY' " +
			//		"           ) PT " +
			//		"    ON PT.TABLE_NAME = PK.TABLE_NAME " +
			//		$"WHERE (PK.TABLE_NAME = '{table.Name}' AND PT.COLUMN_NAME = '{column.Name}') " +
			//		$"	OR (FK.TABLE_NAME = '{table.Name}' AND CU.COLUMN_NAME = '{column.Name}')";
			//	command.Connection = connection;
			//	using (var reader = command.ExecuteReader())
			//	{
			//		while (reader.Read())
			//		{
			//			string tableName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
			//			string constraintName = reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"));
			//			constraints.Add($"ALTER TABLE [{tableName}] DROP CONSTRAINT [{constraintName}];");
			//		}
			//	}
			//}

			return constraints;
		}

		protected virtual List<string> GetPrimaryKeyConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			// TODO:
			//// Get primary key constraints for this column
			//using (var command = CreateCommand(connection))
			//{
			//	command.CommandText = "" +
			//		"SELECT C.CONSTRAINT_NAME " +
			//		"FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS C " +
			//		"	INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME " +
			//		"WHERE C.CONSTRAINT_TYPE = 'PRIMARY KEY' " +
			//		$"	AND C.TABLE_NAME = '{table.Name}' AND CU.COLUMN_NAME = '{column.Name}' ";
			//	command.Connection = connection;
			//	using (var reader = command.ExecuteReader())
			//	{
			//		while (reader.Read())
			//		{
			//			string constraintName = reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"));
			//			constraints.Add($"ALTER TABLE [{table.Name}] DROP CONSTRAINT [{constraintName}];");
			//		}
			//	}
			//}

			return constraints;
		}

		protected virtual List<string> GetDefaultValueConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			// TODO:
			//// Get default value constraints for this column
			//using (var command = CreateCommand(connection))
			//{
			//	command.CommandText = "" +
			//		"SELECT c.name " +
			//		"FROM sys.all_columns a " +
			//		"	INNER JOIN sys.tables b on a.object_id = b.object_id " +
			//		"	INNER JOIN sys.default_constraints c on a.default_object_id = c.object_id " +
			//		$"WHERE b.name = '{table.Name}' AND a.name = '{column.Name}' ";
			//	command.Connection = connection;
			//	using (var reader = command.ExecuteReader())
			//	{
			//		while (reader.Read())
			//		{
			//			string constraintName = reader.GetString(reader.GetOrdinal("name"));
			//			constraints.Add($"ALTER TABLE [{table.Name}] DROP CONSTRAINT [{constraintName}];");
			//		}
			//	}
			//}

			return constraints;
		}

		protected virtual void UpdateTableData(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			// TODO: This is a bit messy and could be tidied up a bit
			var existingTableData = new List<int>();

			// Load the existing table data
			var selectExistingTableData = Select.From(table.Name).Columns(table.PrimaryKeyColumnName);
			using (var existingTableDataCommand = _dataAccessProvider.BuildCommand(selectExistingTableData, _configuration))
			{
				existingTableDataCommand.Connection = connection;
				using (var reader = existingTableDataCommand.ExecuteReader())
				{
					while (reader.Read())
					{
						existingTableData.Add(reader.GetInt32(0));
					}
				}
			}

			// If any of the table data doesn't exist then insert it
			// NOTE: Table data is not updated if it does exist
			foreach (var data in table.Values)
			{
				if (!existingTableData.Contains((int)data["ID"]))
				{
					using (var identityInsertCommand = CreateCommand(connection))
					{
						var insertData = Insert.Into(table.Name);
						foreach (var key in data.Keys)
						{
							insertData = insertData.Value(key, data[key]);
						}
						using (var command = _dataAccessProvider.BuildCommand(insertData, _configuration))
						{
							command.Connection = connection;
							ExecuteSql(command, doUpdate, script);
						}
					}
				}
			}
		}

		protected virtual void CreateView(MappedView view, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			var b = new StringBuilder();
			b.AppendLine($"CREATE VIEW [{view.Name}] AS");
			using (var viewCommand = _configuration.DataAccessProvider.BuildCommand(view.SelectStatement, _configuration))
			{
				b.Append(viewCommand.CommandText);
			}
			b.AppendLine();
			using (var command = CreateCommand(connection))
			{
				command.CommandText = b.ToString();
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		protected virtual void UpdateView(MappedView view, MappedView oldView, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			var b = new StringBuilder();
			b.AppendLine($"CREATE VIEW [{view.Name}] AS");
			using (var viewCommand = _configuration.DataAccessProvider.BuildCommand(view.SelectStatement, _configuration))
			{
				b.Append(viewCommand.CommandText);
			}
			b.AppendLine();
			if (oldView.SelectStatementText != b.ToString())
			{
				using (var command = CreateCommand(connection))
				{
					command.CommandText = b.ToString().Replace("CREATE VIEW", "ALTER VIEW");
					command.Connection = connection;
					ExecuteSql(command, doUpdate, script);
				}
			}
		}

		protected virtual void ExecuteSql(DbCommand command, bool doUpdate, StringBuilder script)
		{
			if (script != null)
			{
				script.Append(command.CommandText);
				script.AppendLine(";");
				if (command.Parameters.Count > 0)
				{
					script.Append(" { ");
					for (var i = 0; i < command.Parameters.Count; i++)
					{
						if (i > 0)
						{
							script.Append(", ");
						}
						script.Append(command.Parameters[i].ParameterName);
						script.Append(" = '");
						script.Append(command.Parameters[i].Value);
						script.Append("'");
					}
					script.Append(" }");
				}
				script.AppendLine();
			}

#if DEBUG
			System.Diagnostics.Trace.WriteLine(command.CommandText.Replace(Environment.NewLine, " "), "Update Database");
#endif

			if (doUpdate)
			{
				command.ExecuteNonQuery();
			}
		}

		protected virtual DbCommand CreateCommand(DbConnection connection)
		{
			var command = new SqliteCommand();
			command.Connection = (SqliteConnection)connection;
			return command;
		}

		public string GetUnmappedColumns(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views)
		{
			var columns = new StringBuilder();

			using (var connection = _dataAccessProvider.OpenConnection(_configuration))
			{
				// Load the existing columns
				var existingTables = LoadExistingTables(connection);
				var existingColumns = LoadExistingColumns(existingTables, connection);

				// Check whether each existing column is mapped
				foreach (var columnKey in existingColumns.Keys)
				{
					var tableName = columnKey.Split('.')[0];
					var columnName = columnKey.Split('.')[1];

					var table = tables.FirstOrDefault(m => m.Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase));
					var isColumnMapped = (table != null && table.Columns.Any(c => c.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase)));

					if (!isColumnMapped)
					{
						columns.AppendLine(columnKey + " " + ColumnTypeText(existingColumns[columnKey]));
					}
				}
			}

			return columns.ToString();
		}
	}
}

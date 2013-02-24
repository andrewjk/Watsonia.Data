using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.SqlServer
{
	internal sealed class SqlServerDatabaseUpdater
	{
		private IDataAccessProvider _dataAccessProvider;
		private DatabaseConfiguration _configuration;

		public SqlServerDatabaseUpdater(IDataAccessProvider dataAccessProvider, DatabaseConfiguration configuration)
		{
			_dataAccessProvider = dataAccessProvider;
			_configuration = configuration;
		}

		public void UpdateDatabase(IEnumerable<MappedTable> tables)
		{
			UpdateDatabase(tables, true);
		}

		public string GetUpdateScript(IEnumerable<MappedTable> tables)
		{
			StringBuilder script = new StringBuilder();
			UpdateDatabase(tables, false, script);
			return script.ToString();
		}

		private void UpdateDatabase(IEnumerable<MappedTable> tables, bool doUpdate, StringBuilder script = null)
		{
			using (var connection = _dataAccessProvider.OpenConnection(_configuration))
			{
				// Load the existing tables and columns
				var existingTables = LoadExistingTables(connection);
				var existingColumns = LoadExistingColumns(connection);

				// First pass - create or update tables and columns
				foreach (MappedTable table in tables)
				{
					if (existingTables.ContainsKey(table.Name.ToUpperInvariant()))
					{
						// The table exists so we need to check whether it should be updated
						foreach (MappedColumn column in table.Columns)
						{
							string key = table.Name.ToUpperInvariant() + "." + column.Name.ToUpperInvariant();
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
				foreach (MappedTable table in tables.Where(t => t.Values.Count > 0))
				{
					if (existingTables.ContainsKey(table.Name.ToUpperInvariant()))
					{
						UpdateTableData(table, connection, doUpdate, script);
					}
				}

				// Third pass - create relationship constraints
				var existingForeignKeys = LoadExistingForeignKeys(connection);
				foreach (MappedTable table in tables)
				{
					if (existingTables.ContainsKey(table.Name.ToUpperInvariant()))
					{
						foreach (MappedColumn column in table.Columns.Where(c => c.Relationship != null))
						{
							if (!existingForeignKeys.Contains(column.Relationship.ConstraintName))
							{
								CreateForeignKey(table, column, connection, doUpdate, script);
								existingForeignKeys.Add(column.Relationship.ConstraintName);
							}
						}
					}
				}
			}
		}

		private Dictionary<string, MappedTable> LoadExistingTables(DbConnection connection)
		{
			var existingTables = new Dictionary<string, MappedTable>();
			using (var existingTablesCommand = CreateCommand(connection))
			{
				existingTablesCommand.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES";
				existingTablesCommand.Connection = connection;
				using (var reader = existingTablesCommand.ExecuteReader())
				{
					while (reader.Read())
					{
						string tableName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
						MappedTable table = new MappedTable(tableName);
						existingTables.Add(tableName.ToUpperInvariant(), table);
					}
				}
			}
			return existingTables;
		}

		private Dictionary<string, MappedColumn> LoadExistingColumns(DbConnection connection)
		{
			var existingColumns = new Dictionary<string, MappedColumn>();
			using (var existingColumnsCommand = CreateCommand(connection))
			{
				existingColumnsCommand.CommandText = "SELECT TABLE_NAME, COLUMN_NAME, IS_NULLABLE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS";
				existingColumnsCommand.Connection = connection;
				using (var reader = existingColumnsCommand.ExecuteReader())
				{
					while (reader.Read())
					{
						string tableName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
						string columnName = reader.GetString(reader.GetOrdinal("COLUMN_NAME"));
						bool allowNulls = (reader.GetString(reader.GetOrdinal("IS_NULLABLE")).ToUpperInvariant() == "YES");
						object defaultValue = reader.GetValue(reader.GetOrdinal("COLUMN_DEFAULT"));
						if (defaultValue == DBNull.Value)
						{
							defaultValue = null;
						}
						int maxLength = 0;
						Type columnType;
						switch (reader.GetString(reader.GetOrdinal("DATA_TYPE")).ToUpperInvariant())
						{
							case "BIT":
							{
								columnType = allowNulls ? typeof(bool?) : typeof(bool);
								break;
							}
							case "DATETIME":
							{
								columnType = allowNulls ? typeof(DateTime?) : typeof(DateTime);
								break;
							}
							case "DECIMAL":
							{
								columnType = allowNulls ? typeof(decimal?) : typeof(decimal);
								break;
							}
							case "FLOAT":
							{
								columnType = allowNulls ? typeof(double?) : typeof(double);
								break;
							}
							case "INT":
							{
								columnType = allowNulls ? typeof(int?) : typeof(int);
								break;
							}
							case "BIGINT":
							{
								columnType = allowNulls ? typeof(long?) : typeof(long);
								break;
							}
							case "VARCHAR":
							case "NVARCHAR":
							{
								columnType = typeof(string);
								maxLength = reader.GetInt32(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"));
								break;
							}
							case "UNIQUEIDENTIFIER":
							{
								columnType = typeof(Guid);
								break;
							}
							default:
							{
								throw new InvalidOperationException("Invalid data type: " + reader.GetString(reader.GetOrdinal("DATA_TYPE")));
							}
						}
						MappedColumn column = new MappedColumn(columnName, columnType, "");
						column.MaxLength = maxLength;
						column.AllowNulls = allowNulls;
						column.DefaultValue = defaultValue;
						string key = tableName.ToUpperInvariant() + "." + columnName.ToUpperInvariant();
						existingColumns.Add(key, column);
					}
				}
			}
			return existingColumns;
		}

		private List<string> LoadExistingForeignKeys(DbConnection connection)
		{
			var existingForeignKeys = new List<string>();
			using (var existingForeignKeysCommand = CreateCommand(connection))
			{
				existingForeignKeysCommand.CommandText = "SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS";
				existingForeignKeysCommand.Connection = connection;
				using (var reader = existingForeignKeysCommand.ExecuteReader())
				{
					while (reader.Read())
					{
						existingForeignKeys.Add(reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME")));
					}
				}
			}
			return existingForeignKeys;
		}

		private void CreateTable(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				StringBuilder b = new StringBuilder();
				b.AppendFormat("CREATE TABLE [{0}] (", table.Name);
				b.AppendLine();
				b.Append(string.Join(", ", Array.ConvertAll(table.Columns.ToArray(), c => ColumnText(table, c, true, true))));
				b.AppendLine(",");
				b.AppendFormat("CONSTRAINT [{0}] PRIMARY KEY CLUSTERED ([{1}] ASC)", table.PrimaryKeyConstraintName, table.PrimaryKeyColumnName);
				b.AppendLine();
				b.Append(")");
				command.CommandText = b.ToString();
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		private void CreateColumn(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format("ALTER TABLE [{0}] ADD {1}", table.Name, ColumnText(table, column, true, true));
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		private string ColumnText(MappedTable table, MappedColumn column, bool includeDefault, bool includeIdentity)
		{
			StringBuilder b = new StringBuilder();
			b.AppendFormat("[{0}] ", column.Name);
			b.Append(ColumnTypeText(column));
			if (column.Name.Equals(table.PrimaryKeyColumnName, StringComparison.InvariantCultureIgnoreCase))
			{
				if (includeIdentity)
				{
					b.Append(" IDENTITY(1,1)");
				}
			}
			else
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

		private string ColumnTypeText(MappedColumn column)
		{
			if (column.ColumnType == typeof(bool) || column.ColumnType == typeof(bool?))
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
			else if (column.ColumnType == typeof(string))
			{
				if (column.MaxLength >= 4000)
				{
					return "NVARCHAR(MAX)";
				}
				else
				{
					return string.Format("NVARCHAR({0})", column.MaxLength);
				}
			}
			else if (column.ColumnType == typeof(Guid))
			{
				return "UNIQUEIDENTIFIER";
			}
			else
			{
				throw new InvalidOperationException("Invalid column type: " + column.ColumnType);
			}
		}

		private string ColumnDefaultText(MappedColumn column)
		{
			// TODO: This isn't really the best spot for this
			if (column.ColumnType == typeof(bool) || column.ColumnType == typeof(bool?))
			{
				return column.DefaultValue != null && (bool)column.DefaultValue ? "1" : "0";
			}
			else if (column.ColumnType == typeof(DateTime) || column.ColumnType == typeof(DateTime?))
			{
				return column.DefaultValue != null ? string.Format("'{0:d-MMM-yyyy}'", (DateTime)column.DefaultValue) : "'1-JAN-1753'";
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
			else if (column.ColumnType == typeof(string))
			{
				return "''";
			}
			else if (column.ColumnType == typeof(Guid))
			{
				return column.DefaultValue != null ? string.Format("'{0:D}'", column.DefaultValue) : "'00000000-0000-0000-0000-000000000000'";
			}
			else
			{
				throw new InvalidOperationException("Invalid column type: " + column.ColumnType);
			}
		}

		private void CreateForeignKey(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format(
					"ALTER TABLE [{0}] WITH CHECK ADD CONSTRAINT [{1}] FOREIGN KEY ([{2}]) REFERENCES [{3}] ({4})",
					table.Name, column.Relationship.ConstraintName, column.Name, column.Relationship.ForeignTableName, column.Relationship.ForeignTableColumnName);
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		private void UpdateColumn(MappedTable table, MappedColumn oldColumn, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			if (oldColumn.AllowNulls && !column.AllowNulls)
			{
				// We're changing the column from NULL to NOT NULL so we need to first replace all null
				// values with the default value
				using (var command = CreateCommand(connection))
				{
					command.CommandText = string.Format(
						"UPDATE [{0}] SET {1} = {2} WHERE {3} IS NULL",
						table.Name, column.Name, ColumnDefaultText(column), column.Name);
					command.Connection = connection;
					ExecuteSql(command, doUpdate, script);
				}
			}

			bool columnTypeChanged = (oldColumn.ColumnType != column.ColumnType);
			// TODO: Figure out how to compare database default values to CLR default values.  For the time being we can
			// only go from no default to default
			bool defaultValueChanged = (oldColumn.DefaultValue == null && oldColumn.DefaultValue != column.DefaultValue);
			bool allowNullsChanged = (oldColumn.AllowNulls != column.AllowNulls);
			bool maxLengthChanged = (oldColumn.MaxLength != column.MaxLength && !(oldColumn.MaxLength == -1 && column.MaxLength >= 4000));

			if (columnTypeChanged || defaultValueChanged || allowNullsChanged || maxLengthChanged)
			{
				using (var command = CreateCommand(connection))
				{
					// Drop all constraints before updating the column.  They will be re-created later
					List<string> constraints = GetColumnConstraintsToDrop(table, column, connection);
					if (constraints.Count > 0)
					{
						command.CommandText = string.Join(Environment.NewLine, constraints.ToArray());
						command.Connection = connection;
						ExecuteSql(command, doUpdate, script);
					}
					
					// Update the column
					command.CommandText = string.Format("ALTER TABLE [{0}] ALTER COLUMN {1}", table.Name, ColumnText(table, column, false, false));
					ExecuteSql(command, doUpdate, script);

					// If the column is the primary key, add that constraint back now
					if (column.IsPrimaryKey)
					{
						command.CommandText = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] PRIMARY KEY CLUSTERED ([{2}] ASC)", table.Name, table.PrimaryKeyConstraintName, table.PrimaryKeyColumnName);
						ExecuteSql(command, doUpdate, script);
					}

					// If the column has a default value, add that constraint back now
					if (column.DefaultValue != null)
					{
						command.CommandText = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] DEFAULT {2} FOR [{3}]", table.Name, column.DefaultValueConstraintName, ColumnDefaultText(column), column.Name);
						ExecuteSql(command, doUpdate, script);
					}
				}
			}
		}

		private List<string> GetColumnConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			List<string> constraints = new List<string>();

			// Get foreign key constraints on this column or that reference this column
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format("" +
					"SELECT FK.TABLE_NAME, C.CONSTRAINT_NAME " +
					"FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C " +
					"INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME " +
					"INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME " +
					"INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME " +
					"INNER JOIN ( " +
					"            SELECT i1.TABLE_NAME, i2.COLUMN_NAME " +
					"            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1 " +
					"				INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME " +
					"            WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY' " +
					"           ) PT " +
					"    ON PT.TABLE_NAME = PK.TABLE_NAME " +
					"WHERE (PK.TABLE_NAME = '{0}' AND PT.COLUMN_NAME = '{1}') " +
					"	OR (FK.TABLE_NAME = '{0}' AND CU.COLUMN_NAME = '{1}')", table.Name, column.Name);
				command.Connection = connection;
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						constraints.Add(string.Format(
							"ALTER TABLE [{0}] DROP CONSTRAINT [{1}];",
							reader.GetString(reader.GetOrdinal("TABLE_NAME")),
							reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"))));
					}
				}
			}

			// Get primary key constraints for this column
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format("" +
					"SELECT C.CONSTRAINT_NAME " +
					"FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS C " +
					"	INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME " +
					"WHERE C.CONSTRAINT_TYPE = 'PRIMARY KEY' " +
					"	AND C.TABLE_NAME = '{0}' AND CU.COLUMN_NAME = '{1}' ", table.Name, column.Name);
				command.Connection = connection;
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						constraints.Add(string.Format(
							"ALTER TABLE [{0}] DROP CONSTRAINT [{1}];",
							table.Name,
							reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"))));
					}
				}
			}

			// Get default value constraints for this column
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format("" +
					"SELECT c.name " +
					"FROM sys.all_columns a " +
					"	INNER JOIN sys.tables b on a.object_id = b.object_id " +
					"	INNER JOIN sys.default_constraints c on a.default_object_id = c.object_id " +
					"WHERE b.name = '{0}' AND a.name = '{1}' ", table.Name, column.Name);
				command.Connection = connection;
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						constraints.Add(string.Format(
							"ALTER TABLE [{0}] DROP CONSTRAINT [{1}];",
							table.Name,
							reader.GetString(reader.GetOrdinal("name"))));
					}
				}
			}

			return constraints;
		}

		private void UpdateTableData(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script)
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
						identityInsertCommand.Connection = connection;
						identityInsertCommand.CommandText = "SET IDENTITY_INSERT " + table.Name + " ON";
						ExecuteSql(identityInsertCommand, doUpdate, script);

						Insert insertData = Insert.Into(table.Name);
						foreach (string key in data.Keys)
						{
							insertData = insertData.Value(key, data[key]);
						}
						using (DbCommand command = _dataAccessProvider.BuildCommand(insertData, _configuration))
						{
							command.Connection = connection;
							ExecuteSql(command, doUpdate, script);
						}

						identityInsertCommand.CommandText = "SET IDENTITY_INSERT " + table.Name + " OFF";
						ExecuteSql(identityInsertCommand, doUpdate, script);
					}
				}
			}
		}

		private void ExecuteSql(DbCommand command, bool doUpdate, StringBuilder script)
		{
			if (script != null)
			{
				script.Append(command.CommandText);
				script.AppendLine(";");
				if (command.Parameters.Count > 0)
				{
					script.Append(" { ");
					for (int i = 0; i < command.Parameters.Count; i++)
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

		private DbCommand CreateCommand(DbConnection connection)
		{
			var command = new SqlCommand();
			command.Connection = (SqlConnection)connection;
			return command;
		}
	}
}

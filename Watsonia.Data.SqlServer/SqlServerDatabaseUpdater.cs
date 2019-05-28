﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Mapping;
using Watsonia.QueryBuilder;

namespace Watsonia.Data.SqlServer
{
	public class SqlServerDatabaseUpdater
	{
		protected IDataAccessProvider _dataAccessProvider;
		protected DatabaseConfiguration _configuration;

		private bool _compactEdition = false;

		protected virtual bool CompactEdition
		{
			get
			{
				return _compactEdition;
			}
			set
			{
				_compactEdition = value;
			}
		}

		public SqlServerDatabaseUpdater(IDataAccessProvider dataAccessProvider, DatabaseConfiguration configuration)
		{
			_dataAccessProvider = dataAccessProvider;
			_configuration = configuration;
		}

		public async Task UpdateDatabaseAsync(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions)
		{
			await UpdateDatabaseAsync(tables, views, procedures, functions, true);
		}

		public async Task<string> GetUpdateScriptAsync(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions)
		{
			var script = new StringBuilder();
			await UpdateDatabaseAsync(tables, views, procedures, functions, false, script);
			return script.ToString();
		}

		protected virtual async Task UpdateDatabaseAsync(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, IEnumerable<MappedProcedure> procedures, IEnumerable<MappedFunction> functions, bool doUpdate, StringBuilder script = null)
		{
			using (var connection = await _dataAccessProvider.OpenConnectionAsync(_configuration))
			{
				// Load the existing tables and columns
				var existingTables = await LoadExistingTablesAsync(connection);
				var existingColumns = await LoadExistingColumnsAsync(connection);
				var existingViews = await LoadExistingViewsAsync(connection);
				var existingProcedures = await LoadExistingProceduresAsync(connection);
				var existingFunctions = await LoadExistingFunctionsAsync(connection);

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
								await UpdateColumnAsync(table, existingColumns[key], column, connection, doUpdate, script);
							}
							else
							{
								// The column doesn't exist so it needs to be created
								await CreateColumnAsync(table, column, connection, doUpdate, script);
							}
						}
					}
					else
					{
						// The table doesn't exist so it needs to be created
						await CreateTableAsync(table, connection, doUpdate, script);
					}
				}

				// Second pass - fill table data
				foreach (var table in tables.Where(t => t.Values.Count > 0))
				{
					var tableExists = existingTables.ContainsKey(table.Name.ToUpperInvariant());
					await UpdateTableDataAsync(table, connection, doUpdate, script, tableExists);
				}

				// Third pass - create relationship constraints
				var existingForeignKeys = await LoadExistingForeignKeysAsync(connection);
				foreach (var table in tables)
				{
					foreach (var column in table.Columns.Where(c => c.Relationship != null))
					{
						if (!existingForeignKeys.Contains(column.Relationship.ConstraintName))
						{
							await CreateForeignKeyAsync(table, column, connection, doUpdate, script);
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
						await UpdateViewAsync(view, existingViews[key], connection, doUpdate, script);
					}
					else
					{
						// The view doesn't exist so it needs to be created
						await CreateViewAsync(view, connection, doUpdate, script);
					}
				}

				// Fifth pass - create procedures
				foreach (var procedure in procedures)
				{
					var key = procedure.Name.ToUpperInvariant();
					if (existingProcedures.ContainsKey(key))
					{
						// The procedure exists so we need to check whether it should be updated
						await UpdateProcedureAsync(procedure, existingProcedures[key], connection, doUpdate, script);
					}
					else
					{
						// The procedure doesn't exist so it needs to be created
						await CreateProcedureAsync(procedure, connection, doUpdate, script);
					}
				}

				// Sixth pass - create functions
				foreach (var function in functions)
				{
					var key = function.Name.ToUpperInvariant();
					if (existingFunctions.ContainsKey(key))
					{
						// The function exists so we need to check whether it should be updated
						await UpdateFunctionAsync(function, existingFunctions[key], connection, doUpdate, script);
					}
					else
					{
						// The function doesn't exist so it needs to be created
						await CreateFunctionAsync(function, connection, doUpdate, script);
					}
				}
			}
		}

		protected virtual async Task<Dictionary<string, MappedTable>> LoadExistingTablesAsync(DbConnection connection)
		{
			var existingTables = new Dictionary<string, MappedTable>();
			using (var existingTablesCommand = CreateCommand(connection))
			{
				existingTablesCommand.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES";
				existingTablesCommand.Connection = connection;
				using (var reader = await existingTablesCommand.ExecuteReaderAsync())
				{
					while (reader.Read())
					{
						var tableName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
						var table = new MappedTable(tableName);
						existingTables.Add(tableName.ToUpperInvariant(), table);
					}
				}
			}
			return existingTables;
		}

		protected virtual async Task<Dictionary<string, MappedColumn>> LoadExistingColumnsAsync(DbConnection connection)
		{
			var existingColumns = new Dictionary<string, MappedColumn>();
			using (var existingColumnsCommand = CreateCommand(connection))
			{
				existingColumnsCommand.CommandText = "SELECT TABLE_NAME, COLUMN_NAME, IS_NULLABLE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS";
				existingColumnsCommand.Connection = connection;
				using (var reader = await existingColumnsCommand.ExecuteReaderAsync())
				{
					while (reader.Read())
					{
						var tableName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
						var columnName = reader.GetString(reader.GetOrdinal("COLUMN_NAME"));
						var allowNulls = (reader.GetString(reader.GetOrdinal("IS_NULLABLE")).ToUpperInvariant() == "YES");
						var defaultValue = reader.GetValue(reader.GetOrdinal("COLUMN_DEFAULT"));
						if (defaultValue == DBNull.Value)
						{
							defaultValue = null;
						}
						var dataTypeName = reader.GetString(reader.GetOrdinal("DATA_TYPE"));

						var columnType = FrameworkTypeFromDatabase(dataTypeName, allowNulls);
						var maxLength = 0;
						if (columnType == typeof(string))
						{
							maxLength = reader.GetInt32(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"));
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
			return existingColumns;
		}

		protected virtual async Task<Dictionary<string, MappedView>> LoadExistingViewsAsync(DbConnection connection)
		{
			var existingViews = new Dictionary<string, MappedView>();
			if (!this.CompactEdition)
			{
				using (var existingViewsCommand = CreateCommand(connection))
				{
					existingViewsCommand.CommandText = "SELECT TABLE_NAME, VIEW_DEFINITION FROM INFORMATION_SCHEMA.VIEWS";
					existingViewsCommand.Connection = connection;
					using (var reader = await existingViewsCommand.ExecuteReaderAsync())
					{
						while (reader.Read())
						{
							var viewName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
							var selectStatementText = reader.GetString(reader.GetOrdinal("VIEW_DEFINITION"));
							var view = new MappedView(viewName);
							view.SelectStatementText = selectStatementText;
							var key = viewName.ToUpperInvariant();
							existingViews.Add(key, view);
						}
					}
				}
			}
			return existingViews;
		}

		protected virtual async Task<Dictionary<string, MappedProcedure>> LoadExistingProceduresAsync(DbConnection connection)
		{
			var existingProcedures = new Dictionary<string, MappedProcedure>();
			if (!this.CompactEdition)
			{
				using (var existingProceduresCommand = CreateCommand(connection))
				{
					existingProceduresCommand.CommandText = "SELECT ROUTINE_NAME, ROUTINE_DEFINITION FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'";
					existingProceduresCommand.Connection = connection;
					using (var reader = await existingProceduresCommand.ExecuteReaderAsync())
					{
						while (reader.Read())
						{
							var procedureName = reader.GetString(reader.GetOrdinal("ROUTINE_NAME"));
							var statementText = reader.GetString(reader.GetOrdinal("ROUTINE_DEFINITION"));
							var procedure = new MappedProcedure(procedureName);
							procedure.StatementText = statementText;
							var key = procedureName.ToUpperInvariant();
							existingProcedures.Add(key, procedure);
						}
					}
				}
			}
			return existingProcedures;
		}

		protected virtual async Task<Dictionary<string, MappedFunction>> LoadExistingFunctionsAsync(DbConnection connection)
		{
			var existingFunctions = new Dictionary<string, MappedFunction>();
			if (!this.CompactEdition)
			{
				using (var existingFunctionsCommand = CreateCommand(connection))
				{
					existingFunctionsCommand.CommandText = "SELECT ROUTINE_NAME, ROUTINE_DEFINITION FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'FUNCTION'";
					existingFunctionsCommand.Connection = connection;
					using (var reader = await existingFunctionsCommand.ExecuteReaderAsync())
					{
						while (reader.Read())
						{
							var functionName = reader.GetString(reader.GetOrdinal("ROUTINE_NAME"));
							var statementText = reader.GetString(reader.GetOrdinal("ROUTINE_DEFINITION"));
							var function = new MappedFunction(functionName);
							function.StatementText = statementText;
							var key = functionName.ToUpperInvariant();
							existingFunctions.Add(key, function);
						}
					}
				}
			}
			return existingFunctions;
		}

		protected virtual Type FrameworkTypeFromDatabase(string databaseTypeName, bool allowNulls)
		{
			switch (databaseTypeName.ToUpperInvariant())
			{
				case "BIT":
				{
					return allowNulls ? typeof(bool?) : typeof(bool);
				}
				case "DATETIME":
				case "DATETIME2":
				{
					return allowNulls ? typeof(DateTime?) : typeof(DateTime);
				}
				case "DATETIMEOFFSET":
				{
					return allowNulls ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
				}
				case "DECIMAL":
				case "NUMERIC":
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
				{
					return allowNulls ? typeof(long?) : typeof(long);
				}
				case "SMALLINT":
				{
					return allowNulls ? typeof(short?) : typeof(short);
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
				case "VARBINARY":
				case "IMAGE":
				{
					return typeof(byte[]);
				}
				case "UNIQUEIDENTIFIER":
				{
					return allowNulls ? typeof(Guid?) : typeof(Guid);
				}
				default:
				{
					throw new InvalidOperationException("Invalid data type: " + databaseTypeName);
				}
			}
		}

		protected virtual async Task<List<string>> LoadExistingForeignKeysAsync(DbConnection connection)
		{
			var existingForeignKeys = new List<string>();
			using (var existingForeignKeysCommand = CreateCommand(connection))
			{
				existingForeignKeysCommand.CommandText = "SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS";
				existingForeignKeysCommand.Connection = connection;
				using (var reader = await existingForeignKeysCommand.ExecuteReaderAsync())
				{
					while (reader.Read())
					{
						existingForeignKeys.Add(reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME")));
					}
				}
			}
			return existingForeignKeys;
		}

		protected virtual async Task CreateTableAsync(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			var b = new StringBuilder();
			b.AppendLine($"CREATE TABLE [{table.Name}] (");
			b.Append(string.Join(", ", Array.ConvertAll(table.Columns.ToArray(), c => ColumnText(table, c, true, true))));
			b.AppendLine(",");
			var maybeClustered = (this.CompactEdition ? "" : "CLUSTERED");
			var direction = (this.CompactEdition ? "" : " ASC");
			b.AppendLine($"CONSTRAINT [{table.PrimaryKeyConstraintName}] PRIMARY KEY {maybeClustered} ([{table.PrimaryKeyColumnName}]{direction})");
			b.Append(")");
			using (var command = CreateCommand(connection))
			{
				command.CommandText = b.ToString();
				command.Connection = connection;
				await ExecuteSqlAsync(command, doUpdate, script);
			}
		}

		protected virtual async Task CreateColumnAsync(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				command.CommandText = $"ALTER TABLE [{table.Name}] ADD {ColumnText(table, column, true, true)}";
				command.Connection = connection;
				await ExecuteSqlAsync(command, doUpdate, script);
			}
		}

		protected virtual string ColumnText(MappedTable table, MappedColumn column, bool includeDefault, bool includeIdentity)
		{
			var b = new StringBuilder();
			b.Append($"[{column.Name}] {ColumnTypeText(column)}");
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

		protected virtual string ColumnTypeText(MappedColumn column)
		{
			return ColumnTypeText(column.ColumnType, column.MaxLength);
		}

		protected virtual string ColumnTypeText(Type columnType, int maxLength)
		{
			if (columnType == typeof(bool) || columnType == typeof(bool?))
			{
				return "BIT";
			}
			else if (columnType == typeof(DateTime) || columnType == typeof(DateTime?))
			{
				return "DATETIME2";
			}
			else if (columnType == typeof(DateTimeOffset) || columnType == typeof(DateTimeOffset?))
			{
				return "DATETIMEOFFSET";
			}
			else if (columnType == typeof(decimal) || columnType == typeof(decimal?))
			{
				return "DECIMAL(19,5)";
			}
			else if (columnType == typeof(double) || columnType == typeof(double?))
			{
				return "FLOAT";
			}
			else if (columnType == typeof(int) || columnType == typeof(int?))
			{
				return "INT";
			}
			else if (columnType == typeof(long) || columnType == typeof(long?))
			{
				return "BIGINT";
			}
			else if (columnType == typeof(byte) || columnType == typeof(byte?))
			{
				return "TINYINT";
			}
			else if (columnType == typeof(string))
			{
				if (maxLength >= 4000)
				{
					if (this.CompactEdition)
					{
						return "NTEXT";
					}
					else
					{
						return "NVARCHAR(MAX)";
					}
				}
				else
				{
					return $"NVARCHAR({maxLength})";
				}
			}
			else if (columnType == typeof(byte[]))
			{
				if (maxLength == 0)
				{
					if (this.CompactEdition)
					{
						// NOTE: VARBINARY has a max of 8000 in CE which very well might not be large
						// enough, so instead we use IMAGE
						return "IMAGE";
					}
					else
					{
						return "VARBINARY(MAX)";
					}
				}
				else
				{
					return $"VARBINARY({maxLength})";
				}
			}
			else if (columnType == typeof(Guid) || columnType == typeof(Guid?))
			{
				return "UNIQUEIDENTIFIER";
			}
			else
			{
				throw new InvalidOperationException("Invalid column type: " + columnType);
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

		protected virtual async Task CreateForeignKeyAsync(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				var maybeCheck = (this.CompactEdition ? "" : "WITH CHECK");
				var constraintName = column.Relationship.ConstraintName;
				var foreignTableName = column.Relationship.ForeignTableName;
				var foreignTableColumnName = column.Relationship.ForeignTableColumnName;
				command.CommandText = $"ALTER TABLE [{table.Name}] {maybeCheck} ADD CONSTRAINT [{constraintName}] FOREIGN KEY ([{column.Name}]) REFERENCES [{foreignTableName}] ({foreignTableColumnName})";
				command.Connection = connection;
				await ExecuteSqlAsync(command, doUpdate, script);
			}
		}

		protected virtual async Task UpdateColumnAsync(MappedTable table, MappedColumn oldColumn, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			if (oldColumn.AllowNulls && !column.AllowNulls)
			{
				// We're changing the column from NULL to NOT NULL so we need to first replace all null
				// values with the default value
				using (var command = CreateCommand(connection))
				{
					command.CommandText = $"UPDATE [{table.Name}] SET {column.Name} = {ColumnDefaultText(column)} WHERE {column.Name} IS NULL";
					command.Connection = connection;
					await ExecuteSqlAsync(command, doUpdate, script);
				}
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
					var constraints = await GetColumnConstraintsToDropAsync(table, column, connection);
					if (constraints.Count > 0)
					{
						command.CommandText = string.Join(Environment.NewLine, constraints.ToArray());
						command.Connection = connection;
						await ExecuteSqlAsync(command, doUpdate, script);
					}

					// Update the column
					command.CommandText = $"ALTER TABLE [{table.Name}] ALTER COLUMN {ColumnText(table, column, false, false)}";
					await ExecuteSqlAsync(command, doUpdate, script);

					// If the column is the primary key, add that constraint back now
					if (column.IsPrimaryKey)
					{
						command.CommandText = $"ALTER TABLE [{table.Name}] ADD CONSTRAINT [{table.PrimaryKeyConstraintName}] PRIMARY KEY {((this.CompactEdition ? "" : "CLUSTERED"))} ([{table.PrimaryKeyColumnName}]{((this.CompactEdition ? "" : " ASC"))})";
						await ExecuteSqlAsync(command, doUpdate, script);
					}

					// If the column has a default value, add that constraint back now
					if (column.DefaultValue != null)
					{
						command.CommandText = $"ALTER TABLE [{table.Name}] ADD CONSTRAINT [{column.DefaultValueConstraintName}] DEFAULT {ColumnDefaultText(column)} FOR [{column.Name}]";
						await ExecuteSqlAsync(command, doUpdate, script);
					}
				}
			}
		}

		protected virtual async Task<List<string>> GetColumnConstraintsToDropAsync(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			constraints.AddRange(await GetForeignKeyConstraintsToDropAsync(table, column, connection));
			constraints.AddRange(await GetPrimaryKeyConstraintsToDropAsync(table, column, connection));
			constraints.AddRange(await GetDefaultValueConstraintsToDropAsync(table, column, connection));

			return constraints;
		}

		protected virtual async Task<List<string>> GetForeignKeyConstraintsToDropAsync(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			// Get foreign key constraints on this column or that reference this column
			using (var command = CreateCommand(connection))
			{
				command.CommandText = "" +
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
					$"WHERE (PK.TABLE_NAME = '{table.Name}' AND PT.COLUMN_NAME = '{column.Name}') " +
					$"	OR (FK.TABLE_NAME = '{table.Name}' AND CU.COLUMN_NAME = '{column.Name}')";
				command.Connection = connection;
				using (var reader = await command.ExecuteReaderAsync())
				{
					while (reader.Read())
					{
						var tableName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
						var constraintName = reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"));
						constraints.Add($"ALTER TABLE [{tableName}] DROP CONSTRAINT [{constraintName}];");
					}
				}
			}

			return constraints;
		}

		protected virtual async Task<List<string>> GetPrimaryKeyConstraintsToDropAsync(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			// Get primary key constraints for this column
			using (var command = CreateCommand(connection))
			{
				command.CommandText = "" +
					"SELECT C.CONSTRAINT_NAME " +
					"FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS C " +
					"	INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME " +
					"WHERE C.CONSTRAINT_TYPE = 'PRIMARY KEY' " +
					$"	AND C.TABLE_NAME = '{table.Name}' AND CU.COLUMN_NAME = '{column.Name}' ";
				command.Connection = connection;
				using (var reader = await command.ExecuteReaderAsync())
				{
					while (reader.Read())
					{
						var constraintName = reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"));
						constraints.Add($"ALTER TABLE [{table.Name}] DROP CONSTRAINT [{constraintName}];");
					}
				}
			}

			return constraints;
		}

		protected virtual async Task<List<string>> GetDefaultValueConstraintsToDropAsync(MappedTable table, MappedColumn column, DbConnection connection)
		{
			var constraints = new List<string>();

			// Can't get default value constraints in CE so just return an empty list
			if (this.CompactEdition)
			{
				return constraints;
			}

			// Get default value constraints for this column
			using (var command = CreateCommand(connection))
			{
				command.CommandText = "" +
					"SELECT c.name " +
					"FROM sys.all_columns a " +
					"	INNER JOIN sys.tables b on a.object_id = b.object_id " +
					"	INNER JOIN sys.default_constraints c on a.default_object_id = c.object_id " +
					$"WHERE b.name = '{table.Name}' AND a.name = '{column.Name}' ";
				command.Connection = connection;
				using (var reader = await command.ExecuteReaderAsync())
				{
					while (reader.Read())
					{
						var constraintName = reader.GetString(reader.GetOrdinal("name"));
						constraints.Add($"ALTER TABLE [{table.Name}] DROP CONSTRAINT [{constraintName}];");
					}
				}
			}

			return constraints;
		}

		protected virtual async Task UpdateTableDataAsync(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script, bool tableExists)
		{
			// TODO: This is a bit messy and could be tidied up a bit
			var existingTableData = new List<int>();

			// Load the existing table data
			if (tableExists)
			{
				var selectExistingTableData = Select.From(table.Name).Columns(table.PrimaryKeyColumnName);
				using (var existingTableDataCommand = _dataAccessProvider.BuildCommand(selectExistingTableData, _configuration))
				{
					existingTableDataCommand.Connection = connection;
					using (var reader = await existingTableDataCommand.ExecuteReaderAsync())
					{
						while (reader.Read())
						{
							existingTableData.Add(reader.GetInt32(0));
						}
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
						await ExecuteSqlAsync(identityInsertCommand, doUpdate, script);

						var insertData = Insert.Into(table.Name);
						foreach (var key in data.Keys)
						{
							insertData = insertData.Value(key, data[key]);
						}
						using (var command = _dataAccessProvider.BuildCommand(insertData, _configuration))
						{
							command.Connection = connection;
							await ExecuteSqlAsync(command, doUpdate, script);
						}

						identityInsertCommand.CommandText = "SET IDENTITY_INSERT " + table.Name + " OFF";
						await ExecuteSqlAsync(identityInsertCommand, doUpdate, script);
					}
				}
			}
		}

		protected virtual async Task CreateViewAsync(MappedView view, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			if (this.CompactEdition)
			{
				// No views in CE
				return;
			}

			var commandText = BuildViewSql(view);
			using (var command = CreateCommand(connection))
			{
				command.CommandText = commandText;
				command.Connection = connection;
				await ExecuteSqlAsync(command, doUpdate, script);
			}
		}

		protected virtual async Task UpdateViewAsync(MappedView view, MappedView oldView, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			var commandText = BuildViewSql(view);
			if (oldView.SelectStatementText != commandText)
			{
				using (var command = CreateCommand(connection))
				{
					command.CommandText = commandText.Replace("CREATE VIEW", "ALTER VIEW");
					command.Connection = connection;
					await ExecuteSqlAsync(command, doUpdate, script);
				}
			}
		}

		protected virtual string BuildViewSql(MappedView view)
		{
			var b = new StringBuilder();
			b.AppendLine($"CREATE VIEW [{view.Name}] AS");

			using (var viewCommand = _configuration.DataAccessProvider.BuildCommand(view.SelectStatement, _configuration))
			{
				// Views can't have parameters, so replace all parameter calls with values
				var commandText = viewCommand.CommandText;
				for (var i = 0; i < viewCommand.Parameters.Count; i++)
				{
					if (viewCommand.Parameters[i].Value is string ||
						viewCommand.Parameters[i].Value is char)
					{
						commandText = commandText.Replace("@" + i, "'" + viewCommand.Parameters[i].Value.ToString() + "'");
					}
					else
					{
						commandText = commandText.Replace("@" + i, viewCommand.Parameters[i].Value.ToString());
					}
				}
				b.AppendLine(commandText);
			}

			return b.ToString();
		}

		protected virtual async Task CreateProcedureAsync(MappedProcedure procedure, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			if (this.CompactEdition)
			{
				// No procedures in CE
				return;
			}

			var commandText = BuildProcedureSql(procedure);
			using (var command = CreateCommand(connection))
			{
				command.CommandText = commandText;
				command.Connection = connection;
				await ExecuteSqlAsync(command, doUpdate, script);
			}
		}

		protected virtual async Task UpdateProcedureAsync(MappedProcedure procedure, MappedProcedure oldProcedure, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			var commandText = BuildProcedureSql(procedure);
			if (oldProcedure.StatementText != commandText)
			{
				using (var command = CreateCommand(connection))
				{
					command.CommandText = commandText.Replace("CREATE PROCEDURE", "ALTER PROCEDURE");
					command.Connection = connection;
					await ExecuteSqlAsync(command, doUpdate, script);
				}
			}
		}

		protected virtual string BuildProcedureSql(MappedProcedure procedure)
		{
			var b = new StringBuilder();
			b.AppendLine($"CREATE PROCEDURE [{procedure.Name}]");
			var parameterDeclarations = new List<string>();
			for (var i = 0; i < procedure.Parameters.Count; i++)
			{
				var parameter = procedure.Parameters[i];
				var parameterText = ColumnTypeText(parameter.ParameterType, parameter.MaxLength);
				var parameterDeclaration = $"{parameter.Name} {parameterText}";
				if (!parameterDeclarations.Contains(parameterDeclaration))
				{
					parameterDeclarations.Add(parameterDeclaration);
				}
			}
			b.AppendLine(string.Join("," + Environment.NewLine, parameterDeclarations));
			b.AppendLine("AS");
			b.AppendLine("BEGIN");
			b.AppendLine("SET NOCOUNT ON;");
			b.AppendLine();
			using (var procedureCommand = _configuration.DataAccessProvider.BuildCommand(procedure.Statement, _configuration))
			{
				// Procedures can't have parameters, so replace all parameter calls with values
				var commandText = procedureCommand.CommandText;
				for (var i = 0; i < procedureCommand.Parameters.Count; i++)
				{
					if (procedureCommand.Parameters[i].Value is MappedParameter parameter)
					{
						commandText = commandText.Replace("@" + i, parameter.Name);
					}
					else if (procedureCommand.Parameters[i].Value is string ||
						procedureCommand.Parameters[i].Value is char)
					{
						commandText = commandText.Replace("@" + i, "'" + procedureCommand.Parameters[i].Value.ToString() + "'");
					}
					else
					{
						commandText = commandText.Replace("@" + i, procedureCommand.Parameters[i].Value.ToString());
					}
				}
				b.AppendLine(commandText);
			}
			b.AppendLine();
			b.AppendLine("END");

			return b.ToString();
		}

		protected virtual async Task CreateFunctionAsync(MappedFunction function, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			if (this.CompactEdition)
			{
				// No functions in CE
				return;
			}

			var commandText = BuildFunctionSql(function);
			using (var command = CreateCommand(connection))
			{
				command.CommandText = commandText;
				command.Connection = connection;
				await ExecuteSqlAsync(command, doUpdate, script);
			}
		}

		protected virtual async Task UpdateFunctionAsync(MappedFunction function, MappedFunction oldFunction, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			var commandText = BuildFunctionSql(function);
			if (oldFunction.StatementText != commandText)
			{
				using (var command = CreateCommand(connection))
				{
					command.CommandText = commandText.Replace("CREATE FUNCTION", "ALTER FUNCTION");
					command.Connection = connection;
					await ExecuteSqlAsync(command, doUpdate, script);
				}
			}
		}

		protected virtual string BuildFunctionSql(MappedFunction function)
		{
			var b = new StringBuilder();
			b.AppendLine($"CREATE FUNCTION [{function.Name}]");
			var parameterDeclarations = new List<string>();
			for (var i = 0; i < function.Parameters.Count; i++)
			{
				var parameter = function.Parameters[i];
				var parameterText = ColumnTypeText(parameter.ParameterType, parameter.MaxLength);
				var parameterDeclaration = $"{parameter.Name} {parameterText}";
				if (!parameterDeclarations.Contains(parameterDeclaration))
				{
					parameterDeclarations.Add(parameterDeclaration);
				}
			}
			if (parameterDeclarations.Count > 0)
			{
				b.AppendLine("(");
				b.AppendLine(string.Join("," + Environment.NewLine, parameterDeclarations));
				b.AppendLine(")");
			}
			b.AppendLine("RETURNS table");
			b.AppendLine("AS");
			b.AppendLine("RETURN (");
			b.AppendLine();
			using (var functionCommand = _configuration.DataAccessProvider.BuildCommand(function.Statement, _configuration))
			{
				// Functions can't have parameters, so replace all parameter calls with values
				var commandText = functionCommand.CommandText;
				for (var i = 0; i < functionCommand.Parameters.Count; i++)
				{
					if (functionCommand.Parameters[i].Value is MappedParameter parameter)
					{
						commandText = commandText.Replace("@" + i, parameter.Name);
					}
					else if (functionCommand.Parameters[i].Value is string ||
						functionCommand.Parameters[i].Value is char)
					{
						commandText = commandText.Replace("@" + i, "'" + functionCommand.Parameters[i].Value.ToString() + "'");
					}
					else
					{
						commandText = commandText.Replace("@" + i, functionCommand.Parameters[i].Value.ToString());
					}
				}
				b.AppendLine(commandText);
			}
			b.AppendLine();
			b.AppendLine(")");

			return b.ToString();
		}

		protected virtual async Task ExecuteSqlAsync(DbCommand command, bool doUpdate, StringBuilder script)
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
				await command.ExecuteNonQueryAsync();
			}
		}

		protected virtual DbCommand CreateCommand(DbConnection connection)
		{
			var command = new SqlCommand();
			command.Connection = (SqlConnection)connection;
			return command;
		}

		public async Task<string> GetUnmappedColumnsAsync(IEnumerable<MappedTable> tables)
		{
			var columns = new StringBuilder();

			using (var connection = await _dataAccessProvider.OpenConnectionAsync(_configuration))
			{
				// Load the existing columns
				var existingColumns = await LoadExistingColumnsAsync(connection);

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

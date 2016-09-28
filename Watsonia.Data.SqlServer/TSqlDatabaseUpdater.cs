﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.SqlServer
{
	public class TSqlDatabaseUpdater
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

		public TSqlDatabaseUpdater(IDataAccessProvider dataAccessProvider, DatabaseConfiguration configuration)
		{
			_dataAccessProvider = dataAccessProvider;
			_configuration = configuration;
		}

		public void UpdateDatabase(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views)
		{
			UpdateDatabase(tables, views, true);
		}

		public string GetUpdateScript(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views)
		{
			StringBuilder script = new StringBuilder();
			UpdateDatabase(tables, views, false, script);
			return script.ToString();
		}

		protected virtual void UpdateDatabase(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views, bool doUpdate, StringBuilder script = null)
		{
			using (var connection = _dataAccessProvider.OpenConnection(_configuration))
			{
				// Load the existing tables and columns
				var existingTables = LoadExistingTables(connection);
				var existingColumns = LoadExistingColumns(connection);
				var existingViews = LoadExistingViews(connection);

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
					bool tableExists = existingTables.ContainsKey(table.Name.ToUpperInvariant());
					UpdateTableData(table, connection, doUpdate, script, tableExists);
				}

				// Third pass - create relationship constraints
				var existingForeignKeys = LoadExistingForeignKeys(connection);
				foreach (MappedTable table in tables)
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

				// Fourth pass - create views
				foreach (MappedView view in views)
				{
					string key = view.Name.ToUpperInvariant();
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

		protected virtual Dictionary<string, MappedColumn> LoadExistingColumns(DbConnection connection)
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
						string dataTypeName = reader.GetString(reader.GetOrdinal("DATA_TYPE"));

						Type columnType = FrameworkTypeFromDatabase(dataTypeName, allowNulls);
						int maxLength = 0;
						if (columnType == typeof(string))
						{
							maxLength = reader.GetInt32(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"));
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

		protected virtual Dictionary<string, MappedView> LoadExistingViews(DbConnection connection)
		{
			var existingViews = new Dictionary<string, MappedView>();
			if (!this.CompactEdition)
			{
				using (var existingViewsCommand = CreateCommand(connection))
				{
					existingViewsCommand.CommandText = "SELECT TABLE_NAME, VIEW_DEFINITION FROM INFORMATION_SCHEMA.VIEWS";
					existingViewsCommand.Connection = connection;
					using (var reader = existingViewsCommand.ExecuteReader())
					{
						while (reader.Read())
						{
							string viewName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));
							string selectStatementText = reader.GetString(reader.GetOrdinal("VIEW_DEFINITION"));
							MappedView view = new MappedView(viewName);
							view.SelectStatementText = selectStatementText;
							string key = viewName.ToUpperInvariant();
							existingViews.Add(key, view);
						}
					}
				}
			}
			return existingViews;
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

		protected virtual List<string> LoadExistingForeignKeys(DbConnection connection)
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

		protected virtual void CreateTable(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			StringBuilder b = new StringBuilder();
			b.AppendFormat("CREATE TABLE [{0}] (", table.Name);
			b.AppendLine();
			b.Append(string.Join(", ", Array.ConvertAll(table.Columns.ToArray(), c => ColumnText(table, c, true, true))));
			b.AppendLine(",");
			b.AppendFormat("CONSTRAINT [{0}] PRIMARY KEY {1} ([{2}]{3})", table.PrimaryKeyConstraintName, (this.CompactEdition ? "" : "CLUSTERED"), table.PrimaryKeyColumnName, (this.CompactEdition ? "" : " ASC"));
			b.AppendLine();
			b.Append(")");
			using (var command = CreateCommand(connection))
			{
				command.CommandText = b.ToString();
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		protected virtual void CreateColumn(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format("ALTER TABLE [{0}] ADD {1}", table.Name, ColumnText(table, column, true, true));
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		protected virtual string ColumnText(MappedTable table, MappedColumn column, bool includeDefault, bool includeIdentity)
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

		protected virtual string ColumnTypeText(MappedColumn column)
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
			else if (column.ColumnType == typeof(byte) || column.ColumnType == typeof(byte?))
			{
				return "TINYINT";
			}
			else if (column.ColumnType == typeof(string))
			{
				if (column.MaxLength >= 4000)
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
					return string.Format("NVARCHAR({0})", column.MaxLength);
				}
			}
			else if (column.ColumnType == typeof(byte[]))
			{
				if (column.MaxLength == 0)
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
					return string.Format("VARBINARY({0})", column.MaxLength);
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
				return column.DefaultValue != null ? string.Format("'{0:D}'", column.DefaultValue) : "'00000000-0000-0000-0000-000000000000'";
			}
			else
			{
				throw new InvalidOperationException("Invalid column type: " + column.ColumnType);
			}
		}

		protected virtual void CreateForeignKey(MappedTable table, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			using (var command = CreateCommand(connection))
			{
				command.CommandText = string.Format(
					"ALTER TABLE [{0}] {1} ADD CONSTRAINT [{2}] FOREIGN KEY ([{3}]) REFERENCES [{4}] ({5})",
					table.Name, (this.CompactEdition ? "" : "WITH CHECK"), column.Relationship.ConstraintName, column.Name, column.Relationship.ForeignTableName, column.Relationship.ForeignTableColumnName);
				command.Connection = connection;
				ExecuteSql(command, doUpdate, script);
			}
		}

		protected virtual void UpdateColumn(MappedTable table, MappedColumn oldColumn, MappedColumn column, DbConnection connection, bool doUpdate, StringBuilder script)
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
						command.CommandText = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [{1}] PRIMARY KEY {2} ([{3}]{4})", table.Name, table.PrimaryKeyConstraintName, (this.CompactEdition ? "" : "CLUSTERED"), table.PrimaryKeyColumnName, (this.CompactEdition ? "" : " ASC"));
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

		protected virtual List<string> GetColumnConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			List<string> constraints = new List<string>();

			constraints.AddRange(GetForeignKeyConstraintsToDrop(table, column, connection));
			constraints.AddRange(GetPrimaryKeyConstraintsToDrop(table, column, connection));
			constraints.AddRange(GetDefaultValueConstraintsToDrop(table, column, connection));

			return constraints;
		}

		protected virtual List<string> GetForeignKeyConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
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

			return constraints;
		}

		protected virtual List<string> GetPrimaryKeyConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			List<string> constraints = new List<string>();

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

			return constraints;
		}

		protected virtual List<string> GetDefaultValueConstraintsToDrop(MappedTable table, MappedColumn column, DbConnection connection)
		{
			List<string> constraints = new List<string>();

			// Can't get default value constraints in CE so just return an empty list
			if (this.CompactEdition)
			{
				return constraints;
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

		protected virtual void UpdateTableData(MappedTable table, DbConnection connection, bool doUpdate, StringBuilder script, bool tableExists)
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
					using (var reader = existingTableDataCommand.ExecuteReader())
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
						ExecuteSql(identityInsertCommand, doUpdate, script);

						InsertStatement insertData = Insert.Into(table.Name);
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

		protected virtual void CreateView(MappedView view, DbConnection connection, bool doUpdate, StringBuilder script)
		{
			if (this.CompactEdition)
			{
				// No views in CE
				return;
			}

			StringBuilder b = new StringBuilder();
			b.AppendFormat("CREATE VIEW [{0}] AS", view.Name);
			b.AppendLine();
			using (var viewCommand = _configuration.DataAccessProvider.BuildCommand(view.SelectStatement, _configuration))
			{
				var commandText = BuildViewSql(viewCommand);
				b.Append(commandText);
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
			StringBuilder b = new StringBuilder();
			b.AppendFormat("CREATE VIEW [{0}] AS", view.Name);
			b.AppendLine();
			using (var viewCommand = _configuration.DataAccessProvider.BuildCommand(view.SelectStatement, _configuration))
			{
				var commandText = BuildViewSql(viewCommand);
				b.Append(commandText);
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

		protected virtual string BuildViewSql(DbCommand viewCommand)
		{
			// Views can't have parameters, so replace all parameter calls with values
			var commandText = viewCommand.CommandText;
			for (int i = 0; i < viewCommand.Parameters.Count; i++)
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
			return commandText;
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

		protected virtual DbCommand CreateCommand(DbConnection connection)
		{
			var command = new SqlCommand();
			command.Connection = (SqlConnection)connection;
			return command;
		}

		public string GetUnmappedColumns(IEnumerable<MappedTable> tables, IEnumerable<MappedView> views)
		{
			StringBuilder columns = new StringBuilder();

			using (var connection = _dataAccessProvider.OpenConnection(_configuration))
			{
				// Load the existing columns
				var existingColumns = LoadExistingColumns(connection);

				// Check whether each existing column is mapped
				foreach (string columnKey in existingColumns.Keys)
				{
					string tableName = columnKey.Split('.')[0];
					string columnName = columnKey.Split('.')[1];

					MappedTable table = tables.FirstOrDefault(m => m.Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase));
					bool isColumnMapped = (table != null && table.Columns.Any(c => c.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase)));

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

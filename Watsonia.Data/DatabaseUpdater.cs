using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Watsonia.Data
{
	internal sealed class DatabaseUpdater
	{
		public void UpdateDatabase(DatabaseConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			var tables = new List<MappedTable>();
			var views = new List<MappedView>();
			var procedures = new List<MappedProcedure>();
			var functions = new List<MappedFunction>();
			GetMappedObjects(tables, views, procedures, functions, configuration);
			configuration.DataAccessProvider.UpdateDatabase(tables, views, procedures, functions, configuration);
		}

		public string GetUpdateScript(DatabaseConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			var tables = new List<MappedTable>();
			var views = new List<MappedView>();
			var procedures = new List<MappedProcedure>();
			var functions = new List<MappedFunction>();
			GetMappedObjects(tables, views, procedures, functions, configuration);
			return configuration.DataAccessProvider.GetUpdateScript(tables, views, procedures, functions, configuration);
		}

		public string GetUnmappedColumns(DatabaseConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			var tables = new List<MappedTable>();
			var views = new List<MappedView>();
			var procedures = new List<MappedProcedure>();
			var functions = new List<MappedFunction>();
			GetMappedObjects(tables, views, procedures, functions, configuration);
			return configuration.DataAccessProvider.GetUnmappedColumns(tables, views, procedures, functions, configuration);
		}

		private void GetMappedObjects(List<MappedTable> tables, List<MappedView> views, List<MappedProcedure> procedures, List<MappedFunction> functions, DatabaseConfiguration configuration)
		{
			if (tables == null)
			{
				throw new ArgumentNullException("tables");
			}

			if (views == null)
			{
				throw new ArgumentNullException("views");
			}

			if (procedures == null)
			{
				throw new ArgumentNullException("procedures");
			}

			if (functions == null)
			{
				throw new ArgumentNullException("functions");
			}

			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			tables.Clear();
			views.Clear();

			var tableDictionary = new Dictionary<string, MappedTable>();
			var tableRelationships = new Dictionary<string, MappedRelationship>();
			var viewDictionary = new Dictionary<string, MappedView>();
			var procedureDictionary = new Dictionary<string, MappedProcedure>();
			var functionDictionary = new Dictionary<string, MappedFunction>();
			foreach (Type type in configuration.TypesToMap())
			{
				if (configuration.IsView(type))
				{
					GetMappedView(viewDictionary, type, configuration);
				}
				else if (configuration.IsProcedure(type))
				{
					GetMappedProcedure(procedureDictionary, type, configuration);
				}
				else if (configuration.IsFunction(type))
				{
					GetMappedFunction(functionDictionary, type, configuration);
				}
				else if (configuration.IsTable(type))
				{
					GetMappedTable(tableDictionary, tableRelationships, type, configuration);
				}
			}

			// Assign each relationship to the appropriate table
			foreach (var relationshipKey in tableRelationships.Keys)
			{
				MappedRelationship relationship = tableRelationships[relationshipKey];

				string tableName = relationshipKey.Split('.')[0];
				string columnName = relationshipKey.Split('.')[1];
				MappedTable table;
				if (tableDictionary.TryGetValue(tableName, out table))
				{
					MappedColumn column = table.Columns.FirstOrDefault(c => c.Name == columnName);
					if (column == null)
					{
						Type foreignKeyType = typeof(Nullable<>).MakeGenericType(configuration.GetPrimaryKeyColumnType(relationship.ForeignTableType));
						column = new MappedColumn(columnName, foreignKeyType, "");
						table.Columns.Add(column);
					}
					column.Relationship = relationship;
				}
			}

			tables.AddRange(tableDictionary.Values);
			views.AddRange(viewDictionary.Values);
			procedures.AddRange(procedureDictionary.Values);
			functions.AddRange(functionDictionary.Values);
		}

		private void GetMappedTable(Dictionary<string, MappedTable> tableDictionary, Dictionary<string, MappedRelationship> tableRelationships, Type tableType,  DatabaseConfiguration configuration)
		{
			string tableName = configuration.GetTableName(tableType);
			string primaryKeyColumnName = configuration.GetPrimaryKeyColumnName(tableType);
			string primaryKeyConstraintName = configuration.GetPrimaryKeyConstraintName(tableType);

			var table = new MappedTable(tableName, primaryKeyColumnName, primaryKeyConstraintName);

			bool tableHasColumns = false;
			bool tableHasRelationships = false;

			foreach (PropertyInfo property in configuration.PropertiesToMap(tableType))
			{
				string columnName = configuration.GetColumnName(property);
				string defaultValueConstraintName = configuration.GetDefaultValueConstraintName(tableName, property);

				var column = new MappedColumn(columnName, property.PropertyType, defaultValueConstraintName);
				bool addColumn = true;
				if (property.PropertyType == typeof(string))
				{
					// It's a string so get the max length from any StringLength attribute that is applied
					object[] stringLengthAttributes = property.GetCustomAttributes(typeof(StringLengthAttribute), false);
					if (stringLengthAttributes.Length > 0)
					{
						StringLengthAttribute attribute = (StringLengthAttribute)stringLengthAttributes[0];
						column.MaxLength = attribute.MaximumLength;
					}
					else
					{
						column.MaxLength = 255;
					}

					// And its default value is the empty string
					column.DefaultValue = "";
				}
				else if (property.PropertyType.IsEnum)
				{
					// It's an enum so we might need to create a table for it
					string enumTableName = configuration.GetTableName(property.PropertyType);
					string enumPrimaryKeyColumnName = configuration.GetPrimaryKeyColumnName(property.PropertyType);
					string enumPrimaryKeyConstraintName = configuration.GetPrimaryKeyConstraintName(property.PropertyType);

					if (!tableDictionary.ContainsKey(property.PropertyType.Name))
					{
						var enumTable = new MappedTable(enumTableName, enumPrimaryKeyColumnName, enumPrimaryKeyConstraintName);
						enumTable.Columns.Add(new MappedColumn(enumPrimaryKeyColumnName, typeof(int), "") { IsPrimaryKey = true });
						enumTable.Columns.Add(new MappedColumn("Text", typeof(string), "DF_" + enumTableName + "_Text") { MaxLength = 255 });
						foreach (object value in Enum.GetValues(property.PropertyType))
						{
							enumTable.Values.Add(new Dictionary<string, object>() { 
										{ "ID", (int)value },
										{ "Text", Enum.GetName(property.PropertyType, value) }
									});
						}
						tableDictionary.Add(enumTable.Name, enumTable);
					}

					// We also have to set it up as a foreign key
					column.ColumnType = typeof(int);
					column.Relationship = new MappedRelationship(
						configuration.GetForeignKeyConstraintName(property),
						property.PropertyType,
						enumTableName,
						enumPrimaryKeyColumnName);

					// And its default value is the default value for the enum
					object[] enumDefaultValueAttributes = property.PropertyType.GetCustomAttributes(typeof(DefaultValueAttribute), false);
					if (enumDefaultValueAttributes.Length > 0)
					{
						DefaultValueAttribute attribute = (DefaultValueAttribute)enumDefaultValueAttributes[0];
						column.DefaultValue = (int)attribute.Value;
					}
					else
					{
						column.DefaultValue = (int)Enum.GetValues(property.PropertyType).GetValue(0);
					}
				}
				else if (configuration.IsRelatedItem(property))
				{
					// It's a property referencing another table so change its name and type
					column.Name = configuration.GetForeignKeyColumnName(property);
					column.ColumnType = typeof(Nullable<>).MakeGenericType(configuration.GetPrimaryKeyColumnType(property.PropertyType));
					column.Relationship = new MappedRelationship(
						configuration.GetForeignKeyConstraintName(property),
						property.PropertyType,
						configuration.GetTableName(property.PropertyType),
						configuration.GetPrimaryKeyColumnName(property.PropertyType));
				}
				else
				{
					Type itemType;
					if (configuration.IsRelatedCollection(property, out itemType))
					{
						// It's a collection property referencing another table so add it to the table relationships
						// collection for wiring up when we have all tables
						string key = $"{configuration.GetTableName(itemType)}.{configuration.GetForeignKeyColumnName(itemType, tableType)}";
						tableRelationships.Add(key,
							new MappedRelationship(
								configuration.GetForeignKeyConstraintName(itemType, tableType),
								property.PropertyType,
								configuration.GetTableName(tableType),
								configuration.GetPrimaryKeyColumnName(tableType)));
						addColumn = false;
						tableHasRelationships = true;
					}
				}

				// Get the default value from any DefaultValue attribute that is applied
				object[] defaultValueAttributes = property.GetCustomAttributes(typeof(DefaultValueAttribute), false);
				if (defaultValueAttributes.Length > 0)
				{
					DefaultValueAttribute attribute = (DefaultValueAttribute)defaultValueAttributes[0];
					column.DefaultValue = attribute.Value;
				}

				if (addColumn)
				{
					table.Columns.Add(column);
					tableHasColumns = true;
				}
			}

			if (tableHasColumns || tableHasRelationships)
			{
				// Add the primary key column (or move it to the first column position for nicety)
				MappedColumn primaryKeyColumn = table.Columns.FirstOrDefault(c => c.Name.Equals(primaryKeyColumnName, StringComparison.InvariantCultureIgnoreCase));
				if (primaryKeyColumn == null)
				{
					primaryKeyColumn = new MappedColumn(primaryKeyColumnName, configuration.GetPrimaryKeyColumnType(tableType), "");
					table.Columns.Insert(0, primaryKeyColumn);
				}
				else
				{
					table.Columns.Remove(primaryKeyColumn);
					table.Columns.Insert(0, primaryKeyColumn);
				}
				primaryKeyColumn.IsPrimaryKey = true;

				// Add the table to the dictionary
				tableDictionary.Add(table.Name, table);

				// Remove any duplicate relationship columns e.g. if the user has added properties for
				// Author and AuthorID, we want AuthorID to be populated from the relationship
				var columnsToRemove = new List<MappedColumn>();
				foreach (MappedColumn column in table.Columns)
				{
					if (column.Relationship == null &&
						table.Columns.Any(c => c.Relationship != null && c.Name == column.Name))
					{
						columnsToRemove.Add(column);
					}
				}
				foreach (MappedColumn column in columnsToRemove)
				{
					table.Columns.Remove(column);
				}
			}
		}

		private void GetMappedView(Dictionary<string, MappedView> viewDictionary, Type viewType, DatabaseConfiguration configuration)
		{
			string viewName = configuration.GetViewName(viewType);

			var view = new MappedView(viewName);

			bool viewHasColumns = false;

			foreach (PropertyInfo property in configuration.PropertiesToMap(viewType))
			{
				string columnName = configuration.GetColumnName(property);

				var column = new MappedColumn(columnName, property.PropertyType, "");
				bool addColumn = true;
				if (configuration.IsRelatedItem(property))
				{
					// It's a property referencing another view so change its name and type
					column.Name = configuration.GetForeignKeyColumnName(property);
					column.ColumnType = typeof(Nullable<>).MakeGenericType(configuration.GetPrimaryKeyColumnType(property.PropertyType));
					column.Relationship = new MappedRelationship(
						configuration.GetForeignKeyConstraintName(property),
						property.PropertyType,
						configuration.GetViewName(property.PropertyType),
						configuration.GetPrimaryKeyColumnName(property.PropertyType));
				}
				else
				{
					Type itemType;
					if (configuration.IsRelatedCollection(property, out itemType))
					{
						// It's a collection property referencing another table so add it to the view relationships
						// collection for wiring up when we have all views
						addColumn = false;
					}
				}

				if (addColumn)
				{
					viewHasColumns = true;
				}
			}

			PropertyInfo statementProperty = viewType.GetProperty("SelectStatement", BindingFlags.Public | BindingFlags.Static);
			if (statementProperty != null)
			{
				view.SelectStatement = (Statement)statementProperty.GetValue(null);
			}

			if (viewHasColumns)
			{
				// Add the view to the dictionary
				viewDictionary.Add(view.Name, view);
			}
		}
		
		private void GetMappedProcedure(Dictionary<string, MappedProcedure> procedureDictionary, Type procedureType, DatabaseConfiguration configuration)
		{
			string procedureName = configuration.GetProcedureName(procedureType);

			var procedure = new MappedProcedure(procedureName);

			PropertyInfo statementProperty = procedureType.GetProperty("Statement", BindingFlags.Public | BindingFlags.Static);
			if (statementProperty != null)
			{
				procedure.Statement = (Statement)statementProperty.GetValue(null);
			}

			// Get the parameters from the statement property
			if (procedure.Statement is SelectStatement)
			{
				var select = (SelectStatement)procedure.Statement;
				GatherMappedParameters(procedure.Parameters, select.Conditions);
				foreach (var source in select.SourceFields)
				{
					if (source is Sql.SelectExpression)
					{
						var sourceSelect = (Sql.SelectExpression)source;
						GatherMappedParameters(procedure.Parameters, sourceSelect.Select.Conditions);
					}
				}
			}

			// Add the procedure to the dictionary
			procedureDictionary.Add(procedure.Name, procedure);
		}

		private void GetMappedFunction(Dictionary<string, MappedFunction> functionDictionary, Type functionType, DatabaseConfiguration configuration)
		{
			string functionName = configuration.GetFunctionName(functionType);

			var function = new MappedFunction(functionName);

			PropertyInfo statementProperty = functionType.GetProperty("Statement", BindingFlags.Public | BindingFlags.Static);
			if (statementProperty != null)
			{
				function.Statement = (Statement)statementProperty.GetValue(null);
			}

			// Get the parameters from the statement property
			if (function.Statement is SelectStatement)
			{
				var select = (SelectStatement)function.Statement;
				GatherMappedParameters(function.Parameters, select.Conditions);
				foreach (var source in select.SourceFields)
				{
					if (source is Sql.SelectExpression)
					{
						var sourceSelect = (Sql.SelectExpression)source;
						GatherMappedParameters(function.Parameters, sourceSelect.Select.Conditions);
					}
				}
			}

			// Add the function to the dictionary
			functionDictionary.Add(function.Name, function);
		}

		private void GatherMappedParameters(ICollection<MappedParameter> parameters, ConditionCollection conditions)
		{
			foreach (var condition in conditions)
			{
				if (condition is ConditionCollection)
				{
					GatherMappedParameters(parameters, (ConditionCollection)condition);
				}
				else if (condition is Condition)
				{
					GatherMappedParametersFromCondition(parameters, (Condition)condition);
				}
			}
		}

		private void GatherMappedParametersFromCondition(ICollection<MappedParameter> parameters, Condition condition)
		{
			if (condition.Value is Sql.ConstantPart &&
				((Sql.ConstantPart)condition.Value).Value is MappedParameter)
			{
				var parameterValue = ((Sql.ConstantPart)condition.Value).Value;
				parameters.Add((MappedParameter)parameterValue);
			}
		}
	}
}

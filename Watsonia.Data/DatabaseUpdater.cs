using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
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
			GetMappedTablesAndViews(tables, views, configuration);
			configuration.DataAccessProvider.UpdateDatabase(tables, views, configuration);
		}

		public string GetUpdateScript(DatabaseConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			var tables = new List<MappedTable>();
			var views = new List<MappedView>();
			GetMappedTablesAndViews(tables, views, configuration);
			return configuration.DataAccessProvider.GetUpdateScript(tables, views, configuration);
		}

		public string GetUnmappedColumns(DatabaseConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}

			var tables = new List<MappedTable>();
			var views = new List<MappedView>();
			GetMappedTablesAndViews(tables, views, configuration);
			return configuration.DataAccessProvider.GetUnmappedColumns(tables, views, configuration);
		}

		private void GetMappedTablesAndViews(List<MappedTable> tables, List<MappedView> views, DatabaseConfiguration configuration)
		{
			if (tables == null)
			{
				throw new ArgumentNullException("tables");
			}

			if (views == null)
			{
				throw new ArgumentNullException("views");
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
			foreach (Type type in configuration.TypesToMap())
			{
				if (configuration.IsTable(type))
				{
					GetMappedTable(tableDictionary, tableRelationships, type, configuration);
				}
				else if (configuration.IsView(type))
				{
					GetMappedView(viewDictionary, type, configuration);
				}
			}

			// Assign each relationship to the appropriate table
			foreach (var relationshipKey in tableRelationships.Keys)
			{
				string tableName = relationshipKey.Split('.')[0];
				string columnName = relationshipKey.Split('.')[1];
				MappedTable table;
				if (tableDictionary.TryGetValue(tableName, out table))
				{
					MappedColumn column = table.Columns.FirstOrDefault(c => c.Name == columnName);
					if (column == null)
					{
						column = new MappedColumn(columnName, typeof(long?), "");
						table.Columns.Add(column);
					}
					column.Relationship = tableRelationships[relationshipKey];
				}
			}

			tables.AddRange(tableDictionary.Values);
			views.AddRange(viewDictionary.Values);
		}

		private void GetMappedTable(Dictionary<string, MappedTable> tableDictionary, Dictionary<string, MappedRelationship> tableRelationships, Type type,  DatabaseConfiguration configuration)
		{
			string tableName = configuration.GetTableName(type);
			string primaryKeyColumnName = configuration.GetPrimaryKeyColumnName(type);
			string primaryKeyConstraintName = configuration.GetPrimaryKeyConstraintName(type);

			MappedTable table = new MappedTable(tableName, primaryKeyColumnName, primaryKeyConstraintName);

			bool tableHasColumns = false;
			bool tableHasRelationships = false;

			foreach (PropertyInfo property in configuration.PropertiesToMap(type))
			{
				string columnName = configuration.GetColumnName(property);
				string defaultValueConstraintName = configuration.GetDefaultValueConstraintName(property);

				MappedColumn column = new MappedColumn(columnName, property.PropertyType, defaultValueConstraintName);
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
						MappedTable enumTable = new MappedTable(enumTableName, enumPrimaryKeyColumnName, enumPrimaryKeyConstraintName);
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
						string key = string.Format("{0}.{1}", configuration.GetTableName(itemType), configuration.GetForeignKeyColumnName(itemType, type));
						tableRelationships.Add(key,
							new MappedRelationship(
							configuration.GetForeignKeyConstraintName(itemType, type),
							configuration.GetTableName(type),
							configuration.GetPrimaryKeyColumnName(type)));
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
					primaryKeyColumn = new MappedColumn(primaryKeyColumnName, configuration.GetPrimaryKeyColumnType(type), "");
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

		private void GetMappedView(Dictionary<string, MappedView> viewDictionary, Type type, DatabaseConfiguration configuration)
		{
			string viewName = configuration.GetViewName(type);

			MappedView view = new MappedView(viewName);

			bool viewHasColumns = false;

			foreach (PropertyInfo property in configuration.PropertiesToMap(type))
			{
				string columnName = configuration.GetColumnName(property);

				MappedColumn column = new MappedColumn(columnName, property.PropertyType, "");
				bool addColumn = true;
				if (configuration.IsRelatedItem(property))
				{
					// It's a property referencing another view so change its name and type
					column.Name = configuration.GetForeignKeyColumnName(property);
					column.ColumnType = typeof(Nullable<>).MakeGenericType(configuration.GetPrimaryKeyColumnType(property.PropertyType));
					column.Relationship = new MappedRelationship(
						configuration.GetForeignKeyConstraintName(property),
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

			PropertyInfo statementProperty = type.GetProperty("SelectStatement", BindingFlags.Public | BindingFlags.Static);
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
	}
}

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
			var tables = GetMappedTables(configuration);
			configuration.DataAccessProvider.UpdateDatabase(tables);
		}

		public string GetUpdateScript(DatabaseConfiguration configuration)
		{
			var tables = GetMappedTables(configuration);
			return configuration.DataAccessProvider.GetUpdateScript(tables);
		}

		private IEnumerable<MappedTable> GetMappedTables(DatabaseConfiguration configuration)
		{
			var tables = new Dictionary<string, MappedTable>();
			var tableRelationships = new Dictionary<string, MappedRelationship>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in configuration.TypesToMap(assembly))
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
						}
						else if (property.PropertyType.IsEnum)
						{
							// It's an enum so we might need to create a table for it
							string enumTableName = configuration.GetTableName(property.PropertyType);
							string enumPrimaryKeyColumnName = configuration.GetPrimaryKeyColumnName(property.PropertyType);
							string enumPrimaryKeyConstraintName = configuration.GetPrimaryKeyConstraintName(property.PropertyType);

							if (!tables.ContainsKey(property.PropertyType.Name))
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
								tables.Add(enumTable.Name, enumTable);
							}

							// We also have to set it up as a foreign key
							column.ColumnType = typeof(int);
							column.Relationship = new MappedRelationship(
								configuration.GetForeignKeyConstraintName(property),
								enumTableName,
								enumPrimaryKeyColumnName);
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
						MappedColumn primaryKeyColumn = table.Columns.FirstOrDefault(c => c.Name.Equals(primaryKeyColumnName, StringComparison.InvariantCultureIgnoreCase));
						if (primaryKeyColumn == null)
						{
							primaryKeyColumn = new MappedColumn(primaryKeyColumnName, configuration.GetPrimaryKeyColumnType(type), "");
							table.Columns.Add(primaryKeyColumn);
						}
						primaryKeyColumn.IsPrimaryKey = true;
						tables.Add(table.Name, table);
					}
				}
			}

			// Assign each relationship to the appropriate table
			foreach (var relationshipKey in tableRelationships.Keys)
			{
				string tableName = relationshipKey.Split('.')[0];
				string columnName = relationshipKey.Split('.')[1];
				MappedTable table;
				if (!tables.TryGetValue(tableName, out table))
				{
					table = new MappedTable(tableName);
					tables.Add(table.Name, table);
				}
				MappedColumn column = table.Columns.FirstOrDefault(c => c.Name == columnName);
				if (column == null)
				{
					column = new MappedColumn(columnName, typeof(long?), "");
					table.Columns.Add(column);
				}
				column.Relationship = tableRelationships[relationshipKey];
			}

			return tables.Values;
		}
	}
}

using Remotion.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Sql;
using Remotion.Linq.Clauses;
using System.Linq.Expressions;

namespace Watsonia.Data
{
	internal class SelectFieldExpander : QueryModelVisitorBase
	{
		private QueryModel QueryModel
		{
			get;
			set;
		}

		private Select SelectStatement
		{
			get;
			set;
		}

		private DatabaseConfiguration Configuration
		{
			get;
			set;
		}

		private SelectFieldExpander(QueryModel queryModel, Select selectStatement, DatabaseConfiguration configuration)
		{
			this.QueryModel = queryModel;
			this.SelectStatement = selectStatement;
			this.Configuration = configuration;
		}

		public static void Visit(QueryModel queryModel, Select selectStatement, DatabaseConfiguration configuration)
		{
			var visitor = new SelectFieldExpander(queryModel, selectStatement, configuration);
			queryModel.Accept(visitor);
		}

		public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
		{
			if (selectClause.Selector.NodeType == ExpressionType.Extension)
			{
				// If we are selecting an object, specify its fields
				// This will avoid the case where selecting fields from multiple tables with non-unique field
				// names (e.g. two tables with an ID field) fills the object with the wrong value
				var columnNames = new List<string>();
				string primaryKeyColumnName = this.Configuration.GetPrimaryKeyColumnName(selectClause.Selector.Type);
				foreach (PropertyInfo property in this.Configuration.PropertiesToMap(selectClause.Selector.Type))
				{
					if (this.Configuration.IsRelatedItem(property))
					{
						// It's a property referencing another table so change its name and type
						string columnName = this.Configuration.GetForeignKeyColumnName(property);
						if (!columnNames.Any(c => c.Equals(columnName, StringComparison.InvariantCultureIgnoreCase)))
						{
							columnNames.Add(columnName);
						}
					}
					else if (this.Configuration.IsRelatedCollection(property))
					{
						// It's a collection property referencing another table so ignore it
					}
					else
					{
						// It's a regular mapped column
						string columnName = this.Configuration.GetColumnName(property);
						if (!columnName.Equals(primaryKeyColumnName, StringComparison.InvariantCultureIgnoreCase) &&
							!columnNames.Any(c => c.Equals(columnName, StringComparison.InvariantCultureIgnoreCase)))
						{
							columnNames.Add(columnName);
						}
					}
				}

				// Add the primary key column in the first position for nicety
				columnNames.Insert(0, primaryKeyColumnName);

				string tableName = this.Configuration.GetTableName(selectClause.Selector.Type);
				foreach (string columnName in columnNames)
				{
					this.SelectStatement.SourceFields.Add(new Column(tableName, columnName));
				}
			}

			base.VisitSelectClause(selectClause, queryModel);
		}
	}
}

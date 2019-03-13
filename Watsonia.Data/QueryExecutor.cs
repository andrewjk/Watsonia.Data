using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	internal class QueryExecutor<T> : IQueryExecutor
	{
		public Database Database { get; private set; }

		public DatabaseQuery<T> Query
		{
			get;
			internal set;
		}

		public QueryExecutor(Database database)
		{
			this.Database = database;
		}

		internal SelectStatement BuildSelectStatement(QueryModel queryModel)
		{
            // Add joins for fields in tables that haven't been joined explicitly
            // e.g. when using something like DB.Query<T>().Where(x => x.Item.Property == y)
            SelectSourceExpander.Visit(queryModel, this.Database, this.Database.Configuration);

            // Create the select statement
            var select = SelectStatementCreator.Visit(queryModel, this.Database.Configuration, true);

			// Add include paths if necessary
			if (!select.IsAggregate)
			{
				select.IncludePaths.AddRange(this.Query.IncludePaths);
			}

			// Add parameters if the source is a user-defined function
			if (select.Source.PartType == StatementPartType.UserDefinedFunction)
			{
				var function = (UserDefinedFunction)select.Source;
				function.Parameters.AddRange(this.Query.Parameters);
			}

			// Check whether we need to expand fields (if the select has no fields)
			// This will avoid the case where selecting fields from multiple tables with non-unique field
			// names (e.g. two tables with an ID field) fills the object with the wrong value
			if (select.SourceFields.Count == 0)
			{
				SelectFieldExpander.Visit(queryModel, select, this.Database.Configuration);
			}

			return select;
		}

		public IEnumerable<T2> ExecuteCollection<T2>(QueryModel queryModel)
		{
			var select = BuildSelectStatement(queryModel);
			return this.Database.LoadCollection<T2>(select);
		}

		public T2 ExecuteScalar<T2>(QueryModel queryModel)
		{
			return ExecuteCollection<T2>(queryModel).Single();
		}

		public T2 ExecuteSingle<T2>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			var sequence = ExecuteCollection<T2>(queryModel);
			return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
		}
	}
}

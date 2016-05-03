using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;

namespace Watsonia.Data
{
	internal class QueryExecutor<T> : IQueryExecutor
	{
		public Database Database
		{
			get;
			private set;
		}

		public DatabaseQuery<T> Query
		{
			get;
			internal set;
		}

		public QueryExecutor(Database database)
		{
			this.Database = database;
		}

		internal Select BuildSelectStatement(QueryModel queryModel)
		{
			// Create the select statement
			Select select = SelectStatementCreator.Visit(queryModel, this.Database.Configuration);
			select.IncludePaths.AddRange(this.Query.IncludePaths);

			// Add joins for fields in tables that haven't been joined explicitly
			// e.g. when using something like DB.Query<T>().Where(x => x.Item.Property == y)
			SelectSourceExpander.Visit(queryModel, select, this.Database.Configuration);

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

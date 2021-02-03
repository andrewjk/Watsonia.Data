using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Remotion.Linq;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	internal class QueryExecutor<T> : IAsyncQueryExecutor
	{
		public Database Database { get; private set; }

		public DatabaseQuery<T> Query { get; internal set; }

		public QueryExecutor(Database database)
		{
			this.Database = database;
		}

		// TODO: This needs to be moved to Watsonia.QueryBuilder
		internal SelectStatement BuildSelectStatement(QueryModel queryModel)
		{
			// Add joins for fields in tables that haven't been joined explicitly
			// e.g. when using something like DB.Query<T>().Where(x => x.Item.Property == y)
			SelectSourceExpander.Visit(queryModel, this.Database, this.Database.Configuration);

			// Create the select statement
			var select = StatementCreator.Visit(queryModel, new QueryMapper(this.Database.Configuration), true);

			// Add include paths if necessary
			if (!select.IsAggregate)
			{
				select.IncludePaths.AddRange(this.Query.IncludePaths);
			}

			// Add parameters if the source is a user-defined function
			if (select.Source.PartType == StatementPartType.UserDefinedFunction)
			{
				var function = (UserDefinedFunction)select.Source;
				function.Parameters.AddRange(this.Query.Parameters.Select(p => new Parameter(p.Name, p.Value)));
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
			var sequence = ExecuteCollection<T2>(queryModel);
			return sequence.Single();
		}

		public T2 ExecuteSingle<T2>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			var sequence = ExecuteCollection<T2>(queryModel);
			return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
		}

		public async Task<IList<T2>> ExecuteCollectionAsync<T2>(QueryModel queryModel)
		{
			var select = BuildSelectStatement(queryModel);
			return await this.Database.LoadCollectionAsync<T2>(select);
		}

		public async Task<T2> ExecuteScalarAsync<T2>(QueryModel queryModel)
		{
			var sequence = await ExecuteCollectionAsync<T2>(queryModel);
			return sequence.Single();
		}

		public async Task<T2> ExecuteSingleAsync<T2>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			var sequence = await ExecuteCollectionAsync<T2>(queryModel);
			return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
		}

#if NET5_0
		public async IAsyncEnumerable<T2> EnumerateCollectionAsync<T2>(QueryModel queryModel)
		{
			var select = BuildSelectStatement(queryModel);
			await foreach (var item in this.Database.EnumerateCollectionAsync<T2>(select))
			{
				yield return item;
			}
		}
#endif
	}
}

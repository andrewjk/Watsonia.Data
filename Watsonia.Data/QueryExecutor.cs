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
			set;
		}

		public QueryExecutor(Database database)
		{
			this.Database = database;
		}

		public IEnumerable<T2> ExecuteCollection<T2>(QueryModel queryModel)
		{
			// Create the select statement
			Select select = SelectStatementCreator.Visit(queryModel, this.Database.Configuration);
			select.IncludePaths.AddRange(this.Query.IncludePaths);

			// NOTE: I started down this track and then decided I didn't really like it
			//		 Seems like it's too bug-prone and will degrade performance when you can just add joins manually?
			//// Add joins for fields that don't already exist
			//SelectSourceExpander.Visit(queryModel, select, this.Database.Configuration);

			// And load the collection
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

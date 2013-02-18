using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using System.Linq.Expressions;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// A LINQ IQueryable query provider that executes database queries against a database.
	/// </summary>
	internal class DynamicEntityProvider : EntityProvider
	{
		private Database _database;

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicEntityProvider" /> class.
		/// </summary>
		/// <param name="database">The database execute queries against.</param>
		public DynamicEntityProvider(Database database)
			: base(FluentSqlLanguage.Default, new EntityMapping(database), DynamicQueryPolicy.Default)
		{
			_database = database;
		}

		/// <summary>
		/// Executes the query expression after first translating it and building an execution plan.
		/// </summary>
		/// <param name="expression">The query expression to execute.</param>
		/// <returns>The result of the query expression's execution.</returns>
		public override object Execute(Expression expression)
		{
			// NOTE: This is where the magic happens.  This method just exists so that I can easily find the entry point
			// without having to dig through class files
			return base.Execute(expression);
		}

		/// <summary>
		/// Creates the object that executes queries.
		/// </summary>
		/// <returns>The executor.</returns>
		protected override QueryExecutor CreateExecutor()
		{
			return new DynamicQueryExecutor(_database);
		}

		public override void DoConnected(Action action)
		{
			throw new NotImplementedException();
		}

		public override void DoTransacted(Action action)
		{
			throw new NotImplementedException();
		}

		public override int ExecuteCommand(string commandText)
		{
			throw new NotImplementedException();
		}
	}
}

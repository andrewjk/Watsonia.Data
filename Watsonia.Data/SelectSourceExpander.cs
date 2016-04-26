using Remotion.Linq;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	/// <summary>
	/// Adds joins for fields that aren't already selected in the main source.
	/// </summary>
	internal class SelectSourceExpander : RelinqExpressionVisitor
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

		private SelectSourceExpander(QueryModel queryModel, Select selectStatement, DatabaseConfiguration configuration)
		{
			this.QueryModel = queryModel;
			this.SelectStatement = selectStatement;
			this.Configuration = configuration;
		}

		public static void Visit(QueryModel queryModel, Select selectStatement, DatabaseConfiguration configuration)
		{
			var visitor = new SelectSourceExpander(queryModel, selectStatement, configuration);
			foreach (var clause in queryModel.BodyClauses)
			{
				if (clause is Remotion.Linq.Clauses.WhereClause)
				{
					visitor.Visit(((Remotion.Linq.Clauses.WhereClause)clause).Predicate);
				}
			}
		}
		
		protected override Expression VisitMember(MemberExpression expression)
		{
			if (this.Configuration.ShouldMapType(expression.Type))
			{
				// TODO: ?
			}

			return base.VisitMember(expression);
		}
	}
}

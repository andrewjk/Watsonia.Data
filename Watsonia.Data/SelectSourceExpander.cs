using Remotion.Linq;
using Remotion.Linq.Clauses;
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
				if (clause is WhereClause)
				{
					visitor.Visit(((WhereClause)clause).Predicate);
				}
				else if (clause is OrderByClause)
				{
					foreach (Ordering order in ((OrderByClause)clause).Orderings)
					{
						visitor.Visit(order.Expression);
					}
				}
			}
		}

		protected override Expression VisitMember(MemberExpression expression)
		{
			if (expression.Expression != null &&
				expression.Expression is MemberExpression &&
				this.Configuration.ShouldMapType(expression.Expression.Type) &&
				!expression.Expression.Type.IsEnum)
			{
				var subexpression = (MemberExpression)expression.Expression;

				string tableName = this.Configuration.GetTableName(subexpression.Type);

				bool foundTable = false;

				// Check the source
				if ((this.SelectStatement.Source is Table) &&
					((Table)this.SelectStatement.Source).Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
				{
					foundTable = true;
				}

				// Check joins
				if (!foundTable)
				{
					foreach (Join join in this.SelectStatement.SourceJoins)
					{
						if ((join.Left is Table) &&
							((Table)join.Left).Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
						{
							foundTable = true;
						}
						if ((join.Right is Table) &&
							((Table)join.Right).Name.Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
						{
							foundTable = true;
						}
					}
				}

				if (!foundTable)
				{
					// Add an outer join, so that we don't exclude any items from the source table
					string leftTableName = this.Configuration.GetTableName(subexpression.Expression.Type);
					string leftColumnName = this.Configuration.GetForeignKeyColumnName(subexpression.Expression.Type, subexpression.Type);
					string rightTableName = this.Configuration.GetTableName(subexpression.Type);
					string rightColumnName = this.Configuration.GetPrimaryKeyColumnName(subexpression.Type);
					this.SelectStatement.SourceJoins.Add(new Join(rightTableName, leftTableName, leftColumnName, rightTableName, rightColumnName) { JoinType = JoinType.Left });
				}
			}

			return base.VisitMember(expression);
		}
	}
}

using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
    /// <summary>
    /// Adds joins for fields that aren't already selected in the main source.
    /// </summary>
    internal class SelectSourceExpander : RelinqExpressionVisitor
    {
        private int _newJoinNumber = 1;

        private QueryModel QueryModel
        {
            get;
            set;
        }
        
        private Database Database
        {
            get;
            set;
        }

        private DatabaseConfiguration Configuration
        {
            get;
            set;
        }

        private SelectSourceExpander(QueryModel queryModel, Database database, DatabaseConfiguration configuration)
        {
            this.QueryModel = queryModel;
            this.Database = database;
            this.Configuration = configuration;
        }

		public static void Visit(QueryModel queryModel, Database database, DatabaseConfiguration configuration)
		{
			var visitor = new SelectSourceExpander(queryModel, database, configuration);

			// Copy the body clauses list with ToList() as we will be modifying it
			foreach (var clause in queryModel.BodyClauses.ToList())
			{
				if (clause is WhereClause whereClause)
				{
					whereClause.Predicate = visitor.Visit(whereClause.Predicate);
				}
				else if (clause is OrderByClause orderByClause)
				{
					foreach (var order in orderByClause.Orderings)
					{
						order.Expression = visitor.Visit(order.Expression);
					}
				}
			}

			// Do joins as well, after we've created any
			// TODO: This needs recursion for joins that we create in this go around
			foreach (var clause in queryModel.BodyClauses.ToList())
			{
				if (clause is JoinClause joinClause)
				{
					joinClause.OuterKeySelector = visitor.Visit(joinClause.OuterKeySelector);
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

                JoinClause existingJoin = null;
                foreach (var clause in this.QueryModel.BodyClauses)
                {
					if (clause is JoinClause joinClause)
					{
						if (joinClause.OuterKeySelector.Type == subexpression.Type)
						{
							existingJoin = joinClause;
							break;
						}
					}
				}

                if (existingJoin != null)
                {
                    // Change the expression to point to the previously existing join
                    expression = MemberExpression.MakeMemberAccess(existingJoin.InnerKeySelector, expression.Member);
                }
                else
                {
                    // Get a new DatabaseQuery<T> by calling the Database.Query<T> method
                    var databaseQueryMethod = typeof(Database).GetMethod("Query");
                    var databaseQueryGeneric = databaseQueryMethod.MakeGenericMethod(subexpression.Type);
                    var databaseQueryItem = databaseQueryGeneric.Invoke(this.Database, null);

                    // Build the item name based on what's come before
                    var itemName = $"j_" + _newJoinNumber++;

                    // Build the join sequences and keys
                    Expression innerSequence = Expression.Constant(databaseQueryItem);
                    Expression outerKeySelector = subexpression;
                    Expression innerKeySelector = new QuerySourceReferenceExpression(new MainFromClause(itemName, subexpression.Type, innerSequence));

                    // Create the join and add it to the new body clauses
					// Insert at the start in case we're creating multiple new joins for deep-nested expressions
                    var newJoin = new JoinClause(itemName, expression.Expression.Type, innerSequence, outerKeySelector, innerKeySelector);
                    this.QueryModel.BodyClauses.Insert(0, newJoin);

                    // Change the expression to point to the newly created join
                    expression = MemberExpression.MakeMemberAccess(innerKeySelector, expression.Member);
                }
            }

            return base.VisitMember(expression);
        }
    }
}

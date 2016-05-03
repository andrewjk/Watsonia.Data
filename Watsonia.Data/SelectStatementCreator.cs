using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	/// <summary>
	/// Converts QueryModels into Select statements for passing to the database.
	/// </summary>
	internal class SelectStatementCreator : QueryModelVisitorBase
	{
		private DatabaseConfiguration Configuration
		{
			get;
			set;
		}

		private Select SelectStatement
		{
			get;
			set;
		}

		private SelectStatementCreator(DatabaseConfiguration configuration)
		{
			this.Configuration = configuration;
			this.SelectStatement = new Select();
		}

		public static Select Visit(QueryModel queryModel, DatabaseConfiguration configuration)
		{
			var visitor = new SelectStatementCreator(configuration);
			queryModel.Accept(visitor);
			return visitor.SelectStatement;
		}

		public static ConditionCollection VisitStatementConditions<T>(Expression<Func<T, bool>> conditions, DatabaseConfiguration configuration)
		{
			// Build a new query
			var queryParser = QueryParser.CreateDefault();
			var queryExecutor = new QueryExecutor<T>(null);
			var query = new DatabaseQuery<T>(queryParser, queryExecutor);

			// Select from the query with the conditions so that we have a sequence for Re-Linq to parse
			var select = (from t in query select t).Where(conditions);
			var expression = Expression.Constant(select, query.GetType());
			QueryModel queryModel = queryParser.GetParsedQuery(expression);

			// Get the conditions from the query model
			var visitor = new SelectStatementCreator(configuration);
			visitor.SelectStatement = new Select();
			queryModel.Accept(visitor);
			return visitor.SelectStatement.Conditions;
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
			else
			{
				StatementPart fields = StatementPartCreator.Visit(queryModel, selectClause.Selector, this.Configuration);
				this.SelectStatement.SourceFields.Add((SourceExpression)fields);
			}

			base.VisitSelectClause(selectClause, queryModel);
		}

		public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
		{
			this.SelectStatement.Source = new Table(this.Configuration.GetTableName(fromClause.ItemType));

			base.VisitMainFromClause(fromClause, queryModel);
		}

		public override void VisitJoinClause(JoinClause joinClause, QueryModel queryModel, int index)
		{
			// TODO: This seems heavy...
			// TODO: And like it's only going to deal with certain types of joins
			Table table = (Table)StatementPartCreator.Visit(queryModel, joinClause.InnerSequence, this.Configuration);
			Column leftColumn = (Column)StatementPartCreator.Visit(queryModel, joinClause.OuterKeySelector, this.Configuration);
			Column rightColumn = (Column)StatementPartCreator.Visit(queryModel, joinClause.InnerKeySelector, this.Configuration);

			this.SelectStatement.SourceJoins.Add(new Join(table, leftColumn, rightColumn));

			base.VisitJoinClause(joinClause, queryModel, index);
		}

		public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
		{
			Column column = (Column)StatementPartCreator.Visit(queryModel, ordering.Expression, this.Configuration);

			switch (ordering.OrderingDirection)
			{
				case OrderingDirection.Asc:
				{
					this.SelectStatement.OrderByFields.Add(new OrderByExpression(column, OrderDirection.Ascending));
					break;
				}
				case OrderingDirection.Desc:
				{
					this.SelectStatement.OrderByFields.Add(new OrderByExpression(column, OrderDirection.Descending));
					break;
				}
				default:
				{
					throw new InvalidOperationException(string.Format("Invalid ordering direction: {0}", ordering.OrderingDirection));
				}
			}

			base.VisitOrdering(ordering, queryModel, orderByClause, index);
		}

		public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
		{
			if (resultOperator is AnyResultOperator)
			{
				this.SelectStatement.IsAny = true;
				return;
			}

			if (resultOperator is AllResultOperator)
			{
				this.SelectStatement.IsAll = true;
				return;
			}

			if (resultOperator is FirstResultOperator)
			{
				this.SelectStatement.Limit = 1;
				return;
			}

			if (resultOperator is LastResultOperator)
			{
				this.SelectStatement.Limit = 1;
				foreach (OrderByExpression orderBy in this.SelectStatement.OrderByFields)
				{
					orderBy.Direction = (orderBy.Direction == OrderDirection.Ascending) ? OrderDirection.Descending : OrderDirection.Ascending;
				}
				return;
			}

			if (resultOperator is CountResultOperator || resultOperator is LongCountResultOperator)
			{
				// Throw an exception if there is more than one field
				if (this.SelectStatement.SourceFields.Count > 1)
				{
					throw new InvalidOperationException("can't count multiple fields");
				}

				// Count the first field
				if (this.SelectStatement.SourceFields.Count == 0)
				{
					this.SelectStatement.SourceFields.Add(new Aggregate(AggregateType.Count, new Column("*")));
				}
				else
				{
					this.SelectStatement.SourceFields[0] = new Aggregate(AggregateType.Count, (Field)this.SelectStatement.SourceFields[0]);
				}

				return;
			}

			if (resultOperator is SumResultOperator)
			{
				// Throw an exception if there is not one field
				if (this.SelectStatement.SourceFields.Count != 1)
				{
					throw new InvalidOperationException("can't sum multiple or no fields");
				}

				// Sum the first field
				this.SelectStatement.SourceFields[0] = new Aggregate(AggregateType.Sum, (Field)this.SelectStatement.SourceFields[0]);

				return;
			}

			if (resultOperator is DistinctResultOperator)
			{
				this.SelectStatement.IsDistinct = true;
				return;
			}

			if (resultOperator is TakeResultOperator)
			{
				var exp = ((TakeResultOperator)resultOperator).Count;
				if (exp.NodeType == ExpressionType.Constant)
				{
					this.SelectStatement.Limit = (int)((ConstantExpression)exp).Value;
				}
				else
				{
					throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
				}
				return;
			}

			if (resultOperator is SkipResultOperator)
			{
				var exp = ((SkipResultOperator)resultOperator).Count;
				if (exp.NodeType == ExpressionType.Constant)
				{
					this.SelectStatement.StartIndex = (int)((ConstantExpression)exp).Value;
				}
				else
				{
					throw new NotSupportedException("Currently not supporting methods or variables in the Skip or Take clause.");
				}
				return;
			}

			if (resultOperator is ReverseResultOperator)
			{
				foreach (OrderByExpression orderBy in this.SelectStatement.OrderByFields)
				{
					orderBy.Direction = (orderBy.Direction == OrderDirection.Ascending) ? OrderDirection.Descending : OrderDirection.Ascending;
				}
				return;
			}

			base.VisitResultOperator(resultOperator, queryModel, index);
		}

		public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
		{
			StatementPart whereStatement = StatementPartCreator.Visit(queryModel, whereClause.Predicate, this.Configuration);
			ConditionExpression condition;
			if (whereStatement is ConditionExpression)
			{
				condition = (ConditionExpression)whereStatement;
			}
			else if (whereStatement is UnaryOperation && ((UnaryOperation)whereStatement).Expression is ConditionExpression)
			{
				condition = (ConditionExpression)((UnaryOperation)whereStatement).Expression;
			}
			else if (whereStatement is UnaryOperation && ((UnaryOperation)whereStatement).Expression is Column)
			{
				var unary = (UnaryOperation)whereStatement;
				var column = (Column)unary.Expression;
				condition = new Condition(column, SqlOperator.Equals, new ConstantPart(unary.Operator != UnaryOperator.Not));
			}
			else if (whereStatement is ConstantPart && ((ConstantPart)whereStatement).Value is bool)
			{
				bool value = (bool)((ConstantPart)whereStatement).Value;
				condition = new Condition() { Field = new ConstantPart(value), Operator = SqlOperator.Equals, Value = new ConstantPart(true) };
			}
			else if (whereStatement is Column && ((Column)whereStatement).PropertyType == typeof(bool))
			{
				condition = new Condition((Column)whereStatement, SqlOperator.Equals, new ConstantPart(true));
			}
			else
			{
				throw new InvalidOperationException();
			}
			this.SelectStatement.Conditions.Add(condition);

			base.VisitWhereClause(whereClause, queryModel, index);
		}
	}
}

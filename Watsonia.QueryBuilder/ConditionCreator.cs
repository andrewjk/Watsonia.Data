using System;
using System.Linq.Expressions;

namespace Watsonia.QueryBuilder
{
	/// <summary>
	/// Creates statement parts from expressions.
	/// </summary>
	public static class ConditionCreator
	{
		/// <summary>
		/// Visits the statement conditions.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="conditions">The conditions.</param>
		/// <param name="mapper">The mapper.</param>
		/// <param name="aliasTables">if set to <c>true</c> [alias tables].</param>
		/// <param name="sourceAlias">The alias of the source table, if any.</param>
		/// <returns></returns>
		public static ConditionCollection VisitStatementConditions<T>(
			Expression<Func<T, bool>> conditions,
			DatabaseMapper mapper,
			bool aliasTables,
			string sourceAlias = null)
		{
			if (conditions == null)
			{
				return new ConditionCollection();
			}

			var part = StatementPartCreator.Visit(conditions.Body, mapper, aliasTables, typeof(T), sourceAlias);
			var collection = new ConditionCollection();
			collection.Add(ToCondition(part));
			return collection;
		}

		/// <summary>
		/// Converts a statement part into a condition.
		/// </summary>
		/// <param name="part">The part.</param>
		/// <returns></returns>
		public static ConditionExpression ToCondition(StatementPart part)
		{
			if (part is ConditionExpression condition)
			{
				return condition;
			}

			if (part is UnaryOperation unary)
			{
				if (unary.Expression is ConditionExpression innerCondition)
				{
					if (unary.Operator == UnaryOperator.Not)
					{
						innerCondition.Not = true;
					}
					return innerCondition;
				}

				if (unary.Expression is Column innerColumn && innerColumn.PropertyType == typeof(bool))
				{
					return new Condition(innerColumn, SqlOperator.Equals, new ConstantPart(unary.Operator != UnaryOperator.Not));
				}
			}

			if (part is ConstantPart constant && constant.Value is bool boolean)
			{
				return new Condition(new ConstantPart(boolean), SqlOperator.Equals, new ConstantPart(true));
			}

			if (part is Column column && column.PropertyType == typeof(bool))
			{
				return new Condition(column, SqlOperator.Equals, new ConstantPart(true));
			}

			if (part is ConditionalCase conditional)
			{
				// A conditional expression used as a predicate (e.g. cond ? a : b)
				return new Condition(conditional, SqlOperator.Equals, new ConstantPart(true));
			}

			throw new InvalidOperationException("The condition could not be translated to a statement condition");
		}
	}
}

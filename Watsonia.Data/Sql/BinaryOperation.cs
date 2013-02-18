using System;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// An operation with a binary operator e.g. 1 + 2.
	/// </summary>
	public sealed class BinaryOperation : StatementPart
	{
		/// <summary>
		/// Gets the type of the statement part.
		/// </summary>
		/// <value>
		/// The type of the part.
		/// </value>
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.BinaryOperation;
			}
		}

		/// <summary>
		/// Gets or sets the expression on the left of the operator.
		/// </summary>
		/// <value>
		/// The left expression.
		/// </value>
		public SourceExpression LeftExpression
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets or sets the operator.
		/// </summary>
		/// <value>
		/// The operator.
		/// </value>
		public BinaryOperator Operator
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets or sets the expression on the right of the operator.
		/// </summary>
		/// <value>
		/// The right expression.
		/// </value>
		public SourceExpression RightExpression
		{
			get;
			internal set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BinaryOperation" /> class.
		/// </summary>
		internal BinaryOperation()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BinaryOperation" /> class.
		/// </summary>
		/// <param name="leftExpression">The expression on the left of the operator.</param>
		/// <param name="op">The operator.</param>
		/// <param name="rightExpression">The expression on the right of the operator.</param>
		public BinaryOperation(SourceExpression leftExpression, BinaryOperator op, SourceExpression rightExpression)
		{
			this.LeftExpression = leftExpression;
			this.Operator = op;
			this.RightExpression = rightExpression;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return this.LeftExpression.ToString() + " " + this.Operator.ToString() + " " + this.RightExpression.ToString();
		}
	}
}

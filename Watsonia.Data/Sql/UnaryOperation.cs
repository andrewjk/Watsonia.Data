using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// An operation with a single operator e.g. negative 1.
	/// </summary>
	public sealed class UnaryOperation : StatementPart
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
				return StatementPartType.UnaryOperation;
			}
		}

		/// <summary>
		/// Gets or sets the operator.
		/// </summary>
		/// <value>
		/// The operator.
		/// </value>
		public UnaryOperator Operator
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the expression.
		/// </summary>
		/// <value>
		/// The expression.
		/// </value>
		public StatementPart Expression
		{
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnaryOperation" /> class.
		/// </summary>
		internal UnaryOperation()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnaryOperation" /> class.
		/// </summary>
		/// <param name="op">The operator.</param>
		/// <param name="expression">The expression.</param>
		public UnaryOperation(UnaryOperator op, StatementPart expression)
		{
			this.Operator = op;
			this.Expression = expression;
		}
	}
}

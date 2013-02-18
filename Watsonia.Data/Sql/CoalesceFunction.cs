using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// Returns the first non-null expression.
	/// </summary>
	public sealed class CoalesceFunction : Field
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
				return StatementPartType.CoalesceFunction;
			}
		}

		/// <summary>
		/// Gets or sets the first expression.
		/// </summary>
		/// <value>
		/// The first expression.
		/// </value>
		public SourceExpression First
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the second expression.
		/// </summary>
		/// <value>
		/// The second expression.
		/// </value>
		public SourceExpression Second
		{
			get;
			set;
		}

		// TODO: Do coalesce properly
		///// <summary>
		///// Gets the expressions.
		///// </summary>
		///// <value>
		///// The expressions.
		///// </value>
		//public SourceExpression[] Expressions
		//{
		//	get;
		//	internal set;
		//}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoalesceFunction" /> class.
		/// </summary>
		internal CoalesceFunction()
		{
		}

		///// <summary>
		///// Initializes a new instance of the <see cref="CoalesceFunction" /> class.
		///// </summary>
		///// <param name="expressions">The expressions.</param>
		//public CoalesceFunction(params SourceExpression[] expressions)
		//{
		//	this.Expressions = expressions;
		//}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoalesceFunction" /> class.
		/// </summary>
		/// <param name="first">The first.</param>
		/// <param name="second">The second.</param>
		public CoalesceFunction(SourceExpression first, SourceExpression second)
		{
			this.First = first;
			this.Second = second;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return "Coalesce(" + this.First + ", " + this.Second + ")";
		}
	}
}

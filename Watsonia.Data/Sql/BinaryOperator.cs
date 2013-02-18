using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// An operator that is performed on two expressions.
	/// </summary>
	public enum BinaryOperator
	{
		/// <summary>
		/// Add the expressions together.
		/// </summary>
		Add,
		/// <summary>
		/// Subtract the right expression from the left.
		/// </summary>
		Subtract,
		/// <summary>
		/// Multiply the expressions together.
		/// </summary>
		Multiply,
		/// <summary>
		/// Divide the left expression by the right.
		/// </summary>
		Divide,
		/// <summary>
		/// Divide the left expression by the right and return the remainder.
		/// </summary>
		Remainder,
		/// <summary>
		/// Perform an exclusive OR operation on the expressions.
		/// </summary>
		ExclusiveOr,
		/// <summary>
		/// Perform a left shift operation on the expressions.
		/// </summary>
		LeftShift,
		/// <summary>
		/// Perform a right shift operation on the expressions.
		/// </summary>
		RightShift,
	}
}

using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	/// <summary>
	/// An operator that is performed on a single expression.
	/// </summary>
	public enum UnaryOperator
	{
		/// <summary>
		/// Makes the expression logically opposite.
		/// </summary>
		Not,
		/// <summary>
		/// Negates the expression.
		/// </summary>
		Negate,
	}
}

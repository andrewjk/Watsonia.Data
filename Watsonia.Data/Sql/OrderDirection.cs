using System;
using System.Linq;

namespace Watsonia.Data
{
	/// <summary>
	/// The direction in which an expression is ordered.
	/// </summary>
	public enum OrderDirection
	{
		/// <summary>
		/// The expression is ordered from lowest to highest.
		/// </summary>
		Ascending,
		/// <summary>
		/// The expression is ordered from highest to lowest.
		/// </summary>
		Descending,
	}
}

using System;
using System.Linq;

namespace Watsonia.Data
{
	public enum SqlOperator
	{
		Equals,
		NotEquals,
		IsLessThan,
		IsLessThanOrEqualTo,
		IsGreaterThan,
		IsGreaterThanOrEqualTo,
		IsIn,
		Contains,
		StartsWith,
		EndsWith
	}
}

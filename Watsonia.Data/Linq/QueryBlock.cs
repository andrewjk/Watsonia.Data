using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using IQToolkit.Data.Common;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// A query block that is part of a LINQ command execution plan.
	/// </summary>
	/// <remarks>
	/// This is required because the IQ Toolkit assumes that you want strings when it builds an execution plan whereas we
	/// want expressions that we can turn into Selects.  We can't just replace the QueryBlocks with a new class because
	/// they get passed around so instead we just inherit from them and add a new Expression property.
	/// </remarks>
	internal class QueryBlock : QueryCommand
	{
		public Expression Expression
		{
			get;
			private set;
		}

		public QueryBlock(string commandText, Expression expression, IEnumerable<QueryParameter> parameters)
			: base(commandText, parameters)
		{
			this.Expression = expression;
		}
	}
}

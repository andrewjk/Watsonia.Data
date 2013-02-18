using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using IQToolkit.Data.Common;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// Gathers QueryBlocks from a LINQ expression.
	/// </summary>
	internal class QueryBlockGatherer : DbExpressionVisitor
	{
		List<QueryBlock> _blocks = new List<QueryBlock>();

		private List<QueryBlock> Blocks
		{
			get
			{
				return _blocks;
			}
		}

		/// <summary>
		/// Gathers QueryBlocks from the specified expression.
		/// </summary>
		/// <param name="expression">The expression.</param>
		/// <returns>A read-only collection of QueryBlocks.</returns>
		public static ReadOnlyCollection<QueryBlock> Gather(Expression expression)
		{
			var gatherer = new QueryBlockGatherer();
			gatherer.Visit(expression);
			return gatherer.Blocks.AsReadOnly();
		}

		/// <summary>
		/// Visits the constant expression.
		/// </summary>
		/// <param name="c">The constant expression.</param>
		/// <returns></returns>
		protected override Expression VisitConstant(ConstantExpression c)
		{
			QueryBlock qc = c.Value as QueryBlock;
			if (qc != null)
			{
				this.Blocks.Add(qc);
			}
			return c;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Query.Expressions;

namespace Watsonia.Data.Query
{
	internal class QueryCommandGatherer : DbExpressionVisitor
	{
		private readonly List<QueryCommand> _commands = new List<QueryCommand>();

		public static ReadOnlyCollection<QueryCommand> Gather(Expression expression)
		{
			var gatherer = new QueryCommandGatherer();
			gatherer.Visit(expression);
			return gatherer._commands.AsReadOnly();
		}

		protected override Expression VisitConstant(ConstantExpression c)
		{
			QueryCommand qc = c.Value as QueryCommand;
			if (qc != null)
			{
				this._commands.Add(qc);
			}
			return c;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query
{
	internal sealed class ConstructorBindResult
	{
		public NewExpression Expression
		{
			get;
			private set;
		}

		public ReadOnlyCollection<EntityAssignment> Remaining
		{
			get;
			private set;
		}

		public ConstructorBindResult(NewExpression expression, IEnumerable<EntityAssignment> remaining)
		{
			this.Expression = expression;
			this.Remaining = remaining.ToReadOnly();
		}
	}
}

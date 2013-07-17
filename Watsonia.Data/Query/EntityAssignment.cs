using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query
{
	internal sealed class EntityAssignment
	{
		public MemberInfo Member
		{
			get;
			private set;
		}

		public Expression Expression
		{
			get;
			private set;
		}

		public EntityAssignment(MemberInfo member, Expression expression)
		{
			this.Member = member;
			System.Diagnostics.Debug.Assert(expression != null);
			this.Expression = expression;
		}
	}
}

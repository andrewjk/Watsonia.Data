using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class Insert<T> : Statement
	{
		private readonly List<Tuple<PropertyInfo, object>> _setValues = new List<Tuple<PropertyInfo, object>>();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.GenericInsert;
			}
		}

		public Type Target
		{
			get;
			internal set;
		}

		public List<Tuple<PropertyInfo, object>> SetValues
		{
			get
			{
				return _setValues;
			}
		}

		public Expression Conditions
		{
			get;
			private set;
		}

		internal Insert()
		{
			this.Target = typeof(T);
		}

		public Insert<T> Value(Expression<Func<T, object>> property, object value)
		{
			this.SetValues.Add(new Tuple<PropertyInfo, object>(FuncToPropertyInfo(property), value));
			return this;
		}

		// TODO: This should go into a helper
		private static PropertyInfo FuncToPropertyInfo(Expression<Func<T, object>> selector)
		{
			if (selector.Body is MemberExpression)
			{
				MemberExpression mex = (MemberExpression)selector.Body;
				return (PropertyInfo)mex.Member;
			}
			else if (selector.Body is UnaryExpression)
			{
				// Throw away Converts
				UnaryExpression uex = (UnaryExpression)selector.Body;
				if (uex.Operand is MemberExpression)
				{
					MemberExpression mex = (MemberExpression)uex.Operand;
					return (PropertyInfo)mex.Member;
				}
			}

			throw new InvalidOperationException();
		}

		public Insert CreateStatement(DatabaseConfiguration configuration)
		{
			Insert insert = new Insert();
			insert.Target = new Table(configuration.GetTableName(this.Target));
			insert.SetValues.AddRange(this.SetValues.Select(sv => new SetValue(new Column(configuration.GetColumnName(sv.Item1)), sv.Item2)));
			return insert;
		}
	}
}

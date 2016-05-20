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
	public sealed class Update<T> : Statement
	{
		private readonly List<Tuple<PropertyInfo, object>> _setValues = new List<Tuple<PropertyInfo, object>>();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.GenericUpdate;
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

		public Expression<Func<T, bool>> Conditions
		{
			get;
			private set;
		}

		internal Update()
		{
			this.Target = typeof(T);
		}

		public static Update<T> Table()
		{
			return new Update<T>() { Target = typeof(T) };
		}

		public Update<T> Set(Expression<Func<T, object>> property, object value)
		{
			this.SetValues.Add(new Tuple<PropertyInfo, object>(FuncToPropertyInfo(property), value));
			return this;
		}

		public Update<T> Where(Expression<Func<T, bool>> condition)
		{
			this.Conditions = condition;
			return this;
		}

		public Update<T> And(Expression<Func<T, bool>> condition)
		{
			Expression combined = this.Conditions.Body.AndAlso(condition.Body);
			combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
			this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
			return this;
		}

		public Update<T> Or(Expression<Func<T, bool>> condition)
		{
			Expression combined = this.Conditions.Body.OrElse(condition.Body);
			combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
			this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
			return this;
		}

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

		public Update CreateStatement(DatabaseConfiguration configuration)
		{
			Update update = new Update();
			update.Target = new Table(configuration.GetTableName(this.Target));
			update.SetValues.AddRange(this.SetValues.Select(sv => new SetValue(new Column(configuration.GetColumnName(sv.Item1)), sv.Item2)));
			update.Conditions.Add(SelectStatementCreator.VisitStatementConditions<T>(this.Conditions, configuration, false));
			return update;
		}
	}
}

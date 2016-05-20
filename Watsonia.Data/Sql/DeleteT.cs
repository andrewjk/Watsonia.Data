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
	public sealed class Delete<T> : Statement
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.GenericDelete;
			}
		}

		public Type Target
		{
			get;
			internal set;
		}

		public Expression<Func<T, bool>> Conditions
		{
			get;
			private set;
		}

		internal Delete()
		{
			this.Target = typeof(T);
		}

		public static Delete<T> From()
		{
			return new Delete<T>() { Target = typeof(T) };
		}

		public Delete<T> Where(Expression<Func<T, bool>> condition)
		{
			this.Conditions = condition;
			return this;
		}

		public Delete<T> And(Expression<Func<T, bool>> condition)
		{
			Expression combined = this.Conditions.Body.AndAlso(condition.Body);
			combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
			this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
			return this;
		}

		public Delete<T> Or(Expression<Func<T, bool>> condition)
		{
			Expression combined = this.Conditions.Body.OrElse(condition.Body);
			combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
			this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
			return this;
		}

		public Delete CreateStatement(DatabaseConfiguration configuration)
		{
			Delete delete = new Delete();
			delete.Target = new Table(configuration.GetTableName(this.Target));
			delete.Conditions.Add(SelectStatementCreator.VisitStatementConditions<T>(this.Conditions, configuration, false));
			return delete;
		}
	}
}

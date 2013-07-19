using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Query;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	internal sealed class Insert<T> : Statement
	{
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

		public Expression Conditions
		{
			get;
			private set;
		}

		internal Insert()
		{
			this.Target = typeof(T);
		}

		public static Insert<T> From()
		{
			return new Insert<T>() { Target = typeof(T) };
		}

		public Insert<T> Where(Expression<Func<T, bool>> condition)
		{
			this.Conditions = condition.Body;
			return this;
		}

		public Insert<T> And(Expression<Func<T, bool>> condition)
		{
			this.Conditions = this.Conditions.AndAlso(condition.Body);
			return this;
		}

		public Insert<T> Or(Expression<Func<T, bool>> condition)
		{
			this.Conditions = this.Conditions.OrElse(condition.Body);
			return this;
		}

		public Insert CreateStatement(DatabaseConfiguration configuration)
		{
			Insert insert = new Insert();
			insert.Target = new Table(configuration.GetTableName(this.Target));
			return insert;
		}
	}
}

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
	public sealed class DeleteStatement<T> : Statement
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
			internal set;
		}

		internal DeleteStatement()
		{
			this.Target = typeof(T);
		}

		public DeleteStatement CreateStatement(DatabaseConfiguration configuration)
		{
			var delete = new DeleteStatement();
			delete.Target = new Table(configuration.GetTableName(this.Target));
			delete.Conditions.Add(SelectStatementCreator.VisitStatementConditions<T>(this.Conditions, configuration, false));
			return delete;
		}
	}
}

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
	public sealed class UpdateStatement<T> : Statement
	{
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

		public List<Tuple<PropertyInfo, object>> SetValues { get; } = new List<Tuple<PropertyInfo, object>>();

		public Expression<Func<T, bool>> Conditions
		{
			get;
			internal set;
		}

		internal UpdateStatement()
		{
			this.Target = typeof(T);
		}

		public UpdateStatement CreateStatement(DatabaseConfiguration configuration)
		{
			var update = new UpdateStatement();
			update.Target = new Table(configuration.GetTableName(this.Target));
			update.SetValues.AddRange(this.SetValues.Select(sv => new SetValue(new Column(configuration.GetColumnName(sv.Item1)), sv.Item2)));
			update.Conditions.Add(SelectStatementCreator.VisitStatementConditions<T>(this.Conditions, configuration, false));
			return update;
		}
	}
}

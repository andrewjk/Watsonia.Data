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
	public sealed class InsertStatement<T> : Statement
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

		public List<Tuple<PropertyInfo, object>> SetValues
		{
			get;
			private set;
		}

		public Expression Conditions
		{
			get;
			private set;
		}

		internal InsertStatement()
		{
			this.Target = typeof(T);
			this.SetValues = new List<Tuple<PropertyInfo, object>>();
		}

		public InsertStatement CreateStatement(DatabaseConfiguration configuration)
		{
			var insert = new InsertStatement();
			insert.Target = new Table(configuration.GetTableName(this.Target));
			insert.SetValues.AddRange(this.SetValues.Select(sv => new SetValue(new Column(configuration.GetColumnName(sv.Item1)), sv.Item2)));
			return insert;
		}
	}
}

using System.Collections.Generic;
using Watsonia.Data.Sql;
using System;
using System.Linq;

namespace Watsonia.Data
{
	public sealed class Delete : Statement
	{
		private readonly List<Condition> _conditions = new List<Condition>();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Delete;
			}
		}

		public Table Target
		{
			get;
			set;
		}

		public IList<Condition> Conditions
		{
			get
			{
				return _conditions;
			}
		}

		public static Delete From(string tableName)
		{
			return new Delete(tableName);
		}

		private Delete(string tableName)
		{
			this.Target = new Table(tableName);
		}

		public Delete Where(bool all)
		{
			if (all)
			{
				Condition newCondition = new Condition();
				newCondition.Field = new ConstantPart(true);
				newCondition.Values.Add(new ConstantPart(true));
				this.Conditions.Add(newCondition);
			}
			return this;
		}

		public Delete Where(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values));
			return this;
		}

		public Delete WhereNot(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values) { Not = true });
			return this;
		}

		public Delete And(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values) { Relationship = ConditionRelationship.And });
			return this;
		}

		public Delete Or(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values) { Relationship = ConditionRelationship.Or });
			return this;
		}
	}
}

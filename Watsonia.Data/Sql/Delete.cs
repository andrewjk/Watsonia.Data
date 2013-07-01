using System.Collections.Generic;
using Watsonia.Data.Sql;
using System;
using System.Linq;

namespace Watsonia.Data
{
	public sealed class Delete : Statement
	{
		private readonly List<ConditionExpression> _conditions = new List<ConditionExpression>();

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

		public IList<ConditionExpression> Conditions
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
				newCondition.Value = new ConstantPart(true);
				this.Conditions.Add(newCondition);
			}
			return this;
		}

		public Delete Where(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value));
			return this;
		}

		public Delete WhereNot(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Not = true });
			return this;
		}

		public Delete And(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.And });
			return this;
		}

		public Delete Or(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.Or });
			return this;
		}
	}
}

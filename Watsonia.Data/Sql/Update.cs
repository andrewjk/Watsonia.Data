using System;
using System.Collections.Generic;
using System.Linq;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class Update : Statement
	{
		private readonly List<SetValue> _setValues = new List<SetValue>();
		private readonly List<ConditionExpression> _conditions = new List<ConditionExpression>();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Update;
			}
		}

		public Table Target
		{
			get;
			set;
		}

		public List<SetValue> SetValues
		{
			get
			{
				return _setValues;
			}
		}

		public List<ConditionExpression> Conditions
		{
			get
			{
				return _conditions;
			}
		}

		internal Update()
		{
		}

		public static Update Table(string tableName)
		{
			return Update.Table(new Table(tableName));
		}

		public static Update Table(Table table)
		{
			return new Update() { Target = table };
		}

		public static Update<T> Table<T>()
		{
			return new Update<T>();
		}

		public Update Set(string columnName, object value)
		{
			this.SetValues.Add(new SetValue(columnName, value));
			return this;
		}

		public Update Where(bool all)
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

		public Update Where(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value));
			return this;
		}

		public Update WhereNot(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Not = true });
			return this;
		}

		public Update And(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.And });
			return this;
		}

		public Update Or(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.Or });
			return this;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections;
using Watsonia.Data.Sql;
using System.Text;

namespace Watsonia.Data
{
	public class Condition : StatementPart
	{
		private readonly List<StatementPart> _values = new List<StatementPart>();
		private readonly ConditionCollection _subConditions = new ConditionCollection();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Condition;
			}
		}

		public ConditionRelationship Relationship
		{
			get;
			set;
		}

		public StatementPart Field
		{
			get;
			set;
		}

		public SqlOperator Operator
		{
			get;
			set;
		}

		public List<StatementPart> Values
		{
			get
			{
				return _values;
			}
		}

		public bool Not
		{
			get;
			set;
		}

		public List<Condition> SubConditions
		{
			get
			{
				return _subConditions;
			}
		}

		internal Condition()
		{
		}

		// TODO: Make these static i.e. public static Condition Where(...) ??
		public Condition(string fieldName, SqlOperator op, params object[] values)
		{
			this.Field = new Column(fieldName);
			this.Operator = op;
			AddValues(values);
		}

		public Condition(string tableName, string fieldName, SqlOperator op, params object[] values)
		{
			this.Field = new Column(tableName, fieldName);
			this.Operator = op;
			AddValues(values);
		}

		public Condition(Column column, SqlOperator op, params object[] values)
		{
			this.Field = column;
			this.Operator = op;
			AddValues(values);
		}

		public Condition(params Condition[] subConditions)
		{
			this.SubConditions.AddRange(subConditions);
		}

		public static Condition Where(string fieldName, SqlOperator op, params object[] values)
		{
			return new Condition(fieldName, op, values);
		}

		public static Condition Where(string tableName, string fieldName, SqlOperator op, params object[] values)
		{
			return new Condition(tableName, fieldName, op, values);
		}

		public static Condition Where(params Condition[] subConditions)
		{
			return new Condition(subConditions);
		}

		public static Condition Or(string fieldName, SqlOperator op, params object[] values)
		{
			return new Condition(fieldName, op, values) { Relationship = ConditionRelationship.Or };
		}

		public static Condition Or(string tableName, string fieldName, SqlOperator op, params object[] values)
		{
			return new Condition(tableName, fieldName, op, values) { Relationship = ConditionRelationship.Or };
		}

		public static Condition Or(params Condition[] subConditions)
		{
			return new Condition(subConditions) { Relationship = ConditionRelationship.Or };
		}

		public static Condition And(string fieldName, SqlOperator op, params object[] values)
		{
			return new Condition(fieldName, op, values) { Relationship = ConditionRelationship.And };
		}

		public static Condition And(string tableName, string fieldName, SqlOperator op, params object[] values)
		{
			return new Condition(tableName, fieldName, op, values) { Relationship = ConditionRelationship.And };
		}

		public static Condition And(params Condition[] subConditions)
		{
			return new Condition(subConditions) { Relationship = ConditionRelationship.And };
		}

		private void AddValues(params object[] values)
		{
			if (values == null)
			{
				this.Values.Add(new ConstantPart(null));
				return;
			}

			foreach (object val in values)
			{
				if (val is IEnumerable && !(val is string))
				{
					foreach (object subval in (IEnumerable)val)
					{
						if (subval is StatementPart)
						{
							this.Values.Add((StatementPart)subval);
						}
						else
						{
							this.Values.Add(new ConstantPart(subval));
						}
					}
				}
				else
				{
					if (val is StatementPart)
					{
						this.Values.Add((StatementPart)val);
					}
					else
					{
						this.Values.Add(new ConstantPart(val));
					}
				}
			}
		}

		public override string ToString()
		{
			if (this.SubConditions.Count > 0)
			{
				return this.SubConditions.ToString();
			}
			else
			{
				return string.Format("{0} {1} {2}", this.Field, this.Operator, string.Join(", ", this.Values));
			}
		}
	}
}

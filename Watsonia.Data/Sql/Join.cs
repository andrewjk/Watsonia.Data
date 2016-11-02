using System;
using System.Linq;
using System.Text;

namespace Watsonia.Data.Sql
{
	public sealed class Join : StatementPart
	{
		private ConditionCollection _conditions = new ConditionCollection();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Join;
			}
		}

		public JoinType JoinType
		{
			get;
			internal set;
		}

		public StatementPart Left
		{
			get;
			internal set;
		}

		public StatementPart Right
		{
			get;
			internal set;
		}

		public ConditionCollection Conditions
		{
			get
			{
				return _conditions;
			}
		}

		internal Join()
		{
		}

		public Join(JoinType joinType, StatementPart left, StatementPart right, ConditionExpression condition)
		{
			this.JoinType = joinType;
			this.Left = left;
			this.Right = right;
			this.Conditions.Add(condition);
		}

		public Join(string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			this.JoinType = JoinType.Inner;
			// TODO: Fix this pug fugly syntax
			// TODO: Change field => column in all the SQL stuff?  Column if it's a column, field if it's a statement part
			//this.Left = new Table(leftTableName);
			this.Right = new Table(tableName);
			this.Conditions.Add(new Condition(leftTableName, leftColumnName, SqlOperator.Equals, new Column(rightTableName, rightColumnName)));
		}

		public Join(JoinType joinType, string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			this.JoinType = joinType;
			// TODO: Fix this pug fugly syntax
			// TODO: Change field => column in all the SQL stuff?  Column if it's a column, field if it's a statement part
			//this.Left = new Table(leftTableName);
			this.Right = new Table(tableName);
			this.Conditions.Add(new Condition(leftTableName, leftColumnName, SqlOperator.Equals, new Column(rightTableName, rightColumnName)));
		}

		public Join(Table table, Column leftColumn, Column rightColumn)
		{
			this.JoinType = JoinType.Inner;
			this.Right = table;
			this.Conditions.Add(new Condition(leftColumn, SqlOperator.Equals, rightColumn));
		}

		public override string ToString()
		{
			var b = new StringBuilder();
            // HACK: Should Left be able to be set to null?
            if (this.Left != null)
            {
                b.Append(this.Left.ToString());
                b.Append(" ");
            }
			b.Append(this.JoinType.ToString());
			b.Append(" Join ");
			b.Append(this.Right.ToString());
			if (this.Conditions.Count > 0)
			{
				b.Append(" On ");
				b.Append(this.Conditions.ToString());
			}
			return b.ToString();
		}
	}
}

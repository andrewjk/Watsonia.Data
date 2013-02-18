using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class Join : StatementPart
	{
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

		// TODO: List<Condition>
		public Condition Condition
		{
			get;
			internal set;
		}

		internal Join()
		{
		}

		public Join(JoinType joinType, StatementPart left, StatementPart right, Condition condition)
		{
			this.JoinType = joinType;
			this.Left = left;
			this.Right = right;
			this.Condition = condition;
		}

		public Join(string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			this.JoinType = JoinType.Inner;
			// TODO: Fix this pug fugly syntax
			// TODO: Change field => column in all the SQL stuff?  Column if it's a column, field if it's a statement part
			//this.Left = new Table(leftTableName);
			this.Right = new Table(tableName);
			this.Condition = new Condition(leftTableName, leftColumnName, SqlOperator.Equals, new Column(rightTableName, rightColumnName));
		}

		public Join(Table table, Column leftColumn, Column rightColumn)
		{
			this.JoinType = Data.JoinType.Inner;
			this.Right = table;
			this.Condition = new Condition(leftColumn, SqlOperator.Equals, rightColumn);
		}

		public override string ToString()
		{
			return string.Format("{0}, {1}, {2}, {3},", this.JoinType, this.Left, this.Condition, this.Right);
		}
	}
}

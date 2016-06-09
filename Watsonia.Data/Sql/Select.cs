using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public static partial class Select
	{
		public static SelectStatement From(string tableName)
		{
			return Select.From(new Table(tableName));
		}

		public static SelectStatement From(Table table)
		{
			return new SelectStatement() { Source = table };
		}

		public static SelectStatement From(Join join)
		{
			return new SelectStatement() { Source = join };
		}

		public static SelectStatement From(StatementPart part)
		{
			return new SelectStatement() { Source = part };
		}

		public static SelectStatement Join(this SelectStatement select, string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			select.SourceJoins.Add(new Join(tableName, leftTableName, leftColumnName, rightTableName, rightColumnName));
			return select;
		}

		public static SelectStatement Join(this SelectStatement select, JoinType joinType, string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			select.SourceJoins.Add(new Join(joinType, tableName, leftTableName, leftColumnName, rightTableName, rightColumnName));
			return select;
		}

		public static SelectStatement Join(this SelectStatement select, Table table, Column leftColumn, Column rightColumn)
		{
			select.SourceJoins.Add(new Join(table, leftColumn, rightColumn));
			return select;
		}

		public static SelectStatement Columns(this SelectStatement select, params string[] columnNames)
		{
			select.SourceFields.AddRange(columnNames.Select(cn => new Column(cn)));
			return select;
		}

		public static SelectStatement Columns(this SelectStatement select, params Column[] columns)
		{
			select.SourceFields.AddRange(columns);
			return select;
		}

		public static SelectStatement ColumnsFrom(this SelectStatement select, params string[] tableNames)
		{
			select.SourceFieldsFrom.AddRange(tableNames.Select(tn => new Table(tn)));
			return select;
		}

		public static SelectStatement ColumnsFrom(this SelectStatement select, params Table[] tables)
		{
			select.SourceFieldsFrom.AddRange(tables);
			return select;
		}

		public static SelectStatement Count(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Count, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Count, new Column("*")));
			}
			return select;
		}

		public static SelectStatement Sum(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Sum, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Sum, new Column("*")));
			}
			return select;
		}

		public static SelectStatement Min(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Min, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Min, new Column("*")));
			}
			return select;
		}

		public static SelectStatement Max(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Max, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Max, new Column("*")));
			}
			return select;
		}

		public static SelectStatement Average(this SelectStatement select, params string[] columnNames)
		{
			if (columnNames.Any())
			{
				select.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Average, new Column(cn))));
			}
			else
			{
				select.SourceFields.Add(new Aggregate(AggregateType.Average, new Column("*")));
			}
			return select;
		}

		public static SelectStatement Distinct(this SelectStatement select)
		{
			select.IsDistinct = true;
			return select;
		}

		public static SelectStatement Skip(this SelectStatement select, int startIndex)
		{
			select.StartIndex = startIndex;
			return select;
		}

		public static SelectStatement Take(this SelectStatement select, int limit)
		{
			select.Limit = limit;
			return select;
		}

		public static SelectStatement Where(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value));
			return select;
		}

		public static SelectStatement WhereNot(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value) { Not = true });
			return select;
		}

		public static SelectStatement Where(this SelectStatement select, params ConditionExpression[] conditions)
		{
			select.Conditions.AddRange(conditions);
			return select;
		}

		public static SelectStatement And(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.And });
			return select;
		}

		public static SelectStatement And(this SelectStatement select, Condition condition)
		{
			condition.Relationship = ConditionRelationship.And;
			select.Conditions.Add(condition);
			return select;
		}

		public static SelectStatement And(this SelectStatement select, params Condition[] conditions)
		{
			select.Conditions.Add(new ConditionCollection(conditions) { Relationship = ConditionRelationship.And });
			return select;
		}

		public static SelectStatement Or(this SelectStatement select, string columnName, SqlOperator op, object value)
		{
			select.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.Or });
			return select;
		}

		public static SelectStatement Or(this SelectStatement select, Condition condition)
		{
			condition.Relationship = ConditionRelationship.Or;
			select.Conditions.Add(condition);
			return select;
		}

		public static SelectStatement Or(this SelectStatement select, params Condition[] conditions)
		{
			select.Conditions.Add(new ConditionCollection(conditions) { Relationship = ConditionRelationship.Or });
			return select;
		}

		public static SelectStatement OrderBy(this SelectStatement select, params string[] columnNames)
		{
			select.OrderByFields.AddRange(columnNames.Select(cn => new OrderByExpression(cn)));
			return select;
		}

		public static SelectStatement OrderBy(this SelectStatement select, params Column[] columns)
		{
			select.OrderByFields.AddRange(columns.Select(c => new OrderByExpression(c)));
			return select;
		}

		public static SelectStatement OrderBy(this SelectStatement select, params OrderByExpression[] columns)
		{
			select.OrderByFields.AddRange(columns);
			return select;
		}

		public static SelectStatement OrderByDescending(this SelectStatement select, params string[] columnNames)
		{
			select.OrderByFields.AddRange(columnNames.Select(c => new OrderByExpression(c, OrderDirection.Descending)));
			return select;
		}

		public static SelectStatement GroupBy(this SelectStatement select, params string[] columnNames)
		{
			select.GroupByFields.AddRange(columnNames.Select(c => new Column(c)));
			return select;
		}

		public static SelectStatement Include(this SelectStatement select, string path)
		{
			select.IncludePaths.Add(path);
			return select;
		}
	}
}

using System;
using System.Collections.Generic;
using Watsonia.Data.Sql;
using System.Text;

namespace Watsonia.Data
{
	public sealed class Select : Statement
	{
		////private readonly List<string> _includePaths = new List<string>();
		private readonly List<Join> _sourceJoins = new List<Join>();
		private readonly List<SourceExpression> _sourceFields = new List<SourceExpression>();
		private readonly List<Table> _sourceFieldsFrom = new List<Table>();
		private readonly ConditionCollection _conditions = new ConditionCollection();
		private readonly List<OrderByExpression> _orderByFields = new List<OrderByExpression>();
		private readonly List<Column> _groupByFields = new List<Column>();

		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.Select;
			}
		}

		public StatementPart Source
		{
			get;
			internal set;
		}

		public List<Join> SourceJoins
		{
			get
			{
				return _sourceJoins;
			}
		}

		////internal List<string> IncludePaths
		////{
		////	get
		////	{
		////		return _includePaths;
		////	}
		////}

		public List<SourceExpression> SourceFields
		{
			get
			{
				return _sourceFields;
			}
		}

		public List<Table> SourceFieldsFrom
		{
			get
			{
				return _sourceFieldsFrom;
			}
		}

		public bool IsDistinct
		{
			get;
			set;
		}

		public int SelectStart
		{
			get;
			set;
		}

		public int SelectLimit
		{
			get;
			set;
		}

		public List<Condition> Conditions
		{
			get
			{
				return _conditions;
			}
		}

		public List<OrderByExpression> OrderByFields
		{
			get
			{
				return _orderByFields;
			}
		}

		public List<Column> GroupByFields
		{
			get
			{
				return _groupByFields;
			}
		}

		public string Alias
		{
			get;
			set;
		}

		internal Select()
		{
		}

		public static Select From(string tableName)
		{
			return Select.From(new Table(tableName));
		}

		public static Select From(Table table)
		{
			return new Select() { Source = table };
		}

		public Select Join(string tableName, string leftTableName, string leftColumnName, string rightTableName, string rightColumnName)
		{
			this.SourceJoins.Add(new Join(tableName, leftTableName, leftColumnName, rightTableName, rightColumnName));
			return this;
		}

		public Select Join(Table table, Column leftColumn, Column rightColumn)
		{
			this.SourceJoins.Add(new Join(table, leftColumn, rightColumn));
			return this;
		}

		public Select Columns(params string[] columnNames)
		{
			return Columns(Array.ConvertAll(columnNames, name => new Column(name)));
		}

		public Select Columns(params Column[] columns)
		{
			this.SourceFields.AddRange(columns);
			return this;
		}

		public Select ColumnsFrom(params string[] tableNames)
		{
			return ColumnsFrom(Array.ConvertAll(tableNames, name => new Table(name)));
		}

		public Select ColumnsFrom(params Table[] tables)
		{
			this.SourceFieldsFrom.AddRange(tables);
			return this;
		}

		public Select Count(params string[] columnNames)
		{
			this.SourceFields.AddRange(Array.ConvertAll(columnNames, cn => new Aggregate(AggregateType.Count, new Column(cn))));
			return this;
		}

		public Select Sum(params string[] columnNames)
		{
			this.SourceFields.AddRange(Array.ConvertAll(columnNames, cn => new Aggregate(AggregateType.Sum, new Column(cn))));
			return this;
		}

		public Select Min(params string[] columnNames)
		{
			this.SourceFields.AddRange(Array.ConvertAll(columnNames, cn => new Aggregate(AggregateType.Min, new Column(cn))));
			return this;
		}

		public Select Max(params string[] columnNames)
		{
			this.SourceFields.AddRange(Array.ConvertAll(columnNames, cn => new Aggregate(AggregateType.Max, new Column(cn))));
			return this;
		}

		public Select Average(params string[] columnNames)
		{
			this.SourceFields.AddRange(Array.ConvertAll(columnNames, cn => new Aggregate(AggregateType.Average, new Column(cn))));
			return this;
		}

		public Select Distinct()
		{
			this.IsDistinct = true;
			return this;
		}

		public Select Start(int startIndex)
		{
			this.SelectStart = startIndex;
			return this;
		}

		public Select Limit(int limit)
		{
			this.SelectLimit = limit;
			return this;
		}

		public Select Where(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values));
			return this;
		}

		public Select WhereNot(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values) { Not = true });
			return this;
		}

		public Select Where(params Condition[] subConditions)
		{
			this.Conditions.Add(new Condition(subConditions));
			return this;
		}

		public Select And(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values) { Relationship = ConditionRelationship.And });
			return this;
		}

		public Select And(params Condition[] subConditions)
		{
			this.Conditions.Add(new Condition(subConditions) { Relationship = ConditionRelationship.And });
			return this;
		}

		public Select Or(string columnName, SqlOperator op, params object[] values)
		{
			this.Conditions.Add(new Condition(columnName, op, values) { Relationship = ConditionRelationship.Or });
			return this;
		}

		public Select Or(params Condition[] subConditions)
		{
			this.Conditions.Add(new Condition(subConditions) { Relationship = ConditionRelationship.Or });
			return this;
		}

		public Select OrderBy(params string[] columnNames)
		{
			return OrderBy(Array.ConvertAll(columnNames, c => new OrderByExpression(c)));
		}

		public Select OrderBy(params OrderByExpression[] columns)
		{
			this.OrderByFields.AddRange(columns);
			return this;
		}

		public Select OrderByDescending(params string[] columnNames)
		{
			this.OrderByFields.AddRange(Array.ConvertAll(columnNames, c => new OrderByExpression(c, OrderDirection.Descending)));
			return this;
		}

		public Select GroupBy(params string[] columnNames)
		{
			this.GroupByFields.AddRange(Array.ConvertAll(columnNames, c => new Column(c)));
			return this;
		}

		////public Select Include(string path)
		////{
		////	this.IncludePaths.Add(path);
		////	return this;
		////}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append("{ ");
			b.Append("Select ");
			if (this.SourceFields.Count > 0)
			{
				b.Append(string.Join(", ", Array.ConvertAll(this.SourceFields.ToArray(), f => f.ToString())));
			}
			else
			{
				b.Append("All ");
			}
			b.AppendLine(" ");
			b.Append("From ");
			b.Append(this.Source.ToString());
			b.AppendLine(" ");
			if (this.SourceJoins.Count > 0)
			{
				b.Append("Join ");
				b.Append(string.Join(" And ", Array.ConvertAll(this.SourceJoins.ToArray(), j => j.ToString())));
				b.AppendLine(" ");
			}
			b.Append("Where ");
			b.Append(this.Conditions.ToString());
			b.AppendLine(" ");
			if (this.OrderByFields.Count > 0)
			{
				b.Append("Order By ");
				b.Append(string.Join(", ", Array.ConvertAll(this.OrderByFields.ToArray(), f => f.ToString())));
			}
			b.Append(" }");
			return b.ToString();
		}

		public Select Clone()
		{
			var clone = new Select();

			clone.Source = this.Source;
			////clone.IncludePaths.AddRange(this.IncludePaths);
			clone.SourceFields.AddRange(this.SourceFields);
			clone.IsDistinct = this.IsDistinct;
			clone.SelectStart = this.SelectStart;
			clone.SelectLimit = this.SelectLimit;
			clone.Conditions.AddRange(this.Conditions);
			clone.OrderByFields.AddRange(this.OrderByFields);
			clone.GroupByFields.AddRange(this.GroupByFields);
			clone.Alias = this.Alias;

			return clone;
		}
	}
}

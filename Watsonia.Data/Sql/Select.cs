using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class Select : Statement
	{
		private readonly List<string> _includePaths = new List<string>();
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

		public List<string> IncludePaths
		{
			get
			{
				return _includePaths;
			}
		}

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

		public int SelectStartIndex
		{
			get;
			set;
		}

		public int SelectLimit
		{
			get;
			set;
		}

		public ConditionCollection Conditions
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

		// TODO: Should this just be a string?
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
			this.SourceFields.AddRange(columnNames.Select(cn => new Column(cn)));
			return this;
		}

		public Select Columns(params Column[] columns)
		{
			this.SourceFields.AddRange(columns);
			return this;
		}

		public Select ColumnsFrom(params string[] tableNames)
		{
			this.SourceFieldsFrom.AddRange(tableNames.Select(tn => new Table(tn)));
			return this;
		}

		public Select ColumnsFrom(params Table[] tables)
		{
			this.SourceFieldsFrom.AddRange(tables);
			return this;
		}

		public Select Count(params string[] columnNames)
		{
			this.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Count, new Column(cn))));
			return this;
		}

		public Select Sum(params string[] columnNames)
		{
			this.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Sum, new Column(cn))));
			return this;
		}

		public Select Min(params string[] columnNames)
		{
			this.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Min, new Column(cn))));
			return this;
		}

		public Select Max(params string[] columnNames)
		{
			this.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Max, new Column(cn))));
			return this;
		}

		public Select Average(params string[] columnNames)
		{
			this.SourceFields.AddRange(columnNames.Select(cn => new Aggregate(AggregateType.Average, new Column(cn))));
			return this;
		}

		public Select Distinct()
		{
			this.IsDistinct = true;
			return this;
		}

		public Select Start(int startIndex)
		{
			this.SelectStartIndex = startIndex;
			return this;
		}

		public Select Limit(int limit)
		{
			this.SelectLimit = limit;
			return this;
		}

		public Select Where(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value));
			return this;
		}

		public Select WhereNot(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Not = true });
			return this;
		}

		public Select Where(params ConditionExpression[] conditions)
		{
			this.Conditions.AddRange(conditions);
			return this;
		}

		public Select And(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.And });
			return this;
		}

		public Select And(Condition condition)
		{
			condition.Relationship = ConditionRelationship.And;
			this.Conditions.Add(condition);
			return this;
		}

		public Select And(params Condition[] conditions)
		{
			this.Conditions.Add(new ConditionCollection(conditions) { Relationship = ConditionRelationship.And });
			return this;
		}

		public Select Or(string columnName, SqlOperator op, object value)
		{
			this.Conditions.Add(new Condition(columnName, op, value) { Relationship = ConditionRelationship.Or });
			return this;
		}

		public Select Or(Condition condition)
		{
			condition.Relationship = ConditionRelationship.Or;
			this.Conditions.Add(condition);
			return this;
		}

		public Select Or(params Condition[] conditions)
		{
			this.Conditions.Add(new ConditionCollection(conditions) { Relationship = ConditionRelationship.Or });
			return this;
		}

		public Select OrderBy(params string[] columnNames)
		{
			this.OrderByFields.AddRange(columnNames.Select(cn => new OrderByExpression(cn)));
			return this;
		}

		public Select OrderBy(params OrderByExpression[] columns)
		{
			this.OrderByFields.AddRange(columns);
			return this;
		}

		public Select OrderByDescending(params string[] columnNames)
		{
			this.OrderByFields.AddRange(columnNames.Select(c => new OrderByExpression(c, OrderDirection.Descending)));
			return this;
		}

		public Select GroupBy(params string[] columnNames)
		{
			this.GroupByFields.AddRange(columnNames.Select(c => new Column(c)));
			return this;
		}

		public Select Include(string path)
		{
			this.IncludePaths.Add(path);
			return this;
		}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append("(");
			b.Append("Select ");
			if (this.IsDistinct)
			{
				b.Append("Distinct ");
			}
			if (this.SelectLimit == 1)
			{
				b.Append("(Row ");
				b.Append(this.SelectStartIndex);
				b.Append(") ");
			}
			else if (this.SelectStartIndex != 0 || this.SelectLimit != 0)
			{
				b.Append("(Rows ");
				b.Append(this.SelectStartIndex);
				if (this.SelectLimit == 0)
				{
					b.Append("+");
				}
				else
				{
					b.Append("-");
					b.Append(this.SelectStartIndex + this.SelectLimit);
				}
				b.Append(") ");
			}
			if (this.SourceFields.Count > 0)
			{
				b.Append(string.Join(", ", Array.ConvertAll(this.SourceFields.ToArray(), f => f.ToString())));
			}
			else
			{
				b.Append("All ");
			}
			b.AppendLine(" ");
			if (this.Source != null)
			{
				b.Append("From ");
				b.Append(this.Source.ToString());
				b.AppendLine(" ");
			}
			// TODO: Do these ever get used?
			if (this.SourceJoins.Count > 0)
			{
				b.Append("Join ");
				b.Append(string.Join(" And ", Array.ConvertAll(this.SourceJoins.ToArray(), j => j.ToString())));
				b.AppendLine(" ");
			}
			if (this.Conditions.Count > 0)
			{
				b.Append("Where ");
				b.Append(this.Conditions.ToString());
				b.AppendLine(" ");
			}
			if (this.GroupByFields.Count > 0)
			{
				b.Append("Group By ");
				b.Append(string.Join(", ", Array.ConvertAll(this.GroupByFields.ToArray(), f => f.ToString())));
			}
			if (this.OrderByFields.Count > 0)
			{
				b.Append("Order By ");
				b.Append(string.Join(", ", Array.ConvertAll(this.OrderByFields.ToArray(), f => f.ToString())));
			}
			b.Append(")");
			if (!string.IsNullOrEmpty(this.Alias))
			{
				b.Append(" As ");
				b.Append(this.Alias);
			}
			return b.ToString();
		}

		//public Select Clone()
		//{
		//	var clone = new Select();

		//	clone.Source = this.Source;
		//	////clone.IncludePaths.AddRange(this.IncludePaths);
		//	clone.SourceFields.AddRange(this.SourceFields);
		//	clone.IsDistinct = this.IsDistinct;
		//	clone.SelectStart = this.SelectStart;
		//	clone.SelectLimit = this.SelectLimit;
		//	clone.Conditions.AddRange(this.Conditions);
		//	clone.OrderByFields.AddRange(this.OrderByFields);
		//	clone.GroupByFields.AddRange(this.GroupByFields);
		//	clone.Alias = this.Alias;

		//	return clone;
		//}
	}
}

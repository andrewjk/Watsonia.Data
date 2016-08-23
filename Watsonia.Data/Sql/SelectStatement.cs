using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class SelectStatement : Statement
	{
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
			get;
			private set;
		}

		public List<string> IncludePaths
		{
			get;
			private set;
		}

		public List<SourceExpression> SourceFields
		{
			get;
			private set;
		}

		public List<Table> SourceFieldsFrom
		{
			get;
			private set;
		}

		public bool IsAny
		{
			get;
			set;
		}

		public bool IsAll
		{
			get;
			set;
		}

		public bool IsContains
		{
			get;
			set;
		}

		public StatementPart ContainsItem
		{
			get;
			set;
		}

		public bool IsDistinct
		{
			get;
			set;
		}

		public int StartIndex
		{
			get;
			set;
		}

		public int Limit
		{
			get;
			set;
		}

		public ConditionCollection Conditions
		{
			get;
			private set;
		}

		public List<OrderByExpression> OrderByFields
		{
			get;
			private set;
		}

		public List<Column> GroupByFields
		{
			get;
			private set;
		}

		public List<SelectStatement> UnionStatements
		{
			get;
			private set;
		}

		public string Alias
		{
			get;
			set;
		}

		public bool IsAggregate
		{
			get;
			set;
		}

		internal SelectStatement()
		{
			this.IncludePaths = new List<string>();
			this.SourceJoins = new List<Join>();
			this.SourceFields = new List<SourceExpression>();
			this.SourceFieldsFrom = new List<Table>();
			this.Conditions = new ConditionCollection();
			this.OrderByFields = new List<OrderByExpression>();
			this.GroupByFields = new List<Column>();
			this.UnionStatements = new List<SelectStatement>();
		}

		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append("(");
			b.Append("Select ");
			if (this.IsAny)
			{
				b.Append("Any ");
			}
			if (this.IsAll)
			{
				b.Append("All ");
			}
			if (this.IsContains)
			{
				b.Append("Contains ");
				b.Append(this.ContainsItem);
				b.Append(" In ");
			}
			if (this.IsDistinct)
			{
				b.Append("Distinct ");
			}
			if (this.Limit == 1)
			{
				b.Append("(Row ");
				b.Append(this.StartIndex);
				b.Append(") ");
			}
			else if (this.StartIndex != 0 || this.Limit != 0)
			{
				b.Append("(Rows ");
				b.Append(this.StartIndex);
				if (this.Limit == 0)
				{
					b.Append("+");
				}
				else
				{
					b.Append("-");
					b.Append(this.StartIndex + this.Limit);
				}
				b.Append(") ");
			}
			if (this.SourceFields.Count > 0)
			{
				b.Append(string.Join(", ", Array.ConvertAll(this.SourceFields.ToArray(), f => f.ToString())));
			}
			else
			{
				b.Append("* ");
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
	}
}

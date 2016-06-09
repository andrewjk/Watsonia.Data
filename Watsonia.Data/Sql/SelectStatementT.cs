using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Sql;

namespace Watsonia.Data
{
	public sealed class SelectStatement<T> : Statement
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.GenericSelect;
			}
		}

		public Type Source
		{
			get;
			internal set;
		}

		public List<PropertyInfo> SourceFields
		{
			get;
			private set;
		}

		public List<Tuple<PropertyInfo, AggregateType>> AggregateFields
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

		public Expression<Func<T, bool>> Conditions
		{
			get;
			internal set;
		}

		public List<Tuple<PropertyInfo, OrderDirection>> OrderByFields
		{
			get;
			internal set;
		}

		internal SelectStatement()
		{
			this.Source = typeof(T);
			this.SourceFields = new List<PropertyInfo>();
			this.AggregateFields = new List<Tuple<PropertyInfo, AggregateType>>();
			this.OrderByFields = new List<Tuple<PropertyInfo, OrderDirection>>();
		}

		public SelectStatement CreateStatement(DatabaseConfiguration configuration)
		{
			var select = new SelectStatement();
			select.Source = new Table(configuration.GetTableName(this.Source));
			select.SourceFields.AddRange(this.SourceFields.Select(s => new Column(configuration.GetColumnName(s))));
			select.SourceFields.AddRange(this.AggregateFields.Select(s => new Aggregate(s.Item2, new Column(s.Item1 != null ? configuration.GetColumnName(s.Item1) : "*"))));
			select.IsAny = this.IsAny;
			select.IsAll = this.IsAll;
			select.IsDistinct = this.IsDistinct;
			select.StartIndex = this.StartIndex;
			select.Limit = this.Limit;
			if (this.Conditions != null)
			{
				select.Conditions.Add(SelectStatementCreator.VisitStatementConditions<T>(this.Conditions, configuration, false));
			}
			select.OrderByFields.AddRange(this.OrderByFields.Select(s => new OrderByExpression(configuration.GetColumnName(s.Item1), s.Item2)));
			return select;
		}
	}
}

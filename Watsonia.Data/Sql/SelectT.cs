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
	public sealed class Select<T> : Statement
	{
		#region Declarations

		private readonly List<PropertyInfo> _sourceFields = new List<PropertyInfo>();
		private readonly List<Tuple<PropertyInfo, AggregateType>> _aggregateFields = new List<Tuple<PropertyInfo, AggregateType>>();
		private readonly List<Tuple<PropertyInfo, OrderDirection>> _orderByFields = new List<Tuple<PropertyInfo, OrderDirection>>();

		#endregion Declarations

		#region Properties

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
			get
			{
				return _sourceFields;
			}
		}

		public List<Tuple<PropertyInfo, AggregateType>> AggregateFields
		{
			get
			{
				return _aggregateFields;
			}
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
			private set;
		}

		public List<Tuple<PropertyInfo, OrderDirection>> OrderByFields
		{
			get
			{
				return _orderByFields;
			}
		}

		#endregion Properties

		#region Constructor

		internal Select()
		{
			this.Source = typeof(T);
		}

		#endregion Constructor

		#region Fluent Methods

		public static Select<T> From()
		{
			return new Select<T>() { Source = typeof(T) };
		}

		public Select<T> Columns(Expression<Func<T, object>> property)
		{
			this.SourceFields.Add(FuncToPropertyInfo(property));
			return this;
		}

		public Select<T> Count(Expression<Func<T, object>> property)
		{
			this.AggregateFields.Add(new Tuple<PropertyInfo, AggregateType>(FuncToPropertyInfo(property), AggregateType.Count));
			return this;
		}

		public Select<T> Count()
		{
			this.AggregateFields.Add(new Tuple<PropertyInfo, AggregateType>(null, AggregateType.Count));
			return this;
		}

		public Select<T> Skip(int startIndex)
		{
			this.StartIndex = startIndex;
			return this;
		}

		public Select<T> Take(int limit)
		{
			this.Limit = limit;
			return this;
		}

		public Select<T> Where(Expression<Func<T, bool>> condition)
		{
			this.Conditions = condition;
			return this;
		}

		public Select<T> And(Expression<Func<T, bool>> condition)
		{
			if (this.Conditions != null)
			{
				Expression combined = this.Conditions.Body.AndAlso(condition.Body);
				combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
				this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
			}
			else
			{
				this.Conditions = condition;
			}
			return this;
		}

		public Select<T> Or(Expression<Func<T, bool>> condition)
		{
			if (this.Conditions != null)
			{
				Expression combined = this.Conditions.Body.OrElse(condition.Body);
				combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
				this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
			}
			else
			{
				this.Conditions = condition;
			}
			return this;
		}

		public Select<T> OrderBy(Expression<Func<T, object>> property)
		{
			this.OrderByFields.Add(new Tuple<PropertyInfo, OrderDirection>(FuncToPropertyInfo(property), OrderDirection.Ascending));
			return this;
		}

		public Select<T> OrderByDescending(Expression<Func<T, object>> property)
		{
			this.OrderByFields.Add(new Tuple<PropertyInfo, OrderDirection>(FuncToPropertyInfo(property), OrderDirection.Descending));
			return this;
		}

		private static PropertyInfo FuncToPropertyInfo(Expression<Func<T, object>> selector)
		{
			if (selector.Body is MemberExpression)
			{
				MemberExpression mex = (MemberExpression)selector.Body;
				return (PropertyInfo)mex.Member;
			}
			else if (selector.Body is UnaryExpression)
			{
				// Throw away Converts
				UnaryExpression uex = (UnaryExpression)selector.Body;
				if (uex.Operand is MemberExpression)
				{
					MemberExpression mex = (MemberExpression)uex.Operand;
					return (PropertyInfo)mex.Member;
				}
			}

			throw new InvalidOperationException();
		}

		#endregion Fluent Methods

		#region Methods

		public Select CreateStatement(DatabaseConfiguration configuration)
		{
			Select select = new Select();
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
				select.Conditions.Add(SelectStatementCreator.VisitStatementConditions<T>(this.Conditions, configuration));
			}
			select.OrderByFields.AddRange(this.OrderByFields.Select(s => new OrderByExpression(configuration.GetColumnName(s.Item1), s.Item2)));
			return select;
		}

		#endregion Methods
	}
}

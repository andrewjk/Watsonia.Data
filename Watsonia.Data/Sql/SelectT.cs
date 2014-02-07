using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Query;
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

		public Select<T> Start(int startIndex)
		{
			this.SelectStartIndex = startIndex;
			return this;
		}

		public Select<T> Limit(int limit)
		{
			this.SelectLimit = limit;
			return this;
		}

		public Select<T> Where(Expression<Func<T, bool>> condition)
		{
			this.Conditions = condition;
			return this;
		}

		public Select<T> And(Expression<Func<T, bool>> condition)
		{
			Expression combined = this.Conditions.Body.AndAlso(condition.Body);
			combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
			this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
			return this;
		}

		public Select<T> Or(Expression<Func<T, bool>> condition)
		{
			Expression combined = this.Conditions.Body.OrElse(condition.Body);
			combined = AnonymousParameterReplacer.Replace(combined, condition.Parameters);
			this.Conditions = Expression.Lambda<Func<T, bool>>(combined, condition.Parameters);
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
			QueryProvider provider = new QueryProvider(null);

			Select select = new Select();
			select.Source = new Table(configuration.GetTableName(this.Source));
			select.SourceFields.AddRange(this.SourceFields.Select(s => new Column(configuration.GetColumnName(s))));
			select.SourceFields.AddRange(this.AggregateFields.Select(s => new Aggregate(s.Item2, new Column(s.Item1 != null ? configuration.GetColumnName(s.Item1) : "*"))));
			select.IsDistinct = this.IsDistinct;
			select.SelectStartIndex = this.SelectStartIndex;
			select.SelectLimit = this.SelectLimit;
			select.Conditions.Add((ConditionCollection)StatementCreator.CompileStatementPart(configuration, this.Source, new DatabaseQuery<T>(provider, this.Source), this.Conditions));
			select.OrderByFields.AddRange(this.OrderByFields.Select(s => new OrderByExpression(configuration.GetColumnName(s.Item1), s.Item2)));
			return select;
		}

		#endregion Methods
	}
}

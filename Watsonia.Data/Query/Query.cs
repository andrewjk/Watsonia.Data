// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// A default implementation of IQueryable for use with QueryProvider.
	/// </summary>
	public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
	{
		private Type _elementType = typeof(T);

		public IQueryProvider Provider
		{
			get;
			private set;
		}

		public Expression Expression
		{
			get;
			private set;
		}

		public Type ElementType
		{
			get
			{
				return _elementType;
			}
			protected set
			{
				_elementType = value;
			}
		}

		public Query(IQueryProvider provider)
			: this(provider, null)
		{
		}

		public Query(IQueryProvider provider, Type staticType)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}

			this.Provider = provider;
			this.Expression = staticType != null ? Expression.Constant(this, staticType) : Expression.Constant(this);
		}

		public Query(QueryProvider provider, Expression expression)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}

			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}

			if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
			{
				throw new ArgumentOutOfRangeException("expression");
			}

			this.Provider = provider;
			this.Expression = expression;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)Provider.Execute(Expression)).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Provider.Execute(Expression)).GetEnumerator();
		}

		public override string ToString()
		{
			if (Expression.NodeType == ExpressionType.Constant &&
				((ConstantExpression)Expression).Value == this)
			{
				return "Query(" + typeof(T) + ")";
			}
			else
			{
				return Expression.ToString();
			}
		}
	}
}

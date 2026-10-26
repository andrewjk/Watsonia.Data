// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query.Expressions
{
	/// <summary>
	/// A custom expression node used to represent a SQL SELECT expression
	/// </summary>
	internal sealed class SelectExpression : AliasedExpression
	{
		private readonly ReadOnlyCollection<ColumnDeclaration> columns;
		private readonly bool isDistinct;
		private readonly Expression from;
		private List<string> includePaths = new List<string>();
		private readonly Expression where;
		private readonly ReadOnlyCollection<OrderExpression> orderBy;
		private readonly ReadOnlyCollection<Expression> groupBy;
		private readonly Expression take;
		private readonly Expression skip;
		private readonly bool reverse;

		public SelectExpression(
			TableAlias alias,
			IEnumerable<ColumnDeclaration> columns,
			Expression from,
			Expression where,
			IEnumerable<OrderExpression> orderBy,
			IEnumerable<Expression> groupBy,
			bool isDistinct,
			Expression skip,
			Expression take,
			bool reverse
			)
			: base(typeof(void), alias)
		{
			this.columns = columns.ToReadOnly();
			this.isDistinct = isDistinct;
			this.from = from;
			this.where = where;
			this.orderBy = orderBy.ToReadOnly();
			this.groupBy = groupBy.ToReadOnly();
			this.take = take;
			this.skip = skip;
			this.reverse = reverse;
		}

		public SelectExpression(
			TableAlias alias,
			IEnumerable<ColumnDeclaration> columns,
			Expression from,
			Expression where,
			IEnumerable<OrderExpression> orderBy,
			IEnumerable<Expression> groupBy
			)
			: this(alias, columns, from, where, orderBy, groupBy, false, null, null, false)
		{
		}

		public SelectExpression(
			TableAlias alias, IEnumerable<ColumnDeclaration> columns,
			Expression from, Expression where
			)
			: this(alias, columns, from, where, null, null)
		{
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.SelectExpression; }
		}

		public ReadOnlyCollection<ColumnDeclaration> Columns
		{
			get { return this.columns; }
		}

		public Expression From
		{
			get { return this.from; }
		}

		public List<string> IncludePaths
		{
			get
			{
				return this.includePaths;
			}
		}

		public Expression Where
		{
			get { return this.where; }
		}

		public ReadOnlyCollection<OrderExpression> OrderBy
		{
			get { return this.orderBy; }
		}

		public ReadOnlyCollection<Expression> GroupBy
		{
			get { return this.groupBy; }
		}

		public bool IsDistinct
		{
			get { return this.isDistinct; }
		}

		public Expression Skip
		{
			get { return this.skip; }
		}

		public Expression Take
		{
			get { return this.take; }
		}

		public bool IsReverse
		{
			get { return this.reverse; }
		}
	}
}

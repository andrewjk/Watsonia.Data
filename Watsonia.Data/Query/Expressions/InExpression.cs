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
	/// A SQL 'In' subquery:
	///   expr in (select x from y where z)
	///   expr in (a, b, c)
	/// </summary>
	internal sealed class InExpression : SubqueryExpression
	{
		private readonly Expression expression;
		private readonly ReadOnlyCollection<Expression> values;  // either select or expressions are assigned

		public InExpression(Expression expression, SelectExpression select)
			: base(typeof(bool), select)
		{
			this.expression = expression;
		}

		public InExpression(Expression expression, IEnumerable<Expression> values)
			: base(typeof(bool), null)
		{
			this.expression = expression;
			this.values = values.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.In; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}

		public ReadOnlyCollection<Expression> Values
		{
			get { return this.values; }
		}
	}
}

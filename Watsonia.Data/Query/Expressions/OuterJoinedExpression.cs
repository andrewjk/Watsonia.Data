// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query.Expressions
{
	/// <summary>
	/// A wrapper around and expression that is part of an outer joined projection
	/// including a test expression to determine if the expression ought to be considered null.
	/// </summary>
	internal sealed class OuterJoinedExpression : DbExpression
	{
		private readonly Expression test;
		private readonly Expression expression;

		public OuterJoinedExpression(Expression test, Expression expression)
			: base(expression.Type)
		{
			this.test = test;
			this.expression = expression;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.OuterJoined; }
		}

		public Expression Test
		{
			get { return this.test; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}
	}
}

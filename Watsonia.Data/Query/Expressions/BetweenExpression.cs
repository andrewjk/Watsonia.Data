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
	internal sealed class BetweenExpression : DbExpression
	{
		private readonly Expression expression;
		private readonly Expression lower;
		private readonly Expression upper;

		public BetweenExpression(Expression expression, Expression lower, Expression upper)
			: base(expression.Type)
		{
			this.expression = expression;
			this.lower = lower;
			this.upper = upper;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Between; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}

		public Expression Lower
		{
			get { return this.lower; }
		}

		public Expression Upper
		{
			get { return this.upper; }
		}
	}
}

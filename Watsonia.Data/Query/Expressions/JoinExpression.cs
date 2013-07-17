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
	/// A SQL join clause expression
	/// </summary>
	internal sealed class JoinExpression : DbExpression
	{
		private readonly ExpressionJoinType joinType;
		private readonly Expression left;
		private readonly Expression right;
		private readonly Expression condition;

		public JoinExpression(ExpressionJoinType joinType, Expression left, Expression right, Expression condition)
			: base(typeof(void))
		{
			this.joinType = joinType;
			this.left = left;
			this.right = right;
			this.condition = condition;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Join; }
		}

		public ExpressionJoinType Join
		{
			get { return this.joinType; }
		}

		public Expression Left
		{
			get { return this.left; }
		}

		public Expression Right
		{
			get { return this.right; }
		}

		public new Expression Condition
		{
			get { return this.condition; }
		}
	}
}

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
	/// Allows is-null tests against value-types like int and float
	/// </summary>
	internal sealed class IsNullExpression : DbExpression
	{
		private readonly Expression expression;

		public IsNullExpression(Expression expression)
			: base(typeof(bool))
		{
			this.expression = expression;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.IsNull; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}
	}
}

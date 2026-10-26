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
	/// A SQL Exists subquery expression.
	/// </summary>
	internal sealed class ExistsExpression : SubqueryExpression
	{
		public ExistsExpression(SelectExpression select)
			: base(typeof(bool), select)
		{
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Exists; }
		}
	}
}

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
	/// A SQL scalar subquery expression:
	///   exists(select x from y where z)
	/// </summary>
	internal sealed class ScalarExpression : SubqueryExpression
	{
		public ScalarExpression(Type type, SelectExpression select)
			: base(type, select)
		{
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Scalar; }
		}
	}
}

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
	/// A pairing of an expression and an order type for use in a SQL Order By clause
	/// </summary>
	internal sealed class OrderExpression
	{
		private readonly OrderType orderType;
		private readonly Expression expression;

		public OrderExpression(OrderType orderType, Expression expression)
		{
			this.orderType = orderType;
			this.expression = expression;
		}

		public OrderType OrderType
		{
			get { return this.orderType; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}
	}
}

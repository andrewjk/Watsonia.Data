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
	internal sealed class ColumnAssignment
	{
		private readonly ColumnExpression column;
		private readonly Expression expression;

		public ColumnAssignment(ColumnExpression column, Expression expression)
		{
			this.column = column;
			this.expression = expression;
		}

		public ColumnExpression Column
		{
			get { return this.column; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}
	}
}

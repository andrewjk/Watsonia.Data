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
	internal sealed class DeleteCommand : CommandExpression
	{
		private readonly TableExpression table;
		private readonly Expression where;

		public DeleteCommand(TableExpression table, Expression where)
			: base(typeof(int))
		{
			this.table = table;
			this.where = where;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Delete; }
		}

		public TableExpression Table
		{
			get { return this.table; }
		}

		public Expression Where
		{
			get { return this.where; }
		}
	}
}

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
	internal sealed class RowNumberExpression : DbExpression
	{
		private readonly ReadOnlyCollection<OrderExpression> orderBy;

		public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
			: base(typeof(int))
		{
			this.orderBy = orderBy.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.RowCount; }
		}

		public ReadOnlyCollection<OrderExpression> OrderBy
		{
			get { return this.orderBy; }
		}
	}
}

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
	internal sealed class AggregateSubqueryExpression : DbExpression
	{
		private readonly TableAlias groupByAlias;
		private readonly Expression aggregateInGroupSelect;
		private readonly ScalarExpression aggregateAsSubquery;

		public AggregateSubqueryExpression(TableAlias groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
			: base(aggregateAsSubquery.Type)
		{
			this.aggregateInGroupSelect = aggregateInGroupSelect;
			this.groupByAlias = groupByAlias;
			this.aggregateAsSubquery = aggregateAsSubquery;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.AggregateSubquery; }
		}

		public TableAlias GroupByAlias
		{
			get { return this.groupByAlias; }
		}

		public Expression AggregateInGroupSelect
		{
			get { return this.aggregateInGroupSelect; }
		}

		public ScalarExpression AggregateAsSubquery
		{
			get { return this.aggregateAsSubquery; }
		}
	}
}

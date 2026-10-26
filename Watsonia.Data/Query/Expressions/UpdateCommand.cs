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
	internal sealed class UpdateCommand : CommandExpression
	{
		private readonly TableExpression table;
		private readonly Expression where;
		private readonly ReadOnlyCollection<ColumnAssignment> assignments;

		public UpdateCommand(TableExpression table, Expression where, IEnumerable<ColumnAssignment> assignments)
			: base(typeof(int))
		{
			this.table = table;
			this.where = where;
			this.assignments = assignments.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Update; }
		}

		public TableExpression Table
		{
			get { return this.table; }
		}

		public Expression Where
		{
			get { return this.where; }
		}

		public ReadOnlyCollection<ColumnAssignment> Assignments
		{
			get { return this.assignments; }
		}
	}
}

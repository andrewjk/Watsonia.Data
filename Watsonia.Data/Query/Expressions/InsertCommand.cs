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
	internal sealed class InsertCommand : CommandExpression
	{
		private readonly TableExpression table;
		private readonly ReadOnlyCollection<ColumnAssignment> assignments;

		public InsertCommand(TableExpression table, IEnumerable<ColumnAssignment> assignments)
			: base(typeof(int))
		{
			this.table = table;
			this.assignments = assignments.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Insert; }
		}

		public TableExpression Table
		{
			get { return this.table; }
		}

		public ReadOnlyCollection<ColumnAssignment> Assignments
		{
			get { return this.assignments; }
		}
	}
}

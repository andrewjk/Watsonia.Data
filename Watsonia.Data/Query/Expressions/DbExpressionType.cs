// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query.Expressions
{
	/// <summary>
	/// Extended node types for custom expressions
	/// </summary>
	public enum DbExpressionType
	{
		Table = 1000, // make sure these don't overlap with ExpressionType
		ClientJoin,
		Column,
		SelectExpression,
		Projection,
		Entity,
		Join,
		Aggregate,
		Scalar,
		Exists,
		In,
		Grouping,
		AggregateSubquery,
		IsNull,
		Between,
		RowCount,
		NamedValue,
		OuterJoined,
		Insert,
		Update,
		Delete,
		Batch,
		Function,
		Block,
		If,
		Declaration,
		Variable
	}
}

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
	/// An SQL Aggregate expression:
	///     MIN, MAX, AVG, COUNT
	/// </summary>
	internal sealed class AggregateExpression : DbExpression
	{
		private readonly string aggregateName;
		private readonly Expression argument;
		private readonly bool isDistinct;

		public AggregateExpression(Type type, string aggregateName, Expression argument, bool isDistinct)
			: base(type)
		{
			this.aggregateName = aggregateName;
			this.argument = argument;
			this.isDistinct = isDistinct;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Aggregate; }
		}

		public string AggregateName
		{
			get { return this.aggregateName; }
		}

		public Expression Argument
		{
			get { return this.argument; }
		}

		public bool IsDistinct
		{
			get { return this.isDistinct; }
		}
	}
}

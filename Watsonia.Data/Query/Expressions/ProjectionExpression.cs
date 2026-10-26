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
	/// A custom expression representing the construction of one or more result objects from a 
	/// SQL select expression
	/// </summary>
	internal sealed class ProjectionExpression : DbExpression
	{
		private readonly SelectExpression select;
		private readonly Expression projector;
		private readonly LambdaExpression aggregator;

		public ProjectionExpression(SelectExpression source, Expression projector)
			: this(source, projector, null)
		{
		}

		public ProjectionExpression(SelectExpression source, Expression projector, LambdaExpression aggregator)
			: base(aggregator != null ? aggregator.Body.Type : typeof(IEnumerable<>).MakeGenericType(projector.Type))
		{
			this.select = source;
			this.projector = projector;
			this.aggregator = aggregator;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Projection; }
		}

		public SelectExpression SelectExpression
		{
			get { return this.select; }
		}

		public Expression Projector
		{
			get { return this.projector; }
		}

		public LambdaExpression Aggregator
		{
			get { return this.aggregator; }
		}

		public bool IsSingleton
		{
			get { return this.aggregator != null && this.aggregator.Body.Type == projector.Type; }
		}

		public override string ToString()
		{
			return DbExpressionWriter.WriteToString(this);
		}
	}
}

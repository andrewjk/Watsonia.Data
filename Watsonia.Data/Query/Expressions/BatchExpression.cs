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
	internal sealed class BatchExpression : Expression
	{
		private readonly Type type;
		private readonly Expression input;
		private readonly LambdaExpression operation;
		private readonly Expression batchSize;
		private readonly Expression stream;

		public BatchExpression(Expression input, LambdaExpression operation, Expression batchSize, Expression stream)
		{
			this.input = input;
			this.operation = operation;
			this.batchSize = batchSize;
			this.stream = stream;
			this.type = typeof(IEnumerable<>).MakeGenericType(operation.Body.Type);
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Batch; }
		}

		public override Type Type
		{
			get { return this.type; }
		}

		public Expression Input
		{
			get { return this.input; }
		}

		public LambdaExpression Operation
		{
			get { return this.operation; }
		}

		public Expression BatchSize
		{
			get { return this.batchSize; }
		}

		public Expression Stream
		{
			get { return this.stream; }
		}
	}
}

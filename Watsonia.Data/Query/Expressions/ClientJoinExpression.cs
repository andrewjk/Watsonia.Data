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
	internal sealed class ClientJoinExpression : DbExpression
	{
		private readonly ReadOnlyCollection<Expression> outerKey;
		private readonly ReadOnlyCollection<Expression> innerKey;
		private readonly ProjectionExpression projection;

		public ClientJoinExpression(ProjectionExpression projection, IEnumerable<Expression> outerKey, IEnumerable<Expression> innerKey)
			: base(projection.Type)
		{
			this.outerKey = outerKey.ToReadOnly();
			this.innerKey = innerKey.ToReadOnly();
			this.projection = projection;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.ClientJoin; }
		}

		public ReadOnlyCollection<Expression> OuterKey
		{
			get { return this.outerKey; }
		}

		public ReadOnlyCollection<Expression> InnerKey
		{
			get { return this.innerKey; }
		}

		public ProjectionExpression Projection
		{
			get { return this.projection; }
		}
	}
}

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
	/// An expression node that introduces an entity mapping.
	/// </summary>
	internal sealed class EntityExpression : DbExpression
	{
		private readonly MappingEntity entity;
		private readonly Expression expression;

		public EntityExpression(MappingEntity entity, Expression expression)
			: base(expression.Type)
		{
			this.entity = entity;
			this.expression = expression;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Entity; }
		}

		public MappingEntity Entity
		{
			get { return this.entity; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}
	}
}

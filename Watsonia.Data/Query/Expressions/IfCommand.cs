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
	internal sealed class IfCommand : CommandExpression
	{
		private readonly Expression check;
		private readonly Expression ifTrue;
		private readonly Expression ifFalse;

		public IfCommand(Expression check, Expression ifTrue, Expression ifFalse)
			: base(ifTrue.Type)
		{
			this.check = check;
			this.ifTrue = ifTrue;
			this.ifFalse = ifFalse;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.If; }
		}

		public Expression Check
		{
			get { return this.check; }
		}

		public Expression IfTrue
		{
			get { return this.ifTrue; }
		}

		public Expression IfFalse
		{
			get { return this.ifFalse; }
		}
	}
}

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
	internal sealed class FunctionExpression : DbExpression
	{
		private readonly string name;
		private readonly ReadOnlyCollection<Expression> arguments;

		public FunctionExpression(Type type, string name, IEnumerable<Expression> arguments)
			: base(type)
		{
			this.name = name;
			this.arguments = arguments.ToReadOnly();
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Function; }
		}

		public string Name
		{
			get { return this.name; }
		}

		public ReadOnlyCollection<Expression> Arguments
		{
			get { return this.arguments; }
		}
	}
}

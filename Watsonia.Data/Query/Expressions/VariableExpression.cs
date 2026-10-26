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
	internal sealed class VariableExpression : Expression
	{
		private Type type;

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Variable; }
		}

		public override Type Type
		{
			get
			{
				return type;
			}
		}

		public string Name
		{
			get;
			private set;
		}

		public VariableExpression(string name, Type type)
		{
			this.Name = name;
			this.type = type;
		}
	}
}

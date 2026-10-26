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
	internal sealed class VariableDeclaration
	{
		private readonly string name;
		private readonly Type variableType;
		private readonly Expression expression;

		public VariableDeclaration(string name, Type variableType, Expression expression)
		{
			this.name = name;
			this.variableType = variableType;
			this.expression = expression;
		}

		public string Name
		{
			get { return this.name; }
		}

		public Type QueryType
		{
			get { return this.variableType; }
		}

		public Expression Expression
		{
			get { return this.expression; }
		}
	}
}

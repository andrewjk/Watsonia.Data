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
	internal sealed class DeclarationCommand : CommandExpression
	{
		private readonly ReadOnlyCollection<VariableDeclaration> variables;
		private readonly SelectExpression source;

		public DeclarationCommand(IEnumerable<VariableDeclaration> variables, SelectExpression source)
			: base(typeof(void))
		{
			this.variables = variables.ToReadOnly();
			this.source = source;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Declaration; }
		}

		public ReadOnlyCollection<VariableDeclaration> Variables
		{
			get { return this.variables; }
		}

		public SelectExpression Source
		{
			get { return this.source; }
		}
	}}

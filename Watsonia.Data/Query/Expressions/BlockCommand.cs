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
	internal sealed class BlockCommand : CommandExpression
	{
		private readonly ReadOnlyCollection<Expression> commands;

		public BlockCommand(IList<Expression> commands)
			: base(commands[commands.Count - 1].Type)
		{
			this.commands = commands.ToReadOnly();
		}

		public BlockCommand(params Expression[] commands)
			: this((IList<Expression>)commands)
		{
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Block; }
		}

		public ReadOnlyCollection<Expression> Commands
		{
			get { return this.commands; }
		}
	}
}

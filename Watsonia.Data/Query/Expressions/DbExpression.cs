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
	internal abstract class DbExpression : Expression
	{
		private readonly Type type;

		protected DbExpression(Type type)
		{
			this.type = type;
		}

		public override Type Type
		{
			get { return this.type; }
		}

		public override string ToString()
		{
			return DbExpressionWriter.WriteToString(this);
		}
	}
}

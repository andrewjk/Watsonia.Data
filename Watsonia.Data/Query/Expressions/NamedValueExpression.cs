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
	internal sealed class NamedValueExpression : DbExpression
	{
		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.NamedValue; }
		}

		public string Name
		{
			get;
			private set;
		}

		public Type ValueType
		{
			get;
			private set;
		}

		public Expression Value
		{
			get;
			private set;
		}

		public NamedValueExpression(string name, Type valueType, Expression value)
			: base(value.Type)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			this.Name = name;
			this.ValueType = valueType;
			this.Value = value;
		}
	}
}

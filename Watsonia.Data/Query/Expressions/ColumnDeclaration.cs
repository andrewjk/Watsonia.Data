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
	/// A declaration of a column in a SQL SELECT expression
	/// </summary>
	internal sealed class ColumnDeclaration
	{
		public string Name
		{
			get;
			private set;
		}

		public Expression Expression
		{
			get;
			private set;
		}

		public Type ColumnType
		{
			get;
			private set;
		}

		public ColumnDeclaration(string name, Expression expression, Type columnType)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (expression == null)
			{
				throw new ArgumentNullException("expression");
			}
			if (columnType == null)
			{
				throw new ArgumentNullException("columnType");
			}
			this.Name = name;
			this.Expression = expression;
			this.ColumnType = columnType;
		}
	}
}

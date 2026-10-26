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
	/// A custom expression node that represents a table reference in a SQL query
	/// </summary>
	internal sealed class TableExpression : AliasedExpression
	{
		private readonly MappingEntity entity;
		private readonly string name;

		public TableExpression(TableAlias alias, MappingEntity entity, string name)
			: base(typeof(void), alias)
		{
			this.entity = entity;
			this.name = name;
		}

		public override ExpressionType NodeType
		{
			get { return (ExpressionType)DbExpressionType.Table; }
		}

		public MappingEntity Entity
		{
			get { return this.entity; }
		}

		public string Name
		{
			get { return this.name; }
		}

		public override string ToString()
		{
			return "T(" + this.Name + ")";
		}
	}
}

// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watsonia.Data.Query.Expressions
{
	/// <summary>
	/// A base class for expressions that declare table aliases.
	/// </summary>
	internal abstract class AliasedExpression : DbExpression
	{
		private readonly TableAlias alias;

		protected AliasedExpression(Type type, TableAlias alias)
			: base(type)
		{
			this.alias = alias;
		}

		public TableAlias Alias
		{
			get { return this.alias; }
		}
	}
}

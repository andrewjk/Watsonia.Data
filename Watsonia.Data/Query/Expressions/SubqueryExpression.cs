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
	/// An base class for SQL subqueries.
	/// </summary>
	internal abstract class  SubqueryExpression : DbExpression
	{
		private readonly SelectExpression select;

		protected SubqueryExpression(Type type, SelectExpression select)
			: base(type)
		{
			this.select = select;
		}

		public SelectExpression SelectExpression
		{
			get { return this.select; }
		}
	}
}

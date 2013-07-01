// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Watsonia.Data.Query
{
	public class QueryCommand
	{
		public Expression Expression
		{
			get;
			private set;
		}

		public ReadOnlyCollection<QueryCommandParameter> Parameters
		{
			get;
			private set;
		}

		public QueryCommand(Expression expression, IEnumerable<QueryCommandParameter> parameters)
		{
			this.Expression = expression;
			this.Parameters = parameters.ToReadOnly();
		}
	}
}

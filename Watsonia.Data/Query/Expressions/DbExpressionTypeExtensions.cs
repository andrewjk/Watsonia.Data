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
	internal static class DbExpressionTypeExtensions
	{
		public static bool IsDbExpression(this ExpressionType et)
		{
			return ((int)et) >= 1000;
		}
	}
}

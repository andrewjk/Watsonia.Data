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
	/// A kind of SQL join
	/// </summary>
	public enum ExpressionJoinType
	{
		CrossJoin,
		InnerJoin,
		CrossApply,
		OuterApply,
		LeftOuter,
		SingletonLeftOuter
	}
}

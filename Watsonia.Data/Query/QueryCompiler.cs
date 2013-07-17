// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// Creates a reusable, parameterized representation of a query that caches the execution plan
	/// </summary>
	internal static class QueryCompiler
	{
		public static Delegate Compile(LambdaExpression query)
		{
			CompiledQuery cq = new CompiledQuery(query);
			return StrongDelegate.CreateDelegate(query.Type, (Func<object[], object>)cq.Invoke);
		}

		public static D Compile<D>(Expression<D> query)
		{
			return (D)(object)Compile((LambdaExpression)query);
		}

		public static Func<TResult> Compile<TResult>(Expression<Func<TResult>> query)
		{
			return new CompiledQuery(query).Invoke<TResult>;
		}

		public static Func<T1, TResult> Compile<T1, TResult>(Expression<Func<T1, TResult>> query)
		{
			return new CompiledQuery(query).Invoke<T1, TResult>;
		}

		public static Func<T1, T2, TResult> Compile<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> query)
		{
			return new CompiledQuery(query).Invoke<T1, T2, TResult>;
		}

		public static Func<T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> query)
		{
			return new CompiledQuery(query).Invoke<T1, T2, T3, TResult>;
		}

		public static Func<T1, T2, T3, T4, TResult> Compile<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> query)
		{
			return new CompiledQuery(query).Invoke<T1, T2, T3, T4, TResult>;
		}

		public static Func<IEnumerable<T>> Compile<T>(this IQueryable<T> source)
		{
			return Compile<IEnumerable<T>>(
				Expression.Lambda<Func<IEnumerable<T>>>(((IQueryable)source).Expression)
				);
		}
	}
}
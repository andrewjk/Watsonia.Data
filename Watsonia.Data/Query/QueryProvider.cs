// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// A LINQ IQueryable query provider that executes database queries over a DbConnection
	/// </summary>
	public class QueryProvider : IQueryProvider
	{
		private Database _database;

		IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
		{
			return new Query<S>(this, expression);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			Type elementType = TypeHelper.GetElementType(expression.Type);
			try
			{
				return (IQueryable)Activator.CreateInstance(
					typeof(Query<>).MakeGenericType(elementType),
					new object[] { this, expression });
			}
			catch (TargetInvocationException tiex)
			{
				throw tiex.InnerException;
			}
		}

		S IQueryProvider.Execute<S>(Expression expression)
		{
			return (S)this.Execute(expression);
		}

		object IQueryProvider.Execute(Expression expression)
		{
			return this.Execute(expression);
		}

		public QueryProvider(Database database)
		{
			_database = database;
		}

		protected QueryExecutor CreateExecutor()
		{
			return new QueryExecutor(_database);
		}

		/// <summary>
		/// Execute the query expression (does translation, etc.)
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public object Execute(Expression expression)
		{
			LambdaExpression lambda = expression as LambdaExpression;

			// TODO: Implement the cache...
			////if (lambda == null && this.cache != null && expression.NodeType != ExpressionType.Constant)
			////{
			////	return this.cache.Execute(expression);
			////}

			Expression plan = GetExecutionPlan(expression);

			if (lambda != null)
			{
				// compile & return the execution plan so it can be used multiple times
				LambdaExpression fn = Expression.Lambda(lambda.Type, plan, lambda.Parameters);
#if NOREFEMIT
				return ExpressionEvaluator.CreateDelegate(fn);
#else
				return fn.Compile();
#endif
			}
			else
			{
				// compile the execution plan and invoke it
				Expression<Func<object>> efn = Expression.Lambda<Func<object>>(Expression.Convert(plan, typeof(object)));
#if NOREFEMIT
				return ExpressionEvaluator.Eval(efn, new object[] { });
#else
				Func<object> fn = efn.Compile();
				return fn();
#endif
			}
		}

		/// <summary>
		/// Convert the query expression into an execution plan
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public Expression GetExecutionPlan(Expression expression)
		{
			// Strip off lambda for now
			LambdaExpression lambda = expression as LambdaExpression;
			if (lambda != null)
			{
				expression = lambda.Body;
			}

			// TODO: Move the stuff from mapping into DatabaseConfig
			var mapping = new QueryMapping(_database);
			var translator = new QueryTranslator(mapping);

			// Translate the query into client and server parts
			Expression translation = translator.Translate(_database, expression);

			var parameters = lambda != null ? lambda.Parameters : null;
			Expression provider = Find(expression, parameters, typeof(QueryProvider));
			if (provider == null)
			{
				Expression rootQueryable = Find(expression, parameters, typeof(IQueryable));
				provider = Expression.Property(rootQueryable, typeof(IQueryable).GetProperty("Provider"));
			}

			return ExecutionBuilder.Build(translation, provider);
		}

		private Expression Find(Expression expression, IList<ParameterExpression> parameters, Type type)
		{
			if (parameters != null)
			{
				Expression found = parameters.FirstOrDefault(p => type.IsAssignableFrom(p.Type));
				if (found != null)
				{
					return found;
				}
			}
			return TypedSubtreeFinder.Find(expression, type);
		}
	}
}

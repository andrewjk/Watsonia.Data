// Adapted from https://github.com/re-motion/Relinq/blob/develop/Core/QueryProvider.cs
// and https://github.com/re-motion/Relinq/blob/develop/Core/DefaultQueryProvider.cs
// TODO: And QueryModel.Execute

// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Remotion.Linq;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Utilities;
using Remotion.Utilities;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	/// <summary>
	/// Provides a default implementation of <see cref="IQueryProvider"/> that executes queries (subclasses of <see cref="QueryableBase{T}"/>) by
	/// first parsing them into a <see cref="QueryModel"/> and then passing that to a given implementation of <see cref="IQueryExecutor"/>.
	/// Usually, <see cref="DefaultQueryProvider"/> should be used unless <see cref="CreateQuery{T}"/> must be manually implemented.
	/// </summary>
	public class QueryProvider : IAsyncQueryProvider
	{
		//private static readonly MethodInfo _genericCreateQueryMethod =
		//	typeof(QueryProvider).GetRuntimeMethods().Single(m => m.Name == "CreateQuery" && m.IsGenericMethod);

		// TODO: Make a factory to return a single query provider rather than doing this for every query
		private static readonly MethodInfo _executeCollectionMethod =
			(typeof(QueryProvider).GetRuntimeMethod(nameof(ExecuteCollection), new[] { typeof(QueryModel) }));
		private static readonly MethodInfo _executeScalarMethod =
			(typeof(QueryProvider).GetRuntimeMethod(nameof(ExecuteScalar), new[] { typeof(QueryModel) }));
		private static readonly MethodInfo _executeSingleMethod =
			(typeof(QueryProvider).GetRuntimeMethod(nameof(ExecuteSingle), new[] { typeof(QueryModel), typeof(bool) }));

		private static readonly MethodInfo _executeCollectionAsyncMethod =
			(typeof(QueryProvider).GetRuntimeMethod(nameof(ExecuteCollectionAsync), new[] { typeof(QueryModel) }));
		private static readonly MethodInfo _executeScalarAsyncMethod =
			(typeof(QueryProvider).GetRuntimeMethod(nameof(ExecuteScalarAsync), new[] { typeof(QueryModel) }));
		private static readonly MethodInfo _executeSingleAsyncMethod =
			(typeof(QueryProvider).GetRuntimeMethod(nameof(ExecuteSingleAsync), new[] { typeof(QueryModel), typeof(bool) }));

#if NET5_0
		private static readonly MethodInfo _enumerateCollectionAsyncMethod =
			(typeof(QueryProvider).GetRuntimeMethod(nameof(EnumerateCollectionAsync), new[] { typeof(QueryModel) }));
#endif

		private readonly Type _queryableType;
		private readonly IQueryParser _queryParser;
		private readonly IAsyncQueryExecutor _executor;

		/// <summary>
		/// Initializes a new instance of <see cref="DefaultQueryProvider"/> using a custom <see cref="IQueryParser"/>.
		/// </summary>
		/// <param name="queryableType">
		///   A type implementing <see cref="IQueryable{T}"/>. This type is used to construct the chain of query operators. Must be a generic type
		///   definition.
		/// </param>
		/// <param name="queryParser">The <see cref="IQueryParser"/> used to parse queries. Specify an instance of 
		///   <see cref="Parsing.Structure.QueryParser"/> for default behavior. See also <see cref="QueryParser.CreateDefault"/>.</param>
		/// <param name="executor">The <see cref="IQueryExecutor"/> used to execute queries against a specific query backend.</param>
		public QueryProvider(Type queryableType, IQueryParser queryParser, IAsyncQueryExecutor executor)
		{
			//ArgumentUtility.CheckNotNull("queryableType", queryableType);
			//ArgumentUtility.CheckNotNull("queryParser", queryParser);
			//ArgumentUtility.CheckNotNull("executor", executor);

			CheckQueryableType(queryableType);

			_queryableType = queryableType;
			_queryParser = queryParser;
			_executor = executor;
		}

		private void CheckQueryableType(Type queryableType)
		{
			//ArgumentUtility.CheckTypeIsAssignableFrom("queryableType", queryableType, typeof(IQueryable));

			var queryableTypeInfo = queryableType.GetTypeInfo();
			if (!queryableTypeInfo.IsGenericTypeDefinition)
			{
				var message = string.Format(
					"Expected the generic type definition of an implementation of IQueryable<T>, but was '{0}'.",
					queryableType);
				throw new ArgumentException(message, "queryableType");
			}
			var genericArgumentCount = queryableTypeInfo.GenericTypeParameters.Length;
			if (genericArgumentCount != 1)
			{
				var message = string.Format(
					"Expected the generic type definition of an implementation of IQueryable<T> with exactly one type argument, but found {0} arguments on '{1}.",
					genericArgumentCount,
					queryableType);
				throw new ArgumentException(message, "queryableType");
			}
		}

		/// <summary>
		/// Gets the type of queryable created by this provider. This is the generic type definition of an implementation of <see cref="IQueryable{T}"/>
		/// (usually a subclass of <see cref="QueryableBase{T}"/>) with exactly one type argument.
		/// </summary>
		public Type QueryableType
		{
			get { return _queryableType; }
		}

		/// <summary>
		/// Gets the <see cref="QueryParser"/> used by this <see cref="QueryProvider"/> to parse LINQ queries.
		/// </summary>
		/// <value>The query parser.</value>
		public IQueryParser QueryParser
		{
			get { return _queryParser; }
		}

		/// <summary>
		/// Gets or sets the implementation of <see cref="IQueryExecutor"/> used to execute queries created via <see cref="CreateQuery{T}"/>.
		/// </summary>
		/// <value>The executor used to execute queries.</value>
		public IAsyncQueryExecutor Executor
		{
			get { return _executor; }
		}

		/// <summary>
		/// Constructs an <see cref="IQueryable"/> object that can evaluate the query represented by a specified expression tree. This
		/// method delegates to <see cref="CreateQuery{T}"/>.
		/// </summary>
		/// <param name="expression">An expression tree that represents a LINQ query.</param>
		/// <returns>
		/// An <see cref="IQueryable"/> that can evaluate the query represented by the specified expression tree.
		/// </returns>
		public IQueryable CreateQuery(Expression expression)
		{
			//ArgumentUtility.CheckNotNull("expression", expression);

			//Type elementType = ReflectionUtility.GetItemTypeOfClosedGenericIEnumerable(expression.Type, "expression");
			//return (IQueryable)_genericCreateQueryMethod.MakeGenericMethod(elementType).Invoke(this, new object[] { expression });

			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates a new <see cref="IQueryable"/> (of type <see cref="QueryableType"/> with <typeparamref name="T"/> as its generic argument) that
		/// represents the query defined by <paramref name="expression"/> and is able to enumerate its results.
		/// </summary>
		/// <typeparam name="T">The type of the data items returned by the query.</typeparam>
		/// <param name="expression">An expression representing the query for which a <see cref="IQueryable{T}"/> should be created.</param>
		/// <returns>An <see cref="IQueryable{T}"/> that represents the query defined by <paramref name="expression"/>.</returns>
		public IQueryable<T> CreateQuery<T>(Expression expression)
		{
			return (IQueryable<T>)Activator.CreateInstance(QueryableType.MakeGenericType(typeof(T)), this, expression);
		}

		/// <summary>
		/// Executes the query defined by the specified expression by parsing it with a
		/// <see cref="QueryParser"/> and then running it through the <see cref="Executor"/>.
		/// The result is cast to <typeparamref name="TResult"/>.
		/// </summary>
		/// <typeparam name="TResult">The type of the query result.</typeparam>
		/// <param name="expression">The query expression to be executed.</param>
		/// <returns>The result of the query cast to <typeparamref name="TResult"/>.</returns>
		/// <remarks>
		/// This method is called by the standard query operators that return a single value, such as 
		/// <see cref="Queryable.Count{TSource}(System.Linq.IQueryable{TSource})"/> or 
		/// <see cref="Queryable.First{TSource}(System.Linq.IQueryable{TSource})"/>.
		/// In addition, it is called by <see cref="QueryableBase{T}"/> to execute queries that return sequences.
		/// </remarks>
		public TResult Execute<TResult>(Expression expression)
		{
			//ArgumentUtility.CheckNotNull("expression", expression);

			var queryModel = GenerateQueryModel(expression);
			var outputDataInfo = queryModel.GetOutputDataInfo();

			if (outputDataInfo is StreamedSequenceInfo sequenceDataInfo)
			{
				var executeMethod = _executeCollectionMethod.MakeGenericMethod(sequenceDataInfo.ResultItemType);
				// wrap executeMethod into a delegate instead of calling Invoke in order to allow for exceptions that are bubbled up correctly
				var func = (Func<QueryModel, TResult>)executeMethod.CreateDelegate(
					 typeof(Func<QueryModel, TResult>), this);
				return func(queryModel);
			}
			else if (outputDataInfo is StreamedScalarValueInfo scalarDataInfo)
			{
				var executeMethod = _executeScalarMethod.MakeGenericMethod(scalarDataInfo.DataType);
				// wrap executeMethod into a delegate instead of calling Invoke in order to allow for exceptions that are bubbled up correctly
				var func = (Func<QueryModel, TResult>)executeMethod.CreateDelegate(
					 typeof(Func<QueryModel, TResult>), this);
				return func(queryModel);
			}
			else if (outputDataInfo is StreamedSingleValueInfo singleDataInfo)
			{
				var executeMethod = _executeSingleMethod.MakeGenericMethod(singleDataInfo.DataType);
				// wrap executeMethod into a delegate instead of calling Invoke in order to allow for exceptions that are bubbled up correctly
				var func = (Func<QueryModel, bool, TResult>)executeMethod.CreateDelegate(
					 typeof(Func<QueryModel, bool, TResult>), this);
				return func(queryModel, singleDataInfo.ReturnDefaultWhenEmpty);
			}
			else
			{
				throw new NotSupportedException("Unsupported result type; expected Sequence, Scalar or Single Value");
			}
		}

		/// <summary>
		/// Executes the query defined by the specified expression by parsing it with a
		/// <see cref="QueryParser"/> and then running it through the <see cref="Executor"/>.
		/// </summary>
		/// <param name="expression">The query expression to be executed.</param>
		/// <returns>The result of the query.</returns>
		/// <remarks>
		/// This method is similar to the <see cref="IQueryProvider.Execute{TResult}"/> method, but without the cast to a defined return type.
		/// </remarks>
		public object Execute(Expression expression)
		{
			//ArgumentUtility.CheckNotNull("expression", expression);

			// In Re-linq this is used instead in conjunction with Execute<T> but I don't think
			// it's necessary for us (it doesn't appear to be called from anywhere, at least)
			throw new NotImplementedException();
		}

		public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var queryModel = GenerateQueryModel(expression);
			var outputDataInfo = queryModel.GetOutputDataInfo();

			if (outputDataInfo is StreamedSequenceInfo sequenceDataInfo)
			{
				var executeMethod = _executeCollectionAsyncMethod.MakeGenericMethod(sequenceDataInfo.ResultItemType);
				// wrap executeMethod into a delegate instead of calling Invoke in order to allow for exceptions that are bubbled up correctly
				var func = (Func<QueryModel, TResult>)executeMethod.CreateDelegate(
					 typeof(Func<QueryModel, TResult>), this);
				return func(queryModel);
			}
			else if (outputDataInfo is StreamedScalarValueInfo scalarDataInfo)
			{
				var executeMethod = _executeScalarAsyncMethod.MakeGenericMethod(scalarDataInfo.DataType);
				// wrap executeMethod into a delegate instead of calling Invoke in order to allow for exceptions that are bubbled up correctly
				var func = (Func<QueryModel, TResult>)executeMethod.CreateDelegate(
					 typeof(Func<QueryModel, TResult>), this);
				return func(queryModel);
			}
			else if (outputDataInfo is StreamedSingleValueInfo singleDataInfo)
			{
				var executeMethod = _executeSingleAsyncMethod.MakeGenericMethod(singleDataInfo.DataType);
				// wrap executeMethod into a delegate instead of calling Invoke in order to allow for exceptions that are bubbled up correctly
				var func = (Func<QueryModel, bool, TResult>)executeMethod.CreateDelegate(
					 typeof(Func<QueryModel, bool, TResult>), this);
				return func(queryModel, singleDataInfo.ReturnDefaultWhenEmpty);
			}
			else
			{
				throw new NotSupportedException("Unsupported result type; expected Sequence, Scalar or Single Value");
			}
		}

#if NET5_0
		public TResult EnumerateAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var queryModel = GenerateQueryModel(expression);
			var outputDataInfo = queryModel.GetOutputDataInfo();

			if (outputDataInfo is StreamedSequenceInfo sequenceDataInfo)
			{
				var enumerateMethod = _enumerateCollectionAsyncMethod.MakeGenericMethod(sequenceDataInfo.ResultItemType);
				// wrap executeMethod into a delegate instead of calling Invoke in order to allow for exceptions that are bubbled up correctly
				var func = (Func<QueryModel, TResult>)enumerateMethod.CreateDelegate(
					 typeof(Func<QueryModel, TResult>), this);
				return func(queryModel);
			}
			else
			{
				throw new NotSupportedException("Unsupported result type; expected Sequence, Scalar or Single Value");
			}
		}
#endif

		/// <summary>
		/// The method generates a <see cref="QueryModel"/>.
		/// </summary>
		/// <param name="expression">The query as expression chain.</param>
		/// <returns>a <see cref="QueryModel"/></returns>
		public virtual QueryModel GenerateQueryModel(Expression expression)
		{
			//ArgumentUtility.CheckNotNull("expression", expression);

			return _queryParser.GetParsedQuery(expression);
		}

		public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
		{
			//ArgumentUtility.CheckNotNull("queryModel", queryModel);
			//ArgumentUtility.CheckNotNull("executor", executor);

			return Executor.ExecuteCollection<T>(queryModel);
		}

		public T ExecuteScalar<T>(QueryModel queryModel)
		{
			//ArgumentUtility.CheckNotNull("queryModel", queryModel);
			//ArgumentUtility.CheckNotNull("executor", executor);

			return Executor.ExecuteScalar<T>(queryModel);
		}

		public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			//ArgumentUtility.CheckNotNull("queryModel", queryModel);
			//ArgumentUtility.CheckNotNull("executor", executor);

			return Executor.ExecuteSingle<T>(queryModel, returnDefaultWhenEmpty);
		}

		public async Task<IList<T>> ExecuteCollectionAsync<T>(QueryModel queryModel)
		{
			//ArgumentUtility.CheckNotNull("queryModel", queryModel);
			//ArgumentUtility.CheckNotNull("executor", executor);

			return await Executor.ExecuteCollectionAsync<T>(queryModel);
		}

		public async Task<T> ExecuteScalarAsync<T>(QueryModel queryModel)
		{
			//ArgumentUtility.CheckNotNull("queryModel", queryModel);
			//ArgumentUtility.CheckNotNull("executor", executor);

			return await Executor.ExecuteScalarAsync<T>(queryModel);
		}

		public async Task<T> ExecuteSingleAsync<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			//ArgumentUtility.CheckNotNull("queryModel", queryModel);
			//ArgumentUtility.CheckNotNull("executor", executor);

			return await Executor.ExecuteSingleAsync<T>(queryModel, returnDefaultWhenEmpty);
		}

#if NET5_0
		public async IAsyncEnumerable<T> EnumerateCollectionAsync<T>(QueryModel queryModel)
		{
			//ArgumentUtility.CheckNotNull("queryModel", queryModel);
			//ArgumentUtility.CheckNotNull("executor", executor);

			await foreach (var item in Executor.EnumerateCollectionAsync<T>(queryModel))
			{
				yield return item;
			}
		}
#endif
	}
}

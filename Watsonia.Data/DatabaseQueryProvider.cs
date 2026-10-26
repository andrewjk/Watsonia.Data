using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Watsonia.Data.Query;

namespace Watsonia.Data
{
	/// <summary>
	/// The LINQ query provider for the database, based on the IQToolkit-derived query engine.
	/// </summary>
	public sealed class DatabaseQueryProvider : Query.QueryProvider, IAsyncQueryProvider
	{
		public DatabaseQueryProvider(Database database)
			: base(database)
		{
		}

		IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
		{
			// Keep the DatabaseQuery surface (Include paths etc.) through query composition
			var elementType = TypeHelper.GetElementType(expression.Type);
			return (IQueryable<S>)Activator.CreateInstance(
				typeof(DatabaseQuery<>).MakeGenericType(elementType),
				this,
				expression,
				elementType);
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			var elementType = TypeHelper.GetElementType(expression.Type);
			return (IQueryable)Activator.CreateInstance(
				typeof(DatabaseQuery<>).MakeGenericType(elementType),
				this,
				expression,
				elementType);
		}

		/// <summary>
		/// Executes the query asynchronously. The heavy lifting is still synchronous - this wraps the
		/// result in a completed task so that the async API surface works without Remotion.Linq.
		/// </summary>
		public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var result = this.Execute(expression);
			return (TResult)WrapInCompletedTask(result, typeof(TResult));
		}

		/// <summary>
		/// Executes the query asynchronously as a stream of results.
		/// </summary>
#if NET5_0_OR_GREATER
		public TResult EnumerateAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var result = this.Execute(expression);
			var itemType = typeof(TResult).GetGenericArguments()[0];
			var enumerateCoreMethod = typeof(DatabaseQueryProvider)
				.GetMethod(nameof(EnumerateAsyncCore), BindingFlags.NonPublic | BindingFlags.Static)
				.MakeGenericMethod(itemType);
			return (TResult)enumerateCoreMethod.Invoke(null, new object[] { result, cancellationToken });
		}

		private static async IAsyncEnumerable<T> EnumerateAsyncCore<T>(IEnumerable<T> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var item in items)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return item;
			}

			await Task.CompletedTask;
		}
#endif

		private static object WrapInCompletedTask(object result, Type taskType)
		{
			if (!taskType.IsGenericType || taskType.GetGenericTypeDefinition() != typeof(Task<>))
			{
				return result;
			}

			var innerType = taskType.GetGenericArguments()[0];
			var innerResult = ConvertResult(result, innerType);
			var fromResultMethod = typeof(Task)
				.GetMethod("FromResult", BindingFlags.Public | BindingFlags.Static)
				.MakeGenericMethod(innerType);
			return fromResultMethod.Invoke(null, new[] { innerResult });
		}

		private static object ConvertResult(object result, Type targetType)
		{
			if (result == null)
			{
				return null;
			}

			if (targetType.IsInstanceOfType(result))
			{
				return result;
			}

			if (typeof(IEnumerable).IsAssignableFrom(targetType))
			{
				var elementType = targetType.IsGenericType ? targetType.GetGenericArguments()[0] : typeof(object);
				var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
				foreach (var item in (IEnumerable)result)
				{
					list.Add(item);
				}
				return list;
			}

			return System.Convert.ChangeType(result, Nullable.GetUnderlyingType(targetType) ?? targetType);
		}
	}
}

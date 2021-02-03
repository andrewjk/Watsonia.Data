using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	public sealed class DatabaseQuery<T> : QueryableBase<T>, IDatabaseQuery
#if NET5_0
		, IAsyncEnumerable<T>
#endif
	{
		public List<string> IncludePaths { get; } = new List<string>();

		public List<Parameter> Parameters { get; } = new List<Parameter>();

		public DatabaseQuery(IQueryParser queryParser, IAsyncQueryExecutor executor)
			: base(new /*Default*/QueryProvider(typeof(DatabaseQuery<>), queryParser, executor))
		{
		}

		public DatabaseQuery(IQueryProvider provider, Expression expression)
			: base(provider, expression)
		{
		}

		public DatabaseQuery<T> Include(string path)
		{
			this.IncludePaths.Add(path);
			return this;
		}

		public DatabaseQuery<T> Include(Expression<Func<T, object>> property)
		{
			// TODO: Probably shouldn't be throwing away the property and type information...
			return Include(FuncToString(property.Body));
		}

		public DatabaseQuery<T> Include<T2>(Expression<Func<T, object>> property, Expression<Func<T2, object>> property2)
		{
			// TODO: Probably shouldn't be throwing away the property and type information...
			return Include(FuncToString(property.Body) + "." + FuncToString(property2.Body));
		}

		public DatabaseQuery<T> Include<T2, T3>(Expression<Func<T, object>> property, Expression<Func<T2, object>> property2, Expression<Func<T3, object>> property3)
		{
			// TODO: Probably shouldn't be throwing away the property and type information...
			return Include(FuncToString(property.Body) + "." + FuncToString(property2.Body) + "." + FuncToString(property3.Body));
		}

#if NET5_0
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			// TODO: ExecuteAsync needs to be able to return an IAsyncEnumerable somehow
			// Maybe we can create something called ResultSequence and return that from LoadCollection??
			var asyncProvider = (IAsyncQueryProvider)this.Provider;
			return asyncProvider
				.EnumerateAsync<IAsyncEnumerable<T>>(this.Expression, cancellationToken)
				.GetAsyncEnumerator(cancellationToken);
		}
#endif

		private static string FuncToString(Expression selector)
		{
			// This came from http://msmvps.com/blogs/matthieu/archive/2008/06/06/entity-framework-include-with-func-next.aspx
			switch (selector.NodeType)
			{
				case ExpressionType.MemberAccess:
				{
					return ((selector as MemberExpression).Member as PropertyInfo).Name;
				}
				case ExpressionType.Call:
				{
					var method = selector as MethodCallExpression;
					return FuncToString(method.Arguments[0]) + "." + FuncToString(method.Arguments[1]);
				}
				case ExpressionType.Quote:
				{
					return FuncToString(((selector as UnaryExpression).Operand as LambdaExpression).Body);
				}
			}

			throw new InvalidOperationException();
		}
	}
}

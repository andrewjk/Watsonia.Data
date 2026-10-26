using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Watsonia.Data.Query;
using Watsonia.QueryBuilder;

namespace Watsonia.Data
{
	/// <summary>
	/// A query against the database, powered by the IQToolkit-derived query engine.
	/// </summary>
	public sealed class DatabaseQuery<T> : Query<T>, IDatabaseQuery
	{
		public List<string> IncludePaths { get; } = new List<string>();

		public List<Parameter> Parameters { get; } = new List<Parameter>();

		public DatabaseQuery(Query.QueryProvider provider)
			: base(provider)
		{
		}

		public DatabaseQuery(Query.QueryProvider provider, Type elementType)
			: base(provider)
		{
			this.ElementType = elementType;
		}

		public DatabaseQuery(Query.QueryProvider provider, Expression expression)
			: base(provider, expression)
		{
		}

		public DatabaseQuery(Query.QueryProvider provider, Expression expression, Type elementType)
			: base(provider, expression)
		{
			this.ElementType = elementType;
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

		private static string FuncToString(Expression selector)
		{
			switch (selector.NodeType)
			{
				case (ExpressionType.MemberAccess):
					return ((MemberExpression)selector).Member.Name;
				case (ExpressionType.Call):
					return ((MethodCallExpression)selector).Method.Name;
				default:
					throw new NotSupportedException();
			}
		}
	}
}

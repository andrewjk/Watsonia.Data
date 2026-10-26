using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Watsonia.QueryBuilder
{
	/// <summary>
	/// Removes compiler-generated array-to-span conversions from expression trees.
	/// </summary>
	/// <remarks>
	/// Newer C# compilers bind methods like <c>MemoryExtensions.Contains</c> inside expression
	/// trees, emitting an <c>op_Implicit</c> conversion of the array to a <c>ReadOnlySpan</c>.
	/// Those conversions cannot be compiled or walked by the query engine, so they are replaced
	/// with the underlying array expression.
	/// </remarks>
	internal class SpanConversionStripper : ExpressionVisitor
	{
		public static Expression Strip(Expression expression)
		{
			return new SpanConversionStripper().Visit(expression);
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			// The compiler binds ids.Contains(item) to MemoryExtensions.Contains(ReadOnlySpan<T>, T)
			// with an op_Implicit conversion of the array; rewrite it to Enumerable.Contains, which
			// the query engine can walk and which compiles without ref-struct types
			if (node.Method.IsStatic
				&& node.Method.Name == "Contains"
				&& node.Method.DeclaringType?.Name == "MemoryExtensions"
				&& node.Arguments.Count == 2)
			{
				var collection = this.Visit(node.Arguments[0]);
				var item = this.Visit(node.Arguments[1]);
				var elementType = collection.Type.IsArray
					? collection.Type.GetElementType()
					: (collection.Type.IsGenericType ? collection.Type.GetGenericArguments()[0] : item.Type);
				var containsMethod = typeof(System.Linq.Enumerable)
					.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
					.Single(m => m.Name == "Contains" && m.GetParameters().Length == 2
						&& m.GetParameters()[0].ParameterType.IsGenericType
						&& m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					.MakeGenericMethod(elementType);
				return Expression.Call(null, containsMethod, collection, item);
			}

			if (node.Method.IsStatic
				&& node.Arguments.Count == 1
				&& (node.Method.Name == "op_Implicit" || node.Method.Name == "op_Explicit"))
			{
				var returnTypeName = node.Method.ReturnType.Name;
				if (returnTypeName.StartsWith("ReadOnlySpan") || returnTypeName.StartsWith("Span"))
				{
					return this.Visit(node.Arguments[0]);
				}
			}

			return base.VisitMethodCall(node);
		}
	}
}

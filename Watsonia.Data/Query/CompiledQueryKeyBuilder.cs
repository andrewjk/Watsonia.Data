using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ExpressionVisitor = Watsonia.QueryBuilder.ExpressionVisitor;

namespace Watsonia.Data.Query
{
	/// <summary>
	/// Creates cache keys for compiled query plans.
	/// </summary>
	/// <remarks>
	/// The key combines the expression's structure with the values of the captured constants
	/// (including a query's Include paths) so that a cached plan is only reused when it would
	/// produce the same statement. Expressions containing constructs whose value can't be
	/// captured safely (method calls other than LINQ operators, mutable collections) are not
	/// cacheable.
	/// </remarks>
	internal static class CompiledQueryKeyBuilder
	{
		public static bool TryCreate(Expression expression, out string key)
		{
			var builder = new StringBuilder(expression.ToString());
			var walker = new Walker(builder);
			walker.Walk(expression);
			key = builder.ToString();
			return walker.Cacheable;
		}

		private static bool IsSafeValue(object value)
		{
			if (value == null)
			{
				return true;
			}

			var type = value.GetType();
			return type.IsPrimitive
				|| type.IsEnum
				|| value is string
				|| value is decimal
				|| value is DateTime
				|| value is DateTimeOffset
				|| value is TimeSpan
				|| value is Guid;
		}

		private static string Format(object value)
		{
			if (value == null)
			{
				return "null";
			}

			if (value is DateTime dateTime)
			{
				return dateTime.ToString("O", CultureInfo.InvariantCulture);
			}

			if (value is DateTimeOffset dateTimeOffset)
			{
				return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
			}

			if (value is IFormattable formattable)
			{
				return formattable.ToString(null, CultureInfo.InvariantCulture);
			}

			return value.ToString();
		}

		private static bool TryEvaluate(Expression expression, out object value)
		{
			var members = new Stack<MemberInfo>();
			var current = expression;
			while (current is MemberExpression member)
			{
				members.Push(member.Member);
				current = member.Expression;
			}

			while (current is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
			{
				current = unary.Operand;
			}

			if (!(current is ConstantExpression constant))
			{
				value = null;
				return false;
			}

			value = constant.Value;
			while (members.Count > 0)
			{
				if (value == null)
				{
					return false;
				}

				var member = members.Pop();
				if (member is FieldInfo field)
				{
					value = field.GetValue(value);
				}
				else if (member is PropertyInfo property)
				{
					value = property.GetValue(value, null);
				}
				else
				{
					return false;
				}
			}

			return true;
		}

		private sealed class Walker : ExpressionVisitor
		{
			private readonly StringBuilder _builder;

			public Walker(StringBuilder builder)
			{
				_builder = builder;
			}

			public bool Cacheable { get; private set; } = true;

			public void Walk(Expression expression)
			{
				base.Visit(expression);
			}

			protected override Expression VisitConstant(ConstantExpression node)
			{
				if (node.Value is IDatabaseQuery query)
				{
					// Include paths are query state rather than expression state
					_builder.Append("|dq[").Append(string.Join(",", query.IncludePaths)).Append(']');
				}
				else if (IsSafeValue(node.Value))
				{
					_builder.Append('|').Append(Format(node.Value));
				}
				else
				{
					Cacheable = false;
				}

				return node;
			}

			protected override Expression VisitMemberAccess(MemberExpression node)
			{
				if (TryEvaluate(node, out var value))
				{
					if (IsSafeValue(value))
					{
						_builder.Append('|').Append(Format(value));
					}
					else
					{
						Cacheable = false;
					}

					return node;
				}

				// Members rooted at a query parameter are entity property accesses and are part of
				// the structure. Anything else (e.g. DateTime.Now) could change between calls.
				if (IsRootedAtParameter(node))
				{
					return node;
				}

				Cacheable = false;
				return node;
			}

			private static bool IsRootedAtParameter(Expression expression)
			{
				var current = expression;
				while (current is MemberExpression member)
				{
					current = member.Expression;
				}

				while (current is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
				{
					current = unary.Operand;
				}

				return current is ParameterExpression;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				// Only LINQ operators may appear; any other call could produce volatile values
				if (node.Method.DeclaringType != typeof(System.Linq.Queryable))
				{
					Cacheable = false;
					return node;
				}

				return base.VisitMethodCall(node);
			}
		}
	}
}

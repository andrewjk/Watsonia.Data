using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Watsonia.Data;
using Watsonia.Data.Query.Expressions;

namespace Watsonia.Data.Query.Translation
{
	internal sealed class QueryTypeReplacer : DbExpressionVisitor
	{
		private Database _database;

		private QueryTypeReplacer(Database database, Expression expr)
		{
			_database = database;
		}

		public static Expression Replace(Database database, Expression expr)
		{
			return new QueryTypeReplacer(database, expr).Visit(expr);
		}

		protected override Expression VisitMemberAccess(MemberExpression node)
		{
			Type nodeType = node.Member.ReflectedType;
			if (_database.Configuration.ShouldMapType(nodeType))
			{
				Type newNodeType = DynamicProxyFactory.GetDynamicProxyType(nodeType, _database);
				MemberExpression newNode = Expression.MakeMemberAccess(
					base.Visit(node.Expression),
					newNodeType.GetProperty(node.Member.Name));
				return newNode;
			}
			else
			{
				return base.VisitMemberAccess(node);
			}
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			Type nodeType = node.Type;
			if (_database.Configuration.ShouldMapType(nodeType))
			{
				Type newNodeType = DynamicProxyFactory.GetDynamicProxyType(nodeType, _database);
				ParameterExpression newNode = Expression.Parameter(newNodeType, node.Name);
				return newNode;
			}
			else
			{
				return base.VisitParameter(node);
			}
		}
	}
}
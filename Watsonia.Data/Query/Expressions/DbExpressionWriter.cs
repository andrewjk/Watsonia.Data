﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.IO;
using Watsonia.Data.Query;

namespace Watsonia.Data.Query.Expressions
{
	/// <summary>
	/// Writes out an expression tree (including DbExpression nodes) in a C#-ish syntax.
	/// </summary>
	internal class DbExpressionWriter : ExpressionWriter
	{
		Dictionary<TableAlias, int> aliasMap = new Dictionary<TableAlias, int>();

		protected DbExpressionWriter(TextWriter writer)
			: base(writer)
		{
		}

		public static new void Write(TextWriter writer, Expression expression)
		{
			new DbExpressionWriter(writer).Visit(expression);
		}

		public static new string WriteToString(Expression expression)
		{
			StringWriter sw = new StringWriter();
			Write(sw, expression);
			return sw.ToString();
		}

		protected override Expression Visit(Expression exp)
		{
			if (exp == null)
			{
				return null;
			}

			switch ((DbExpressionType)exp.NodeType)
			{
				case DbExpressionType.Projection:
				{
					return this.VisitProjection((ProjectionExpression)exp);
				}
				case DbExpressionType.ClientJoin:
				{
					return this.VisitClientJoin((ClientJoinExpression)exp);
				}
				case DbExpressionType.SelectExpression:
				{
					return this.VisitSelect((SelectExpression)exp);
				}
				case DbExpressionType.OuterJoined:
				{
					return this.VisitOuterJoined((OuterJoinedExpression)exp);
				}
				case DbExpressionType.Column:
				{
					return this.VisitColumn((ColumnExpression)exp);
				}
				case DbExpressionType.Insert:
				case DbExpressionType.Update:
				case DbExpressionType.Delete:
				case DbExpressionType.If:
				case DbExpressionType.Block:
				case DbExpressionType.Declaration:
				{
					return this.VisitCommand((CommandExpression)exp);
				}
				case DbExpressionType.Batch:
				{
					return this.VisitBatch((BatchExpression)exp);
				}
				case DbExpressionType.Function:
				{
					return this.VisitFunction((FunctionExpression)exp);
				}
				case DbExpressionType.Entity:
				{
					return this.VisitEntity((EntityExpression)exp);
				}
				default:
				{
					if (exp is DbExpression)
					{
						// TODO: this.Write(this.FormatQuery(exp));
						this.Write(exp.ToString());
						return exp;
					}
					else
					{
						return base.Visit(exp);
					}
				}
			}
		}

		protected void AddAlias(TableAlias alias)
		{
			if (!this.aliasMap.ContainsKey(alias))
			{
				this.aliasMap.Add(alias, this.aliasMap.Count);
			}
		}

		protected virtual Expression VisitProjection(ProjectionExpression projection)
		{
			this.AddAlias(projection.SelectExpression.Alias);
			this.Write("Project(");
			this.WriteLine(Indentation.Inner);
			this.Write("@\"");
			this.Visit(projection.SelectExpression);
			this.Write("\",");
			this.WriteLine(Indentation.Same);
			this.Visit(projection.Projector);
			this.Write(",");
			this.WriteLine(Indentation.Same);
			this.Visit(projection.Aggregator);
			this.WriteLine(Indentation.Outer);
			this.Write(")");
			return projection;
		}

		protected virtual Expression VisitClientJoin(ClientJoinExpression join)
		{
			this.AddAlias(join.Projection.SelectExpression.Alias);
			this.Write("ClientJoin(");
			this.WriteLine(Indentation.Inner);
			this.Write("OuterKey(");
			this.VisitExpressionList(join.OuterKey);
			this.Write("),");
			this.WriteLine(Indentation.Same);
			this.Write("InnerKey(");
			this.VisitExpressionList(join.InnerKey);
			this.Write("),");
			this.WriteLine(Indentation.Same);
			this.Visit(join.Projection);
			this.WriteLine(Indentation.Outer);
			this.Write(")");
			return join;
		}

		protected virtual Expression VisitOuterJoined(OuterJoinedExpression outer)
		{
			this.Write("Outer(");
			this.WriteLine(Indentation.Inner);
			this.Visit(outer.Test);
			this.Write(", ");
			this.WriteLine(Indentation.Same);
			this.Visit(outer.Expression);
			this.WriteLine(Indentation.Outer);
			this.Write(")");
			return outer;
		}

		protected virtual Expression VisitSelect(SelectExpression select)
		{
			// TODO: this.Write(select.QueryText);
			this.Write("SelectExpression");
			return select;
		}

		protected virtual Expression VisitCommand(CommandExpression command)
		{
			// TODO: this.Write(this.FormatQuery(command));
			this.Write("CommandExpression");
			return command;
		}

		protected virtual Expression VisitBatch(BatchExpression batch)
		{
			this.Write("Batch(");
			this.WriteLine(Indentation.Inner);
			this.Visit(batch.Input);
			this.Write(",");
			this.WriteLine(Indentation.Same);
			this.Visit(batch.Operation);
			this.Write(",");
			this.WriteLine(Indentation.Same);
			this.Visit(batch.BatchSize);
			this.Write(", ");
			this.Visit(batch.Stream);
			this.WriteLine(Indentation.Outer);
			this.Write(")");
			return batch;
		}

		protected virtual Expression VisitVariable(VariableExpression vex)
		{
			this.Write("@" + vex.Name);
			return vex;
		}

		protected virtual Expression VisitFunction(FunctionExpression function)
		{
			this.Write("FUNCTION ");
			this.Write(function.Name);
			if (function.Arguments.Count > 0)
			{
				this.Write("(");
				this.VisitExpressionList(function.Arguments);
				this.Write(")");
			}
			return function;
		}

		protected virtual Expression VisitEntity(EntityExpression entity)
		{
			this.Visit(entity.Expression);
			return entity;
		}

		protected override Expression VisitConstant(ConstantExpression c)
		{
			if (c.Type == typeof(QueryCommand))
			{
				QueryCommand qc = (QueryCommand)c.Value;
				this.Write("new QueryCommand {");
				this.WriteLine(Indentation.Inner);
				this.Write("\"");
				this.Visit(qc.Expression);
				this.Write("\"");
				this.Write(",");
				this.WriteLine(Indentation.Same);
				this.Visit(Expression.Constant(qc.Parameters));
				this.Write(")");
				this.WriteLine(Indentation.Outer);
				return c;
			}
			return base.VisitConstant(c);
		}

		protected virtual Expression VisitColumn(ColumnExpression column)
		{
			int iAlias;
			string aliasName =
				this.aliasMap.TryGetValue(column.Alias, out iAlias)
				? "A" + iAlias
				: "A" + (column.Alias != null ? column.Alias.GetHashCode().ToString() : "") + "?";

			this.Write(aliasName);
			this.Write(".");
			this.Write("Column(\"");
			this.Write(column.Name);
			this.Write("\")");
			return column;
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using IQToolkit.Data.Common;
using Watsonia.Data.Sql;

namespace Watsonia.Data.Linq
{
	/// <summary>
	/// Formats a query expression into common SQL language syntax
	/// </summary>
	internal class StatementCreator : DbExpressionVisitor
	{
		private readonly Stack<StatementPart> _stack = new Stack<StatementPart>();

		private readonly Dictionary<TableAlias, string> aliases = new Dictionary<TableAlias, string>();

		private Stack<StatementPart> Stack
		{
			get
			{
				return _stack;
			}
		}

		protected bool HideColumnAliases
		{
			get;
			set;
		}

		protected bool HideTableAliases
		{
			get;
			set;
		}

		private Select Select
		{
			get;
			set;
		}

		private bool IsNested
		{
			get;
			set;
		}

		private StatementCreator()
		{
		}

		public static Select Compile(Expression expression)
		{
			StatementCreator creator = new StatementCreator();
			creator.Visit(expression);
			return creator.Select;
		}

		protected virtual string GetAliasName(TableAlias alias)
		{
			string name;
			if (!this.aliases.TryGetValue(alias, out name))
			{
				name = "A" + alias.GetHashCode() + "?";
				this.aliases.Add(alias, name);
			}
			return name;
		}

		protected void AddAlias(TableAlias alias)
		{
			string name;
			if (!this.aliases.TryGetValue(alias, out name))
			{
				name = "t" + this.aliases.Count;
				this.aliases.Add(alias, name);
			}
		}

		protected virtual void AddAliases(Expression expr)
		{
			AliasedExpression ax = expr as AliasedExpression;
			if (ax != null)
			{
				this.AddAlias(ax.Alias);
			}
			else
			{
				JoinExpression jx = expr as JoinExpression;
				if (jx != null)
				{
					this.AddAliases(jx.Left);
					this.AddAliases(jx.Right);
				}
			}
		}

		protected override Expression Visit(Expression exp)
		{
			if (exp == null) return null;

			// check for supported node types first 
			// non-supported ones should not be visited (as they would produce bad SQL)
			switch (exp.NodeType)
			{
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				case ExpressionType.UnaryPlus:
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Divide:
				case ExpressionType.Modulo:
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.Coalesce:
				case ExpressionType.RightShift:
				case ExpressionType.LeftShift:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.Power:
				case ExpressionType.Conditional:
				case ExpressionType.Constant:
				case ExpressionType.MemberAccess:
				case ExpressionType.Call:
				case ExpressionType.New:
				case (ExpressionType)DbExpressionType.Table:
				case (ExpressionType)DbExpressionType.Column:
				case (ExpressionType)DbExpressionType.Select:
				case (ExpressionType)DbExpressionType.Join:
				case (ExpressionType)DbExpressionType.Aggregate:
				case (ExpressionType)DbExpressionType.Scalar:
				case (ExpressionType)DbExpressionType.Exists:
				case (ExpressionType)DbExpressionType.In:
				case (ExpressionType)DbExpressionType.AggregateSubquery:
				case (ExpressionType)DbExpressionType.IsNull:
				case (ExpressionType)DbExpressionType.Between:
				case (ExpressionType)DbExpressionType.RowCount:
				case (ExpressionType)DbExpressionType.Projection:
				case (ExpressionType)DbExpressionType.NamedValue:
				case (ExpressionType)DbExpressionType.Insert:
				case (ExpressionType)DbExpressionType.Update:
				case (ExpressionType)DbExpressionType.Delete:
				case (ExpressionType)DbExpressionType.Block:
				case (ExpressionType)DbExpressionType.If:
				case (ExpressionType)DbExpressionType.Declaration:
				case (ExpressionType)DbExpressionType.Variable:
				case (ExpressionType)DbExpressionType.Function:
				{
					return base.Visit(exp);
				}
				case ExpressionType.ArrayLength:
				case ExpressionType.Quote:
				case ExpressionType.TypeAs:
				case ExpressionType.ArrayIndex:
				case ExpressionType.TypeIs:
				case ExpressionType.Parameter:
				case ExpressionType.Lambda:
				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds:
				case ExpressionType.Invoke:
				case ExpressionType.MemberInit:
				case ExpressionType.ListInit:
				default:
				{
					throw new NotSupportedException(string.Format("The LINQ expression node of type {0} is not supported", exp.NodeType));
				}
			}
		}

		protected override Expression VisitMemberAccess(MemberExpression m)
		{
			if (m.Member.DeclaringType == typeof(string))
			{
				switch (m.Member.Name)
				{
					case "Length":
					{
						StringLengthFunction newFunction = new StringLengthFunction();
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
				}
			}
			else if (m.Member.DeclaringType == typeof(DateTime) || m.Member.DeclaringType == typeof(DateTimeOffset))
			{
				switch (m.Member.Name)
				{
					case "Date":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Date);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Day":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Day);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Month":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Month);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Year":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Year);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Hour":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Hour);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Minute":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Minute);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Second":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Second);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Millisecond":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.Millisecond);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "DayOfWeek":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.DayOfWeek);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "DayOfYear":
					{
						DatePartFunction newFunction = new DatePartFunction(DatePart.DayOfYear);
						this.Visit(m.Expression);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
				}
			}
			else if (m.Member.DeclaringType == typeof(DateTime?) || m.Member.DeclaringType == typeof(DateTimeOffset?))
			{
				// TODO: Should this apply to all nullable types?
				switch (m.Member.Name)
				{
					case "Value":
					{
						// Ignore .Value as it has no equivalent in SQL
						// (Or should it be ISNULL(x, y)?
						this.Visit(m.Expression);
						return m;
					}
				}
			}

			throw new NotSupportedException(string.Format("The member access '{0}' is not supported", m.Member));
		}

		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
			if (m.Method.DeclaringType == typeof(string))
			{
				switch (m.Method.Name)
				{
					case "StartsWith":
					{
						Condition newCondition = new Condition();
						newCondition.Operator = SqlOperator.StartsWith;
						this.Visit(m.Object);
						newCondition.Field = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newCondition.Values.Add(this.Stack.Pop());
						this.Stack.Push(newCondition);
						return m;
					}
					case "EndsWith":
					{
						Condition newCondition = new Condition();
						newCondition.Operator = SqlOperator.EndsWith;
						this.Visit(m.Object);
						newCondition.Field = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newCondition.Values.Add(this.Stack.Pop());
						this.Stack.Push(newCondition);
						return m;
					}
					case "Contains":
					{
						Condition newCondition = new Condition();
						newCondition.Operator = SqlOperator.Contains;
						this.Visit(m.Object);
						newCondition.Field = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newCondition.Values.Add(this.Stack.Pop());
						this.Stack.Push(newCondition);
						return m;
					}
					case "Concat":
					{
						StringConcatenateFunction newFunction = new StringConcatenateFunction();
						IList<Expression> args = m.Arguments;
						if (args.Count == 1 && args[0].NodeType == ExpressionType.NewArrayInit)
						{
							args = ((NewArrayExpression)args[0]).Expressions;
						}
						for (int i = 0; i < args.Count; i++)
						{
							this.Visit(args[i]);
							newFunction.Arguments.Add(this.Stack.Pop());
						}
						this.Stack.Push(newFunction);
						return m;
					}
					case "IsNullOrEmpty":
					{
						Condition newCondition = new Condition();

						Condition isNullCondition = new Condition();
						this.Visit(m.Arguments[0]);
						isNullCondition.Field = this.Stack.Pop();
						isNullCondition.Operator = SqlOperator.Equals;
						isNullCondition.Values.Add(new ConstantPart(null));
						newCondition.SubConditions.Add(isNullCondition);

						Condition notEqualsCondition = new Condition();
						notEqualsCondition.Relationship = ConditionRelationship.Or;
						this.Visit(m.Arguments[0]);
						notEqualsCondition.Field = this.Stack.Pop();
						notEqualsCondition.Operator = SqlOperator.Equals;
						notEqualsCondition.Values.Add(new ConstantPart(""));
						newCondition.SubConditions.Add(notEqualsCondition);

						this.Stack.Push(newCondition);
						return m;
					}
					case "ToUpper":
					case "ToUpperInvariant":
					{
						StringToUpperFunction newFunction = new StringToUpperFunction();
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "ToLower":
					case "ToLowerInvariant":
					{
						StringToLowerFunction newFunction = new StringToLowerFunction();
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Replace":
					{
						StringReplaceFunction newFunction = new StringReplaceFunction();
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.OldValue = this.Stack.Pop();
						this.Visit(m.Arguments[1]);
						newFunction.NewValue = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Substring":
					{
						SubstringFunction newFunction = new SubstringFunction();
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.StartIndex = this.Stack.Pop();
						if (m.Arguments.Count > 1)
						{
							this.Visit(m.Arguments[1]);
							newFunction.Length = this.Stack.Pop();
						}
						else
						{
							newFunction.Length = new ConstantPart(8000);
						}
						this.Stack.Push(newFunction);
						return m;
					}
					case "Remove":
					{
						StringRemoveFunction newFunction = new StringRemoveFunction();
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.StartIndex = this.Stack.Pop();
						if (m.Arguments.Count > 1)
						{
							this.Visit(m.Arguments[1]);
							newFunction.Length = this.Stack.Pop();
						}
						else
						{
							newFunction.Length = new ConstantPart(8000);
						}
						this.Stack.Push(newFunction);
						return m;
					}
					case "IndexOf":
					{
						StringIndexFunction newFunction = new StringIndexFunction();
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.StringToFind = this.Stack.Pop();
						if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
						{
							this.Visit(m.Arguments[1]);
							newFunction.StartIndex = this.Stack.Pop();
						}
						this.Stack.Push(newFunction);
						return m;
					}
					case "Trim":
					{
						StringTrimFunction newFunction = new StringTrimFunction();
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
				}
			}
			else if (m.Method.DeclaringType == typeof(DateTime))
			{
				switch (m.Method.Name)
				{
					case "op_Subtract":
					{
						if (m.Arguments[1].Type == typeof(DateTime))
						{
							DateDifferenceFunction newFunction = new DateDifferenceFunction();
							this.Visit(m.Arguments[0]);
							newFunction.Date1 = this.Stack.Pop();
							this.Visit(m.Arguments[1]);
							newFunction.Date2 = this.Stack.Pop();
							this.Stack.Push(newFunction);
							return m;
						}
						break;
					}
					case "AddDays":
					{
						DateAddFunction newFunction = new DateAddFunction(DatePart.Day);
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.Number = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "AddMonths":
					{
						DateAddFunction newFunction = new DateAddFunction(DatePart.Month);
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.Number = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "AddYears":
					{
						DateAddFunction newFunction = new DateAddFunction(DatePart.Year);
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.Number = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "AddHours":
					{
						DateAddFunction newFunction = new DateAddFunction(DatePart.Hour);
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.Number = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "AddMinutes":
					{
						DateAddFunction newFunction = new DateAddFunction(DatePart.Minute);
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.Number = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "AddSeconds":
					{
						DateAddFunction newFunction = new DateAddFunction(DatePart.Second);
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.Number = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "AddMilliseconds":
					{
						DateAddFunction newFunction = new DateAddFunction(DatePart.Millisecond);
						this.Visit(m.Object);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[0]);
						newFunction.Number = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
				}
			}
			else if (m.Method.DeclaringType == typeof(Decimal))
			{
				switch (m.Method.Name)
				{
					case "Add":
					case "Subtract":
					case "Multiply":
					case "Divide":
					case "Remainder":
					{
						BinaryOperation newOperation = new BinaryOperation();
						this.VisitValue(m.Arguments[0]);
						newOperation.LeftExpression = (SourceExpression)this.Stack.Pop();
						newOperation.Operator = (BinaryOperator)Enum.Parse(typeof(BinaryOperator), m.Method.Name);
						this.VisitValue(m.Arguments[1]);
						newOperation.RightExpression = (SourceExpression)this.Stack.Pop();
						this.Stack.Push(newOperation);
						return m;
					}
					case "Negate":
					{
						NumberNegateFunction newFunction = new NumberNegateFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Ceiling":
					{
						NumberCeilingFunction newFunction = new NumberCeilingFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Floor":
					{
						NumberFloorFunction newFunction = new NumberFloorFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Round":
					{
						NumberRoundFunction newFunction = new NumberRoundFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
						{
							this.Visit(m.Arguments[1]);
							newFunction.Precision = this.Stack.Pop();
						}
						else
						{
							// TODO: Make it consistent where these are set
							// should they be defaults here, or in the function class, or when making the sql
							// probably when making the sql, because the appropriate default will differ between platforms
							newFunction.Precision = new ConstantPart(0);
						}
						this.Stack.Push(newFunction);
						return m;
					}
					case "Truncate":
					{
						NumberTruncateFunction newFunction = new NumberTruncateFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Compare":
					{
						this.Visit(Expression.Condition(
							Expression.Equal(m.Arguments[0], m.Arguments[1]),
							Expression.Constant(0),
							Expression.Condition(
								Expression.LessThan(m.Arguments[0], m.Arguments[1]),
								Expression.Constant(-1),
								Expression.Constant(1)
								)));
						return m;
					}
				}
			}
			else if (m.Method.DeclaringType == typeof(Math))
			{
				switch (m.Method.Name)
				{
					//case "Acos":
					//case "Asin":
					//case "Atan":
					//case "Atan2":
					//case "Cos":
					//case "Exp":
					//case "Log":
					//case "Log10":
					case "Pow":
					{
						NumberPowerFunction newFunction = new NumberPowerFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Visit(m.Arguments[1]);
						newFunction.Power = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					//case "Sin":
					//case "Tan":
					//case "Sqrt":
					//case "Sign":
					case "Abs":
					{
						NumberAbsoluteFunction newFunction = new NumberAbsoluteFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Ceiling":
					{
						NumberCeilingFunction newFunction = new NumberCeilingFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
					case "Floor":
					{
						NumberFloorFunction newFunction = new NumberFloorFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}

					case "Round":
					{
						NumberRoundFunction newFunction = new NumberRoundFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int))
						{
							this.Visit(m.Arguments[1]);
							newFunction.Precision = this.Stack.Pop();
						}
						else
						{
							// TODO: Make it consistent where these are set
							// should they be defaults here, or in the function class, or when making the sql
							// probably when making the sql, because the appropriate default will differ between platforms
							newFunction.Precision = new ConstantPart(0);
						}
						this.Stack.Push(newFunction);
						return m;
					}
					case "Truncate":
					{
						NumberTruncateFunction newFunction = new NumberTruncateFunction();
						this.Visit(m.Arguments[0]);
						newFunction.Argument = this.Stack.Pop();
						this.Stack.Push(newFunction);
						return m;
					}
				}
			}
			if (m.Method.Name == "ToString")
			{
				if (m.Object.Type == typeof(string))
				{
					this.Visit(m.Object);  // no op
				}
				else
				{
					ConvertFunction newFunction = new ConvertFunction();
					this.Visit(m.Arguments[0]);
					newFunction.Expression = (SourceExpression)this.Stack.Pop();
					this.Stack.Push(newFunction);
					return m;
				}
				return m;
			}
			else if (m.Method.Name == "Equals")
			{
				Condition condition = new Condition();
				condition.Operator = SqlOperator.Equals;
				if (m.Method.IsStatic && m.Method.DeclaringType == typeof(object))
				{
					this.Visit(m.Arguments[0]);
					condition.Field = this.Stack.Pop();
					this.Visit(m.Arguments[1]);
					condition.Values.Add(this.Stack.Pop());
				}
				else if (!m.Method.IsStatic && m.Arguments.Count == 1 && m.Arguments[0].Type == m.Object.Type)
				{
					this.Visit(m.Object);
					condition.Field = this.Stack.Pop();
					this.Visit(m.Arguments[0]);
					condition.Values.Add(this.Stack.Pop());
				}
				this.Stack.Push(condition);
				return m;
			}
			else if (!m.Method.IsStatic && m.Method.Name == "CompareTo" && m.Method.ReturnType == typeof(int) && m.Arguments.Count == 1)
			{
				// TODO:
				//this.Write("(CASE WHEN ");
				//this.Visit(m.Object);
				//this.Write(" = ");
				//this.Visit(m.Arguments[0]);
				//this.Write(" THEN 0 WHEN ");
				//this.Visit(m.Object);
				//this.Write(" < ");
				//this.Visit(m.Arguments[0]);
				//this.Write(" THEN -1 ELSE 1 END)");
				return m;
			}
			else if (m.Method.IsStatic && m.Method.Name == "Compare" && m.Method.ReturnType == typeof(int) && m.Arguments.Count == 2)
			{
				// TODO:
				//this.Write("(CASE WHEN ");
				//this.Visit(m.Arguments[0]);
				//this.Write(" = ");
				//this.Visit(m.Arguments[1]);
				//this.Write(" THEN 0 WHEN ");
				//this.Visit(m.Arguments[0]);
				//this.Write(" < ");
				//this.Visit(m.Arguments[1]);
				//this.Write(" THEN -1 ELSE 1 END)");
				return m;
			}

			throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
		}

		protected override NewExpression VisitNew(NewExpression nex)
		{
			if (nex.Constructor.DeclaringType == typeof(DateTime))
			{
				if (nex.Arguments.Count == 3)
				{
					DateNewFunction newFunction = new DateNewFunction();
					this.Visit(nex.Arguments[0]);
					newFunction.Year = this.Stack.Pop();
					this.Visit(nex.Arguments[1]);
					newFunction.Month = this.Stack.Pop();
					this.Visit(nex.Arguments[2]);
					newFunction.Day = this.Stack.Pop();
					this.Stack.Push(newFunction);
					return nex;
				}
				else if (nex.Arguments.Count == 6)
				{
					DateNewFunction newFunction = new DateNewFunction();
					this.Visit(nex.Arguments[0]);
					newFunction.Year = this.Stack.Pop();
					this.Visit(nex.Arguments[1]);
					newFunction.Month = this.Stack.Pop();
					this.Visit(nex.Arguments[2]);
					newFunction.Day = this.Stack.Pop();
					this.Visit(nex.Arguments[3]);
					newFunction.Hour = this.Stack.Pop();
					this.Visit(nex.Arguments[4]);
					newFunction.Minute = this.Stack.Pop();
					this.Visit(nex.Arguments[5]);
					newFunction.Second = this.Stack.Pop();
					this.Stack.Push(newFunction);
					return nex;
				}
			}

			throw new NotSupportedException(string.Format("The constructor for '{0}' is not supported", nex.Constructor));
		}

		protected override Expression VisitUnary(UnaryExpression u)
		{
			switch (u.NodeType)
			{
				case ExpressionType.Not:
				{
					UnaryOperation newOperation = new UnaryOperation();
					newOperation.Operator = UnaryOperator.Not;
					if (IsBoolean(u.Operand.Type))
					{
						this.VisitPredicate(u.Operand);
					}
					else
					{
						this.VisitValue(u.Operand);
					}
					newOperation.Expression = this.Stack.Pop();
					if (newOperation.Expression is Condition)
					{
						// Push the condition onto the stack instead
						Condition newCondition = (Condition)newOperation.Expression;
						newCondition.Not = true;
						this.Stack.Push(newCondition);
					}
					else
					{
						this.Stack.Push(newOperation);
					}
					break;
				}
				case ExpressionType.Negate:
				case ExpressionType.NegateChecked:
				{
					UnaryOperation newOperation = new UnaryOperation();
					newOperation.Operator = UnaryOperator.Negate;
					this.VisitValue(u.Operand);
					newOperation.Expression = this.Stack.Pop();
					this.Stack.Push(newOperation);
					break;
				}
				case ExpressionType.UnaryPlus:
				{
					this.VisitValue(u.Operand);
					break;
				}
				case ExpressionType.Convert:
				{
					// ignore conversions for now
					this.Visit(u.Operand);
					break;
				}
				default:
				{
					throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
				}
			}
			return u;
		}

		protected override Expression VisitBinary(BinaryExpression b)
		{
			Expression left = b.Left;
			Expression right = b.Right;

			switch (b.NodeType)
			{
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				{
					if (IsBoolean(left.Type))
					{
						this.VisitPredicate(left);
					}
					else
					{
						this.VisitValue(left);
					}
					if (IsBoolean(right.Type))
					{
						this.VisitPredicate(right);
					}
					else
					{
						this.VisitValue(right);
					}

					// Convert the conditions on the stack to a collection and set each condition's relationship
					Condition newCondition = new Condition();
					for (int i = 0; i < 2; i++)
					{
						Condition subCondition = null;
						if (this.Stack.Peek() is Condition)
						{
							subCondition = (Condition)this.Stack.Pop();
						}
						else if (this.Stack.Peek() is UnaryOperation)
						{
							subCondition = (Condition)((UnaryOperation)this.Stack.Pop()).Expression;
						}
						else
						{
							break;
						}

						if (subCondition != null)
						{
							newCondition.SubConditions.Insert(0, subCondition);

							if (b.NodeType == ExpressionType.And ||
								b.NodeType == ExpressionType.AndAlso)
							{
								subCondition.Relationship = ConditionRelationship.And;
							}
							else
							{
								subCondition.Relationship = ConditionRelationship.Or;
							}
						}
					}
					this.Stack.Push(newCondition);

					break;
				}
				case ExpressionType.Equal:
				//if (right.NodeType == ExpressionType.Constant)
				//{
				//    ConstantExpression ce = (ConstantExpression)right;
				//    if (ce.Value == null)
				//    {
				//        this.Visit(left);
				//        this.Write(" IS NULL");
				//        break;
				//    }
				//}
				//else if (left.NodeType == ExpressionType.Constant)
				//{
				//    ConstantExpression ce = (ConstantExpression)left;
				//    if (ce.Value == null)
				//    {
				//        this.Visit(right);
				//        this.Write(" IS NULL");
				//        break;
				//    }
				//}
				//goto case ExpressionType.LessThan;
				case ExpressionType.NotEqual:
				//if (right.NodeType == ExpressionType.Constant)
				//{
				//    ConstantExpression ce = (ConstantExpression)right;
				//    if (ce.Value == null)
				//    {
				//        this.Visit(left);
				//        this.Write(" IS NOT NULL");
				//        break;
				//    }
				//}
				//else if (left.NodeType == ExpressionType.Constant)
				//{
				//    ConstantExpression ce = (ConstantExpression)left;
				//    if (ce.Value == null)
				//    {
				//        this.Visit(right);
				//        this.Write(" IS NOT NULL");
				//        break;
				//    }
				//}
				//goto case ExpressionType.LessThan;
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				{
					// check for special x.CompareTo(y) && type.Compare(x,y)
					if (left.NodeType == ExpressionType.Call && right.NodeType == ExpressionType.Constant)
					{
						MethodCallExpression mc = (MethodCallExpression)left;
						ConstantExpression ce = (ConstantExpression)right;
						if (ce.Value != null && ce.Value.GetType() == typeof(int) && ((int)ce.Value) == 0)
						{
							if (mc.Method.Name == "CompareTo" && !mc.Method.IsStatic && mc.Arguments.Count == 1)
							{
								left = mc.Object;
								right = mc.Arguments[0];
							}
							else if (
								(mc.Method.DeclaringType == typeof(string) || mc.Method.DeclaringType == typeof(decimal))
								  && mc.Method.Name == "Compare" && mc.Method.IsStatic && mc.Arguments.Count == 2)
							{
								left = mc.Arguments[0];
								right = mc.Arguments[1];
							}
						}
					}
					goto case ExpressionType.ExclusiveOr;
				}
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.Divide:
				case ExpressionType.Modulo:
				case ExpressionType.LeftShift:
				case ExpressionType.RightShift:
				{
					BinaryOperation newOperation = new BinaryOperation();
					this.VisitValue(left);
					newOperation.LeftExpression = (SourceExpression)this.Stack.Pop();
					newOperation.Operator = GetBinaryOperator(b);
					this.VisitValue(right);
					newOperation.RightExpression = (SourceExpression)this.Stack.Pop();
					this.Stack.Push(newOperation);
					break;
				}
				case ExpressionType.ExclusiveOr:
				{
					Condition newCondition = new Condition();
					this.VisitValue(left);
					newCondition.Field = this.Stack.Pop();
					newCondition.Operator = this.GetSqlOperator(b);
					this.VisitValue(right);
					newCondition.Values.Add(this.Stack.Pop());
					this.Stack.Push(newCondition);
					break;
				}
				case ExpressionType.Power:
				{
					NumberPowerFunction newPower = new NumberPowerFunction();
					this.VisitValue(left);
					newPower.Argument = this.Stack.Pop();
					this.VisitValue(right);
					newPower.Power = this.Stack.Pop();
					this.Stack.Push(newPower);
					break;
				}
				case ExpressionType.Coalesce:
				{
					CoalesceFunction newCoalesce = new CoalesceFunction();
					this.VisitValue(left);
					newCoalesce.First = (SourceExpression)this.Stack.Pop();
					this.VisitValue(right);
					newCoalesce.Second = (SourceExpression)this.Stack.Pop();
					this.Stack.Push(newCoalesce);
					// TODO: Maybe?
					//while (right.NodeType == ExpressionType.Coalesce)
					//{
					//    BinaryExpression rb = (BinaryExpression)right;
					//    this.VisitValue(rb.Left);
					//    this.Write(", ");
					//    right = rb.Right;
					//}
					break;
				}
				default:
				{
					throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
				}
			}

			return b;
		}

		private SqlOperator GetSqlOperator(BinaryExpression binary)
		{
			switch (binary.NodeType)
			{
				//case ExpressionType.And:
				//case ExpressionType.AndAlso:
				//{
				//    return (IsBoolean(binary.Left.Type)) ? "AND" : "&";
				//}
				//case ExpressionType.Or:
				//case ExpressionType.OrElse:
				//{
				//    return (IsBoolean(binary.Left.Type) ? "OR" : "|");
				//}
				case ExpressionType.Equal:
				{
					return SqlOperator.Equals;
				}
				case ExpressionType.NotEqual:
				{
					return SqlOperator.NotEquals;
				}
				case ExpressionType.LessThan:
				{
					return SqlOperator.IsLessThan;
				}
				case ExpressionType.LessThanOrEqual:
				{
					return SqlOperator.IsLessThanOrEqualTo;
				}
				case ExpressionType.GreaterThan:
				{
					return SqlOperator.IsGreaterThan;
				}
				case ExpressionType.GreaterThanOrEqual:
				{
					return SqlOperator.IsGreaterThanOrEqualTo;
				}
				default:
				{
					throw new InvalidOperationException();
				}
			}
		}

		private BinaryOperator GetBinaryOperator(BinaryExpression binary)
		{
			switch (binary.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				{
					return BinaryOperator.Add;
				}
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked:
				{
					return BinaryOperator.Subtract;
				}
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				{
					return BinaryOperator.Multiply;
				}
				case ExpressionType.Divide:
				{
					return BinaryOperator.Divide;
				}
				case ExpressionType.Modulo:
				{
					return BinaryOperator.Remainder;
				}
				case ExpressionType.ExclusiveOr:
				{
					return BinaryOperator.ExclusiveOr;
				}
				case ExpressionType.LeftShift:
				{
					return BinaryOperator.LeftShift;
				}
				case ExpressionType.RightShift:
				{
					return BinaryOperator.RightShift;
				}
				default:
				{
					throw new InvalidOperationException();
				}
			}
		}

		protected virtual bool IsBoolean(Type type)
		{
			return type == typeof(bool) || type == typeof(bool?);
		}

		protected virtual bool IsPredicate(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
				{
					return IsBoolean(((BinaryExpression)expr).Type);
				}
				case ExpressionType.Not:
				{
					return IsBoolean(((UnaryExpression)expr).Type);
				}
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case (ExpressionType)DbExpressionType.IsNull:
				case (ExpressionType)DbExpressionType.Between:
				case (ExpressionType)DbExpressionType.Exists:
				case (ExpressionType)DbExpressionType.In:
				{
					return true;
				}
				case ExpressionType.Call:
				{
					return IsBoolean(((MethodCallExpression)expr).Type);
				}
				default:
				{
					return false;
				}
			}
		}

		protected virtual Expression VisitPredicate(Expression expr)
		{
			this.Visit(expr);
			if (!IsPredicate(expr))
			{
				Condition newCondition = new Condition();
				newCondition.Field = this.Stack.Pop();
				newCondition.Operator = SqlOperator.NotEquals;
				newCondition.Values.Add(new ConstantPart(false));
				this.Stack.Push(newCondition);
			}
			return expr;
		}

		protected virtual Expression VisitValue(Expression expr)
		{
			if (IsPredicate(expr))
			{
				ConditionPredicate newPredicate = new ConditionPredicate();
				this.Visit(expr);
				newPredicate.Predicate = this.Stack.Pop();
				this.Stack.Push(newPredicate);
				return expr;
			}
			else
			{
				return this.Visit(expr);
			}
		}

		protected override Expression VisitConditional(ConditionalExpression c)
		{
			ConditionalCase newConditional = new ConditionalCase();

			if (IsPredicate(c.Test))
			{
				this.VisitPredicate(c.Test);
				newConditional.Test = this.Stack.Pop();
			}
			else
			{
				this.VisitValue(c.Test);
				newConditional.Test = this.Stack.Pop();
			}

			this.VisitValue(c.IfTrue);
			newConditional.IfTrue = this.Stack.Pop();

			// Kind of a linked list thing going on here
			ConditionalCase parent = newConditional;
			Expression ifFalse = c.IfFalse;
			while (ifFalse != null && ifFalse.NodeType == ExpressionType.Conditional)
			{
				ConditionalExpression fc = (ConditionalExpression)ifFalse;
				ConditionalCase newSubConditional = new ConditionalCase();
				this.VisitPredicate(fc.Test);
				newSubConditional.Test = this.Stack.Pop();
				this.VisitValue(fc.IfTrue);
				newSubConditional.IfTrue = this.Stack.Pop();
				parent.IfFalse = newSubConditional;
				parent = newSubConditional;
				ifFalse = fc.IfFalse;
			}

			if (ifFalse != null)
			{
				this.VisitValue(ifFalse);
				parent.IfFalse = this.Stack.Pop();
			}

			this.Stack.Push(newConditional);
			return c;
		}

		protected override Expression VisitConstant(ConstantExpression c)
		{
			ConstantPart newConstant = new ConstantPart(c.Value);
			this.Stack.Push(newConstant);
			return c;
		}

		protected override Expression VisitColumn(ColumnExpression column)
		{
			string columnName = column.Name;
			Column newColumn = new Column(columnName);
			if (column.Alias != null && !this.HideColumnAliases)
			{
				// TODO: Do this properly
				newColumn.Table = new Table(GetAliasName(column.Alias));
			}
			this.Stack.Push(newColumn);
			return column;
		}

		protected override Expression VisitProjection(ProjectionExpression proj)
		{
			// treat these like scalar subqueries
			if (proj.Projector is ColumnExpression)
			{
				this.Visit(proj.Select);
			}
			else
			{
				throw new NotSupportedException("Non-scalar projections cannot be translated to SQL.");
			}
			return proj;
		}

		protected override Expression VisitSelect(SelectExpression select)
		{
			this.AddAliases(select.From);

			Select newSelect = new Select();
			if (this.Select == null)
			{
				this.Select = newSelect;
			}

			newSelect.IsDistinct = select.IsDistinct;

			if (select.Take != null)
			{
				newSelect.SelectLimit = (int)((ConstantExpression)select.Take).Value;
			}

			if (select.Columns.Count > 0)
			{
				foreach (ColumnDeclaration columnDeclaration in select.Columns)
				{
					ColumnExpression columnExpression = this.VisitValue(columnDeclaration.Expression) as ColumnExpression;
					SourceExpression newSourceField = (SourceExpression)this.Stack.Pop();
					if (!string.IsNullOrEmpty(columnDeclaration.Name) && (columnExpression == null || columnExpression.Name != columnDeclaration.Name))
					{
						newSourceField.Alias = columnDeclaration.Name;
					}
					newSelect.SourceFields.Add(newSourceField);
				}
			}

			if (select.From != null)
			{
				this.VisitSource(select.From);
				newSelect.Source = this.Stack.Pop();
			}

			if (select.Where != null)
			{
				this.VisitPredicate(select.Where);
				if (this.Stack.Count == 0)
				{
					// We want this to throw an exception rather than generate incorrect SQL
					this.Stack.Pop();
				}
				while (this.Stack.Count > 0 && (this.Stack.Peek() is Condition || this.Stack.Peek() is UnaryOperation))
				{
					StatementPart part = this.Stack.Pop();
					if (part is Condition)
					{
						// Insert the conditions in reverse order from the stack
						newSelect.Conditions.Insert(0, (Condition)part);
					}
					else if (part is UnaryOperation)
					{
						UnaryOperation unary = (UnaryOperation)part;
						newSelect.Conditions.Insert(0, (Condition)unary.Expression);
						if (unary.Operator == UnaryOperator.Not)
						{
							newSelect.Conditions[0].Not = true;
						}
					}
				}
			}

			if (select.GroupBy != null && select.GroupBy.Count > 0)
			{
				foreach (Expression expression in select.GroupBy)
				{
					this.VisitValue(expression);
					// TODO: I think that should be a generic querypart rather than a column
					newSelect.GroupByFields.Add((Column)this.Stack.Pop());
				}
			}

			if (select.OrderBy != null && select.OrderBy.Count > 0)
			{
				foreach (OrderExpression expression in select.OrderBy)
				{
					OrderByExpression newOrderByField = new OrderByExpression();
					this.VisitValue(expression.Expression);
					newOrderByField.Expression = (SourceExpression)this.Stack.Pop();
					newOrderByField.Direction = GetOrderDirectionFromExpression(expression);
					newSelect.OrderByFields.Add(newOrderByField);
				}
			}

			if (select.Alias != null)
			{
				newSelect.Alias = GetAliasName(select.Alias);
			}

			this.Stack.Push(newSelect);
			return select;
		}

		private OrderDirection GetOrderDirectionFromExpression(OrderExpression expression)
		{
			switch (expression.OrderType)
			{
				case OrderType.Ascending:
				{
					return OrderDirection.Ascending;
				}
				case OrderType.Descending:
				{
					return OrderDirection.Descending;
				}
				default:
				{
					throw new NotSupportedException();
				}
			}
		}

		protected override Expression VisitSource(Expression source)
		{
			bool previousIsNested = this.IsNested;
			this.IsNested = true;
			switch ((DbExpressionType)source.NodeType)
			{
				case DbExpressionType.Table:
				{
					this.VisitTable((TableExpression)source);
					break;
				}
				case DbExpressionType.Select:
				{
					this.VisitSelect((SelectExpression)source);
					break;
				}
				case DbExpressionType.Join:
				{
					this.VisitJoin((JoinExpression)source);
					break;
				}
				default:
				{
					throw new InvalidOperationException("Select source is not valid type");
				}
			}
			this.IsNested = previousIsNested;
			return source;
		}

		protected override Expression VisitTable(TableExpression table)
		{
			Table newTable = new Table(table.Name);
			if (table.Alias != null && !this.HideTableAliases)
			{
				newTable.Alias = GetAliasName(table.Alias);
			}
			if (table.Entity is EntityMappingEntity)
			{
				newTable.IncludePaths.AddRange(((EntityMappingEntity)table.Entity).IncludePaths);
			}
			this.Stack.Push(newTable);
			return table;
		}

		protected override Expression VisitJoin(JoinExpression join)
		{
			Join newJoin = new Join();

			this.VisitSource(join.Left);
			newJoin.Left = this.Stack.Pop();

			newJoin.JoinType = GetJoinTypeFromExpression(join);

			this.VisitSource(join.Right);
			newJoin.Right = this.Stack.Pop();

			if (join.Condition != null)
			{
				this.VisitPredicate(join.Condition);
				newJoin.Condition = (Condition)this.Stack.Pop();
			}

			this.Stack.Push(newJoin);
			return join;
		}

		private JoinType GetJoinTypeFromExpression(JoinExpression join)
		{
			switch (join.Join)
			{
				case IQToolkit.Data.Common.JoinType.CrossApply:
				{
					return JoinType.CrossApply;
				}
				case IQToolkit.Data.Common.JoinType.CrossJoin:
				{
					return JoinType.Cross;
				}
				case IQToolkit.Data.Common.JoinType.InnerJoin:
				{
					return JoinType.Inner;
				}
				case IQToolkit.Data.Common.JoinType.LeftOuter:
				{
					return JoinType.Left;
				}
				case IQToolkit.Data.Common.JoinType.OuterApply:
				{
					throw new NotSupportedException();
				}
				case IQToolkit.Data.Common.JoinType.SingletonLeftOuter:
				{
					throw new NotSupportedException();
				}
				default:
				{
					throw new NotSupportedException();
				}
			}
		}

		protected override Expression VisitAggregate(AggregateExpression aggregate)
		{
			Aggregate newAggregate = new Aggregate();
			newAggregate.AggregateType = GetAggregateTypeFromExpression(aggregate);
			if (aggregate.Argument != null)
			{
				this.VisitValue(aggregate.Argument);
				newAggregate.Field = (Field)this.Stack.Pop();
			}
			newAggregate.IsDistinct = aggregate.IsDistinct;
			this.Stack.Push(newAggregate);
			return aggregate;
		}

		private AggregateType GetAggregateTypeFromExpression(AggregateExpression aggregate)
		{
			switch (aggregate.AggregateName)
			{
				case "Count":
				{
					return AggregateType.Count;
				}
				case "LongCount":
				{
					return AggregateType.BigCount;
				}
				case "Sum":
				{
					return AggregateType.Sum;
				}
				case "Min":
				{
					return AggregateType.Min;
				}
				case "Max":
				{
					return AggregateType.Max;
				}
				case "Average":
				{
					return AggregateType.Average;
				}
				default:
				{
					throw new NotSupportedException("Aggregate name not supported: " + aggregate.AggregateName);
				}
			}
		}

		protected override Expression VisitIsNull(IsNullExpression isnull)
		{
			this.VisitValue(isnull.Expression);
			Condition newCondition = new Condition();
			newCondition.Field = this.Stack.Pop();
			newCondition.Operator = SqlOperator.Equals;
			newCondition.Values.Add(new ConstantPart(null));
			this.Stack.Push(newCondition);
			return isnull;
		}

		protected override Expression VisitBetween(BetweenExpression between)
		{
			this.VisitValue(between.Expression);
			Condition newCondition = new Condition();
			newCondition.Field = this.Stack.Pop();
			newCondition.Operator = SqlOperator.IsBetween;
			this.VisitValue(between.Lower);
			newCondition.Values.Add(this.Stack.Pop());
			this.VisitValue(between.Upper);
			newCondition.Values.Add(this.Stack.Pop());
			this.Stack.Push(newCondition);
			return between;
		}

		protected override Expression VisitRowNumber(RowNumberExpression rowNumber)
		{
			RowNumber newRowNumber = new RowNumber();
			if (rowNumber.OrderBy != null && rowNumber.OrderBy.Count > 0)
			{
				foreach (OrderExpression expression in rowNumber.OrderBy)
				{
					OrderByExpression newOrderByField = new OrderByExpression();
					this.VisitValue(expression.Expression);
					newOrderByField.Expression = (SourceExpression)this.Stack.Pop();
					newOrderByField.Direction = GetOrderDirectionFromExpression(expression);
					newRowNumber.OrderByFields.Add(newOrderByField);
				}
			}
			this.Stack.Push(newRowNumber);
			return rowNumber;
		}

		protected override Expression VisitScalar(ScalarExpression subquery)
		{
			// TODO:
			//this.Write("(");
			//this.WriteLine(Indentation.Inner);
			//this.Visit(subquery.Select);
			//this.WriteLine(Indentation.Same);
			//this.Write(")");
			//this.Indent(Indentation.Outer);
			return subquery;
		}

		protected override Expression VisitExists(ExistsExpression exists)
		{
			Exists newExists = new Exists();
			this.Visit(exists.Select);
			newExists.Select = (Select)this.Stack.Pop();
			this.Stack.Push(newExists);
			return exists;
		}

		protected override Expression VisitIn(InExpression @in)
		{
			Condition newCondition = new Condition();
			newCondition.Operator = SqlOperator.IsIn;
			this.Visit(@in.Expression);
			newCondition.Field = this.Stack.Pop();

			if (@in.Select != null)
			{
				this.Visit(@in.Select);
				newCondition.Values.Add(this.Stack.Pop());
			}
			else if (@in.Values != null)
			{
				foreach (Expression exp in @in.Values)
				{
					this.Visit(exp);
					newCondition.Values.Add(this.Stack.Pop());
				}
			}

			this.Stack.Push(newCondition);

			// TODO: This is slightly differente
			//if (@in.Values != null)
			//{
			//    if (@in.Values.Count == 0)
			//    {
			//        this.Write("0 <> 0");
			//    }
			//    else
			//    {
			//        this.VisitValue(@in.Expression);
			//        this.Write(" IN (");
			//        for (int i = 0, n = @in.Values.Count; i < n; i++)
			//        {
			//            if (i > 0) this.Write(", ");
			//            this.VisitValue(@in.Values[i]);
			//        }
			//        this.Write(")");
			//    }
			//}
			//else
			//{
			//    this.VisitValue(@in.Expression);
			//    this.Write(" IN (");
			//    this.WriteLine(Indentation.Inner);
			//    this.Visit(@in.Select);
			//    this.WriteLine(Indentation.Same);
			//    this.Write(")");
			//    this.Indent(Indentation.Outer);
			//}
			return @in;
		}

		protected override Expression VisitNamedValue(NamedValueExpression value)
		{
			object containedValue = value.Value;
			if (containedValue is ConstantExpression)
			{
				containedValue = ((ConstantExpression)containedValue).Value;
			}
			Parameter newParameter = new Parameter("@" + value.Name, containedValue);
			this.Stack.Push(newParameter);
			return value;
		}

		//protected override Expression VisitInsert(InsertCommand insert)
		//{
		//    this.Write("INSERT INTO ");
		//    this.WriteTableName(insert.Table.Name);
		//    this.Write("(");
		//    for (int i = 0, n = insert.Assignments.Count; i < n; i++)
		//    {
		//        ColumnAssignment ca = insert.Assignments[i];
		//        if (i > 0) this.Write(", ");
		//        this.WriteColumnName(ca.Column.Name);
		//    }
		//    this.Write(")");
		//    this.WriteLine(Indentation.Same);
		//    this.Write("VALUES (");
		//    for (int i = 0, n = insert.Assignments.Count; i < n; i++)
		//    {
		//        ColumnAssignment ca = insert.Assignments[i];
		//        if (i > 0) this.Write(", ");
		//        this.Visit(ca.Expression);
		//    }
		//    this.Write(")");
		//    return insert;
		//}

		//protected override Expression VisitUpdate(UpdateCommand update)
		//{
		//    this.Write("UPDATE ");
		//    this.WriteTableName(update.Table.Name);
		//    this.WriteLine(Indentation.Same);
		//    bool saveHide = this.HideColumnAliases;
		//    this.HideColumnAliases = true;
		//    this.Write("SET ");
		//    for (int i = 0, n = update.Assignments.Count; i < n; i++)
		//    {
		//        ColumnAssignment ca = update.Assignments[i];
		//        if (i > 0) this.Write(", ");
		//        this.Visit(ca.Column);
		//        this.Write(" = ");
		//        this.Visit(ca.Expression);
		//    }
		//    if (update.Where != null)
		//    {
		//        this.WriteLine(Indentation.Same);
		//        this.Write("WHERE ");
		//        this.VisitPredicate(update.Where);
		//    }
		//    this.HideColumnAliases = saveHide;
		//    return update;
		//}

		//protected override Expression VisitDelete(DeleteCommand delete)
		//{
		//    this.Write("DELETE FROM ");
		//    bool saveHideTable = this.HideTableAliases;
		//    bool saveHideColumn = this.HideColumnAliases;
		//    this.HideTableAliases = true;
		//    this.HideColumnAliases = true;
		//    this.VisitSource(delete.Table);
		//    if (delete.Where != null)
		//    {
		//        this.WriteLine(Indentation.Same);
		//        this.Write("WHERE ");
		//        this.VisitPredicate(delete.Where);
		//    }
		//    this.HideTableAliases = saveHideTable;
		//    this.HideColumnAliases = saveHideColumn;
		//    return delete;
		//}

		//protected override Expression VisitIf(IFCommand ifx)
		//{
		//    if (!this.Language.AllowsMultipleCommands)
		//    {
		//        return base.VisitIf(ifx);
		//    }
		//    this.Write("IF ");
		//    this.Visit(ifx.Check);
		//    this.WriteLine(Indentation.Same);
		//    this.Write("BEGIN");
		//    this.WriteLine(Indentation.Inner);
		//    this.VisitStatement(ifx.IfTrue);
		//    this.WriteLine(Indentation.Outer);
		//    if (ifx.IfFalse != null)
		//    {
		//        this.Write("END ELSE BEGIN");
		//        this.WriteLine(Indentation.Inner);
		//        this.VisitStatement(ifx.IfFalse);
		//        this.WriteLine(Indentation.Outer);
		//    }
		//    this.Write("END");
		//    return ifx;
		//}

		// TODO:
		//protected override Expression VisitBlock(BlockCommand block)
		//{
		//    if (!this.Language.AllowsMultipleCommands)
		//    {
		//        return base.VisitBlock(block);
		//    }

		//    for (int i = 0, n = block.Commands.Count; i < n; i++)
		//    {
		//        if (i > 0)
		//        {
		//            this.WriteLine(Indentation.Same);
		//            this.WriteLine(Indentation.Same);
		//        }
		//        this.VisitStatement(block.Commands[i]);
		//    }
		//    return block;
		//}

		// TODO:
		//protected override Expression VisitDeclaration(DeclarationCommand decl)
		//{
		//    if (!this.Language.AllowsMultipleCommands)
		//    {
		//        return base.VisitDeclaration(decl);
		//    }

		//    for (int i = 0, n = decl.Variables.Count; i < n; i++)
		//    {
		//        var v = decl.Variables[i];
		//        if (i > 0)
		//            this.WriteLine(Indentation.Same);
		//        this.Write("DECLARE @");
		//        this.Write(v.Name);
		//        this.Write(" ");
		//        this.Write(this.Language.TypeSystem.GetVariableDeclaration(v.QueryType, false));
		//    }
		//    if (decl.Source != null)
		//    {
		//        this.WriteLine(Indentation.Same);
		//        this.Write("SELECT ");
		//        for (int i = 0, n = decl.Variables.Count; i < n; i++)
		//        {
		//            if (i > 0)
		//                this.Write(", ");
		//            this.Write("@");
		//            this.Write(decl.Variables[i].Name);
		//            this.Write(" = ");
		//            this.Visit(decl.Source.Columns[i].Expression);
		//        }
		//        if (decl.Source.From != null)
		//        {
		//            this.WriteLine(Indentation.Same);
		//            this.Write("FROM ");
		//            this.VisitSource(decl.Source.From);
		//        }
		//        if (decl.Source.Where != null)
		//        {
		//            this.WriteLine(Indentation.Same);
		//            this.Write("WHERE ");
		//            this.Visit(decl.Source.Where);
		//        }
		//    }
		//    else
		//    {
		//        for (int i = 0, n = decl.Variables.Count; i < n; i++)
		//        {
		//            var v = decl.Variables[i];
		//            if (v.Expression != null)
		//            {
		//                this.WriteLine(Indentation.Same);
		//                this.Write("SET @");
		//                this.Write(v.Name);
		//                this.Write(" = ");
		//                this.Visit(v.Expression);
		//            }
		//        }
		//    }
		//    return decl;
		//}

		//protected override Expression VisitVariable(VariableExpression vex)
		//{
		//    this.WriteVariableName(vex.Name);
		//    return vex;
		//}

		//protected virtual void VisitStatement(Expression expression)
		//{
		//    var p = expression as ProjectionExpression;
		//    if (p != null)
		//    {
		//        this.Visit(p.Select);
		//    }
		//    else
		//    {
		//        this.Visit(expression);
		//    }
		//}

		//protected override Expression VisitFunction(FunctionExpression func)
		//{
		//    this.Write(func.Name);
		//    if (func.Arguments.Count > 0)
		//    {
		//        this.Write("(");
		//        for (int i = 0, n = func.Arguments.Count; i < n; i++)
		//        {
		//            if (i > 0) this.Write(", ");
		//            this.Visit(func.Arguments[i]);
		//        }
		//        this.Write(")");
		//    }
		//    return func;
		//}
	}
}

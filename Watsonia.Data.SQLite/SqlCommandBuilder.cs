using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Sql;

namespace Watsonia.Data.SQLite
{
	public class SqlCommandBuilder
	{
		private const int IndentationWidth = 2;

		private enum Indentation
		{
			Same,
			Inner,
			Outer
		}

		private readonly StringBuilder _builder = new StringBuilder();
		private readonly List<object> _parameterValues = new List<object>();

		public StringBuilder CommandText
		{
			get
			{
				return _builder;
			}
		}

		public List<object> ParameterValues
		{
			get
			{
				return _parameterValues;
			}
		}

		private int Depth
		{
			get;
			set;
		}

		private bool IsNested
		{
			get;
			set;
		}

		public void VisitStatement(Statement statement, DatabaseConfiguration configuration)
		{
			switch (statement.PartType)
			{
				case StatementPartType.Select:
				{
					VisitSelect((SelectStatement)statement);
					break;
				}
				case StatementPartType.GenericSelect:
				{
					Type messageType = statement.GetType();
					MethodInfo method = statement.GetType().GetMethod("CreateStatement");
					SelectStatement select = (SelectStatement)method.Invoke(statement, new object[] { configuration });
					VisitSelect(select);
					break;
				}
				case StatementPartType.Insert:
				{
					VisitInsert((InsertStatement)statement);
					break;
				}
				case StatementPartType.GenericInsert:
				{
					Type messageType = statement.GetType();
					MethodInfo method = statement.GetType().GetMethod("CreateStatement");
					InsertStatement select = (InsertStatement)method.Invoke(statement, new object[] { configuration });
					VisitInsert(select);
					break;
				}
				case StatementPartType.Update:
				{
					VisitUpdate((UpdateStatement)statement);
					break;
				}
				case StatementPartType.GenericUpdate:
				{
					Type messageType = statement.GetType();
					MethodInfo method = statement.GetType().GetMethod("CreateStatement");
					UpdateStatement select = (UpdateStatement)method.Invoke(statement, new object[] { configuration });
					VisitUpdate(select);
					break;
				}
				case StatementPartType.Delete:
				{
					VisitDelete((DeleteStatement)statement);
					break;
				}
				case StatementPartType.GenericDelete:
				{
					Type messageType = statement.GetType();
					MethodInfo method = statement.GetType().GetMethod("CreateStatement");
					DeleteStatement select = (DeleteStatement)method.Invoke(statement, new object[] { configuration });
					VisitDelete(select);
					break;
				}
				default:
				{
					// TODO:
					throw new NotSupportedException();
				}
			}
		}

		private void VisitConstant(ConstantPart constant)
		{
			VisitObject(constant.Value);
		}

		private void VisitObject(object value)
		{
			if (value == null)
			{
				this.CommandText.Append("NULL");
			}
			else if (value.GetType() == typeof(bool))
			{
				this.CommandText.Append(((bool)value) ? "1" : "0");
			}
			else if (value.GetType() == typeof(string) && value.ToString() == "")
			{
				this.CommandText.Append("''");
			}
			else if (value is IEnumerable && !(value is string) && !(value is byte[]))
			{
				bool firstValue = true;
				foreach (object innerValue in (IEnumerable)value)
				{
					if (!firstValue)
					{
						this.CommandText.Append(", ");
					}
					firstValue = false;
					if (innerValue is ConstantPart)
					{
						this.VisitConstant((ConstantPart)innerValue);
					}
					else
					{
						this.VisitObject(innerValue);
					}
				}
			}
			else
			{
				int index = this.ParameterValues.IndexOf(value);
				if (index != -1)
				{
					this.CommandText.Append("@");
					this.CommandText.Append(index);
				}
				else
				{
					this.CommandText.Append("@");
					this.CommandText.Append(this.ParameterValues.Count);
					if (value.GetType().IsEnum)
					{
						this.ParameterValues.Add(Convert.ToInt64(value));
					}
					else
					{
						this.ParameterValues.Add(value);
					}
				}
			}
		}

		private void VisitParameter(Parameter parameter)
		{
			int index = this.ParameterValues.IndexOf(parameter.Value);
			if (index == -1)
			{
				this.ParameterValues.Add(parameter.Value);
				index = this.ParameterValues.Count - 1;
			}

			// NOTE: We never want to use the parameter name, because later on we set the command parameters
			// (names and values) from this.ParameterValues.  This is probably indicative of a problem somewhere
			//if (!string.IsNullOrEmpty(parameter.Name))
			//{
			//	this.CommandText.Append(parameter.Name);
			//}
			//else
			//{
			this.CommandText.Append("@");
			this.CommandText.Append(index);
			//}
		}

		private void VisitSelect(SelectStatement select)
		{
			if (select.IsAny)
			{
				VisitSelectWithAny(select);
				return;
			}

			if (select.IsAll)
			{
				VisitSelectWithAll(select);
				return;
			}

			if (select.IsContains)
			{
				VisitSelectWithContains(select);
				return;
			}

			this.CommandText.Append("SELECT ");
			if (select.IsDistinct)
			{
				this.CommandText.Append("DISTINCT ");
			}
			if (select.SourceFieldsFrom.Count > 0)
			{
				// TODO: Should the SourceFIeldsFRom actually be its own class?
				for (int i = 0; i < select.SourceFieldsFrom.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitTable(select.SourceFieldsFrom[i]);
					this.CommandText.Append(".*");
				}
				if (select.SourceFields.Count > 0)
				{
					this.CommandText.Append(", ");
				}
			}
			if (select.SourceFields.Count > 0)
			{
				for (int i = 0; i < select.SourceFields.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitField(select.SourceFields[i]);
				}
			}
			if (select.SourceFieldsFrom.Count == 0 && select.SourceFields.Count == 0)
			{
				if (this.IsNested)
				{
					// TODO: Rename tmp, it sucks
					this.CommandText.Append("NULL ");
					this.CommandText.Append("AS tmp");
				}
				else
				{
					// TODO: When to use "*" vs "NULL"?
					this.CommandText.Append("*");
				}
			}
			if (select.Source != null)
			{
				this.AppendNewLine(Indentation.Same);
				this.CommandText.Append("FROM ");
				this.VisitSource(select.Source);
			}
			if (select.SourceJoins != null)
			{
				for (int i = 0; i < select.SourceJoins.Count; i++)
				{
					this.AppendNewLine(Indentation.Same);
					this.VisitJoin(select.SourceJoins[i]);
				}
			}
			if (select.Conditions.Count > 0)
			{
				this.AppendNewLine(Indentation.Same);
				this.CommandText.Append("WHERE ");
				if (select.Conditions.Not)
				{
					this.CommandText.Append("NOT ");
				}
				for (int i = 0; i < select.Conditions.Count; i++)
				{
					if (i > 0)
					{
						this.AppendNewLine(Indentation.Same);
						switch (select.Conditions[i].Relationship)
						{
							case ConditionRelationship.And:
							{
								this.CommandText.Append(" AND ");
								break;
							}
							case ConditionRelationship.Or:
							{
								this.CommandText.Append(" OR ");
								break;
							}
							default:
							{
								throw new InvalidOperationException();
							}
						}
					}
					this.VisitCondition(select.Conditions[i]);
				}
			}
			if (select.GroupByFields.Count > 0)
			{
				this.AppendNewLine(Indentation.Same);
				this.CommandText.Append("GROUP BY ");
				for (int i = 0; i < select.GroupByFields.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitField(select.GroupByFields[i]);
				}
			}
			if (select.OrderByFields.Count > 0)
			{
				this.AppendNewLine(Indentation.Same);
				this.CommandText.Append("ORDER BY ");
				for (int i = 0; i < select.OrderByFields.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitField(select.OrderByFields[i].Expression);
					if (select.OrderByFields[i].Direction != OrderDirection.Ascending)
					{
						this.CommandText.Append(" DESC");
					}
				}
			}
			if (select.Limit > 0)
			{
				this.CommandText.Append(" LIMIT ");
				this.CommandText.Append(select.Limit);
			}
		}

		private void VisitSelectWithAny(SelectStatement select)
		{
			// It's going to look something like this:
			// SELECT CASE WHEN EXISTS (
			//		SELECT Fields
			//		FROM Table
			//		WHERE Condition
			// ) THEN 1 ELSE 0 END

			this.CommandText.Append("SELECT CASE WHEN EXISTS (");
			this.Indent(Indentation.Inner);
			select.IsAny = false;
			this.VisitSelect(select);
			this.Indent(Indentation.Outer);
			this.CommandText.Append(") THEN 1 ELSE 0 END");
		}

		private void VisitSelectWithAll(SelectStatement select)
		{
			// It's going to look something like this:
			// SELECT CASE WHEN NOT EXISTS (
			//		SELECT Fields
			//		FROM Table
			//		WHERE NOT Condition
			// ) THEN 1 ELSE 0 END

			this.CommandText.Append("SELECT CASE WHEN NOT EXISTS (");
			this.Indent(Indentation.Inner);
			select.IsAll = false;
			select.Conditions.Not = true;
			this.VisitSelect(select);
			this.Indent(Indentation.Outer);
			this.CommandText.Append(") THEN 1 ELSE 0 END");
		}

		private void VisitSelectWithContains(SelectStatement select)
		{
			// It's going to look something like this:
			// SELECT CASE WHEN @0 IN (
			//		SELECT Fields
			//		FROM Table
			// ) THEN 1 ELSE 0 END

			this.CommandText.Append("SELECT CASE WHEN ");
			this.VisitField(select.ContainsItem);
			this.CommandText.Append(" IN (");
			this.Indent(Indentation.Inner);
			select.IsContains = false;
			this.VisitSelect(select);
			this.Indent(Indentation.Outer);
			this.CommandText.Append(") THEN 1 ELSE 0 END");
		}

		private void VisitUpdate(UpdateStatement update)
		{
			this.CommandText.Append("UPDATE ");
			this.VisitTable(update.Target);
			this.CommandText.Append(" SET ");
			if (update.SetValues != null && update.SetValues.Count > 0)
			{
				for (int i = 0; i < update.SetValues.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitColumn(update.SetValues[i].Column);
					this.CommandText.Append(" = ");
					this.VisitField(update.SetValues[i].Value);
				}
			}
			if (update.Conditions != null && update.Conditions.Count > 0)
			{
				this.AppendNewLine(Indentation.Same);
				this.CommandText.Append("WHERE ");
				for (int i = 0; i < update.Conditions.Count; i++)
				{
					if (i > 0)
					{
						this.AppendNewLine(Indentation.Same);
						switch (update.Conditions[i].Relationship)
						{
							case ConditionRelationship.And:
							{
								this.CommandText.Append(" AND ");
								break;
							}
							case ConditionRelationship.Or:
							{
								this.CommandText.Append(" OR ");
								break;
							}
							default:
							{
								throw new InvalidOperationException();
							}
						}
					}
					this.VisitCondition(update.Conditions[i]);
				}
			}
			else
			{
				throw new InvalidOperationException("An update statement must have at least one condition to avoid accidentally updating all data in a table");
			}
		}

		private void VisitInsert(InsertStatement insert)
		{
			this.CommandText.Append("INSERT INTO ");
			this.VisitTable(insert.Target);
			if (insert.SetValues != null && insert.SetValues.Count > 0)
			{
				this.CommandText.Append(" (");
				for (int i = 0; i < insert.SetValues.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitColumn(insert.SetValues[i].Column);
				}
				this.CommandText.Append(")");
				this.AppendNewLine(Indentation.Same);
				this.CommandText.Append("VALUES (");
				for (int i = 0; i < insert.SetValues.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitField(insert.SetValues[i].Value);
				}
				this.CommandText.Append(")");
			}
			else if (insert.TargetFields != null && insert.TargetFields.Count > 0 && insert.Source != null)
			{
				this.CommandText.Append(" (");
				for (int i = 0; i < insert.TargetFields.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitColumn(insert.TargetFields[i]);
				}
				this.CommandText.Append(")");
				this.AppendNewLine(Indentation.Same);
				this.VisitSelect(insert.Source);
			}
			else
			{
				this.CommandText.Append(" DEFAULT VALUES");
			}
		}

		private void VisitDelete(DeleteStatement delete)
		{
			this.CommandText.Append("DELETE FROM ");
			this.VisitTable(delete.Target);
			if (delete.Conditions != null && delete.Conditions.Count > 0)
			{
				this.AppendNewLine(Indentation.Same);
				this.CommandText.Append("WHERE ");
				for (int i = 0; i < delete.Conditions.Count; i++)
				{
					if (i > 0)
					{
						this.AppendNewLine(Indentation.Same);
						switch (delete.Conditions[i].Relationship)
						{
							case ConditionRelationship.And:
							{
								this.CommandText.Append(" AND ");
								break;
							}
							case ConditionRelationship.Or:
							{
								this.CommandText.Append(" OR ");
								break;
							}
							default:
							{
								throw new InvalidOperationException("Invalid relationship: " + delete.Conditions[i].Relationship);
							}
						}
					}
					this.VisitCondition(delete.Conditions[i]);
				}
			}
			else
			{
				throw new InvalidOperationException("A delete statement must have at least one condition to avoid accidentally deleting all data in a table");
			}
		}

		private void VisitField(StatementPart field)
		{
			switch (field.PartType)
			{
				case StatementPartType.Column:
				{
					this.VisitColumn((Column)field);
					break;
				}
				case StatementPartType.RowNumber:
				{
					this.VisitRowNumber((RowNumber)field);
					break;
				}
				case StatementPartType.Aggregate:
				{
					this.VisitAggregate((Aggregate)field);
					break;
				}
				case StatementPartType.ConditionalCase:
				{
					this.VisitConditionalCase((ConditionalCase)field);
					break;
				}
				case StatementPartType.ConditionPredicate:
				{
					this.VisitConditionPredicate((ConditionPredicate)field);
					break;
				}
				case StatementPartType.Exists:
				{
					this.VisitExists((Exists)field);
					break;
				}
				case StatementPartType.CoalesceFunction:
				{
					this.VisitCoalesceFunction((CoalesceFunction)field);
					break;
				}
				case StatementPartType.ConvertFunction:
				{
					this.VisitConvertFunction((ConvertFunction)field);
					break;
				}
				case StatementPartType.StringLengthFunction:
				{
					this.VisitStringLengthFunction((StringLengthFunction)field);
					break;
				}
				case StatementPartType.SubstringFunction:
				{
					this.VisitSubstringFunction((SubstringFunction)field);
					break;
				}
				case StatementPartType.StringRemoveFunction:
				{
					this.VisitStringRemoveFunction((StringRemoveFunction)field);
					break;
				}
				case StatementPartType.StringIndexFunction:
				{
					this.VisitStringCharIndexFunction((StringIndexFunction)field);
					break;
				}
				case StatementPartType.StringToUpperFunction:
				{
					this.VisitStringToUpperFunction((StringToUpperFunction)field);
					break;
				}
				case StatementPartType.StringToLowerFunction:
				{
					this.VisitStringToLowerFunction((StringToLowerFunction)field);
					break;
				}
				case StatementPartType.StringReplaceFunction:
				{
					this.VisitStringReplaceFunction((StringReplaceFunction)field);
					break;
				}
				case StatementPartType.StringTrimFunction:
				{
					this.VisitStringTrimFunction((StringTrimFunction)field);
					break;
				}
				case StatementPartType.StringCompareFunction:
				{
					this.VisitStringCompareFunction((StringCompareFunction)field);
					break;
				}
				case StatementPartType.StringConcatenateFunction:
				{
					this.VisitStringConcatenateFunction((StringConcatenateFunction)field);
					break;
				}
				case StatementPartType.DatePartFunction:
				{
					this.VisitDatePartFunction((DatePartFunction)field);
					break;
				}
				case StatementPartType.DateAddFunction:
				{
					this.VisitDateAddFunction((DateAddFunction)field);
					break;
				}
				case StatementPartType.DateNewFunction:
				{
					this.VisitDateNewFunction((DateNewFunction)field);
					break;
				}
				case StatementPartType.DateDifferenceFunction:
				{
					this.VisitDateDifferenceFunction((DateDifferenceFunction)field);
					break;
				}
				case StatementPartType.NumberAbsoluteFunction:
				{
					this.VisitNumberAbsoluteFunction((NumberAbsoluteFunction)field);
					break;
				}
				case StatementPartType.NumberNegateFunction:
				{
					this.VisitNumberNegateFunction((NumberNegateFunction)field);
					break;
				}
				case StatementPartType.NumberCeilingFunction:
				{
					this.VisitNumberCeilingFunction((NumberCeilingFunction)field);
					break;
				}
				case StatementPartType.NumberFloorFunction:
				{
					this.VisitNumberFloorFunction((NumberFloorFunction)field);
					break;
				}
				case StatementPartType.NumberRoundFunction:
				{
					this.VisitNumberRoundFunction((NumberRoundFunction)field);
					break;
				}
				case StatementPartType.NumberTruncateFunction:
				{
					this.VisitNumberTruncateFunction((NumberTruncateFunction)field);
					break;
				}
				case StatementPartType.NumberSignFunction:
				{
					this.VisitNumberSignFunction((NumberSignFunction)field);
					break;
				}
				case StatementPartType.NumberPowerFunction:
				{
					this.VisitNumberPowerFunction((NumberPowerFunction)field);
					break;
				}
				case StatementPartType.NumberRootFunction:
				{
					this.VisitNumberRootFunction((NumberRootFunction)field);
					break;
				}
				case StatementPartType.NumberExponentialFunction:
				{
					this.VisitNumberExponentialFunction((NumberExponentialFunction)field);
					break;
				}
				case StatementPartType.NumberLogFunction:
				{
					this.VisitNumberLogFunction((NumberLogFunction)field);
					break;
				}
				case StatementPartType.NumberLog10Function:
				{
					this.VisitNumberLog10Function((NumberLog10Function)field);
					break;
				}
				case StatementPartType.NumberTrigFunction:
				{
					this.VisitNumberTrigFunction((NumberTrigFunction)field);
					break;
				}
				case StatementPartType.BinaryOperation:
				{
					this.VisitBinaryOperation((BinaryOperation)field);
					break;
				}
				case StatementPartType.UnaryOperation:
				{
					this.VisitUnaryOperation((UnaryOperation)field);
					break;
				}
				case StatementPartType.LiteralPart:
				{
					this.VisitLiteralPart((LiteralPart)field);
					break;
				}
				case StatementPartType.Select:
				{
					this.VisitSelect((SelectStatement)field);
					break;
				}
				case StatementPartType.ConstantPart:
				{
					this.VisitConstant((ConstantPart)field);
					break;
				}
				case StatementPartType.Condition:
				{
					this.VisitCondition((Condition)field);
					break;
				}
				case StatementPartType.FieldCollection:
				{
					var collection = (FieldCollection)field;
					for (int i = 0; i < collection.Count; i++)
					{
						if (i > 0)
						{
							this.CommandText.Append(", ");
						}
						this.VisitField(collection[i]);
					}
					break;
				}
				case StatementPartType.SelectExpression:
				{
					this.VisitSelectExpression((SelectExpression)field);
					break;
				}
				default:
				{
					// TODO: Words for all exceptions
					throw new InvalidOperationException();
				}
			}
			//if (IsPredicate(expression))
			//{
			//    this.Builder.Append("CASE WHEN (");
			//    this.Visit(expression);
			//    this.Builder.Append(") THEN 1 ELSE 0 END");
			//}
			//else
			//{
			//    this.Visit(expression);
			//}
		}

		private void VisitColumn(Column column)
		{
			////if (column.AggregateFunction != AggregateType.None)
			////{
			////	this.CommandText.Append(AggregateFunctionName(column.AggregateFunction));
			////	this.CommandText.Append("(");
			////}
			////if (!string.IsNullOrEmpty(column.TableName))
			////{
			////	this.CommandText.Append("[");
			////	this.CommandText.Append(column.TableName);
			////	this.CommandText.Append("].");
			////}
			if (column.Table != null)
			{
				VisitTable(column.Table);
				this.CommandText.Append(".");
			}
			if (column.Name == "*")
			{
				this.CommandText.Append("*");
			}
			else
			{
				this.CommandText.Append("[");
				this.CommandText.Append(column.Name);
				this.CommandText.Append("]");
			}
			////if (column.AggregateFunction != AggregateType.None)
			////{
			////	this.CommandText.Append(")");
			////}
		}

		////private string AggregateFunctionName(AggregateType aggregate)
		////{
		////	switch (aggregate)
		////	{
		////		case AggregateType.None:
		////		{
		////			return "";
		////		}
		////		case AggregateType.Count:
		////		{
		////			return "COUNT";
		////		}
		////		case AggregateType.BigCount:
		////		{
		////			return "COUNT_BIG";
		////		}
		////		case AggregateType.Sum:
		////		{
		////			return "SUM";
		////		}
		////		case AggregateType.Average:
		////		{
		////			return "AVG";
		////		}
		////		case AggregateType.Min:
		////		{
		////			return "MIN";
		////		}
		////		case AggregateType.Max:
		////		{
		////			return "MAX";
		////		}
		////		default:
		////		{
		////			throw new InvalidOperationException("Invalid aggregate: " + aggregate.ToString());
		////		}
		////	}
		////}

		private void VisitSource(StatementPart source)
		{
			bool previousIsNested = this.IsNested;
			this.IsNested = true;
			switch (source.PartType)
			{
				case StatementPartType.Table:
				{
					Table table = (Table)source;
					this.VisitTable(table);
					if (!string.IsNullOrEmpty(table.Alias))
					{
						this.CommandText.Append(" AS [");
						this.CommandText.Append(table.Alias);
						this.CommandText.Append("]");
					}
					break;
				}
				case StatementPartType.Select:
				{
					SelectStatement select = (SelectStatement)source;
					this.CommandText.Append("(");
					this.AppendNewLine(Indentation.Inner);
					this.VisitSelect(select);
					this.AppendNewLine(Indentation.Same);
					this.CommandText.Append(")");
					if (!string.IsNullOrEmpty(select.Alias))
					{
						this.CommandText.Append(" AS [");
						this.CommandText.Append(select.Alias);
						this.CommandText.Append("]");
					}
					this.Indent(Indentation.Outer);
					break;
				}
				case StatementPartType.Join:
				{
					this.VisitJoin((Join)source);
					break;
				}
				default:
				{
					throw new InvalidOperationException("Select source is not valid type");
				}
			}
			this.IsNested = previousIsNested;
		}

		private void VisitTable(Table table)
		{
			this.CommandText.Append("[");
			this.CommandText.Append(table.Name);
			this.CommandText.Append("]");
		}

		private void VisitJoin(Join join)
		{
			switch (join.JoinType)
			{
				case JoinType.Inner:
				{
					this.CommandText.Append("INNER JOIN ");
					break;
				}
				case JoinType.Left:
				{
					this.CommandText.Append("LEFT OUTER JOIN ");
					break;
				}
				case JoinType.Right:
				{
					this.CommandText.Append("RIGHT OUTER JOIN ");
					break;
				}
				case JoinType.Cross:
				{
					this.CommandText.Append("CROSS JOIN ");
					break;
				}
				case JoinType.CrossApply:
				{
					this.CommandText.Append("CROSS APPLY ");
					break;
				}
			}
			this.VisitSource(join.Table);
			if (join.Conditions.Count > 0)
			{
				this.CommandText.Append(" ON ");
				this.VisitConditionCollection(join.Conditions);
			}
		}

		private void VisitCondition(ConditionExpression condition)
		{
			// TODO: Should all types of conditions be a class?  Not exposed to the user, because that
			// interface would be gross
			if (condition is Exists)
			{
				VisitExists((Exists)condition);
				return;
			}

			if (condition.Not)
			{
				this.CommandText.Append("NOT ");
			}

			if (condition is Condition)
			{
				VisitCondition((Condition)condition);
			}
			else if (condition is ConditionCollection)
			{
				VisitConditionCollection((ConditionCollection)condition);
			}
		}

		private void VisitCondition(Condition condition)
		{
			// Check for null comparisons first
			bool fieldIsNull = (condition.Field is ConstantPart && ((ConstantPart)condition.Field).Value == null);
			bool valueIsNull = (condition.Value is ConstantPart && ((ConstantPart)condition.Value).Value == null);
			if ((condition.Operator == SqlOperator.Equals || condition.Operator == SqlOperator.NotEquals) &&
				(fieldIsNull || valueIsNull))
			{
				if (fieldIsNull)
				{
					this.VisitField(condition.Value);
				}
				else if (valueIsNull)
				{
					this.VisitField(condition.Field);
				}
				if (condition.Operator == SqlOperator.Equals)
				{
					this.CommandText.Append(" IS NULL");
				}
				else if (condition.Operator == SqlOperator.NotEquals)
				{
					this.CommandText.Append(" IS NOT NULL");
				}
			}
			else
			{
				switch (condition.Operator)
				{
					case SqlOperator.Equals:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" = ");
						this.VisitField(condition.Value);
						break;
					}
					case SqlOperator.NotEquals:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" <> ");
						this.VisitField(condition.Value);
						break;
					}
					case SqlOperator.IsLessThan:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" < ");
						this.VisitField(condition.Value);
						break;
					}
					case SqlOperator.IsLessThanOrEqualTo:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" <= ");
						this.VisitField(condition.Value);
						break;
					}
					case SqlOperator.IsGreaterThan:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" > ");
						this.VisitField(condition.Value);
						break;
					}
					case SqlOperator.IsGreaterThanOrEqualTo:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" >= ");
						this.VisitField(condition.Value);
						break;
					}
					case SqlOperator.IsIn:
					{
						// If it's in an empty list, just check against false
						bool handled = false;
						if (condition.Value.PartType == StatementPartType.ConstantPart)
						{
							var value = ((ConstantPart)condition.Value).Value;
							if (value is IEnumerable && !(value is string) && !(value is byte[]))
							{
								// HACK: Ugh
								bool hasThings = false;
								foreach (var thing in (IEnumerable)value)
								{
									hasThings = true;
								}
								if (!hasThings)
								{
									handled = true;
									this.CommandText.Append(" 0 <> 0");
								}
							}
						}
						if (!handled)
						{
							this.VisitField(condition.Field);
							this.CommandText.Append(" IN (");
							this.AppendNewLine(Indentation.Inner);
							this.VisitField(condition.Value);
							this.AppendNewLine(Indentation.Same);
							this.CommandText.Append(")");
							this.AppendNewLine(Indentation.Outer);
						}
						break;
					}
					case SqlOperator.Contains:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" LIKE '%' || ");
						this.VisitField(condition.Value);
						this.CommandText.Append(" || '%'");
						break;
					}
					case SqlOperator.StartsWith:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" LIKE ");
						this.VisitField(condition.Value);
						this.CommandText.Append(" || '%'");
						break;
					}
					case SqlOperator.EndsWith:
					{
						this.VisitField(condition.Field);
						this.CommandText.Append(" LIKE '%' || ");
						this.VisitField(condition.Value);
						break;
					}
					default:
					{
						throw new InvalidOperationException("Invalid operator: " + condition.Operator);
					}
				}
			}
		}

		private void VisitConditionCollection(ConditionCollection collection)
		{
			this.CommandText.Append("(");
			for (int i = 0; i < collection.Count; i++)
			{
				if (i > 0)
				{
					// TODO: make this a visitrelationship method
					this.AppendNewLine(Indentation.Same);
					switch (collection[i].Relationship)
					{
						case ConditionRelationship.And:
						{
							this.CommandText.Append(" AND ");
							break;
						}
						case ConditionRelationship.Or:
						{
							this.CommandText.Append(" OR ");
							break;
						}
						default:
						{
							throw new InvalidOperationException();
						}
					}
				}
				this.VisitCondition(collection[i]);
			}
			this.CommandText.Append(")");
		}

		private void VisitConditionalCase(ConditionalCase conditional)
		{
			if (conditional.Test is Condition)
			{
				this.CommandText.Append("(CASE WHEN ");
				this.VisitField(conditional.Test);
				this.CommandText.Append(" THEN ");
				this.VisitField(conditional.IfTrue);
				StatementPart ifFalse = conditional.IfFalse;
				while (ifFalse != null && ifFalse.PartType == StatementPartType.ConditionalCase)
				{
					ConditionalCase subconditional = (ConditionalCase)conditional.IfFalse;
					this.CommandText.Append(" WHEN ");
					this.VisitField(subconditional.Test);
					this.CommandText.Append(" THEN ");
					this.VisitField(subconditional.IfTrue);
					ifFalse = subconditional.IfFalse;
				}
				if (ifFalse != null)
				{
					this.CommandText.Append(" ELSE ");
					this.VisitField(ifFalse);
				}
				this.CommandText.Append(" END)");
			}
			else
			{
				this.CommandText.Append("(CASE ");
				this.VisitField(conditional.Test);
				this.CommandText.Append(" WHEN 0 THEN ");
				this.VisitField(conditional.IfFalse);
				this.CommandText.Append(" ELSE ");
				this.VisitField(conditional.IfTrue);
				this.CommandText.Append(" END)");
			}
		}

		private void VisitRowNumber(RowNumber rowNumber)
		{
			this.CommandText.Append("ROW_NUMBER() OVER(");
			if (rowNumber.OrderByFields != null && rowNumber.OrderByFields.Count > 0)
			{
				this.CommandText.Append("ORDER BY ");
				for (int i = 0; i < rowNumber.OrderByFields.Count; i++)
				{
					if (i > 0)
					{
						this.CommandText.Append(", ");
					}
					this.VisitField(rowNumber.OrderByFields[i].Expression);
					if (rowNumber.OrderByFields[i].Direction != OrderDirection.Ascending)
					{
						this.CommandText.Append(" DESC");
					}
				}
			}
			this.CommandText.Append(")");
		}

		private void VisitAggregate(Aggregate aggregate)
		{
			this.CommandText.Append(GetAggregateName(aggregate.AggregateType));
			this.CommandText.Append("(");
			if (aggregate.IsDistinct)
			{
				this.CommandText.Append("DISTINCT ");
			}
			if (aggregate.Field != null)
			{
				this.VisitField(aggregate.Field);
			}
			else if (aggregate.AggregateType == AggregateType.Count ||
				aggregate.AggregateType == AggregateType.BigCount)
			{
				this.CommandText.Append("*");
			}
			this.CommandText.Append(")");
		}

		private string GetAggregateName(AggregateType aggregateType)
		{
			switch (aggregateType)
			{
				case AggregateType.Count:
				{
					return "COUNT";
				}
				case AggregateType.BigCount:
				{
					return "COUNT_BIG";
				}
				case AggregateType.Min:
				{
					return "MIN";
				}
				case AggregateType.Max:
				{
					return "MAX";
				}
				case AggregateType.Sum:
				{
					return "SUM";
				}
				case AggregateType.Average:
				{
					return "AVG";
				}
				default:
				{
					throw new Exception($"Unknown aggregate type: {aggregateType}");
				}
			}
		}

		private void VisitConditionPredicate(ConditionPredicate predicate)
		{
			this.CommandText.Append("(CASE WHEN ");
			this.VisitField(predicate.Predicate);
			this.CommandText.Append(" THEN 1 ELSE 0 END)");
		}

		private void VisitExists(Exists exists)
		{
			//this.CommandText.Append("(");
			if (exists.Not)
			{
				this.CommandText.Append("NOT ");
			}
			this.CommandText.Append("EXISTS (");
			this.AppendNewLine(Indentation.Inner);
			this.VisitSelect(exists.Select);
			this.AppendNewLine(Indentation.Same);
			this.CommandText.Append(")");
			this.Indent(Indentation.Outer);
			//this.CommandText.Append(")");
		}

		private void VisitCoalesceFunction(CoalesceFunction coalesce)
		{
			// TODO:
			////StatementPart first = coalesce.First;
			////StatementPart second = coalesce.Second;

			////this.CommandText.Append("COALESCE(");
			////this.VisitField(first);
			////this.CommandText.Append(", ");
			////while (second.PartType == StatementPartType.CoalesceFunction)
			////{
			////	CoalesceFunction secondCoalesce = (CoalesceFunction)second;
			////	this.VisitField(secondCoalesce.First);
			////	this.CommandText.Append(", ");
			////	second = secondCoalesce.Second;
			////}
			////this.VisitField(second);
			////this.CommandText.Append(")");
		}

		private void VisitFunction(string name, params StatementPart[] arguments)
		{
			this.CommandText.Append(name);
			this.CommandText.Append("(");
			for (int i = 0; i < arguments.Length; i++)
			{
				if (i > 0)
				{
					this.CommandText.Append(", ");
				}
				this.VisitField(arguments[i]);
			}
			this.CommandText.Append(")");
		}

		private void VisitConvertFunction(ConvertFunction function)
		{
			// TODO: Handle more types
			this.CommandText.Append("CONVERT(VARCHAR, ");
			this.VisitField(function.Expression);
			this.CommandText.Append(")");
		}

		private void VisitStringLengthFunction(StringLengthFunction function)
		{
			VisitFunction("LENGTH", function.Argument);
		}

		private void VisitSubstringFunction(SubstringFunction function)
		{
			this.CommandText.Append("SUBSTR(");
			this.VisitField(function.Argument);
			this.CommandText.Append(", ");
			this.VisitField(function.StartIndex);
			this.CommandText.Append(" + 1");
			if (function.Length != null)
			{
				this.CommandText.Append(", ");
				this.VisitField(function.Length);
			}
			this.CommandText.Append(")");
		}

		private void VisitStringRemoveFunction(StringRemoveFunction function)
		{
			this.CommandText.Append("STUFF(");
			this.VisitField(function.Argument);
			this.CommandText.Append(", ");
			this.VisitField(function.StartIndex);
			this.CommandText.Append(" + 1, ");
			this.VisitField(function.Length);
			this.CommandText.Append(", '')");
		}

		private void VisitStringCharIndexFunction(StringIndexFunction function)
		{
			this.CommandText.Append("(");
			if (function.StartIndex != null)
			{
				// e.g. INSTR(SUBSTR(text, startIndex), stringToFind) + startIndex
				var substring = new SubstringFunction() { Argument = function.Argument, StartIndex = function.StartIndex };
				this.VisitFunction("INSTR", substring, function.StringToFind);
				this.CommandText.Append(" + ");
				this.VisitField(function.StartIndex);
			}
			else
			{
				this.VisitFunction("INSTR", function.Argument, function.StringToFind);
			}
			this.CommandText.Append(" - 1)");
		}

		private void VisitStringToUpperFunction(StringToUpperFunction function)
		{
			VisitFunction("UPPER", function.Argument);
		}

		private void VisitStringToLowerFunction(StringToLowerFunction function)
		{
			VisitFunction("LOWER", function.Argument);
		}

		private void VisitStringReplaceFunction(StringReplaceFunction function)
		{
			VisitFunction("REPLACE", function.Argument, function.OldValue, function.NewValue);
		}

		private void VisitStringTrimFunction(StringTrimFunction function)
		{
			this.CommandText.Append("RTRIM(LTRIM(");
			this.VisitField(function.Argument);
			this.CommandText.Append("))");
		}

		private void VisitStringCompareFunction(StringCompareFunction function)
		{
			this.CommandText.Append("(CASE WHEN ");
			this.VisitField(function.Argument);
			this.CommandText.Append(" = ");
			this.VisitField(function.Other);
			this.CommandText.Append(" THEN 0 WHEN ");
			this.VisitField(function.Argument);
			this.CommandText.Append(" < ");
			this.VisitField(function.Other);
			this.CommandText.Append(" THEN -1 ELSE 1 END)");
		}

		private void VisitStringConcatenateFunction(StringConcatenateFunction function)
		{
			for (int i = 0; i < function.Arguments.Count; i++)
			{
				if (i > 0)
				{
					this.CommandText.Append(" + ");
				}
				this.VisitField(function.Arguments[i]);
			}
		}

		private void VisitDatePartFunction(DatePartFunction function)
		{
			switch (function.DatePart)
			{
				case DatePart.Millisecond:
				{
					this.CommandText.Append("CAST(STRFTIME('%s', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.Second:
				{
					this.CommandText.Append("CAST(STRFTIME('%S', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.Minute:
				{
					this.CommandText.Append("CAST(STRFTIME('%M', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.Hour:
				{
					this.CommandText.Append("CAST(STRFTIME('%H', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.Day:
				{
					this.CommandText.Append("CAST(STRFTIME('%d', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.Month:
				{
					this.CommandText.Append("CAST(STRFTIME('%m', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.Year:
				{
					this.CommandText.Append("CAST(STRFTIME('%Y', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.DayOfWeek:
				{
					this.CommandText.Append("CAST(STRFTIME('%w', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT)");
					break;
				}
				case DatePart.DayOfYear:
				{
					this.CommandText.Append("(CAST(STRFTIME('%j', ");
					this.VisitField(function.Argument);
					this.CommandText.Append(") AS INT) - 1)");
					break;
				}
				case DatePart.Date:
				{
					this.CommandText.Append("DATE(");
					this.VisitField(function.Argument);
					this.CommandText.Append(")");
					break;
				}
				default:
				{
					throw new InvalidOperationException("Invalid date part: " + function.DatePart);
				}
			}
		}

		private void VisitDateAddFunction(DateAddFunction function)
		{
			// HACK: This may lose precision, like if you want to add days but keep the time!
			if (function.DatePart == DatePart.Hour ||
				function.DatePart == DatePart.Minute ||
				function.DatePart == DatePart.Second ||
				function.DatePart == DatePart.Millisecond)
			{
				this.CommandText.Append("DATETIME(");
			}
			else
			{
				this.CommandText.Append("DATE(");
			}
			this.VisitField(function.Argument);
			this.CommandText.Append(", ");
			this.VisitField(function.Number);
			this.CommandText.Append(" || ' ");
			this.CommandText.Append(function.DatePart.ToString().ToLowerInvariant());
			this.CommandText.Append("'");
			this.CommandText.Append(")");
		}

		private void VisitDateNewFunction(DateNewFunction function)
		{
			this.VisitDatePartField(function.Year);
			this.CommandText.Append(" || '-' || ");
			this.VisitDatePartField(function.Month);
			this.CommandText.Append(" || '-' || ");
			this.VisitDatePartField(function.Day);

			if (function.Hour != null)
			{
				this.CommandText.Append(" || ' ' || ");
				this.VisitDatePartField(function.Hour);
				this.CommandText.Append(" || ':' || ");
				this.VisitDatePartField(function.Minute);
				this.CommandText.Append(" || ':' || ");
				this.VisitDatePartField(function.Second);
			}
		}

		private void VisitDatePartField(StatementPart datePart)
		{
			if (datePart.PartType == StatementPartType.ConstantPart)
			{
				var constantPart = (ConstantPart)datePart;
				// HACK: Not 100% sure it will always be convertible to an int?
				var value = Convert.ToInt32(constantPart.Value).ToString("D2");
				this.VisitField(new ConstantPart(value));
			}
			else
			{
				this.VisitField(datePart);
			}
		}

		private void VisitDateDifferenceFunction(DateDifferenceFunction function)
		{
			this.VisitFunction("DATEDIFF", function.Date1, function.Date2);
		}

		private void VisitNumberAbsoluteFunction(NumberAbsoluteFunction function)
		{
			this.VisitFunction("ABS", function.Argument);
		}

		private void VisitNumberNegateFunction(NumberNegateFunction function)
		{
			this.CommandText.Append("-");
			this.VisitField(function.Argument);
		}

		private void VisitNumberCeilingFunction(NumberCeilingFunction function)
		{
			this.VisitFunction("CEILING", function.Argument);
		}

		private void VisitNumberFloorFunction(NumberFloorFunction function)
		{
			// HACK: No floor function in SQLite, so we do it with round per https://stackoverflow.com/a/24821301
			this.CommandText.Append("ROUND(");
			this.VisitField(function.Argument);
			this.CommandText.Append(" - 0.5)");
		}

		private void VisitNumberRoundFunction(NumberRoundFunction function)
		{
			this.VisitFunction("ROUND", function.Argument, function.Precision);
		}

		private void VisitNumberTruncateFunction(NumberTruncateFunction function)
		{
			// HACK: No truncate function in SQLite, so we do it by casting to an int per https://stackoverflow.com/a/16628655
			this.CommandText.Append("CAST((");
			this.VisitField(function.Argument);
			this.CommandText.Append(" * 10) AS INT) / 10");
		}

		private void VisitNumberSignFunction(NumberSignFunction function)
		{
			this.VisitFunction("SIGN", function.Argument);
		}

		private void VisitNumberPowerFunction(NumberPowerFunction function)
		{
			this.VisitFunction("POWER", function.Argument, function.Power);
		}

		private void VisitNumberRootFunction(NumberRootFunction function)
		{
			// TODO: I'm being lazy, if root > 3 then we should to convert it to POW(argument, 1 / root)
			this.VisitFunction("SQRT", function.Argument);
		}

		private void VisitNumberExponentialFunction(NumberExponentialFunction function)
		{
			this.VisitFunction("EXP", function.Argument);
		}

		private void VisitNumberLogFunction(NumberLogFunction function)
		{
			this.VisitFunction("LOG", function.Argument);
		}

		private void VisitNumberLog10Function(NumberLog10Function function)
		{
			this.VisitFunction("LOG10", function.Argument);
		}

		private void VisitNumberTrigFunction(NumberTrigFunction function)
		{
			this.VisitFunction(function.Function.ToString().ToUpperInvariant(), function.Argument);
		}

		private void VisitBinaryOperation(BinaryOperation operation)
		{
			if (operation.Operator == BinaryOperator.LeftShift)
			{
				this.CommandText.Append("(");
				this.VisitField(operation.Left);
				this.CommandText.Append(" << ");
				this.VisitField(operation.Right);
				this.CommandText.Append(")");
			}
			else if (operation.Operator == BinaryOperator.RightShift)
			{
				this.CommandText.Append("(");
				this.VisitField(operation.Left);
				this.CommandText.Append(" >> ");
				this.VisitField(operation.Right);
				this.CommandText.Append(")");
			}
			else
			{
				this.CommandText.Append("(");
				this.VisitField(operation.Left);
				this.CommandText.Append(" ");
				this.CommandText.Append(GetOperatorName(operation.Operator));
				this.CommandText.Append(" ");
				this.VisitField(operation.Right);
				this.CommandText.Append(")");
			}
		}

		private string GetOperatorName(BinaryOperator op)
		{
			switch (op)
			{
				case BinaryOperator.Add:
				{
					return "+";
				}
				case BinaryOperator.Subtract:
				{
					return "-";
				}
				case BinaryOperator.Multiply:
				{
					return "*";
				}
				case BinaryOperator.Divide:
				{
					return "/";
				}
				case BinaryOperator.Remainder:
				{
					return "%";
				}
				case BinaryOperator.ExclusiveOr:
				{
					return "&~";
				}
				case BinaryOperator.LeftShift:
				{
					return "<<";
				}
				case BinaryOperator.RightShift:
				{
					return ">>";
				}
				case BinaryOperator.BitwiseAnd:
				{
					return "&";
				}
				case BinaryOperator.BitwiseOr:
				case BinaryOperator.BitwiseExclusiveOr:
				{
					return "|";
				}
				case BinaryOperator.BitwiseNot:
				{
					return "~";
				}
				default:
				{
					throw new InvalidOperationException();
				}
			}
		}

		private void VisitUnaryOperation(UnaryOperation operation)
		{
			this.CommandText.Append(GetOperatorName(operation.Operator));
			// TODO: If isbinary: this.Builder.Append(" ");
			this.VisitField(operation.Expression);
		}

		private string GetOperatorName(UnaryOperator op)
		{
			switch (op)
			{
				case UnaryOperator.Not:
				{
					// TODO: return IsBoolean(unary.Expression.Type) ? "NOT" : "~";
					return "NOT ";
				}
				case UnaryOperator.Negate:
				{
					return "-";
				}
				default:
				{
					throw new InvalidOperationException();
				}
			}
		}

		private void VisitSelectExpression(SelectExpression select)
		{
			this.CommandText.Append("(");
			this.AppendNewLine(Indentation.Inner);
			this.VisitSelect(select.Select);
			this.AppendNewLine(Indentation.Same);
			this.CommandText.Append(")");
			if (!string.IsNullOrEmpty(select.Alias))
			{
				this.CommandText.Append(" AS [");
				this.CommandText.Append(select.Alias);
				this.CommandText.Append("]");
			}
			this.Indent(Indentation.Outer);
		}

		private void VisitLiteralPart(LiteralPart literalPart)
		{
			this.CommandText.Append(literalPart.Value);
		}

		private void AppendNewLine(Indentation style)
		{
			this.CommandText.AppendLine();
			this.Indent(style);
			for (int i = 0; i < this.Depth * IndentationWidth; i++)
			{
				this.CommandText.Append(" ");
			}
		}

		private void Indent(Indentation style)
		{
			if (style == Indentation.Inner)
			{
				this.Depth += 1;
			}
			else if (style == Indentation.Outer)
			{
				this.Depth -= 1;
				System.Diagnostics.Debug.Assert(this.Depth >= 0);
			}
		}
	}
}

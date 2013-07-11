﻿using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public enum StatementPartType
	{
		Select,
		Insert,
		Update,
		Delete,
		Table,
		Column,
		//Projection,
		Join,
		Aggregate,
		//Scalar,
		Exists,
		//In,
		//Grouping,
		//AggregateSubquery,
		//IsNull,
		//Between,
		//RowCount,
		//NamedValue,
		ConstantPart,
		SourceExpression,
		OrderByField,
		BinaryOperation,
		UnaryOperation,
		ConditionalCase,
		CoalesceFunction,
		DateDifferenceFunction,
		DateNewFunction,
		DatePartFunction,
		DateAddFunction,
		NumberAbsoluteFunction,
		NumberCeilingFunction,
		NumberFloorFunction,
		NumberNegateFunction,
		NumberRoundFunction,
		NumberTruncateFunction,
		NumberSignFunction,
		NumberPowerFunction,
		NumberRootFunction,
		NumberExponentialFunction,
		NumberLogFunction,
		NumberLog10Function,
		NumberTrigFunction,
		StringIndexFunction,
		StringCompareFunction,
		StringConcatenateFunction,
		StringLengthFunction,
		StringRemoveFunction,
		StringReplaceFunction,
		StringTrimFunction,
		StringToLowerFunction,
		StringToUpperFunction,
		SubstringFunction,
		ConditionPredicate,
		Parameter,
		ConvertFunction,
		ConditionExpression,
		Condition,
		ConditionCollection,
		RowNumber,
		LiteralPart,
		SelectField,
	}
}

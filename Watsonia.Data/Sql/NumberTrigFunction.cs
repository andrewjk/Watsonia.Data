﻿using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class NumberTrigFunction : Field
	{
		public override StatementPartType PartType
		{
			get
			{
				return StatementPartType.NumberTrigFunction;
			}
		}

		public TrigFunction Function
		{
			get;
			set;
		}

		public StatementPart Argument
		{
			get;
			set;
		}

		public override string ToString()
		{
			return this.Function.ToString() + "(" + this.Argument.ToString() + ")";
		}
	}
}

﻿using System;
using System.Linq;

namespace Watsonia.Data.Sql
{
	public sealed class SetValue
	{
		public Column Column
		{
			get;
			set;
		}

		public StatementPart Value
		{
			get;
			set;
		}

		public SetValue()
		{
		}

		public SetValue(string columnName, object value)
			: this(new Column(columnName), value)
		{
		}

		public SetValue(Column column, object value)
		{
			this.Column = column;
			this.Value = new ConstantPart(value);
		}
	}
}

using System;
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
		{
			this.Column = new Column(columnName);
			this.Value = new ConstantPart(value);
		}
	}
}
